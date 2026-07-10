using System;
using System.Collections.Concurrent;
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
        // Эндпоинт для отправки POST запросов
        public TextNode ServerEndpoint { get; set; } = new TextNode("http://127.0.0.1:8000/uniques");
        // Поле для токена авторизации
        public TextNode ApiKey { get; set; } = new TextNode("YOUR_API_KEY_HERE");
    }

    public class UniqueLogger : BaseSettingsPlugin<UniqueLoggerSettings>
    {
        // Потокобезопасный словарь для сопоставления текстур
        private ConcurrentDictionary<string, string> _artToUniqueMapping = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // Накопительное хранилище уникальных предметов на текущей локации (ID сущности -> (База, Название))
        // Очищается только при смене локации в методе AreaChange
        private readonly ConcurrentDictionary<uint, (string BaseName, string UniqueName)> _trackedUniques = new ConcurrentDictionary<uint, (string, string)>();

        // Время следующей запланированной отправки данных
        private DateTime _nextSendTime = DateTime.UtcNow;

        private static readonly HttpClient HttpClientInstance = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        private const string RePoEUrl = "https://raw.githubusercontent.com/vvto/RePoE/master/RePoE/data/uniques.json";
        private const string RePoEFileName = "repoeUniques.json";

        public override bool Initialise()
        {
            LoadHardcodedFallbacks();
            Task.Run(async () => await LoadMappingDatabaseAsync());
            return true;
        }

        // Сброс накопленных данных при переходе на новую локацию
        public override void AreaChange(AreaInstance area)
        {
            _trackedUniques.Clear();
            _nextSendTime = DateTime.UtcNow.AddSeconds(5); // Сдвигаем первую отправку на 5 секунд после захода на локацию
            base.AreaChange(area);
        }

        private void LoadHardcodedFallbacks()
        {
            _artToUniqueMapping["art/2ditems/belts/heavybeltunique"] = "Mageblood";
            _artToUniqueMapping["art/2ditems/belts/headhunter"] = "Headhunter";
            _artToUniqueMapping["art/2ditems/shields/thesquire"] = "The Squire";
            _artToUniqueMapping["art/2ditems/rings/kalandras_touch"] = "Kalandra's Touch";
        }

        private async Task LoadMappingDatabaseAsync()
        {
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
            }

            var repoePath = Path.Combine(ConfigDirectory, RePoEFileName);

            if (!File.Exists(repoePath))
            {
                try
                {
                    LogMessage("[UniqueLogger] Скачивание базы уникальных предметов (RePoE)...", 5);
                    var json = await HttpClientInstance.GetStringAsync(RePoEUrl);
                    await File.WriteAllTextAsync(repoePath, json);
                    LogMessage("[UniqueLogger] База RePoE успешно сохранена!", 5);
                }
                catch (Exception ex)
                {
                    LogError($"[UniqueLogger] Не удалось скачать базу RePoE (работают локальные фолбеки): {ex.Message}", 10);
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
                        var tempMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var kvp in repoeData)
                        {
                            var itemData = kvp.Value;
                            if (itemData == null) continue;

                            string uniqueName = itemData["name"]?.ToString() ?? itemData["id"]?.ToString();
                            if (string.IsNullOrEmpty(uniqueName)) continue;

                            var visualIdentity = itemData["visual_identity"] as JObject;
                            if (visualIdentity != null)
                            {
                                string ddsFile = visualIdentity["dds_file"]?.ToString();
                                if (!string.IsNullOrEmpty(ddsFile))
                                {
                                    var cleanArt = CleanPathString(ddsFile);
                                    if (!string.IsNullOrEmpty(cleanArt))
                                    {
                                        bool isReplica = uniqueName.StartsWith("Replica ", StringComparison.OrdinalIgnoreCase) || 
                                                         uniqueName.StartsWith("Копия ", StringComparison.OrdinalIgnoreCase);

                                        if (!tempMapping.TryGetValue(cleanArt, out var existingName) || 
                                            (!isReplica && existingName.StartsWith("Replica ", StringComparison.OrdinalIgnoreCase)))
                                        {
                                            tempMapping[cleanArt] = uniqueName;

                                            if (!cleanArt.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                                            {
                                                tempMapping[cleanArt + ".dds"] = uniqueName;
                                            }
                                        }
                                    }
                                }

                                string visualId = visualIdentity["id"]?.ToString();
                                if (!string.IsNullOrEmpty(visualId))
                                {
                                    var cleanVisualId = CleanString(visualId);
                                    bool isReplica = uniqueName.StartsWith("Replica ", StringComparison.OrdinalIgnoreCase) || 
                                                     uniqueName.StartsWith("Копия ", StringComparison.OrdinalIgnoreCase);

                                    if (!tempMapping.TryGetValue(cleanVisualId, out var existingName) || 
                                        (!isReplica && existingName.StartsWith("Replica ", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        tempMapping[cleanVisualId] = uniqueName;
                                    }
                                }
                            }
                        }

                        _artToUniqueMapping = new ConcurrentDictionary<string, string>(tempMapping, StringComparer.OrdinalIgnoreCase);
                        LogMessage($"[UniqueLogger] Успешно загружено {_artToUniqueMapping.Count} соответствий из RePoE.", 5);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[UniqueLogger] Ошибка чтения/парсинга базы RePoE: {ex.Message}", 10);
                }
            }
        }

        public override void Render()
        {
            if (!Settings.Enable) return;

            var currentArea = GameController?.Area?.CurrentArea;
            string areaName = currentArea?.Name ?? "Unknown Area";
            uint areaHash = currentArea?.Hash ?? 0;

            var entities = GameController?.EntityListWrapper?.Entities;
            if (entities != null)
            {
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

                        if (mods.Identified) continue;

                        var baseItemType = itemEntity.GetComponent<Base>()?.Name ?? "Unknown Base";
                        string uniqueName = null;

                        // 1. Опознанный уник
                        if (!string.IsNullOrEmpty(mods.UniqueName))
                        {
                            uniqueName = CleanString(mods.UniqueName);
                        }

                        // 2. Неопознанный уник
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
                                        var fileName = Path.GetFileNameWithoutExtension(rawPath);
                                        if (!string.IsNullOrEmpty(fileName) && _artToUniqueMapping.TryGetValue(fileName, out var fallbackName))
                                        {
                                            uniqueName = fallbackName;
                                        }
                                        else if (!string.IsNullOrEmpty(fileName))
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

                        // Добавляем найденный уникальный предмет в накопительную коллекцию (дубликаты отсекаются по ID на полу)
                        _trackedUniques.TryAdd(entity.Id, (baseItemType, uniqueName));
                    }
                    catch (Exception)
                    {
                        // Игнорируем точечные ошибки чтения памяти во время кадра
                    }
                }
            }

            // Отрисовка накопленного списка на экране
            var windowRect = GameController?.Window?.GetWindowRectangleTimeCache;
            if (windowRect != null)
            {
                var drawPos = new SharpDX.Vector2(windowRect.Value.Width * 0.15f, windowRect.Value.Height * 0.15f);
                var yOffset = 0f;

                Graphics.DrawText($"Area: {areaName} | Area ID: {areaHash}", drawPos, Color.White);
                yOffset += 22f;

                if (_trackedUniques.Count > 0)
                {
                    // Отображаем накопительный список уникальных предметов для текущей сессии локации
                    Graphics.DrawText($"--- Накоплено уников в зоне: {_trackedUniques.Count} ---", drawPos + new SharpDX.Vector2(0, yOffset), Color.Yellow);
                    yOffset += 20f;

                    foreach (var kvp in _trackedUniques)
                    {
                        string itemText = $"{kvp.Value.BaseName} ({kvp.Value.UniqueName}) [Ground ID: {kvp.Key}]";
                        Graphics.DrawText(itemText, drawPos + new SharpDX.Vector2(0, yOffset), Color.White);
                        yOffset += 20f; 
                    }
                }
                else
                {
                    Graphics.DrawText("В текущей зоне уникальные предметы еще не обнаружены", drawPos + new SharpDX.Vector2(0, yOffset), Color.Gray);
                }
            }

            // Логика периодической отправки данных раз в 5 секунд (неблокирующий вызов)
            if (DateTime.UtcNow >= _nextSendTime)
            {
                _nextSendTime = DateTime.UtcNow.AddSeconds(5);
                
                // Вызываем асинхронную отправку в фоновом режиме
                Task.Run(async () => await SendDataToServerAsync(areaHash, areaName));
            }
        }

        private async Task SendDataToServerAsync(uint areaHash, string areaName)
        {
            // Если в коллекции ничего нет, сетевой запрос не отправляется
            if (_trackedUniques.IsEmpty) return;

            // Формируем снимок данных на момент отправки во избежание Race Condition
            var uniquesSnapshot = _trackedUniques.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => new List<string> { kvp.Value.BaseName, kvp.Value.UniqueName }
            );

            var payload = new
            {
                instance_id = areaHash,
                area_name = areaName,
                uniques = uniquesSnapshot
            };

            try
            {
                var json = JsonConvert.SerializeObject(payload);
                var endpoint = Settings.ServerEndpoint?.Value;
                if (string.IsNullOrEmpty(endpoint)) return;

                using (var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"))
                using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                {
                    request.Content = content;

                    // Если токен указан в настройках, добавляем его в заголовок X-API-Key
                    var apiKey = Settings.ApiKey?.Value;
                    if (!string.IsNullOrEmpty(apiKey) && apiKey != "YOUR_API_KEY_HERE")
                    {
                        request.Headers.Add("X-API-Key", apiKey);
                    }

                    var response = await HttpClientInstance.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        LogError($"[UniqueLogger] Ошибка отправки на сервер: {response.StatusCode}", 3);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"[UniqueLogger] Ошибка сети при отправке: {ex.Message}", 3);
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