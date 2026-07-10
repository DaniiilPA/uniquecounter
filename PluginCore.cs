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
        
        private const string MappingFileName = "uniqueArtMapping.json";
        private const string MappingUrl = "https://raw.githubusercontent.com/DetectiveSquirrel/Ground-Items-With-Linq/master/uniqueArtMapping.default.json";

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

            var mappingPath = Path.Combine(ConfigDirectory, MappingFileName);

            if (!File.Exists(mappingPath))
            {
                try
                {
                    LogMessage("[UniqueLogger] Скачивание актуальной базы уникальных предметов...", 5);
                    using (var client = new HttpClient())
                    {
                        var json = await client.GetStringAsync(MappingUrl);
                        File.WriteAllText(mappingPath, json);
                        LogMessage("[UniqueLogger] База успешно скачана и сохранена!", 5);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[UniqueLogger] Не удалось скачать базу: {ex.Message}", 10);
                }
            }

            if (File.Exists(mappingPath))
            {
                try
                {
                    var jsonText = File.ReadAllText(mappingPath);
                    var parsedDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonText);

                    if (parsedDict != null)
                    {
                        var tempMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var kvp in parsedDict)
                        {
                            if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                                continue;

                            // Очищаем ключ от слешей, нуль-байтов и пробелов
                            var cleanKey = CleanPathString(kvp.Key);
                            tempMapping[cleanKey] = kvp.Value.Trim();
                        }

                        _artToUniqueMapping = tempMapping;
                        LogMessage($"[UniqueLogger] Успешно загружено {_artToUniqueMapping.Count} уникальных предметов из базы.", 5);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[UniqueLogger] Ошибка чтения базы: {ex.Message}", 10);
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

                    // 1. Если предмет уже опознан (Identified), берем название из компонентов Mods
                    if (!string.IsNullOrEmpty(mods.UniqueName))
                    {
                        uniqueName = CleanString(mods.UniqueName);
                    }

                    // 2. Если предмет неопознан, ищем совпадение 2D-арта в загруженной базе
                    if (string.IsNullOrEmpty(uniqueName))
                    {
                        var renderItem = itemEntity.GetComponent<RenderItem>();
                        if (renderItem != null && !string.IsNullOrEmpty(renderItem.ResourcePath))
                        {
                            var rawPath = CleanPathString(renderItem.ResourcePath);

                            if (!string.IsNullOrEmpty(rawPath))
                            {
                                // Ищем совпадение в словаре
                                if (_artToUniqueMapping.TryGetValue(rawPath, out var mappedName) && !string.IsNullOrEmpty(mappedName))
                                {
                                    uniqueName = mappedName;
                                }
                                else
                                {
                                    // Резервный вариант: имя файла картинки (напр. Headhunter из Art/2DItems/.../Headhunter.dds)
                                    var fileName = Path.GetFileNameWithoutExtension(rawPath);
                                    if (!string.IsNullOrEmpty(fileName))
                                    {
                                        uniqueName = fileName;
                                    }
                                }
                            }
                        }
                    }

                    // 3. Если все способы не дали результата
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