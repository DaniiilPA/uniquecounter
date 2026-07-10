using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;

namespace UniqueLogger
{
    public class UniqueLoggerSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);
    }

    public class UniqueLogger : BaseSettingsPlugin<UniqueLoggerSettings>
    {
        private Dictionary<string, string> _artToUniqueMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // Ссылка на полную и актуальную базу всех уникальных предметов PoE от сообщества разработчиков (RePoE)
        private const string RePoEUrl = "https://repoe-fork.github.io/poe1/uniques.json";
        private const string RePoEFileName = "repoeUniques.json";

        public override bool Initialise()
        {
            Task.Run(async () => await LoadMappingDatabaseAsync());
            return true;
        }

        private async Task LoadMappingDatabaseAsync()
        {
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
            }

            var repoePath = Path.Combine(ConfigDirectory, RePoEFileName);

            // Если базы на диске нет, скачиваем её
            if (!File.Exists(repoePath))
            {
                try
                {
                    LogMessage("[UniqueLogger] Скачивание полной базы уникальных предметов (RePoE)...", 5);
                    using (var client = new HttpClient())
                    {
                        var json = await client.GetStringAsync(RePoEUrl);
                        File.WriteAllText(repoePath, json);
                        LogMessage("[UniqueLogger] База RePoE успешно скачана и сохранена!", 5);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[UniqueLogger] Не удалось скачать базу RePoE: {ex.Message}", 10);
                }
            }

            // Парсим скачанную базу RePoE
            if (File.Exists(repoePath))
            {
                try
                {
                    var jsonText = File.ReadAllText(repoePath);
                    
                    // RePoE имеет структуру: { "Unique Name": { "base_item": "...", "art": "path/to/art" } }
                    var repoeData = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(jsonText);

                    if (repoeData != null)
                    {
                        var tempMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var kvp in repoeData)
                        {
                            string uniqueName = kvp.Value["name"]?.ToString() ?? kvp.Key;
                            string artPath = kvp.Value["art"]?.ToString();

                            if (!string.IsNullOrEmpty(uniqueName) && !string.IsNullOrEmpty(artPath))
                            {
                                var cleanArt = CleanPathString(artPath);
                                if (!string.IsNullOrEmpty(cleanArt))
                                {
                                    // Сохраняем имя уника под его очищенным путем к арту
                                    tempMapping[cleanArt] = uniqueName;

                                    // В RePoE пути часто указаны без расширения ".dds" (например, "Art/2DItems/Amulets/Amulet36"),
                                    // в то время как игра отдает их с расширением. Добавляем дубликат ключа с ".dds" для надежности.
                                    if (!cleanArt.EndsWith(".dds", System.StringComparison.OrdinalIgnoreCase))
                                    {
                                        tempMapping[cleanArt + ".dds"] = uniqueName;
                                    }
                                }
                            }
                        }

                        _artToUniqueMapping = tempMapping;
                        LogMessage($"[UniqueLogger] Успешно загружено {_artToUniqueMapping.Count} уникальных предметов из RePoE базы.", 5);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[UniqueLogger] Ошибка чтения базы RePoE: {ex.Message}", 10);
                }
            }
        }

        public override void Render()
        {
            if (!Settings.Enable) return;

            var currentArea = GameController?.Area?.CurrentArea;
            string areaName = currentArea?.Name ?? "Unknown Area";
            string areaHash = currentArea?.Hash.ToString() ?? "Unknown Hash";

            var uniqueItems = new List<string>();

            var entities = GameController?.EntityListWrapper?.Entities;
            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    if (entity == null || !entity.IsValid) continue;

                    var worldItem = entity.GetComponent<WorldItem>();
                    if (worldItem == null) continue;

                    var itemEntity = worldItem.ItemEntity;
                    if (itemEntity == null || !itemEntity.IsValid) continue;

                    var mods = itemEntity.GetComponent<Mods>();
                    if (mods == null || mods.ItemRarity != ItemRarity.Unique) continue;

                    var baseItemType = itemEntity.GetComponent<Base>()?.Name ?? "Unknown Base";

                    string uniqueName = null;

                    // 1. Опознанный предмет (UniqueName берется из модов в памяти)
                    if (!string.IsNullOrEmpty(mods.UniqueName))
                    {
                        uniqueName = CleanString(mods.UniqueName);
                    }

                    // 2. Неопознанный предмет (Ищем его 2D-арт в базе RePoE)
                    if (string.IsNullOrEmpty(uniqueName))
                    {
                        var renderItem = itemEntity.GetComponent<RenderItem>();
                        if (renderItem != null && !string.IsNullOrEmpty(renderItem.ResourcePath))
                        {
                            var rawPath = CleanPathString(renderItem.ResourcePath);

                            if (!string.IsNullOrEmpty(rawPath))
                            {
                                if (_artToUniqueMapping.TryGetValue(rawPath, out var mappedName) && !string.IsNullOrEmpty(mappedName))
                                {
                                    uniqueName = mappedName;
                                }
                                else
                                {
                                    // Резервный вариант: имя dds-файла
                                    var fileName = Path.GetFileNameWithoutExtension(rawPath);
                                    if (!string.IsNullOrEmpty(fileName))
                                    {
                                        uniqueName = fileName;
                                    }
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(uniqueName))
                    {
                        uniqueName = "Unidentified";
                    }

                    uniqueItems.Add($"{baseItemType} ({uniqueName}) [Ground ID: {entity.Id}]");
                }
            }

            var windowRect = GameController?.Window?.GetWindowRectangleTimeCache;
            if (windowRect != null)
            {
                var drawPos = new SharpDX.Vector2(windowRect.Value.Width * 0.15f, windowRect.Value.Height * 0.15f);
                var yOffset = 0f;

                Graphics.DrawText($"Area: {areaName} | Area ID: {areaHash}", drawPos, Color.White);
                yOffset += 22f;

                if (uniqueItems.Count > 0)
                {
                    Graphics.DrawText("--- Unique Items on Floor ---", drawPos + new SharpDX.Vector2(0, yOffset), Color.White);
                    yOffset += 20f;

                    foreach (var itemText in uniqueItems.Distinct())
                    {
                        Graphics.DrawText(itemText, drawPos + new SharpDX.Vector2(0, yOffset), Color.White);
                        yOffset += 20f; 
                    }
                }
                else
                {
                    Graphics.DrawText("No uniques on the floor", drawPos + new SharpDX.Vector2(0, yOffset), Color.Gray);
                }
            }
        }

        private static string CleanString(string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            return str.Trim('\0', ' ', '\t', '\r', '\n');
        }

        private static string CleanPathString(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            return path.Replace('\\', '/').Trim('\0', ' ', '\t', '\r', '\n', '/');
        }
    }
}