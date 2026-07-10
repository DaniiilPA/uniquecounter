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
                    var lines = File.ReadAllLines(mappingPath);
                    var tempMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("{") || trimmed.StartsWith("}")) continue;

                        var parts = trimmed.Split(new[] { ':' }, 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim(' ', '\t', '"', ',', '{', '}').Replace('\\', '/');
                            var val = parts[1].Trim(' ', '\t', '"', ',', '{', '}');

                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val))
                            {
                                tempMapping[key] = val;
                            }
                        }
                    }

                    _artToUniqueMapping = tempMapping;
                    LogMessage($"[UniqueLogger] Успешно загружено {_artToUniqueMapping.Count} уникальных предметов из базы.", 5);
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

                    var renderItem = itemEntity.GetComponent<RenderItem>();
                    string uniqueName = "Unidentified";

                    if (renderItem != null && !string.IsNullOrEmpty(renderItem.ResourcePath))
                    {
                        var rawPath = renderItem.ResourcePath.Replace('\\', '/').Trim();

                        if (_artToUniqueMapping.TryGetValue(rawPath, out var cleanName))
                        {
                            uniqueName = cleanName;
                        }
                        else
                        {
                            uniqueName = Path.GetFileNameWithoutExtension(rawPath);
                        }
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
    }
}