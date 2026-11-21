# Unique Counter & Filter (ExileApi Plugin)

Автономный All-in-One плагин для [ExileApi](https://github.com/exApiTools/ExileApi-Compiled) (.NET 10), объединяющий в себе **аналитический счётчик уникальных предметов (HUD)** и **визуальную подсветку плашек уников на полу (Ground Highlighter / Frames)**.

> **Полная автономность:** Работает локально без внешнего сервера, Docker или PostgreSQL. Все данные сохраняются в независимом системном каталоге (`%APPDATA%\PoEUniqueTracker\`) и не стираются при переустановке или обновлении ExileApi.

---

## Что умеет плагин

### 1. Подсветка плашек предметов на полу (Ground Highlighter)
* **Распознавание на лету:** Определяет, какой именно уникальный предмет лежит на земле (даже если он неопознанный — по 2D-арту через RePoE).
* **Кастомная рамка вокруг нативной плашки:**
  * Рисует цветную рамку нужной толщины (`HighlightBorderThickness`).
  * Рисует полупрозрачную заливку фона (`HighlightBackgroundColor`).
  * **Цветовая дифференциация тиров:** Подсвечивает T0 уники (красным), а T1 (оранжевым/золотым).
* **Текст названия прямо на плашке:**
  * Поверх нативной плашки лут-фильтра крупно и четко пишется распознанное имя уника (например, `Mageblood` поверх неидентифицированного *Heavy Belt*).
* **Умный фильтр:** Настройка `HighlightOnlyTiers` позволяет подсвечивать только ценные уники из списков T0 и T1, игнорируя мусорные уники.

### 2. Счётчик уников на карте (Map Uniques)
* Считает, сколько всего уникальных предметов выпало на текущей локации.
* **Защита от дублей:** при возвращении в убежище для разгрузки и заходе обратно в тот же портал предметы не считаются заново (ведется кеш инстансов).
* В убежище и городе сохраняет и выводит результат последней завершенной карты:  
  `Map Uniques: 18 (Last: Crimson Temple Map)`.

### 3. Общая статистика (Grand Total)
* Суммирует все найденные уникальные предметы за всё время / сессию.
* Сохраняется в `%APPDATA%\PoEUniqueTracker\stats.json`.

### 4. Списки T0 и T1 уников
* **T0 (18 предметов):** *Bino's Kitchen Knife, Bloodseeker, Defiance of Destiny, Divinarius, Ephemeral Edge, Essentia Sanguis, Headhunter, Jiquani's Potential, Kalandra's Touch, Lioneye's Glare, Mageblood, Marohi Erqi, Rakiata's Dance, Reefbane, Soul Taker, The Squire, Varunastra, Voltaxic Rift*.
* **T1 (88 предметов):** Полный расширенный список (*Aegis Aurora, Dialla's Malefaction, Ralakesh's Impatience, Taste of Hate, The Covenant, Doryani's Prototype, Unnatural Instinct, Inspired Learning, Nimis, Seven-League Step и др.*).
* В HUD отображаются только те уники, которые уже выпадали, с количеством и пометкой вклада текущей карты (например, `Mageblood: 2 (+1 on map)`).

---

## Где хранятся данные?

Файлы лежат вне директории ExileApi:
```
%APPDATA%\PoEUniqueTracker\
(C:\Users\<Пользователь>\AppData\Roaming\PoEUniqueTracker\)
```

* `stats.json` — сохранённая статистика дропов.
* `tiers.json` — настраиваемые списки T0 и T1. Вы можете в любой момент отредактировать этот файл в блокноте, чтобы добавить или убрать предметы.
* `drops_history.jsonl` — журнал истории дропов.
* `repoeUniques.json` — база артов RePoE (скачивается один раз).

---

## Настройки в игре (F12 -> Unique Counter & Filter)

### Рамочки на полу (Ground Highlight):
* `HighlightLabelsOnGround` — включить/выключить отрисовку рамочек вокруг предметов на полу.
* `HighlightOnlyTiers` — подсвечивать только предметы из T0/T1 (рекомендуется).
* `UseTierColorsForFrames` — красить рамку в цвет соответствующего тира (T0 = красный, T1 = оранжевый).
* `HighlightBorderColor` — базовый цвет рамки.
* `HighlightBorderThickness` — толщина рамки (от 1 до 10).
* `HighlightBackgroundColor` — базовый цвет заливки фона плашки.
* `HighlightBackgroundAlpha` — прозрачность/непрозрачность фона (от `0` — прозрачный, до `255` — плотный; по умолчанию `180`).
* `UseTierColorsForBackground` — автоматически заливать фон цветом тира (красный для T0, оранжевый для T1).
* `DrawNameOnLabel` — писать имя уника прямо поверх плашки на полу.
* `LabelNameTextColor` — цвет текста названия на плашке.
* `LabelTextScale` — масштаб шрифта названия на плашке (от 0.4 до 2.5).
* `LabelTextThickness` — толщина / жирность шрифта на плашке (от 0 до 4, по умолчанию 1).
* `LabelTextOutline` — темная контрастная обводка букв на плашке.

### HUD (Счётчики на экране):
* `ShowMapTotal` — отображать счетчик текущей карты (`Map Total`).
* `ShowGrandTotal` — отображать суммарный счетчик (`Grand Total`).
* `ShowT0Uniques` / `ShowT1Uniques` — отображать списки выпавших T0/T1.
* `ShowMapContributionInList` — показывать `(+X on map)` в списках.
* `TextScale` — масштаб шрифта счетчиков (от 0.3 до 3.0).
* `TextThickness` — толщина / жирность шрифта счетчиков (от 0 до 4, по умолчанию 1).
* `TextOutline` — темная контрастная обводка букв для читаемости на любом фоне.
* Цвета: `MapTotalColor`, `MainTextColor`, `T0Color`, `T1Color`.
* Экранные позиции (X, Y) для каждого блока.

### Фильтры и действия:
* `IgnoreHideoutAndTown` — не сканировать и не подсвечивать предметы в убежище и городе.
* `IgnoreIdentifiedItems` — игнорировать предметы, уже опознанные свитком.
* `ResetMapStats` — сбросить счётчик текущей карты.
* `ResetGrandTotal` — обнулить всю накопленную общую статистику.
* `ReloadTiersAndDatabase` — горячая перезагрузка `tiers.json` и базы текстур.
