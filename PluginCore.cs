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

        // --- ПОДСВЕТКА И РАМКИ ПЛАШЕК НА ПОЛУ (ИЗ UNIQUE-FILTER) ---
        public ToggleNode HighlightLabelsOnGround { get; set; } = new ToggleNode(true);
        public ColorNode BorderColor { get; set; } = new ColorNode(SharpDX.Color.Gold);
        public RangeNode<int> BorderThickness { get; set; } = new RangeNode<int>(3, 1, 10);
        public ColorNode BackgroundColor { get; set; } = new ColorNode(new SharpDX.Color(255, 215, 0, 70));
        public ColorNode TextColor { get; set; } = new ColorNode(SharpDX.Color.White);
        public RangeNode<float> LabelTextScale { get; set; } = new RangeNode<float>(1.0f, 0.4f, 2.5f);
        public RangeNode<float> LabelTextThickness { get; set; } = new RangeNode<float>(0.5f, 0.0f, 2.0f);

        // --- НАСТРОЙКИ ОТОБРАЖЕНИЯ HUD (СЧЕТЧИКИ) ---
        public ToggleNode ShowMapTotal { get; set; } = new ToggleNode(true);
        public ToggleNode ShowGrandTotal { get; set; } = new ToggleNode(false);
        public ToggleNode ShowT0Uniques { get; set; } = new ToggleNode(true);
        public ToggleNode ShowT1Uniques { get; set; } = new ToggleNode(true);
        public ToggleNode ShowGrandTotalInList { get; set; } = new ToggleNode(false);
        public ToggleNode ShowOnlyMapDrops { get; set; } = new ToggleNode(true);

        // --- СЧЕТЧИКИ ВАЛЮТЫ НА КАРТЕ (CURRENCY COUNTER) ---
        public ToggleNode ShowMapCurrencyTotal { get; set; } = new ToggleNode(true);
        public ToggleNode ShowMapValuableCurrency { get; set; } = new ToggleNode(true);
        public ColorNode MapCurrencyTotalColor { get; set; } = new ColorNode(SharpDX.Color.SkyBlue);
        public ColorNode MapValuableCurrencyColor { get; set; } = new ColorNode(new SharpDX.Color(255, 165, 0)); // Gold/Orange
        public RangeNode<System.Numerics.Vector2> MapCurrencyTotalPosition { get; set; } = new RangeNode<System.Numerics.Vector2>(
            new System.Numerics.Vector2(20, 105),
            new System.Numerics.Vector2(0, 0),
            new System.Numerics.Vector2(3840, 2160)
        );
        public RangeNode<System.Numerics.Vector2> MapValuableCurrencyPosition { get; set; } = new RangeNode<System.Numerics.Vector2>(
            new System.Numerics.Vector2(20, 130),
            new System.Numerics.Vector2(0, 0),
            new System.Numerics.Vector2(3840, 2160)
        );

        // --- ЦВЕТА И ШРИФТЫ HUD ---
        public RangeNode<float> TextScale { get; set; } = new RangeNode<float>(1.0f, 0.3f, 3.0f);
        public RangeNode<float> TextThickness { get; set; } = new RangeNode<float>(0.5f, 0.0f, 2.0f);
        public ToggleNode TextOutline { get; set; } = new ToggleNode(true);
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
            new System.Numerics.Vector2(20, 260),
            new System.Numerics.Vector2(0, 0),
            new System.Numerics.Vector2(3840, 2160)
        );

        public RangeNode<System.Numerics.Vector2> T0Position { get; set; } = new RangeNode<System.Numerics.Vector2>(
            new System.Numerics.Vector2(20, 160),
            new System.Numerics.Vector2(0, 0),
            new System.Numerics.Vector2(3840, 2160)
        );

        public RangeNode<System.Numerics.Vector2> T1Position { get; set; } = new RangeNode<System.Numerics.Vector2>(
            new System.Numerics.Vector2(20, 310),
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

        [JsonProperty("ValuableCurrency")]
        public List<string> ValuableCurrency { get; set; } = new List<string>();
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

        // Дефолтный список ценной валюты (от Chaos Orb и выше)
        private static readonly HashSet<string> DefaultValuableCurrency = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Chaos Orb",
            "Divine Orb",
            "Exalted Orb",
            "Mirror of Kalandra",
            "Mirror Shard",
            "Sacred Orb",
            "Orb of Annulment",
            "Annulment Shard",
            "Ancient Orb",
            "Ancient Shard",
            "Exalted Shard",
            "Fracturing Orb",
            "Fracturing Shard",
            "Veiled Orb",
            "Veiled Chaos Orb",
            "Awakener's Orb",
            "Crusader's Exalted Orb",
            "Hunter's Exalted Orb",
            "Redeemer's Exalted Orb",
            "Warlord's Exalted Orb",
            "Orb of Dominance",
            "Eldritch Chaos Orb",
            "Eldritch Exalted Orb",
            "Eldritch Orb of Annulment",
            "Grand Eldritch Ichor",
            "Exceptional Eldritch Ichor",
            "Grand Eldritch Ember",
            "Exceptional Eldritch Ember",
            "Orb of Conflict",
            "Hinekora's Lock",
            "Tailoring Orb",
            "Tempering Orb",
            "Valdo's Puzzle Box",
            "Voidborn Reliquary Key",
            "Apollyon's Reliquary Key",
            "Forgotten Reliquary Key",
            "Visage Reliquary Key",
            "Cosmic Reliquary Key",
            "Decay Reliquary Key",
            "Shiny Reliquary Key",
            "Timeworn Reliquary Key",
            "Votive Hoard",
            "Prime Regrading Lens",
            "Secondary Regrading Lens"
        };

        // Активные наборы тиров (загружаемые из tiers.json либо дефолтные)
        private HashSet<string> _t0Items = new HashSet<string>(DefaultT0Items, StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _t1Items = new HashSet<string>(DefaultT1Items, StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _valuableCurrencyItems = new HashSet<string>(DefaultValuableCurrency, StringComparer.OrdinalIgnoreCase);

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

        // Трекинг валюты по инстансам: instance_id -> (entity_id -> (name, stackSize, isValuable))
        private readonly ConcurrentDictionary<uint, ConcurrentDictionary<uint, (string Name, int StackSize, bool IsValuable)>> _instanceCurrencyHistory =
            new ConcurrentDictionary<uint, ConcurrentDictionary<uint, (string Name, int StackSize, bool IsValuable)>>();

        // Текущая зона
        private uint _currentAreaHash = 0;
        private string _currentAreaName = "Unknown Area";
        private bool _isCurrentAreaTownOrHideout = false;

        // Последняя посещенная боевая карта
        private string _lastMapName = "None";
        private int _lastMapTotal = 0;
        private int _lastMapCurrencyTotal = 0;
        private int _lastMapValuableCurrency = 0;
        private readonly ConcurrentDictionary<string, int> _lastMapT0 = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, int> _lastMapT1 = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static readonly HttpClient HttpClientInstance = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        private const string PrimaryRePoEUrl = "https://repoe-fork.github.io/uniques.json";
        private const string FallbackArtMappingUrl = "https://raw.githubusercontent.com/DetectiveSquirrel/Ground-Items-With-Linq/master/uniqueArtMapping.default.json";
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
                        T1 = DefaultT1Items.OrderBy(x => x).ToList(),
                        ValuableCurrency = DefaultValuableCurrency.OrderBy(x => x).ToList()
                    };
                    var json = JsonConvert.SerializeObject(defaultTiers, Formatting.Indented);
                    File.WriteAllText(tiersPath, json);
                    _t0Items = new HashSet<string>(DefaultT0Items, StringComparer.OrdinalIgnoreCase);
                    _t1Items = new HashSet<string>(DefaultT1Items, StringComparer.OrdinalIgnoreCase);
                    _valuableCurrencyItems = new HashSet<string>(DefaultValuableCurrency, StringComparer.OrdinalIgnoreCase);
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
                        if (cfg.ValuableCurrency != null && cfg.ValuableCurrency.Count > 0)
                        {
                            _valuableCurrencyItems = new HashSet<string>(cfg.ValuableCurrency, StringComparer.OrdinalIgnoreCase);
                        }
                        else
                        {
                            _valuableCurrencyItems = new HashSet<string>(DefaultValuableCurrency, StringComparer.OrdinalIgnoreCase);
                        }
                        LogMessage($"[UniqueCounter] Загружено из tiers.json: {_t0Items.Count} T0, {_t1Items.Count} T1, {_valuableCurrencyItems.Count} ценной валюты", 5);
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

                    _lastMapT0.Clear();
                    _lastMapT1.Clear();
                    foreach (var item in prevMap.Values)
                    {
                        if (item.Tier == 0)
                            _lastMapT0.AddOrUpdate(item.UniqueName, 1, (_, c) => c + 1);
                        else if (item.Tier == 1)
                            _lastMapT1.AddOrUpdate(item.UniqueName, 1, (_, c) => c + 1);
                    }
                }

                if (_instanceCurrencyHistory.TryGetValue(_currentAreaHash, out var prevCurrencyMap))
                {
                    _lastMapCurrencyTotal = 0;
                    _lastMapValuableCurrency = 0;
                    foreach (var curr in prevCurrencyMap.Values)
                    {
                        _lastMapCurrencyTotal += curr.StackSize;
                        if (curr.IsValuable)
                        {
                            _lastMapValuableCurrency += curr.StackSize;
                        }
                    }
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

            if (_instanceCurrencyHistory.Count > 30)
            {
                var keysToRemove = _instanceCurrencyHistory.Keys.Take(10).ToList();
                foreach (var k in keysToRemove)
                {
                    if (k != _currentAreaHash)
                    {
                        _instanceCurrencyHistory.TryRemove(k, out _);
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
            var instanceCurrencyMap = _instanceCurrencyHistory.GetOrAdd(areaHash, _ => new ConcurrentDictionary<uint, (string, int, bool)>());

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

                    // 1. Проверяем уникальные предметы
                    if (mods != null && mods.ItemRarity == ItemRarity.Unique)
                    {
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
                    // 2. Проверяем предметы валюты (любая валюта: от Scroll of Wisdom до Mirror)
                    else if (IsCurrencyItem(itemEntity, out var currName, out var stackSize, out var isValuable))
                    {
                        if (instanceCurrencyMap.ContainsKey(entity.Id)) continue;

                        if (instanceCurrencyMap.TryAdd(entity.Id, (currName, stackSize, isValuable)))
                        {
                            if (isValuable && Settings.LogHistoryToFile)
                            {
                                AppendDropToHistory(areaName, areaHash, "Currency", $"{currName} (x{stackSize})", "ValuableCurrency");
                            }
                        }
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

                    // Подсвечиваем только уники из T0/T1
                    if (cachedInfo.Tier < 0) continue;

                    // 1. Отрисовка фона плашки (как в оригинальном unique-filter)
                    if (Settings.BackgroundColor.Value.A > 0)
                    {
                        Graphics.DrawBox(rect, Settings.BackgroundColor.Value);
                    }

                    // 2. Отрисовка рамки вокруг плашки (как в оригинальном unique-filter)
                    Graphics.DrawFrame(rect, Settings.BorderColor.Value, Settings.BorderThickness.Value);

                    // 3. Отрисовка названия уника поверх плашки (всегда пишем)
                    using (Graphics.SetTextScale(Settings.LabelTextScale.Value))
                    {
                        var textSize = Graphics.MeasureText(cachedInfo.UniqueName);
                        var textPos = new System.Numerics.Vector2(
                            rect.Center.X - textSize.X / 2f,
                            rect.Center.Y - textSize.Y / 2f
                        );
                        DrawThickText(
                            cachedInfo.UniqueName,
                            textPos,
                            Settings.TextColor.Value,
                            Settings.LabelTextThickness.Value,
                            false
                        );
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
            int currentMapCurrencyTotal = 0;
            int currentMapValuableCurrency = 0;
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
            else if (isTownOrHideout && currentMapTotal == 0 && _lastMapTotal > 0)
            {
                currentMapT0 = new Dictionary<string, int>(_lastMapT0, StringComparer.OrdinalIgnoreCase);
                currentMapT1 = new Dictionary<string, int>(_lastMapT1, StringComparer.OrdinalIgnoreCase);
            }

            if (_instanceCurrencyHistory.TryGetValue(areaHash, out var currentCurrencyInstance))
            {
                foreach (var curr in currentCurrencyInstance.Values)
                {
                    currentMapCurrencyTotal += curr.StackSize;
                    if (curr.IsValuable)
                    {
                        currentMapValuableCurrency += curr.StackSize;
                    }
                }
            }
            else if (isTownOrHideout && currentMapCurrencyTotal == 0 && _lastMapCurrencyTotal > 0)
            {
                currentMapCurrencyTotal = _lastMapCurrencyTotal;
                currentMapValuableCurrency = _lastMapValuableCurrency;
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
                float thickness = Settings.TextThickness.Value;
                bool outline = Settings.TextOutline.Value;

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
                    DrawThickText(mapText, mapPos, mapTotalColor, thickness, outline);
                }

                // 2. Отрисовка общего количества валюты за карту (Map Currency Total)
                if (Settings.ShowMapCurrencyTotal)
                {
                    var currPos = Settings.MapCurrencyTotalPosition.Value;
                    string currText = (isTownOrHideout && currentMapCurrencyTotal == 0 && _lastMapCurrencyTotal > 0)
                        ? $"Map Currency: {_lastMapCurrencyTotal} (Last)"
                        : $"Map Currency: {currentMapCurrencyTotal}";
                    DrawThickText(currText, currPos, Settings.MapCurrencyTotalColor.Value, thickness, outline);
                }

                // 3. Отрисовка ценной валюты за карту (от Chaos и выше)
                if (Settings.ShowMapValuableCurrency)
                {
                    var valPos = Settings.MapValuableCurrencyPosition.Value;
                    string valText = (isTownOrHideout && currentMapValuableCurrency == 0 && _lastMapValuableCurrency > 0)
                        ? $"Valuable Currency: {_lastMapValuableCurrency} (Last)"
                        : $"Valuable Currency: {currentMapValuableCurrency}";
                    DrawThickText(valText, valPos, Settings.MapValuableCurrencyColor.Value, thickness, outline);
                }

                // 4. Отрисовка общего числа уников за всё время (Grand Total)
                if (Settings.ShowGrandTotal)
                {
                    var grandPos = Settings.GrandTotalPosition.Value;
                    var grandText = $"Grand Total: {statsSnapshot.GrandTotal}";
                    DrawThickText(grandText, grandPos, mainTextColor, thickness, outline);
                }

                // 3. Отрисовка T0 уников
                if (Settings.ShowT0Uniques)
                {
                    var itemsToShow = new List<(string Name, int MapCount, int GrandCount)>();

                    if (Settings.ShowOnlyMapDrops)
                    {
                        foreach (var kvp in currentMapT0)
                        {
                            int grandCount = (statsSnapshot.T0GrandTotal != null && statsSnapshot.T0GrandTotal.TryGetValue(kvp.Key, out var gc)) ? gc : kvp.Value;
                            itemsToShow.Add((kvp.Key, kvp.Value, grandCount));
                        }
                    }
                    else if (statsSnapshot.T0GrandTotal != null)
                    {
                        foreach (var kvp in statsSnapshot.T0GrandTotal)
                        {
                            int mapCount = currentMapT0.TryGetValue(kvp.Key, out var mc) ? mc : 0;
                            itemsToShow.Add((kvp.Key, mapCount, kvp.Value));
                        }
                    }

                    if (itemsToShow.Count > 0)
                    {
                        var t0Pos = Settings.T0Position.Value;
                        string headerText = (isTownOrHideout && currentMapTotal == 0 && _lastMapTotal > 0)
                            ? "T0 Uniques (Last Map):"
                            : "T0 Uniques:";
                        var headerSize = DrawThickText(headerText, t0Pos, mainTextColor, thickness, outline);
                        t0Pos.Y += headerSize.Y + 2;

                        foreach (var item in itemsToShow.OrderByDescending(x => x.MapCount).ThenByDescending(x => x.GrandCount).ThenBy(x => x.Name))
                        {
                            var nameText = $"  {item.Name}";
                            var nameSize = DrawThickText(nameText, t0Pos, t0Color, thickness, outline);

                            // Изначально пишется только мап контрибьюшн
                            string countText = $": {item.MapCount}";

                            // По галочке можно выводить за все время
                            if (Settings.ShowGrandTotalInList)
                            {
                                countText += $" (Total: {item.GrandCount})";
                            }

                            DrawThickText(countText, t0Pos + new System.Numerics.Vector2(nameSize.X, 0), mainTextColor, thickness, outline);
                            t0Pos.Y += nameSize.Y;
                        }
                    }
                }

                // 4. Отрисовка T1 уников
                if (Settings.ShowT1Uniques)
                {
                    var itemsToShow = new List<(string Name, int MapCount, int GrandCount)>();

                    if (Settings.ShowOnlyMapDrops)
                    {
                        foreach (var kvp in currentMapT1)
                        {
                            int grandCount = (statsSnapshot.T1GrandTotal != null && statsSnapshot.T1GrandTotal.TryGetValue(kvp.Key, out var gc)) ? gc : kvp.Value;
                            itemsToShow.Add((kvp.Key, kvp.Value, grandCount));
                        }
                    }
                    else if (statsSnapshot.T1GrandTotal != null)
                    {
                        foreach (var kvp in statsSnapshot.T1GrandTotal)
                        {
                            int mapCount = currentMapT1.TryGetValue(kvp.Key, out var mc) ? mc : 0;
                            itemsToShow.Add((kvp.Key, mapCount, kvp.Value));
                        }
                    }

                    if (itemsToShow.Count > 0)
                    {
                        var t1Pos = Settings.T1Position.Value;
                        string headerText = (isTownOrHideout && currentMapTotal == 0 && _lastMapTotal > 0)
                            ? "T1 Uniques (Last Map):"
                            : "T1 Uniques:";
                        var headerSize = DrawThickText(headerText, t1Pos, mainTextColor, thickness, outline);
                        t1Pos.Y += headerSize.Y + 2;

                        foreach (var item in itemsToShow.OrderByDescending(x => x.MapCount).ThenByDescending(x => x.GrandCount).ThenBy(x => x.Name))
                        {
                            var nameText = $"  {item.Name}";
                            var nameSize = DrawThickText(nameText, t1Pos, t1Color, thickness, outline);

                            // Изначально пишется только мап контрибьюшн
                            string countText = $": {item.MapCount}";

                            // По галочке можно выводить за все время
                            if (Settings.ShowGrandTotalInList)
                            {
                                countText += $" (Total: {item.GrandCount})";
                            }

                            DrawThickText(countText, t1Pos + new System.Numerics.Vector2(nameSize.X, 0), mainTextColor, thickness, outline);
                            t1Pos.Y += nameSize.Y;
                        }
                    }
                }
            }
        }

        private void ResetCurrentMapStats()
        {
            if (_currentAreaHash != 0)
            {
                _instanceHistory.TryRemove(_currentAreaHash, out _);
                _instanceCurrencyHistory.TryRemove(_currentAreaHash, out _);
            }
            _lastMapTotal = 0;
            _lastMapCurrencyTotal = 0;
            _lastMapValuableCurrency = 0;
            _lastMapName = "Reset";
            _lastMapT0.Clear();
            _lastMapT1.Clear();
            _labelNameCache.Clear();
            LogMessage("[UniqueCounter] Статистика текущей карты (уники и валюта) сброшена.", 3);
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
                var downloadUrls = new[] { PrimaryRePoEUrl, FallbackArtMappingUrl };
                bool downloaded = false;

                foreach (var url in downloadUrls)
                {
                    try
                    {
                        LogMessage($"[UniqueCounter] Скачивание базы уникальных предметов: {url}...", 5);
                        using (var response = await HttpClientInstance.GetAsync(url))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                var json = await response.Content.ReadAsStringAsync();
                                if (!string.IsNullOrWhiteSpace(json) && json.Length > 1000)
                                {
                                    await File.WriteAllTextAsync(repoePath, json);
                                    LogMessage("[UniqueCounter] База предметов успешно скачана и сохранена!", 5);
                                    downloaded = true;
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"[UniqueCounter] Ошибка загрузки из {url}: {ex.Message}", 5);
                    }
                }

                if (!downloaded)
                {
                    LogError("[UniqueCounter] Не удалось скачать базу ни по одной ссылке, используются встроенные fallback соответствия.", 5);
                }
            }

            if (File.Exists(repoePath))
            {
                try
                {
                    var jsonText = await File.ReadAllTextAsync(repoePath);
                    var parsedData = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(jsonText);

                    if (parsedData != null)
                    {
                        foreach (var kvp in parsedData)
                        {
                            var key = kvp.Key;
                            var token = kvp.Value;
                            if (token == null) continue;

                            if (token is JObject itemObj)
                            {
                                // Формат RePoE: { "name": "...", "visual_identity": ... }
                                string uniqueName = itemObj["name"]?.ToString() ?? itemObj["id"]?.ToString() ?? key;
                                if (!string.IsNullOrEmpty(uniqueName))
                                {
                                    if (!string.IsNullOrEmpty(key))
                                    {
                                        RegisterMappingKey(key, uniqueName);
                                    }

                                    var visualIdentity = itemObj["visual_identity"];
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
                            }
                            else if (token is JArray arr)
                            {
                                // Формат Ground-Items: "Art/2DItems/...dds": ["Mageblood"]
                                foreach (var subItem in arr)
                                {
                                    var name = subItem?.ToString();
                                    if (!string.IsNullOrEmpty(name))
                                    {
                                        RegisterMappingKey(key, name);
                                    }
                                }
                            }
                            else if (token.Type == JTokenType.String)
                            {
                                // Простой формат: "Art/...dds": "Mageblood"
                                RegisterMappingKey(key, token.ToString());
                            }
                        }

                        LogMessage($"[UniqueCounter] Успешно загружено {_artToUniqueMapping.Count} соответствий текстур предметов.", 5);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[UniqueCounter] Ошибка парсинга базы предметов: {ex.Message}", 5);
                }
            }
        }

        private System.Numerics.Vector2 DrawThickText(
            string text,
            System.Numerics.Vector2 position,
            SharpDX.Color color,
            float thickness,
            bool outline)
        {
            if (string.IsNullOrEmpty(text)) return System.Numerics.Vector2.Zero;

            var size = Graphics.MeasureText(text);

            // 1. Аккуратная тонкая 4-сторонняя контрастная обводка (Outline)
            if (outline)
            {
                var shadowColor = new SharpDX.Color(0, 0, 0, 230);
                Graphics.DrawText(text, new System.Numerics.Vector2(position.X - 1f, position.Y), shadowColor);
                Graphics.DrawText(text, new System.Numerics.Vector2(position.X + 1f, position.Y), shadowColor);
                Graphics.DrawText(text, new System.Numerics.Vector2(position.X, position.Y - 1f), shadowColor);
                Graphics.DrawText(text, new System.Numerics.Vector2(position.X, position.Y + 1f), shadowColor);

                if (thickness > 1.0f)
                {
                    float diag = Math.Min(1.0f, thickness - 0.5f);
                    Graphics.DrawText(text, new System.Numerics.Vector2(position.X + diag, position.Y + diag), shadowColor);
                    Graphics.DrawText(text, new System.Numerics.Vector2(position.X - diag, position.Y + diag), shadowColor);
                }
            }

            // 2. Плавное субпиксельное утолщение (Faux-Bold):
            // Поддерживает любые промежуточные значения (0.2, 0.4, 0.5, 0.7, 1.0, 1.5...)
            if (thickness > 0.05f)
            {
                // Горизонтальный штрих плавно нарастает от 0 до 1px
                float hOffset = Math.Min(thickness, 1.0f);
                Graphics.DrawText(text, new System.Numerics.Vector2(position.X + hOffset, position.Y), color);

                // Если толщина > 0.7, плавно подключаем диагональ и вертикаль
                if (thickness > 0.7f)
                {
                    float extra = Math.Min(1.0f, (thickness - 0.7f) * 1.5f);
                    Graphics.DrawText(text, new System.Numerics.Vector2(position.X, position.Y + extra * 0.7f), color);
                    Graphics.DrawText(text, new System.Numerics.Vector2(position.X + extra * 0.7f, position.Y + extra * 0.7f), color);
                }
            }

            // 3. Основной текст
            Graphics.DrawText(text, position, color);

            return new System.Numerics.Vector2(size.X + (thickness > 0.5f ? 1 : 0), size.Y);
        }

        private bool IsCurrencyItem(Entity itemEntity, out string currencyName, out int stackSize, out bool isValuable)
        {
            currencyName = string.Empty;
            stackSize = 1;
            isValuable = false;

            if (itemEntity == null || !itemEntity.IsValid) return false;

            string path = itemEntity.Path;
            if (string.IsNullOrEmpty(path)) return false;

            var bit = GameController?.Files?.BaseItemTypes?.Translate(path);
            string className = bit?.ClassName;
            string baseName = bit?.BaseName;

            bool isCurrency = false;
            if (!string.IsNullOrEmpty(className) &&
                (className.Equals("StackableCurrency", StringComparison.OrdinalIgnoreCase) ||
                 className.Contains("Currency", StringComparison.OrdinalIgnoreCase)))
            {
                isCurrency = true;
            }
            else if (path.Contains("Metadata/Items/Currency", StringComparison.OrdinalIgnoreCase))
            {
                isCurrency = true;
            }

            if (!isCurrency) return false;

            currencyName = !string.IsNullOrEmpty(baseName) ? baseName : Path.GetFileNameWithoutExtension(path);

            var stack = itemEntity.GetComponent<Stack>();
            if (stack != null && stack.Size > 0)
            {
                stackSize = stack.Size;
            }

            isValuable = IsValuableCurrency(currencyName, path);
            return true;
        }

        private bool IsValuableCurrency(string name, string path)
        {
            if (string.IsNullOrEmpty(name)) return false;

            if (_valuableCurrencyItems.Contains(name))
                return true;

            // Быстрые проверки по ключевым словам для редких/новых валют
            if (name.Contains("Divine", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Exalted", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Mirror", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Chaos Orb", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Hinekora", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Fracturing", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Awakener", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Reliquary Key", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Valdo", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Sacred Orb", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
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
