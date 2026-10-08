# Typecraft

[Русский](#русский) · [English](#english)

---

## Русский

Инструмент для текста поверх TextMeshPro: короткая разметка (`{key:Interact}`, `{candle}`,
`[warn]…[/warn]`), библиотеки стилей и иконок, подсказки ввода по текущему устройству,
роли шрифтов по языкам, встроенные таблицы строк, интеграция с TMPEffects и Unity Localization.

### Установка

Пакет ставится как embedded (папка в `Packages/`) или по git-URL. Необязательные зависимости
добавляются в `Packages/manifest.json` проекта — UPM не разрешает git-зависимости у пакетов:

```json
"com.luca3317.tmpeffects": "https://github.com/Luca3317/TMPEffects.git?path=/Package#0.3.2",
"com.unity.localization": "1.5.13",
"com.unity.inputsystem": "1.18.0"
```

| Пакет | Что даёт | Без него |
| --- | --- | --- |
| TMPEffects | Анимации `<wave>`, машинка, команды `<!wait>` | Теги эффектов не работают |
| Input System | Глифы `{key:Action}` по биндингам | Глифы ввода не подставляются |
| Unity Localization | Адаптер `UnityLocalizationTextSource` | Работают встроенные таблицы |

Если репозиторий TMPEffects недоступен, в `Vendor~/com.luca3317.tmpeffects-0.3.2.tgz` лежит
снимок той же версии (MIT): `"com.luca3317.tmpeffects": "file:com.velsi.typecraft/Vendor~/com.luca3317.tmpeffects-0.3.2.tgz"`.

### Быстрый старт

1. `Tools/Typecraft/Create Project Settings` — создаёт `TypecraftSettings` в Resources,
   библиотеку стилей, профиль шрифтов, профиль глифов и таблицу строк.
2. На объект с `TMP_Text` добавь `Typecraft Text`: режим **Inline** (разметка прямо в
   компоненте) или **Localized** (таблица + ключ).
3. `Window → Typecraft` (Ctrl+Alt+T) — окно редактора.

### Разметка

| Запись | Что делает |
| --- | --- |
| `{key:Interact}` | Глиф кнопки действия для текущего устройства |
| `{candle}`, `{set/candle}` | Иконка из набора |
| `[warn]…[/warn]` | Стиль из библиотеки стилей |
| `{player:name}` | Свой тег (см. «Расширение») |
| `{0}`, `{coin}` | Аргументы `TypecraftText.SetArguments` / Unity Localization |
| `<wave>…</wave>`, `<!wait=0.5>` | Теги TMP и TMPEffects — проходят как есть |
| `\{`, `\[` | Экранирование |

### Слой разметки

Таблица строк хранит текст для переводчика, слой разметки — оформление поверх него.

| Где | Что лежит |
| --- | --- |
| Таблица (`StringTable` или Unity Localization) | Слова и содержательные токены: `{key:Interact}`, `{candle}`, `{0}`, `{player:name}` — порядок слов в языках разный, переводчик ставит их сам. Экранирование `\[`, `\<` — тоже текст |
| Слой (`MarkupLayer`, один на таблицу) | Оформление: стили `[warn]…[/warn]` и теги TMP/TMPEffects `<…>`, включая `<!wait=…>`. Запись = ключ + язык + полный текст с оформлением + снимок строки таблицы, на которую оформление ставилось |

- Слои перечислены в `TypecraftSettings → Markup Layers` и создаются сами при первой разметке
  рядом с таблицей. Таблица находится по id (у Unity Localization — по имени коллекции) и коду языка.
- В игре: если снимок совпадает с текущей строкой таблицы — показывается текст из слоя, иначе
  чистая строка таблицы. Так работает и встроенный источник, и Unity Localization; псевдолокаль
  `pseudo` строится из оформленного текста основного языка.
- **Строку в таблице поменяли** (переводчик, импорт CSV) — оформление перестаёт применяться,
  ячейка во вкладке «Локализация» помечена ⚠. «Перенести теги» переставляет оформление на новый
  текст по совпадающим словам (что не удалось привязать точно — покажет и спросит), «Сбросить
  разметку» удаляет запись слоя.
- Вкладка «Редактор» показывает текст из слоя. «Применить» без изменения слов пишет только в
  слой; если слова изменились — окно «было / стало» с выбором «Записать в таблицу» или «Вернуть
  слова». Оно же появляется при уходе с объекта и при закрытии окна с незаписанными словами.
- Старые таблицы с оформлением прямо в ячейках: `Tools/Typecraft/Move Markup To Layers`.
- Переименование стиля во вкладке «Библиотека» переписывает слои; иконки `{…}` живут в таблице и
  переписываются там (и в снимках слоя, чтобы оформление не устарело).

### Синхронизация таблиц

Строки можно вести во внешней таблице и подтягивать в редакторе (в рантайме синхронизации нет).
Вкладка «Локализация» → **Источник…** — настроить источник для выбранной таблицы, **Синхронизировать** —
загрузить одним кликом. **Импорт…** — разовая загрузка файла без сохранения источника.

| Источник | Что указать |
| --- | --- |
| CSV / TSV | Путь к файлу (UTF-8; кавычки и переносы строк внутри ячеек поддерживаются) |
| Excel `.xlsx` | Путь к файлу и лист (по умолчанию первый). Читается без сторонних библиотек; старый `.xls` — нет |
| Google Sheets | Обычная ссылка `https://docs.google.com/spreadsheets/d/<SHEET_ID>/edit#gid=<GID>` — лист берётся по `gid` |

- Формат: первая строка — заголовки. `key` — обязательная колонка, `comment` — необязательная,
  остальные — коды языков (`en`, `ru`, `zh-Hans`…). Колонки, не похожие на код языка (`notes`),
  пропускаются. Языков, которых нет в проекте, предложат добавить.
- Google Sheets: нужен доступ «Все, у кого есть ссылка — читатель» (или «Опубликовать в интернете»).
  Без ключей и OAuth; закрытая таблица даёт ошибку «Таблица закрыта» — тогда открой доступ или
  выгрузи `.xlsx`. Загрузка идёт в фоне, её можно отменить.
- Источник хранится в `ProjectSettings/TypecraftTableSync.asset` (только редактор, в билд не
  попадает). Пути внутри проекта пишутся относительными. Ссылка на таблицу попадает в репозиторий —
  по ней таблицу может прочитать любой, у кого есть ссылка.
- Перед применением — отчёт: новые ключи, изменённые строки по языкам («было / стало»), комментарии,
  ключи, которых нет в источнике. Отсутствующие ключи по умолчанию **не удаляются** (флажок
  «Удалить ключи…»); пустая ячейка в источнике по умолчанию **не стирает** перевод (флажок «Стирать
  строки…»). «Применить» — одна операция Undo.
- **Разметка:** если строка изменилась, а у неё есть оформление в слое, оно перестаёт применяться
  (снимок не совпадает) — ячейка получит ⚠. Отчёт заранее показывает «Разметка перестанет
  применяться: N строк»; после синхронизации — фильтр «Только изменившиеся» и «Перенести теги».
  При удалении ключа удаляется и его запись в слое.

### Экспорт и поиск использований

- **Экспорт…** во вкладке «Локализация» — формат (Excel `.xlsx`, CSV, TSV, JSON) и охват (текущая
  таблица или все). Колонки `key`, `comment`, языки (основной первым) — файл читается обратно
  синхронизацией. Все таблицы: `.xlsx` — одна книга, лист на таблицу (имя листа — id таблицы,
  с поправкой на ограничения Excel); CSV/TSV — папка, файл на таблицу; JSON — один файл
  `{ "tables": [ { "id", "locales", "entries": [ { "key", "comment", "values" } ] } ] }` (UTF-8 без BOM).
  В `.xlsx` шапка закреплена, пустой перевод — красная заливка, строка с ⚠ — оранжевая.
  Оформление из слоя разметки в файл не попадает, только текст таблицы.
- **Двойной клик по ключу** — где он используется: `TypecraftText` в режиме Localized (с этой
  таблицей или без таблицы, если ключ находится в ней первым) в открытых и закрытых сценах и
  префабах `Assets/`, плюс строка `"key"` в `.cs` из `Assets/`. Одно место — сразу переход (объект
  выделяется и наводится камера; закрытая сцена открывается после вопроса о сохранении, префаб —
  в Prefab Mode, скрипт — на нужной строке), несколько — список. Переименование ключа — F2 или
  контекстное меню ячейки.

### Окно Typecraft

- **Редактор** — разметка с подсветкой и автодополнением, живое превью с машинкой,
  галерея стилей, иконок и эффектов, выделение мышью прямо в превью, правки в Play Mode
  переживают выход из него.
- **Локализация** — сетка «ключ × язык» со строками таблицы, флаги «нет перевода», «не помещается»,
  «нет символа», «разметка расходится», ✦ «есть разметка в слое» и ⚠ «строка изменилась, разметка
  не применяется» (фильтр «Только изменившиеся»), синхронизация с CSV/TSV, Excel и Google Sheets,
  экспорт в .xlsx/CSV/TSV/JSON, поиск использований ключа, псевдолокаль `pseudo`.
- **Библиотека** — наборы иконок (перетаскивание PNG), редактор стилей, «стиль из TMP_Text»,
  пресеты материалов и свои эффекты.
  Переименование стиля или иконки переписывает теги в таблицах и сценах.
- **Шрифты** — роли × языки, покрытие символов, сборщик статического шрифта по набору
  символов из строк (для CJK).
- **Диалоги** — граф или список нод, инспектор ноды, проигрывание и проверка; см. «Диалоги».

### Свои стили, материалы и эффекты

Создаются прямо в окне — в галерее вкладки «Редактор» (карточки «+ стиль», «+ материал», «+ эффект»)
или кнопками на панели вкладки «Библиотека». Форма открывается поверх окна, превью живое, «Сохранить»
пишет в ассеты (Undo там, где Unity его поддерживает).

| Что | Как работает | Где хранится |
| --- | --- | --- |
| **Стиль** | id, цвет/градиент, b/i/u, регистр, интервал, роль шрифта, пресет материала, эффект. Превью — на выделенном тексте или на образце; после сохранения выделение оборачивается в `[id]…[/id]`. **«Стиль из выделения»** (плавающая панель, меню «Стиль ▾») берёт теги вокруг выделения — `<color>`, `<b>`, `<i>`, `<u>`, `<cspace=…em>`, `<size=…%>`, `<font … material=…>`, `<material>`, эффект — в форму и заменяет их на `[id]…[/id]` | `StyleLibrary` |
| **Материал** | Пресет SDF-материала для роли шрифта: Face Dilate, Softness, обводка, подложка (`UNDERLAY_ON`), свечение (`GLOW_ON`). Создаётся копия материала **для каждого шрифта роли** (у каждого языка свой атлас) с одним id пресета, так что стиль работает на всех языках. Свечение есть только в полном шейдере — пресет со свечением переводится на `TextMeshPro/Distance Field` | `Materials/` рядом с `FontProfile`, запись в `FontProfile` |
| **Эффект** | Тип (постоянный `<x>`, появление `<+x>`, исчезание `<-x>`), основа — копия встроенной анимации TMPEffects, `Generic` (дорожки) или `Stack` (несколько анимаций), параметры — инспектор ассета внутри формы. Имя: строчные латинские буквы, цифры, `_`; не может совпадать со встроенным эффектом или тегом TMP | `Effects/` рядом со `StyleLibrary` |

- **База анимаций проекта.** Первый свой эффект создаёт `Effects/TypecraftAnimationDatabase.asset`: в неё
  ссылками входят все встроенные анимации TMPEffects (сами ассеты TMPEffects не меняются) плюс свои. Ссылка
  на неё — `TypecraftSettings → Animation Database`. Её используют превью окна и `TMPAnimator`, которые
  ставит Typecraft (при «Применить»; аниматор на базе TMPEffects по умолчанию переводится на базу проекта).
  Глобальные настройки TMPEffects не меняются — `TMPAnimator`, добавленный вручную, по умолчанию берёт
  базу TMPEffects; для своих эффектов выбери в нём базу проекта.
- Свои эффекты сразу появляются в автодополнении, галерее (подпись «свой»), меню «Эффект ▾» и в
  выпадающем списке эффектов стиля.
- **Удаление:** правый клик по карточке материала/эффекта в галерее или кнопка «Удалить» во вкладке
  «Библиотека». Материалы и ассеты эффектов уходят в корзину ОС; теги в тексте остаются.
- После «Пересобрать» статического шрифта атлас в материалах пресетов обновляется сам.

### Встраивание в свои инструменты

- `[TypecraftMarkup]` на string-поле — поле с подсветкой, превью и кнопкой ✎ в инспекторе.
- Правый клик по любому string-полю → **Edit in Typecraft**; у `TMP_Text` — пункт в меню компонента.
- `TypecraftEditor.Edit(text, onApply)` — открыть окно и получить результат в колбэк.
- `MarkupTextField` — UI Toolkit-элемент для своих редакторов.

Подробно и с примерами: [`Documentation~/EXTENDING.md`](Documentation~/EXTENDING.md).

### Диалоги

Модуль диалогов — отдельные сборки поверх ядра: `Velsi.Typecraft.Dialogue` (рантайм),
`Velsi.Typecraft.Dialogue.InputSystem` (только при Input System), `Velsi.Typecraft.Dialogue.Editor`
(вкладка «Диалоги») и `Velsi.Typecraft.Dialogue.Editor.Graph` (граф). Тексты реплик — ключи
таблиц Typecraft, поэтому перевод, слой разметки, шрифты и проверки работают как для любого текста.

**Диалог и ноды.** `DialogueAsset` (`Create → Typecraft → Dialogue`) — набор нод со ссылками
друг на друга и таблица по умолчанию. Стабильный `Id` ассета нужен для сохранений; у копии
ассета Id перевыдаётся сам.

| Нода | Что делает |
| --- | --- |
| Start | Точка входа. Их может быть несколько, основная — «Сделать основной» в инспекторе |
| Line | Реплика: говорящий, эмоция, ключ. Дальше — по вводу, через N секунд или после печати + N секунд. **Варианты** — другой ключ и эмоция по условию (первый подходящий) |
| Choice | Варианты ответа. У каждого условие и поведение, если оно не выполнено: **Hide** (скрыть) или **Disable** (показать неактивным) |
| Branch | Развилка: первый случай с выполненным условием или Else |
| Set Variables | Set / Add / Toggle флагов |
| Event | Событие `имя` + `аргумент` в игру |
| Wait | Пауза |
| Jump | Переход в другой диалог (или к ноде этого) |
| End | Конец |

**Говорящие и портреты.** `SpeakerProfile` (`Create → Typecraft → Dialogue Speaker`): ключ имени
(своя таблица или таблица реплики), стиль имени, портреты по эмоциям и портрет по умолчанию,
сторона (`Left`/`Right` — левый или правый портрет окна), звук «бормотания» под машинку.

**Флаги и условия.** Каталог `DialogueVariableCatalog` (`Create → Typecraft → Dialogue Variables`)
объявляет флаги: имя, тип (bool/int/float/string), значение по умолчанию, описание. Условие —
список сравнений (`IsTrue`, `IsFalse`, `=`, `≠`, `>`, `≥`, `<`, `≤`) через «все» или «любое».
Незаданный флаг берёт значение из каталога. Посещения нод считаются сами — флаги
`visited:<id диалога>/<id ноды>` (видны в проигрывании). Переименование флага во вкладке
переписывает его во всех диалогах.

**Выводы.** Ставятся из `GameObject → Typecraft`:

| Вывод | Компонент | Что умеет |
| --- | --- | --- |
| Dialogue Box | `DialogueBoxPresenter` | Имя, портреты слева и справа, машинка (TMPEffects), индикатор «далее», кнопки выбора, звук |
| Subtitles | `SubtitlePresenter` | Строка `{name}: реплика`, скрывается через N секунд |
| World Text | `WorldTextPresenter` | Текст в мире (`TextMeshPro`), разворот к камере |

Свой вывод — наследник `DialoguePresenter` (см. `Documentation~/EXTENDING.md`).

**Запуск.** `DialoguePlayer` — диалог, вывод, отдельный вывод для выбора (если основной не
умеет), стартовая нода, `Play On Start`, `Once` (не играть повторно — помнится во флагах),
время (scaled/unscaled), источник флагов (пусто — `DialogueGlobals`), события
`OnStarted`/`OnEnded`/`OnEvent(name, argument)`. Из кода — `Play()`, `Advance()`, `Choose(i)`,
`Stop()`. `DialogueTrigger` запускает плеер вручную (`Fire()`), на старте или по
`OnTriggerEnter` с тегом. `DialogueInputAdvance` (сборка Input System) вешает «далее» на
`InputActionReference`.

**Вкладка «Диалоги»** окна Typecraft:

- слева — диалоги, говорящие и флаги («+» у раздела создаёт новый; новый флаг заводит каталог,
  если его нет);
- в центре — доска: **граф** (порты, перетаскивание связей, пробел или правый клик — новая нода,
  мини-карта) или **список** нод; переключатель вида и язык текстов — на панели;
- справа — инспектор выделенной ноды: говорящий и эмоция из списков, ключ с поиском и превью
  текста, **«Создать ключ»** — строка в таблице прямо из инспектора (текст на основном языке,
  оформление `[стиль]` и `<тег>` уходит в слой разметки), условия с выбором флага;
- внизу — **«▶ Проигрывание»** без запуска игры: с начала или с выделенной ноды, печать, выбор,
  флаги проигрывания (правятся на лету, «Сбросить»), лог; текущая нода подсвечивается на доске;
- **«Проверить»** — проблемы диалога (нет старта, пустые и битые связи, недостижимые ноды,
  пустой или отсутствующий ключ, нет перевода, устаревшая разметка, нет символов в шрифте,
  необъявленный флаг или не тот тип, неизвестная эмоция…), клик — к ноде; «Проверить все» —
  по всему проекту.

**Сохранение.** Флаги: `DialogueGlobals.Variables` по умолчанию — `DialogueVariables` с
`ToJson()` / `FromJson(json)` (посещения входят в снимок). Место в разговоре:
`player.Runner.State` → `DialogueRunnerState { AssetId, NodeId }` (сериализуется `JsonUtility`).
Вернуть разговор — `player.Restore(сохранённый player.State, id => DialogueAsset)`;
у голого `DialogueRunner` есть такой же `Restore`. Подробно — в `EXTENDING.md`.

**Без GraphView.** Граф — единственный код, который использует `UnityEditor.Experimental.GraphView`,
и он вынесен в сборку `Velsi.Typecraft.Dialogue.Editor.Graph` (папка `Editor.Dialogue.Graph`).
Остальная вкладка от неё не зависит: виды доски находятся через `IDialogueBoardView`, и если
сборки графа нет (удалена из embedded-копии пакета или не компилируется в новой версии Unity),
вкладка открывается в виде «Список» с теми же инспектором, проигрыванием и проверкой.
Отключить граф без удаления файлов — define `TYPECRAFT_NO_DIALOGUE_GRAPH` в Player Settings →
Scripting Define Symbols.

### Сэмплы

Package Manager → Typecraft → Samples:

- **Simple Dialogue** — учебный диалог: таблица на трёх языках, стили, иконки, подсказка
  ввода, TMPEffects, свой тег, переключение языка и устройства.
- **Branching Dialogue** — модуль диалогов: окно с портретами, выбор с Disable/Hide, ветка
  от флага записки, текст в мире, субтитры, событие из диалога, сброс флагов.
- **Extra Input Prompts** — редкие контроллеры и схемы геймпадов.

### Арт

- `Art~/Prompts/` — исходники базовых подсказок ввода (CC0, Nicolae "Xelu" Berbece).
- `Art/Prompts/` — собранные атласы, `TMP_SpriteAsset` и `IconSet`.
- Правила для своих картинок: `Documentation~/ART_RULES.md`.

---

## English

A text toolkit on top of TextMeshPro: short markup (`{key:Interact}`, `{candle}`,
`[warn]…[/warn]`), style and icon libraries, input prompts that follow the current device,
font roles per language, built-in string tables, TMPEffects and Unity Localization integration.

### Installation

Install as an embedded package (a folder in `Packages/`) or via git URL. Optional
dependencies go into the project's `Packages/manifest.json` — UPM does not allow git
dependencies in a package:

```json
"com.luca3317.tmpeffects": "https://github.com/Luca3317/TMPEffects.git?path=/Package#0.3.2",
"com.unity.localization": "1.5.13",
"com.unity.inputsystem": "1.18.0"
```

| Package | Adds | Without it |
| --- | --- | --- |
| TMPEffects | `<wave>` animations, typewriter, `<!wait>` commands | Effect tags do nothing |
| Input System | `{key:Action}` glyphs from bindings | No input glyphs |
| Unity Localization | `UnityLocalizationTextSource` adapter | Built-in tables are used |

If the TMPEffects repository is unavailable, `Vendor~/com.luca3317.tmpeffects-0.3.2.tgz` holds a
snapshot of the same version (MIT): `"com.luca3317.tmpeffects": "file:com.velsi.typecraft/Vendor~/com.luca3317.tmpeffects-0.3.2.tgz"`.

### Quick start

1. `Tools/Typecraft/Create Project Settings` creates `TypecraftSettings` in Resources, a style
   library, a font profile, a glyph profile and a string table.
2. Add `Typecraft Text` to an object with `TMP_Text`: **Inline** mode (markup in the component)
   or **Localized** (table + key).
3. `Window → Typecraft` (Ctrl+Alt+T) opens the editor window.

### Markup

| Markup | Effect |
| --- | --- |
| `{key:Interact}` | Button glyph of an action for the current device |
| `{candle}`, `{set/candle}` | Icon from an icon set |
| `[warn]…[/warn]` | Style from the style library |
| `{player:name}` | Custom tag (see "Extending") |
| `{0}`, `{coin}` | Arguments from `TypecraftText.SetArguments` / Unity Localization |
| `<wave>…</wave>`, `<!wait=0.5>` | TMP and TMPEffects tags, passed through as is |
| `\{`, `\[` | Escaping |

### Markup layer

A string table keeps the text for translators; a markup layer keeps the decoration on top of it.

| Where | What it holds |
| --- | --- |
| Table (`StringTable` or Unity Localization) | Words and content tokens: `{key:Interact}`, `{candle}`, `{0}`, `{player:name}` — word order differs between languages, so the translator places them. Escapes `\[`, `\<` are text too |
| Layer (`MarkupLayer`, one per table) | Decoration: styles `[warn]…[/warn]` and TMP/TMPEffects tags `<…>`, including `<!wait=…>`. An entry = key + locale + the full decorated text + a snapshot of the table string it was made for |

- Layers are listed in `TypecraftSettings → Markup Layers` and are created next to the table on
  the first decoration. A table is matched by its id (the collection name for Unity Localization)
  and the locale code.
- At runtime: when the snapshot equals the current table string, the layer text is shown;
  otherwise the plain table string is. This works the same for the built-in tables and Unity
  Localization; the `pseudo` locale is built from the decorated base-language text.
- **The table string was changed** (by a translator, a CSV import) — the decoration stops applying
  and the cell is marked ⚠ in the Localization tab. «Перенести теги» (transfer tags) moves the
  decoration onto the new text by matching words (tags that can't be placed exactly are listed
  and need confirmation); «Сбросить разметку» (reset) deletes the layer entry.
- The Editor tab shows the layer text. Applying without word changes writes only to the layer;
  when words changed, a "before / after" dialog offers «Записать в таблицу» (write to the table) or
  «Вернуть слова» (restore words). It also appears when you leave the object or close the window
  with unwritten word changes.
- Old tables with decoration inside cells: `Tools/Typecraft/Move Markup To Layers`.
- Renaming a style in the Library tab rewrites the layers; `{…}` icons live in the table and are
  rewritten there (and in the layer snapshots, so the decoration stays current).

### Table sync

Strings can live in an external spreadsheet and be pulled in the editor (there is no runtime sync).
Localization tab → **Источник…** (source) configures the source of the selected table,
**Синхронизировать** (sync) pulls it in one click. **Импорт…** (import) loads a file once without
saving it as the source.

| Source | What to set |
| --- | --- |
| CSV / TSV | File path (UTF-8; quotes and line breaks inside cells are supported) |
| Excel `.xlsx` | File path and sheet (first by default). Read without third-party libraries; legacy `.xls` is not |
| Google Sheets | A regular link `https://docs.google.com/spreadsheets/d/<SHEET_ID>/edit#gid=<GID>` — the sheet comes from `gid` |

- Format: the first row is the header. `key` is required, `comment` is optional, the rest are
  language codes (`en`, `ru`, `zh-Hans`…). Columns that don't look like a language code (`notes`)
  are skipped. Languages missing from the project are offered for adding.
- Google Sheets: the sheet must be shared as "Anyone with the link — Viewer" (or published to the
  web). No keys or OAuth; a private sheet gives the "Таблица закрыта" (sheet is private) error — share
  it or export `.xlsx`. The download runs in the background and can be cancelled.
- The source is stored in `ProjectSettings/TypecraftTableSync.asset` (editor only, not in builds).
  Paths inside the project are stored relative. The sheet link ends up in the repository — anyone
  with the link can read the sheet.
- Before applying you get a report: new keys, changed strings per language (before / after),
  comments, keys missing from the source. Missing keys are **not removed** by default (the «Удалить
  ключи…» checkbox); an empty source cell does **not clear** a translation by default (the «Стирать
  строки…» checkbox). Applying is a single Undo step.
- **Markup:** when a string with decoration in the layer changes, the decoration stops applying
  (the snapshot no longer matches) and the cell gets ⚠. The report says up front how many strings
  lose their markup; afterwards use the «Только изменившиеся» filter and «Перенести теги».
  Removing a key also removes its layer entry.

### Export and key usages

- **Экспорт…** (export) in the Localization tab — format (Excel `.xlsx`, CSV, TSV, JSON) and scope
  (current table or all tables). Columns are `key`, `comment` and languages (base first), so the file
  reads back through sync. All tables: `.xlsx` is one workbook with a sheet per table (sheet name =
  table id, adjusted to Excel limits); CSV/TSV is a folder with a file per table; JSON is one file
  `{ "tables": [ { "id", "locales", "entries": [ { "key", "comment", "values" } ] } ] }` (UTF-8, no BOM).
  In `.xlsx` the header row is frozen, missing translations are red, rows with ⚠ are orange.
  Layer decoration is not exported — only the table text.
- **Double-click a key** to find where it is used: `TypecraftText` in Localized mode (with this table,
  or with no table when the key resolves to it first) in open and closed scenes and prefabs under
  `Assets/`, plus a `"key"` string literal in `.cs` files under `Assets/`. One place — jumps there
  (selects and frames the object; a closed scene opens after the save prompt, a prefab opens in Prefab
  Mode, a script at the line); several — a list. Rename a key with F2 or the cell context menu.

### Typecraft window

- **Editor** — highlighted markup with autocomplete, live preview with typewriter, a gallery of
  styles, icons and effects, selection right in the preview, Play Mode edits survive leaving it.
- **Localization** — key × language grid of table strings, flags for missing translation, overflow,
  missing glyph, markup mismatch, ✦ "decorated in the layer" and ⚠ "string changed, decoration not
  applied" (filter «Только изменившиеся»), sync with CSV/TSV, Excel and Google Sheets, export
  to .xlsx/CSV/TSV/JSON, key usage search, `pseudo` pseudo-locale.
- **Library** — icon sets (drag and drop PNG), style editor, "style from TMP_Text", material
  presets and custom effects. Renaming a
  style or an icon rewrites tags in tables and scenes.
- **Fonts** — roles × languages, glyph coverage, static font builder from the characters used
  in strings (for CJK).
- **Dialogues** — node graph or list, node inspector, playback and checks; see "Dialogues".

### Custom styles, materials and effects

Create them right in the window — in the Editor tab gallery (the «+ стиль», «+ материал», «+ эффект» cards)
or with the buttons on the Library tab toolbar. The form opens on top of the window with a live preview;
«Сохранить» (save) writes the assets (with Undo where Unity supports it).

| What | How it works | Stored in |
| --- | --- | --- |
| **Style** | id, color/gradient, b/i/u, case, spacing, font role, material preset, effect. Previewed on the selected text or on a sample; after saving the selection is wrapped in `[id]…[/id]`. **«Стиль из выделения»** (style from selection; floating bar, «Стиль ▾» menu) takes the tags around the selection — `<color>`, `<b>`, `<i>`, `<u>`, `<cspace=…em>`, `<size=…%>`, `<font … material=…>`, `<material>`, an effect — into the form and replaces them with `[id]…[/id]` | `StyleLibrary` |
| **Material** | An SDF material preset for a font role: Face Dilate, Softness, outline, underlay (`UNDERLAY_ON`), glow (`GLOW_ON`). A material copy is made **for every font of the role** (each language has its own atlas) under one preset id, so a style works in every language. Glow exists only in the full shader — a preset with glow switches to `TextMeshPro/Distance Field` | `Materials/` next to `FontProfile`, entry in `FontProfile` |
| **Effect** | Kind (basic `<x>`, show `<+x>`, hide `<-x>`), base — a copy of a built-in TMPEffects animation, `Generic` (tracks) or `Stack` (several animations), parameters — the asset inspector inside the form. Name: lowercase Latin letters, digits, `_`; must not clash with a built-in effect or a TMP tag | `Effects/` next to `StyleLibrary` |

- **Project animation database.** The first custom effect creates `Effects/TypecraftAnimationDatabase.asset`:
  it references all built-in TMPEffects animations (the TMPEffects assets are not modified) plus your own. It is
  linked from `TypecraftSettings → Animation Database` and used by the window previews and by the `TMPAnimator`s
  Typecraft sets up (on apply; an animator on the TMPEffects default database is switched to the project one).
  The global TMPEffects settings are not changed — a `TMPAnimator` added by hand uses the TMPEffects database by
  default; pick the project database there to use custom effects.
- Custom effects show up at once in autocomplete, the gallery (marked «свой» / custom), the «Эффект ▾» menu and
  the style effect dropdown.
- **Removing:** right-click a material/effect card in the gallery or use «Удалить» (delete) in the Library tab.
  Materials and effect assets go to the OS trash; tags in the text stay.
- Rebuilding a static font updates the atlas in its preset materials automatically.

### Using it in your own tools

- `[TypecraftMarkup]` on a string field — highlighted field with preview and ✎ button in the Inspector.
- Right-click any string field → **Edit in Typecraft**; `TMP_Text` has it in the component menu.
- `TypecraftEditor.Edit(text, onApply)` — open the window and get the result in a callback.
- `MarkupTextField` — a UI Toolkit element for your own editors.

Details and examples: [`Documentation~/EXTENDING.md`](Documentation~/EXTENDING.md).

### Dialogues

The dialogue module is a set of assemblies on top of the core: `Velsi.Typecraft.Dialogue`
(runtime), `Velsi.Typecraft.Dialogue.InputSystem` (only with the Input System),
`Velsi.Typecraft.Dialogue.Editor` (the Dialogues tab) and `Velsi.Typecraft.Dialogue.Editor.Graph`
(the graph). Lines are Typecraft table keys, so translation, the markup layer, fonts and checks
work as for any other text.

**Dialogue and nodes.** A `DialogueAsset` (`Create → Typecraft → Dialogue`) is a set of linked
nodes plus a default table. Its stable `Id` is used by saves; a duplicated asset gets a new Id
automatically.

| Node | What it does |
| --- | --- |
| Start | Entry point. There may be several; the main one is set with «Сделать основной» (make main) |
| Line | A line: speaker, emotion, key. Advances on input, after N seconds, or after the typewriter + N seconds. **Variants** swap the key and emotion by condition (first match wins) |
| Choice | Options. Each has a condition and what happens when it fails: **Hide**, or **Disable** (shown but not clickable) |
| Branch | The first case whose condition holds, otherwise Else |
| Set Variables | Set / Add / Toggle flags |
| Event | Sends `name` + `argument` to the game |
| Wait | A pause |
| Jump | Goes to another dialogue (or a node of this one) |
| End | The end |

**Speakers and portraits.** A `SpeakerProfile` (`Create → Typecraft → Dialogue Speaker`): name
key (own table or the line's table), name style, portraits per emotion plus a default one, side
(`Left`/`Right` — which portrait of the box), and a voice blip for the typewriter.

**Flags and conditions.** A `DialogueVariableCatalog` (`Create → Typecraft → Dialogue Variables`)
declares flags: name, type (bool/int/float/string), default, description. A condition is a list
of comparisons (`IsTrue`, `IsFalse`, `=`, `≠`, `>`, `≥`, `<`, `≤`) joined by "all" or "any". An
unset flag falls back to the catalog default. Node visits are counted automatically as
`visited:<dialogue id>/<node id>` flags (visible in playback). Renaming a flag in the tab
rewrites it in every dialogue.

**Outputs.** Added from `GameObject → Typecraft`:

| Output | Component | Features |
| --- | --- | --- |
| Dialogue Box | `DialogueBoxPresenter` | Name, left and right portraits, typewriter (TMPEffects), "next" indicator, choice buttons, voice |
| Subtitles | `SubtitlePresenter` | `{name}: line`, hides after N seconds |
| World Text | `WorldTextPresenter` | Text in the world (`TextMeshPro`), faces the camera |

A custom output derives from `DialoguePresenter` (see `Documentation~/EXTENDING.md`).

**Playing.** `DialoguePlayer` holds the dialogue, the output, a separate choice output (when the
main one has no choices), the start node, `Play On Start`, `Once` (don't replay — remembered in
the flags), time mode (scaled/unscaled), the flag source (empty — `DialogueGlobals`) and the
`OnStarted`/`OnEnded`/`OnEvent(name, argument)` events. From code: `Play()`, `Advance()`,
`Choose(i)`, `Stop()`. `DialogueTrigger` starts a player manually (`Fire()`), on start, or on
`OnTriggerEnter` with a tag. `DialogueInputAdvance` (Input System assembly) binds "next" to an
`InputActionReference`.

**The Dialogues tab** of the Typecraft window:

- left — dialogues, speakers and flags ("+" next to a section creates one; a new flag creates a
  catalog if there is none);
- center — the board: **graph** (ports, drag links, Space or right-click for a new node,
  minimap) or a **list** of nodes; the view switch and the text language are in the toolbar;
- right — the node inspector: speaker and emotion from lists, a searchable key field with a
  text preview, **«Создать ключ»** (create key) adds the string to the table from the inspector
  (text in the base language; `[style]` and `<tag>` decoration goes to the markup layer),
  conditions with a flag picker;
- bottom — **«▶ Проигрывание»** (playback) without Play Mode: from the start or the selected
  node, typewriter, choices, playback flags (editable live, «Сбросить» resets them), a log; the
  current node is highlighted on the board;
- **«Проверить»** (check) — dialogue problems (no start, empty or broken links, unreachable
  nodes, empty or missing key, missing translation, stale markup, glyphs missing from the font,
  undeclared flag or wrong type, unknown emotion…); click to jump to the node. «Проверить все»
  checks the whole project.

**Saving.** Flags: `DialogueGlobals.Variables` is a `DialogueVariables` by default, with
`ToJson()` / `FromJson(json)` (visits are part of the snapshot). The position in a conversation:
`player.Runner.State` → `DialogueRunnerState { AssetId, NodeId }` (serializable with
`JsonUtility`). To resume, call `player.Restore(savedState, id => DialogueAsset)` (save `player.State`);
a bare `DialogueRunner` has the same `Restore`. Details in
`EXTENDING.md`.

**Without GraphView.** The graph is the only code that uses `UnityEditor.Experimental.GraphView`,
and it lives in its own assembly `Velsi.Typecraft.Dialogue.Editor.Graph` (folder
`Editor.Dialogue.Graph`). The rest of the tab does not depend on it: board views are found
through `IDialogueBoardView`, and without the graph assembly (removed from an embedded copy of the
package, or failing to compile in a future Unity) the tab opens in the "List" view with the same
inspector, playback and checks. To turn the graph off without deleting files, add the
`TYPECRAFT_NO_DIALOGUE_GRAPH` define in Player Settings → Scripting Define Symbols.

### Samples

Package Manager → Typecraft → Samples:

- **Simple Dialogue** — tutorial dialogue: a table in three languages, styles, icons, an input
  prompt, TMPEffects, a custom tag, language and device switching.
- **Branching Dialogue** — the dialogue module: a box with portraits, choices with
  Disable/Hide, a branch on a note flag, world text, subtitles, a dialogue event, flag reset.
- **Extra Input Prompts** — niche controllers and gamepad diagrams.

### Art

- `Art~/Prompts/` — sources of the base input prompts (CC0, Nicolae "Xelu" Berbece).
- `Art/Prompts/` — built atlases, `TMP_SpriteAsset` and `IconSet`.
- Rules for your own images: `Documentation~/ART_RULES.md`.
