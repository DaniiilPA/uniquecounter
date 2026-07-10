using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static readonly Dictionary<string, string> ArtToUniqueNameMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {

            { "Amulet36", "Astramentis" },
            { "Amulet37", "Carnage Heart" },
            { "Amulet7Unique", "Eye of Chayula" },
            { "Amulet5Unique", "Sidhebreath" },
            { "Amulet5Unique2", "The Halcyon" },
            { "AtzirisFoibleAlt", "Atziri's Foible" },
            { "KaruiWardAlt", "Karui Ward" },
            { "MarylenesFallacy", "Marylene's Fallacy" },
            { "TearofExile", "Tear of Purity" },

            { "Ring3Unique", "Doedre's Damning" },
            { "Ring5Unique", "Kaom's Sign" },
            { "Ring1Unique", "Andvarius" },
            { "Ring1Unique2", "Berek's Grip" },
            { "Ring2Unique", "Dream Fragments" },
            { "Ring4Unique", "Perandus Signet" },
            { "Ring11Unique", "Romira's Banquet" },
            { "GiftsFromAbove", "Gifts from Above" },

            { "Belt1", "Wurm's Molt" },
            { "Belt2", "Perandus Blazon" },
            { "Belt3", "Immortal Flesh" },
            { "Belt5", "Bated Breath" },
            { "Sunblast", "Sunblast" }
            
        };

        public override bool Initialise()
        {
            return true;
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
                        var path = renderItem.ResourcePath;
                        var fileName = Path.GetFileNameWithoutExtension(path);
                        if (!string.IsNullOrEmpty(fileName))
                        {

                            if (ArtToUniqueNameMapping.TryGetValue(fileName, out var cleanName))
                            {
                                uniqueName = cleanName;
                            }
                            else
                            {
                                uniqueName = fileName;
                            }
                        }
                    }

                    uniqueItems.Add($"{baseItemType} ({uniqueName}) [ID: {itemEntity.Id} | Ground ID: {entity.Id}]");
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