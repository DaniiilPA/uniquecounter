using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
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

        // --- ФИЛЬТРЫ И СКАНИРОВАНИЕ ---
        public ToggleNode IgnoreIdentifiedItems { get; set; } = new ToggleNode(true);
        public ToggleNode IgnoreHideoutAndTown { get; set; } = new ToggleNode(true);
        public ToggleNode LogHistoryToFile { get; set; } = new ToggleNode(true);

        // --- НАСТРОЙКИ ПОДСВЕТКИ ПЛАШЕК НА ПОЛУ (FRAME / HIGHLIGHT) ---
        public ToggleNode HighlightLabelsOnGround { get; set; } = new ToggleNode(true);
        public ToggleNode HighlightOnlyTiers { get; set; } = new ToggleNode(true);
        public ToggleNode UseTierColorsForFrames { get; set; } = new ToggleNode(true);
        public ColorNode HighlightBorderColor { get; set; } = new ColorNode(SharpDX.Color.Gold);
        public RangeNode<int> HighlightBorderThickness { get; set; } = new RangeNode<int>(3, 1, 10);
        public ColorNode HighlightBackgroundColor { get; set; } = new ColorNode(new SharpDX.Color(255, 215, 0, 50));
        public ToggleNode DrawNameOnLabel { get; set; } = new ToggleNode(true);
        public ColorNode LabelNameTextColor { get; set; } = new ColorNode(SharpDX.Color.White);
        public RangeNode<float> LabelTextScale { get; set; } = new RangeNode<float>(1.0f, 0.4f, 2.5f);

        // --- НАСТРОЙКИ ОТОБРАЖЕНИЯ HUD (СЧЕТЧИКИ) ---
        public ToggleNode ShowMapTotal { get; set; } = new ToggleNode(true);
        public ToggleNode ShowGrandTotal { get; set; } = new ToggleNode(true);
        public ToggleNode ShowT0Uniques { get; set; } = new ToggleNode(true);
        public ToggleNode ShowT1Uniques { get; set; } = new ToggleNode(true);
        public ToggleNode ShowMapContributionInList { get; set; } = new ToggleNode(true);

        // --- ЦВЕТА И ШРИФТЫ HUD ---
        public RangeNode<float> TextScale { get; set; } = new RangeNode<float>(1.0f, 0.3f, 3.0f);
        public ColorNode MapTotalColor { get; set; } = new ColorNode(SharpDX.Color.Yellow);
        public ColorNode MainTextColor { get; set; } = new ColorNode(SharpDX.Color.White);
        public ColorNode T0Color { get; set; } = new ColorNode(SharpDX.Color.Red);
        public ColorNode T1Color { get; set; } = new ColorNode(new SharpDX.Color(255, 140, 0));

        // --- ПОЗИЦИИ HUD НА ЭКРАНЕ ---
        public RangeNode<System.Numerics.Vector2> MapTotalPosition { get; set; } = new RangeNode<System.Numerics.Vector2>(
            new System.Numerics.Vector2(20, 80),
            new System.Numerics.Vector2(0, 0),
            new System.Numerics.Vector2(3840, 2160)
        );

        public RangeNode<System.Numerics.Vector2> GrandTotalPosition { get; set; } = new RangeNode<System.Numerics.Vector2>(
            new System.Numerics.Vector2(20, 105),
            new System.Numerics.Vector2(0, 0),
            new System.Numerics.Vector2(3840, 2160)
        );

        public RangeNode<System.Numerics.Vector2> T0Position { get; set; } = new RangeNode<System.Numerics.Vector2>(
            new System.Numerics.Vector2(20, 135),
            new System.Numerics.Vector2(0, 0),
            new System.Numerics.Vector2(3840, 2160)
        );

        public RangeNode<System.Numerics.Vector2> T1Position { get; set; } = new RangeNode<System.Numerics.Vector2>(
            new System.Numerics.Vector2(20, 290),
            new System.Numerics.Vector2(0, 0),
            new System.Numerics.Vector2(3840, 2160)
        );

        // --- ХРАНИЛИЩЕ И ДЕЙСТВИЯ ---
        public TextNode CustomStoragePath { get; set; } = new TextNode("");
        public ButtonNode ResetMapStats { get; set; } = new ButtonNode();
        public ButtonNode ResetGrandTotal { get; set; } = new ButtonNode();
        public ButtonNode ReloadTiersAndDatabase { get; set; } = new ButtonNode();
    }

    public class StoredStats
    {
        [JsonProperty("grand_total")]
        public int GrandTotal { get; set; } = 0;

        [JsonProperty("t0_grand_total")]
        public Dictionary<string, int> T0GrandTotal { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty("t1_grand_total")]
        public Dictionary<string, int> T1GrandTotal { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty("last_saved_utc")]
        public DateTime LastSavedUtc { get; set; } = DateTime.UtcNow;
    }

    public class TierConfigFile
    {
        [JsonProperty("T0")]
        public List<string> T0 { get; set; } = new List<string>();

        [JsonProperty("T1")]
        public List<string> T1 { get; set; } = new List<string>();
    }

    public class UniqueLogger : BaseSettingsPlugin<UniqueLoggerSettings>
    {
        // ------------------ ДЕФОЛТНЫЕ СПИСКИ T0 И T1 ------------------
        private static readonly HashSet<string> DefaultT0Items = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Bino's Kitchen Knife",
            "Bloodseeker",
            "Defiance of Destiny",
            "Divinarius",
            "Ephemeral Edge",
            "Essentia Sanguis",
            "Headhunter",
            "Jiquani's Potential",
            "Kalandra's Touch",
            "Lioneye's Glare",
            "Mageblood",
            "Marohi Erqi",
            "Rakiata's Dance",
            "Reefbane",
            "Soul Taker",
            "The Squire",
            "Varunastra",
            "Voltaxic Rift"
        };

        // Закомментированный расширенный список T1 + дополнительные ценные предметы из вайтлиста
        private static readonly HashSet<string> DefaultT1Items = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Abberath's Hooves",
            "Aegis Aurora",
            "Akoya's Gaze",
            "Anathema",
            "Ancestral Vision",
            "Arakaali's Fang",
            "Asenath's Mark",
            "Astral Projector",
            "Astramentis",
            "Atziri's Foible",
            "Badge of the Brotherhood",
            "Binds of Bloody Vengeance",
            "Bloodnotch",
            "Cloak of Defiance",
            "Cloak of Flame",
            "Corpsewalker",
            "Cospri's Malice",
            "Darkscorn",
            "Dead Reckoning",
            "Death Rush",
            "Dialla's Malefaction",
            "Doedre's Skin",
            "Doryani's Fist",
            "Doryani's Prototype",
            "Eclipse Solaris",
            "Emperor's Vigilance",
            "Eyes of the Greatwolf",
            "Firesong",
            "Fleshcrafter",
            "Garukhan's Flight",
            "Gravebind",
            "Gruthkul's Pelt",
            "Hand of Heresy",
            "Heatshiver",
            "Heretic's Veil",
            "Hyrri's Ire",
            "Immutable Force",
            "Inpulsa's Broken Heart",
            "Inspired Learning",
            "Intuitive Leap",
            "Kaom's Heart",
            "Kaom's Primacy",
            "Kintsugi",
            "Kitava's Feast",
            "Le Heup of All",
            "Light of Lunaris",
            "Lioneye's Fall",
            "Maata's Teaching",
            "Machina Mitts",
            "Maloney's Mechanism",
            "Martyr of Innocence",
            "Might of the Meek",
            "Mjölner",
            "Nimis",
            "Prism Guardian",
            "Pure Talent",
            "Ralakesh's Impatience",
            "Rathpith Globe",
            "Replica Dragonfang's Flight",
            "Rigwald's Hunt",
            "Rigwald's Quills",
            "Rumi's Concoction",
            "Ryslatha's Coil",
            "Seven-League Step",
            "Shade of Solaris",
            "Shavronne's Revelation",
            "Shavronne's Wrappings",
            "Sin's Rebirth",
            "Skyforth",
            "Stormshroud",
            "Sunblast",
            "Taste of Hate",
            "The Brass Dome",
            "The Brine Crown",
            "The Covenant",
            "The Fourth Vow",
            "The Gull",
            "The Iron Fortress",
            "The Magnate",
            "The Poet's Pen",
            "Thunderfist",
            "Tidebreaker",
            "Tukohama's Fortress",
            "Unending Hunger",
            "Unnatural Instinct",
            "Utula's Hunger",
            "Void Battery",
            "Voll's Devotion",
            "Warrior's Legacy",
            "Willclash",
            "Windripper",
            "Witchbane",
            "Zerphi's Last Breath"
        };

        // Активные наборы тиров (загружаемые из tiers.json либо дефолтные)
        private HashSet<string> _t0Items = new HashSet<string>(DefaultT0Items, StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _t1Items = new HashSet<string>(DefaultT1Items, StringComparer.OrdinalIgnoreCase);

        // Соответствия текстур арта / путей -> название уника
        private ConcurrentDictionary<string, string> _artToUniqueMapping = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Кеш распознанных имен для плашек на полу (Address -> (UniqueName, Tier))
        private readonly ConcurrentDictionary<long, (string UniqueName, int Tier)> _labelNameCache =
            new ConcurrentDictionary<long, (string, int)>();

        // Постоянная сохраненная статистика (Grand Total и общие счетчики T0/T1)
        private readonly object _statsLock = new object();
        private StoredStats _stats = new StoredStats();
        private bool _isDirty = false;
        private DateTime _nextSaveTime = DateTime.UtcNow.AddSeconds(5);

        // Трекинг по инстансам локаций (instance_id -> (entity_id -> (base, unique, tier)))
        private readonly ConcurrentDictionary<uint, ConcurrentDictionary<uint, (string BaseName, string UniqueName, int Tier)>> _instanceHistory =
            new ConcurrentDictionary<uint, ConcurrentDictionary<uint, (string BaseName, string UniqueName, int Tier)>>();

        // Текущая зона
        private uint _currentAreaHash = 0;
        private string _currentAreaName = "Unknown Area";
        private bool _isCurrentAreaTownOrHideout = false;

        // Последняя посещенная боевая карта
        private string _lastMapName = "None";
        private int _lastMapTotal = 0;

        private static readonly HttpClient HttpClientInstance = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        private const string RePoEUrl = "https://repoe-fork.github.io/poe1/uniques.json";
        private const string RePoEFileName = "repoeUniques.json";
        private const string StatsFileName = "stats.json";
        private const string TiersFileName = "tiers.json";
        private const string HistoryFileName = "drops_history.jsonl";

        public override bool Initialise()
        {
            Name = "Unique Counter & Filter";

            if (Settings.ResetMapStats != null)
            {
                Settings.ResetMapStats.OnPressed += ResetCurrentMapStats;
            }

            if (Settings.ResetGrandTotal != null)
            {
                Settings.ResetGrandTotal.OnPressed += ResetLifetimeStats;
            }

            if (Settings.ReloadTiersAndDatabase != null)
            {
                Settings.ReloadTiersAndDatabase.OnPressed += () =>
                {
                    LoadTiersConfig();
                    _labelNameCache.Clear();
                    Task.Run(async () => await LoadMappingDatabaseAsync(forceReload: true));
                };
            }

            LoadHardcodedFallbacks();
            LoadTiersConfig();
            LoadStoredStats();

            Task.Run(async () => await LoadMappingDatabaseAsync(forceReload: false));

            return true;
        }

        private string GetDataDirectory()
        {
            var custom = Settings.CustomStoragePath?.Value?.Trim();
            if (!string.IsNullOrEmpty(custom))
            {
                try
                {
                    if (!Directory.Exists(custom))
                    {
                        Directory.CreateDirectory(custom);
                    }
                    return custom;
                }
                catch (Exception ex)
                {
                    LogError($"[UniqueCounter] Не удалось использовать указанную папку: {ex.Message}");
                }
            }

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var defaultDir = Path.Combine(appData, "PoEUniqueTracker");
            if (!Directory.Exists(defaultDir))
            {
                Directory.CreateDirectory(defaultDir);
            }
            return defaultDir;
        }

        private void LoadTiersConfig()
        {
            try
            {
                var dir = GetDataDirectory();
                var tiersPath = Path.Combine(dir, TiersFileName);

                if (!File.Exists(tiersPath))
                {
                    var defaultTiers = new TierConfigFile
                    {
                        T0 = DefaultT0Items.OrderBy(x => x).ToList(),
                        T1 = DefaultT1Items.OrderBy(x => x).ToList()
                    };
                    var json = JsonConvert.SerializeObject(defaultTiers, Formatting.Indented);
                    File.WriteAllText(tiersPath, json);
                    _t0Items = new HashSet<string>(DefaultT0Items, StringComparer.OrdinalIgnoreCase);
                    _t1Items = new HashSet<string>(DefaultT1Items, StringComparer.OrdinalIgnoreCase);
                    LogMessage($"[UniqueCounter] Создан файл тиров: {tiersPath}", 5);
                }
                else
                {
                    var json = File.ReadAllText(tiersPath);
                    var cfg = JsonConvert.DeserializeObject<TierConfigFile>(json);
                    if (cfg != null)
                    {
                        _t0Items = new HashSet<string>(cfg.T0 ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                        _t1Items = new HashSet<string>(cfg.T1 ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                        LogMessage($"[UniqueCounter] Загружено из tiers.json: {_t0Items.Count} T0, {_t1Items.Count} T1", 5);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"[UniqueCounter] Ошибка загрузки tiers.json: {ex.Message}", 5);
                _t0Items = new HashSet<string>(DefaultT0Items, StringComparer.OrdinalIgnoreCase);
                _t1Items = new HashSet<string>(DefaultT1Items, StringComparer.OrdinalIgnoreCase);
            }
        }

        private void LoadStoredStats()
        {
            lock (_statsLock)
            {
                try
                {
                    var dir = GetDataDirectory();
                    var statsPath = Path.Combine(dir, StatsFileName);

                    if (File.Exists(statsPath))
                    {
                        var json = File.ReadAllText(statsPath);
                        var loaded = JsonConvert.DeserializeObject<StoredStats>(json);
                        if (loaded != null)
                        {
                            _stats = loaded;
                            if (_stats.T0GrandTotal == null)
                                _stats.T0GrandTotal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                            if (_stats.T1GrandTotal == null)
                                _stats.T1GrandTotal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                            LogMessage($"[UniqueCounter] Загружена статистика: {_stats.GrandTotal} уников всего.", 5);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[UniqueCounter] Ошибка чтения stats.json: {ex.Message}", 5);
                }

                _stats = new StoredStats();
            }
        }

        private void SaveStoredStats(bool force = false)
        {
            if (!_isDirty && !force) return;

            StoredStats snapshot;
            string dir;
            lock (_statsLock)
            {
                snapshot = new StoredStats
                {
                    GrandTotal = _stats.GrandTotal,
                    T0GrandTotal = new Dictionary<string, int>(_stats.T0GrandTotal, StringComparer.OrdinalIgnoreCase),
                    T1GrandTotal = new Dictionary<string, int>(_stats.T1GrandTotal, StringComparer.OrdinalIgnoreCase),
                    LastSavedUtc = DateTime.UtcNow
                };
                dir = GetDataDirectory();
                _isDirty = false;
            }

            Task.Run(() =>
            {
                try
                {
                    var statsPath = Path.Combine(dir, StatsFileName);
                    var json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
                    File.WriteAllText(statsPath, json);
                }
                catch (Exception ex)
                {
                    LogError($"[UniqueCounter] Ошибка записи stats.json: {ex.Message}", 3);
                }
            });
        }

        private void AppendDropToHistory(string areaName, uint areaHash, string baseName, string uniqueName, string tierTag)
        {
            if (!Settings.LogHistoryToFile) return;

            var dir = GetDataDirectory();
            var record = new
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                area = areaName,
                instance_id = areaHash,
                base_item = baseName,
                unique_name = uniqueName,
                tier = tierTag
            };

            Task.Run(() =>
            {
                try
                {
                    var line = JsonConvert.SerializeObject(record) + Environment.NewLine;
                    var path = Path.Combine(dir, HistoryFileName);
                    File.AppendAllText(path, line);
                }
                catch
                {
                    // Игнорируем ошибки фоновой записи в журнал
                }
            });
        }

        public override void AreaChange(AreaInstance area)
        {
            SaveStoredStats(force: true);
            _labelNameCache.Clear();

            var areaHash = area?.Hash ?? 0;
            var areaName = area?.Name ?? "Unknown Area";
            bool isTownOrHideout = (area?.IsHideout ?? false) || (area?.IsTown ?? false);

            if (!_isCurrentAreaTownOrHideout && _currentAreaHash != 0)
            {
                if (_instanceHistory.TryGetValue(_currentAreaHash, out var prevMap))
                {
                    _lastMapTotal = prevMap.Count;
                    _lastMapName = _currentAreaName;
                }
            }

            _currentAreaHash = areaHash;
            _currentAreaName = areaName;
            _isCurrentAreaTownOrHideout = isTownOrHideout;

            if (_instanceHistory.Count > 30)
            {
                var keysToRemove = _instanceHistory.Keys.Take(10).ToList();
                foreach (var k in keysToRemove)
                {
                    if (k != _currentAreaHash)
                    {
                        _instanceHistory.TryRemove(k, out _);
                    }
                }
            }

            base.AreaChange(area);
        }

        public override void Render()
        {
            if (!Settings.Enable) return;

            var currentArea = GameController?.Area?.CurrentArea;
            bool isHideout = currentArea?.IsHideout ?? false;
            bool isTown = currentArea?.IsTown ?? false;
            bool skipScanning = Settings.IgnoreHideoutAndTown && (isHideout || isTown);

            uint areaHash = currentArea?.Hash ?? _currentAreaHash;
            string areaName = currentArea?.Name ?? _currentAreaName;

            // 1. Сканирование сущностей пола и учет статистики
            if (!skipScanning && areaHash != 0)
            {
                ScanGroundEntities(areaHash, areaName);
            }

            // 2. Отрисовка рамочек поверх плашек предметов на полу
            if (!skipScanning && Settings.HighlightLabelsOnGround)
            {
                RenderLabelsOnGround();
            }

            // 3. Фоновое периодическое сохранение данных
            if (DateTime.UtcNow >= _nextSaveTime)
            {
                _nextSaveTime = DateTime.UtcNow.AddSeconds(4);
                SaveStoredStats(force: false);
            }

            // 4. Отрисовка статического HUD оверлея
            DrawHUD(areaHash, isHideout || isTown);
        }

        private void ScanGroundEntities(uint areaHash, string areaName)
        {
            var instanceMap = _instanceHistory.GetOrAdd(areaHash, _ => new ConcurrentDictionary<uint, (string, string, int)>());

            var entities = GameController?.EntityListWrapper?.Entities;
            if (entities == null) return;

            foreach (var entity in entities.ToList())
            {
                try
                {
                    if (entity == null || !entity.IsValid) continue;

                    var worldItem = entity.GetComponent<WorldItem>();
                    if (worldItem == null) continue;

                    var itemEntity = worldItem.ItemEntity;
                    if (itemEntity == null || !itemEntity.IsValid) continue;

                    var mods = itemEntity.GetComponent<Mods>();
                    if (mods == null || mods.ItemRarity != ItemRarity.Unique) continue;

                    if (Settings.IgnoreIdentifiedItems && mods.Identified) continue;

                    if (instanceMap.ContainsKey(entity.Id)) continue;

                    var baseItemType = itemEntity.GetComponent<Base>()?.Name ?? "Unknown Base";
                    string uniqueName = ResolveUniqueName(itemEntity, mods);

                    if (string.IsNullOrEmpty(uniqueName))
                    {
                        uniqueName = "Unidentified";
                    }

                    int tier = -1;
                    string canonicalName = uniqueName;
                    if (TryMatchTier(uniqueName, out var matchedName, out var matchedTier))
                    {
                        tier = matchedTier;
                        canonicalName = matchedName;
                    }

                    if (instanceMap.TryAdd(entity.Id, (baseItemType, canonicalName, tier)))
                    {
                        lock (_statsLock)
                        {
                            _stats.GrandTotal++;

                            if (tier == 0)
                            {
                                if (_stats.T0GrandTotal.ContainsKey(canonicalName))
                                    _stats.T0GrandTotal[canonicalName]++;
                                else
                                    _stats.T0GrandTotal[canonicalName] = 1;
                            }
                            else if (tier == 1)
                            {
                                if (_stats.T1GrandTotal.ContainsKey(canonicalName))
                                    _stats.T1GrandTotal[canonicalName]++;
                                else
                                    _stats.T1GrandTotal[canonicalName] = 1;
                            }

                            _isDirty = true;
                        }

                        string tierTag = tier == 0 ? "T0" : (tier == 1 ? "T1" : "Other");
                        AppendDropToHistory(areaName, areaHash, baseItemType, canonicalName, tierTag);
                    }
                }
                catch
                {
                    // Игнорируем точечные сбои чтения памяти
                }
            }
        }

        private void RenderLabelsOnGround()
        {
            var labelsOnGround = GameController?.IngameState?.IngameUi?.ItemsOnGroundLabels;
            if (labelsOnGround == null) return;

            foreach (var labelItem in labelsOnGround)
            {
                try
                {
                    if (labelItem == null) continue;

                    var nativeLabel = labelItem.Label;
                    if (nativeLabel == null || !nativeLabel.IsVisible) continue;

                    var rect = nativeLabel.GetClientRect();
                    if (rect.Width <= 0 || rect.Height <= 0) continue;

                    var itemEntity = labelItem.ItemOnGround;
                    if (itemEntity == null || !itemEntity.IsValid) continue;

                    var worldItem = itemEntity.GetComponent<WorldItem>();
                    if (worldItem == null) continue;

                    var realItem = worldItem.ItemEntity;
                    if (realItem == null || !realItem.IsValid) continue;

                    var mods = realItem.GetComponent<Mods>();
                    bool isUnique = false;

                    if (mods != null && mods.ItemRarity == ItemRarity.Unique)
                    {
                        isUnique = true;
                    }
                    else if (!string.IsNullOrEmpty(realItem.Path) &&
                            (realItem.Path.Contains("Unique", StringComparison.OrdinalIgnoreCase) ||
                             _artToUniqueMapping.ContainsKey(CleanPathString(realItem.Path))))
                    {
                        isUnique = true;
                    }

                    if (!isUnique) continue;

                    if (!_labelNameCache.TryGetValue(realItem.Address, out var cachedInfo))
                    {
                        string uniqueName = ResolveUniqueName(realItem, mods);
                        int tier = -1;
                        string canonicalName = uniqueName;

                        if (!string.IsNullOrEmpty(uniqueName) && TryMatchTier(uniqueName, out var matchedName, out var matchedTier))
                        {
                            tier = matchedTier;
                            canonicalName = matchedName;
                        }

                        cachedInfo = (canonicalName, tier);
                        if (!string.IsNullOrEmpty(canonicalName))
                        {
                            _labelNameCache[realItem.Address] = cachedInfo;
                        }
                    }

                    if (string.IsNullOrEmpty(cachedInfo.UniqueName)) continue;

                    // Если включен фильтр только по тирам - пропускаем предметы не из T0/T1
                    if (Settings.HighlightOnlyTiers && cachedInfo.Tier < 0) continue;

                    // Определение цвета рамки
                    SharpDX.Color borderColor = Settings.HighlightBorderColor.Value;
                    if (Settings.UseTierColorsForFrames)
                    {
                        if (cachedInfo.Tier == 0)
                            borderColor = Settings.T0Color.Value;
                        else if (cachedInfo.Tier == 1)
                            borderColor = Settings.T1Color.Value;
                    }

                    // 1. Отрисовка фона плашки (если задана прозрачность)
                    if (Settings.HighlightBackgroundColor.Value.A > 0)
                    {
                        Graphics.DrawBox(rect, Settings.HighlightBackgroundColor.Value);
                    }

                    // 2. Отрисовка рамки вокруг плашки
                    Graphics.DrawFrame(rect, borderColor, Settings.HighlightBorderThickness.Value);

                    // 3. Отрисовка названия уника поверх нативной плашки
                    if (Settings.DrawNameOnLabel)
                    {
                        using (Graphics.SetTextScale(Settings.LabelTextScale.Value))
                        {
                            var textSize = Graphics.MeasureText(cachedInfo.UniqueName);
                            var textPos = new System.Numerics.Vector2(
                                rect.X + rect.Width / 2f - textSize.X / 2f,
                                rect.Y + rect.Height / 2f - textSize.Y / 2f
                            );
                            Graphics.DrawText(cachedInfo.UniqueName, textPos, Settings.LabelNameTextColor.Value);
                        }
                    }
                }
                catch
                {
                    // Игнорируем кадровые сбои интерфейса
                }
            }
        }

        private string ResolveUniqueName(Entity realItem, Mods mods)
        {
            // 1. Опознанный уник
            if (!string.IsNullOrEmpty(mods?.UniqueName))
            {
                return CleanString(mods.UniqueName);
            }

            // 2. Поиск по Entity Path (Metadata/Items/...)
            if (!string.IsNullOrEmpty(realItem.Path))
            {
                var cleanEntityPath = CleanPathString(realItem.Path);
                if (_artToUniqueMapping.TryGetValue(cleanEntityPath, out var nameByPath))
                    return nameByPath;
            }

            // 3. Поиск по 2D-арту и Visual ID
            var renderItem = realItem.GetComponent<RenderItem>();
            if (renderItem != null && !string.IsNullOrEmpty(renderItem.ResourcePath))
            {
                var rawPath = CleanPathString(renderItem.ResourcePath);
                if (!string.IsNullOrEmpty(rawPath))
                {
                    if (_artToUniqueMapping.TryGetValue(rawPath, out var mappedName) && !string.IsNullOrEmpty(mappedName))
                    {
                        return mappedName;
                    }
                    else
                    {
                        var fileName = Path.GetFileNameWithoutExtension(rawPath);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            if (_artToUniqueMapping.TryGetValue(fileName, out var fallbackName))
                            {
                                return fallbackName;
                            }
                            else
                            {
                                return fileName;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private bool TryMatchTier(string rawName, out string canonicalName, out int tier)
        {
            canonicalName = null;
            tier = -1;
            if (string.IsNullOrWhiteSpace(rawName)) return false;

            var norm = rawName.Replace('’', '\'').Trim();

            // 1. Поиск в T0
            if (_t0Items.TryGetValue(norm, out var t0Match))
            {
                canonicalName = t0Match;
                tier = 0;
                return true;
            }

            string altNorm = norm.StartsWith("The ", StringComparison.OrdinalIgnoreCase)
                ? norm.Substring(4).Trim()
                : "The " + norm;

            if (_t0Items.TryGetValue(altNorm, out var t0AltMatch))
            {
                canonicalName = t0AltMatch;
                tier = 0;
                return true;
            }

            // 2. Поиск в T1
            if (_t1Items.TryGetValue(norm, out var t1Match))
            {
                canonicalName = t1Match;
                tier = 1;
                return true;
            }

            if (_t1Items.TryGetValue(altNorm, out var t1AltMatch))
            {
                canonicalName = t1AltMatch;
                tier = 1;
                return true;
            }

            return false;
        }

        private void DrawHUD(uint areaHash, bool isTownOrHideout)
        {
            int currentMapTotal = 0;
            var currentMapT0 = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var currentMapT1 = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (_instanceHistory.TryGetValue(areaHash, out var currentInstance))
            {
                currentMapTotal = currentInstance.Count;
                foreach (var item in currentInstance.Values)
                {
                    if (item.Tier == 0)
                    {
                        currentMapT0[item.UniqueName] = currentMapT0.TryGetValue(item.UniqueName, out var c) ? c + 1 : 1;
                    }
                    else if (item.Tier == 1)
                    {
                        currentMapT1[item.UniqueName] = currentMapT1.TryGetValue(item.UniqueName, out var c) ? c + 1 : 1;
                    }
                }
            }

            StoredStats statsSnapshot;
            lock (_statsLock)
            {
                statsSnapshot = _stats;
            }

            using (Graphics.SetTextScale(Settings.TextScale.Value))
            {
                var mainTextColor = Settings.MainTextColor.Value;
                var mapTotalColor = Settings.MapTotalColor.Value;
                var t0Color = Settings.T0Color.Value;
                var t1Color = Settings.T1Color.Value;

                // 1. Отрисовка уников за текущую карту
                if (Settings.ShowMapTotal)
                {
                    var mapPos = Settings.MapTotalPosition.Value;
                    string mapText;
                    if (isTownOrHideout && currentMapTotal == 0 && _lastMapTotal > 0)
                    {
                        mapText = $"Map Uniques: {_lastMapTotal} (Last: {_lastMapName})";
                    }
                    else
                    {
                        mapText = $"Map Uniques: {currentMapTotal}";
                    }
                    Graphics.DrawText(mapText, mapPos, mapTotalColor);
                }

                // 2. Отрисовка общего числа (Grand Total)
                if (Settings.ShowGrandTotal)
                {
                    var grandPos = Settings.GrandTotalPosition.Value;
                    var grandText = $"Grand Total: {statsSnapshot.GrandTotal}";
                    Graphics.DrawText(grandText, grandPos, mainTextColor);
                }

                // 3. Отрисовка T0 уников
                if (Settings.ShowT0Uniques && statsSnapshot.T0GrandTotal != null && statsSnapshot.T0GrandTotal.Count > 0)
                {
                    var t0Pos = Settings.T0Position.Value;
                    var headerSize = Graphics.DrawText("T0 Uniques:", t0Pos, mainTextColor);
                    t0Pos.Y += headerSize.Y + 2;

                    foreach (var kvp in statsSnapshot.T0GrandTotal.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
                    {
                        string itemName = kvp.Key;
                        int count = kvp.Value;

                        var nameText = $"  {itemName}";
                        var nameSize = Graphics.DrawText(nameText, t0Pos, t0Color);

                        string countText = $": {count}";
                        if (Settings.ShowMapContributionInList && currentMapT0.TryGetValue(itemName, out var mapCount) && mapCount > 0)
                        {
                            countText += $" (+{mapCount} on map)";
                        }

                        Graphics.DrawText(countText, t0Pos + new System.Numerics.Vector2(nameSize.X, 0), mainTextColor);
                        t0Pos.Y += nameSize.Y;
                    }
                }

                // 4. Отрисовка T1 уников
                if (Settings.ShowT1Uniques && statsSnapshot.T1GrandTotal != null && statsSnapshot.T1GrandTotal.Count > 0)
                {
                    var t1Pos = Settings.T1Position.Value;
                    var headerSize = Graphics.DrawText("T1 Uniques:", t1Pos, mainTextColor);
                    t1Pos.Y += headerSize.Y + 2;

                    foreach (var kvp in statsSnapshot.T1GrandTotal.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
                    {
                        string itemName = kvp.Key;
                        int count = kvp.Value;

                        var nameText = $"  {itemName}";
                        var nameSize = Graphics.DrawText(nameText, t1Pos, t1Color);

                        string countText = $": {count}";
                        if (Settings.ShowMapContributionInList && currentMapT1.TryGetValue(itemName, out var mapCount) && mapCount > 0)
                        {
                            countText += $" (+{mapCount} on map)";
                        }

                        Graphics.DrawText(countText, t1Pos + new System.Numerics.Vector2(nameSize.X, 0), mainTextColor);
                        t1Pos.Y += nameSize.Y;
                    }
                }
            }
        }

        private void ResetCurrentMapStats()
        {
            if (_currentAreaHash != 0)
            {
                _instanceHistory.TryRemove(_currentAreaHash, out _);
            }
            _lastMapTotal = 0;
            _lastMapName = "Reset";
            _labelNameCache.Clear();
            LogMessage("[UniqueCounter] Статистика текущей карты сброшена.", 3);
        }

        private void ResetLifetimeStats()
        {
            lock (_statsLock)
            {
                _stats = new StoredStats();
                _isDirty = true;
            }
            SaveStoredStats(force: true);
            LogMessage("[UniqueCounter] Вся накопленная статистика (Grand Total) сброшена.", 3);
        }

        private void RegisterMappingKey(string key, string uniqueName)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(uniqueName)) return;

            var cleanKey = CleanPathString(key);

            bool isReplica = uniqueName.StartsWith("Replica ", StringComparison.OrdinalIgnoreCase) ||
                             uniqueName.StartsWith("Копия ", StringComparison.OrdinalIgnoreCase);

            if (!_artToUniqueMapping.TryGetValue(cleanKey, out var existingName) ||
                (!isReplica && existingName.StartsWith("Replica ", StringComparison.OrdinalIgnoreCase)))
            {
                _artToUniqueMapping[cleanKey] = uniqueName;

                if (!cleanKey.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                {
                    _artToUniqueMapping[cleanKey + ".dds"] = uniqueName;
                }
            }
        }

        private void LoadHardcodedFallbacks()
        {
            RegisterMappingKey("art/2ditems/belts/heavybeltunique", "Mageblood");
            RegisterMappingKey("art/2ditems/belts/headhunter", "Headhunter");
            RegisterMappingKey("art/2ditems/shields/thesquire", "The Squire");
            RegisterMappingKey("art/2ditems/rings/kalandras_touch", "Kalandra's Touch");
            RegisterMappingKey("art/2ditems/rings/nimis", "Nimis");
            RegisterMappingKey("art/2ditems/amulets/defianceofdestiny", "Defiance of Destiny");
            RegisterMappingKey("art/2ditems/weapons/onehandweapons/onehandswords/ephemeraledge", "Ephemeral Edge");
            RegisterMappingKey("art/2ditems/weapons/onehandweapons/onehandaxes/soultaker", "Soul Taker");
            RegisterMappingKey("art/2ditems/weapons/onehandweapons/claws/bloodseeker", "Bloodseeker");
            RegisterMappingKey("art/2ditems/weapons/onehandweapons/daggers/divinarius", "Divinarius");
            RegisterMappingKey("art/2ditems/armours/bodyarmours/dexintarmours/covenant", "The Covenant");
        }

        private async Task LoadMappingDatabaseAsync(bool forceReload)
        {
            var dir = GetDataDirectory();
            var repoePath = Path.Combine(dir, RePoEFileName);

            if (forceReload && File.Exists(repoePath))
            {
                try { File.Delete(repoePath); } catch { }
            }

            if (!File.Exists(repoePath))
            {
                try
                {
                    LogMessage("[UniqueCounter] Скачивание базы уникальных предметов (RePoE)...", 5);
                    var json = await HttpClientInstance.GetStringAsync(RePoEUrl);
                    await File.WriteAllTextAsync(repoePath, json);
                    LogMessage("[UniqueCounter] База RePoE успешно сохранена!", 5);
                }
                catch (Exception ex)
                {
                    LogError($"[UniqueCounter] Ошибка загрузки базы RePoE: {ex.Message}", 5);
                }
            }

            if (File.Exists(repoePath))
            {
                try
                {
                    var jsonText = await File.ReadAllTextAsync(repoePath);
                    var repoeData = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(jsonText);

                    if (repoeData != null)
                    {
                        foreach (var kvp in repoeData)
                        {
                            var metadataPath = kvp.Key;
                            var itemData = kvp.Value;
                            if (itemData == null) continue;

                            string uniqueName = itemData["name"]?.ToString() ?? itemData["id"]?.ToString() ?? kvp.Key;
                            if (string.IsNullOrEmpty(uniqueName)) continue;

                            if (!string.IsNullOrEmpty(metadataPath))
                            {
                                RegisterMappingKey(metadataPath, uniqueName);
                            }

                            var visualIdentity = itemData["visual_identity"];
                            if (visualIdentity is JObject visualObj)
                            {
                                string ddsFile = visualObj["dds_file"]?.ToString();
                                if (!string.IsNullOrEmpty(ddsFile))
                                {
                                    RegisterMappingKey(ddsFile, uniqueName);
                                }

                                string visualId = visualObj["id"]?.ToString();
                                if (!string.IsNullOrEmpty(visualId))
                                {
                                    RegisterMappingKey(visualId, uniqueName);
                                }
                            }
                            else if (visualIdentity != null)
                            {
                                var visualStr = visualIdentity.ToString();
                                if (!string.IsNullOrEmpty(visualStr))
                                {
                                    RegisterMappingKey(visualStr, uniqueName);
                                }
                            }
                        }

                        LogMessage($"[UniqueCounter] Успешно загружено {_artToUniqueMapping.Count} соответствий из RePoE.", 5);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[UniqueCounter] Ошибка парсинга базы RePoE: {ex.Message}", 5);
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
