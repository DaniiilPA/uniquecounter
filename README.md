# Unique Counter (ExileApi Plugin)

Автономный плагин для [ExileApi](https://github.com/exApiTools/ExileApi-Compiled) (.NET 10), предназначенный для подсчета и трекинга уникальных предметов (T0 и T1) на карте и за всё время с выводом кастомного HUD-оверлея поверх Path of Exile.

> **Полная автономность:** Плагин работает локально без внешнего сервера, Docker или сторонних баз данных. Все данные сохраняются в безопасном системном хранилище и не стираются при переустановке или обновлении ExileApi.

---

## Возможности

* **Трекинг уников на карте (Map Uniques):**
  * Считает, сколько всего уникальных предметов выпало на текущей локации.
  * Защита от дублей: при телепорте в убежище и возвращении в тот же портал предметы не считаются повторно (ведется кеширование по инстансам).
  * В убежище/городе отображает статистику последней пройденной карты: `Map Uniques: X (Last: Area Name)`.

* **Общая статистика (Grand Total):**
  * Ведет суммарный подсчет всех выбитых уникальных предметов за всё время.
  * Сохраняется в файл `%APPDATA%\PoEUniqueTracker\stats.json`.

* **Отображение списков T0 и T1:**
  * **T0 (18 предметов):** *Mageblood, Headhunter, The Squire, Kalandra's Touch, Defiance of Destiny, Ephemeral Edge, Soul Taker, Bloodseeker, Divinarius, Bino's Kitchen Knife, Essentia Sanguis, Jiquani's Potential, Lioneye's Glare, Marohi Erqi, Rakiata's Dance, Reefbane, Varunastra, Voltaxic Rift*.
  * **T1 (82 предмета):** Полный расширенный список редких уников (Aegis Aurora, Dialla's Malefaction, Ralakesh's Impatience, Taste of Hate, The Covenant, Doryani's Prototype, Unnatural Instinct, Inspired Learning и др.).
  * На экране выводятся только те уники, которые уже выпадали, с количеством и пометкой вклада текущей карты (например, `Mageblood: 2 (+1 on map)`).

* **Распознавание неопознанных предметов:**
  * Сканирует 2D-арты на полу через базу [RePoE](https://repoe-fork.github.io/poe1/uniques.json), скачиваемую один раз в системную папку.
  * Имеет встроенные резервные соответствия (fallbacks) для ключевых T0 уников.

* **Гибкая кастомизация тиров (`tiers.json`):**
  * При первом старте в `%APPDATA%\PoEUniqueTracker\tiers.json` создаются списки T0 и T1. Вы можете в любой момент открыть этот файл в блокноте и добавить/удалить любые предметы без перекомпиляции плагина.

---

## Где хранятся данные?

Все файлы плагина хранятся вне директории ExileApi, по пути:
```
%APPDATA%\PoEUniqueTracker\
(обычно: C:\Users\<Имя_Пользователя>\AppData\Roaming\PoEUniqueTracker\)
```

* `stats.json` — накопительная статистика (Grand Total, счетчики T0 и T1).
* `tiers.json` — списки отслеживаемых предметов T0 и T1.
* `drops_history.jsonl` — построчный JSON-журнал каждого выпавшего уника с датой, названием карты, базой и тиром предмета.
* `repoeUniques.json` — локальный кеш базы текстур RePoE.

---

## Установка и использование

1. Скопируйте папку с плагином в `Plugins/Source/UniqueCounter` вашего ExileApi (или скомпилируйте через `dotnet build`).
2. Запустите ExileApi — плагин появится в меню настроек (`F12` -> `Unique Counter`).
3. В настройках доступны:
   * Включение/отключение отображения блоков HUD (`ShowMapTotal`, `ShowGrandTotal`, `ShowT0Uniques`, `ShowT1Uniques`).
   * Настройка цветов (желтый для карты, белый для текста, красный для T0, оранжевый для T1) и масштаба шрифта (`TextScale`).
   * Настройка точных экранных координат (X, Y) для каждого блока.
   * Игнорирование дропа в убежище и городе (`IgnoreHideoutAndTown`).
   * Кнопки сброса: `ResetMapStats` (сбросить карту) и `ResetGrandTotal` (сбросить всю статистику).
   * Кнопка `ReloadTiersAndDatabase` (перечитать `tiers.json` и обновить базу текстур).
