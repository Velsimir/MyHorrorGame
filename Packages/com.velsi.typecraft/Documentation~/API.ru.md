# Typecraft — справочник по программному API

[Русский](API.ru.md) · [English](API.en.md) · [Руководство](Manual.ru.md) · [Manual](Manual.en.md) · [README](../README.md)

Версия пакета `0.2.0` (до 1.0 публичный API может меняться). Unity 6000.0+, TextMeshPro (`com.unity.ugui` 2.0), пространство имён `Velsi.Typecraft.*`.

Документ описывает всё публичное API пакета: рантайм, модуль диалогов и Editor-часть, включая точки встраивания (`[TypecraftMarkup]`, `TypecraftEditor.Edit`, `MarkupTextField`, `ITypecraftTab`). Примеры сверены с исходниками пакета; комментариев в коде нет намеренно. Фрагменты без обрамляющего типа (отдельные вызовы и присваивания) предполагается выполнять внутри метода.

## Содержание

1. [Установка и зависимости](#1-установка-и-зависимости)
2. [Сборки и define-символы](#2-сборки-и-define-символы)
3. [Быстрый старт](#3-быстрый-старт)
4. [Текст и разметка](#4-текст-и-разметка)
5. [Стили, иконки и подсказки ввода](#5-стили-иконки-и-подсказки-ввода)
6. [Шрифты по локалям](#6-шрифты-по-локалям)
7. [Источники текста, таблицы и слой разметки](#7-источники-текста-таблицы-и-слой-разметки)
8. [Эффекты TMPEffects](#8-эффекты-tmpeffects)
9. [Диалоги](#9-диалоги)
10. [Editor API](#10-editor-api)
11. [Расширение](#11-расширение)
12. [Сохранение и загрузка](#12-сохранение-и-загрузка)
13. [Ограничения и подводные камни](#13-ограничения-и-подводные-камни)
14. [Приложение: меню, ассеты, хранилища](#14-приложение-меню-ассеты-хранилища)

---

## 1. Установка и зависимости

Основной способ — архив `com.velsi.typecraft-0.2.0.tgz`: положить в `Packages/` проекта и поставить через `Package Manager → + → Install package from tarball…`. Подходят и embedded (папка в `Packages/`), и git-URL.

При первой загрузке сборка `Velsi.Typecraft.Editor.Welcome` (без зависимостей, компилируется всегда) доставляет обязательное и открывает окно Welcome:

| Что | Как ставится | Проверка наличия |
| --- | --- | --- |
| TMP Essential Resources | `TMP_PackageResourceImporter.ImportResources(true, false, false)` | есть ассет `TMP_Settings` |
| TMPEffects 0.3.2 | `Vendor~/com.luca3317.tmpeffects-0.3.2.tgz` копируется в `Packages/` проекта, `Client.Add("file:com.luca3317.tmpeffects-0.3.2.tgz")`; если копии нет — git-URL | тип `TMPEffects.Components.TMPAnimator` |
| TMPEffects Required Resources | `AssetDatabase.ImportPackage(".../TMPEffects Required Resources.unitypackage", false)` | есть ассет `TMPEffectsSettings` |

Каждый шаг пробуется один раз за сессию редактора (`SessionState`), в batch mode не запускается. Input System и Localization ставятся кнопками окна (`Client.Add`). Окно показывается при первой установке и при смене версии (по проекту, ключ с `PlayerSettings.productGUID`), вручную — `Help → Typecraft Welcome`.

Весь код, использующий TMPEffects, Input System и Localization, закрыт define-символами (раздел 2), поэтому пакет компилируется и до установки зависимостей.

Обязательные зависимости `package.json`: `com.unity.ugui` 2.0.0, `com.unity.2d.sprite` 1.0.0, `com.unity.modules.audio`, `com.unity.modules.physics`, `com.unity.modules.physics2d`.

| Пакет | Что даёт | Без него |
| --- | --- | --- |
| TMPEffects (`com.luca3317.tmpeffects`, от 0.3.0, проверено на 0.3.2) | Анимации `<wave>`, машинка, команды `<!wait>` | Теги эффектов не работают |
| Input System (`com.unity.inputsystem`, от 1.7.0) | Глифы `{key:Action}` по биндингам, `DialogueInputAdvance` | Глифы ввода не подставляются |
| Unity Localization (`com.unity.localization`, от 1.4.0) | Адаптер `UnityLocalizationTextSource` | Работают встроенные таблицы |

Ручная установка TMPEffects из git, если нужна другая версия:

```json
"com.luca3317.tmpeffects": "https://github.com/Luca3317/TMPEffects.git?path=/Package#0.3.2"
```

Сэмплы (Package Manager → Typecraft → Samples): **Simple Dialogue**, **Branching Dialogue**, **Showcase**, **Extra Input Prompts** (см. README).

---

## 2. Сборки и define-символы

| Сборка (asmdef) | Папка | Платформы | Условие компиляции | Содержимое |
| --- | --- | --- | --- | --- |
| `Velsi.Typecraft.Runtime` | `Runtime` | все | — | Ядро: `Typecraft`, `TypecraftText`, разметка, стили, иконки, шрифты, источники текста, слой разметки |
| `Velsi.Typecraft.InputSystem` | `Runtime.InputSystem` | все | `TYPECRAFT_INPUTSYSTEM` | `InputGlyphProfile`, `DeviceTracker`, `ControlGlyph` и родственные типы |
| `Velsi.Typecraft.Localization` | `Runtime.Localization` | все | `TYPECRAFT_LOCALIZATION` | `UnityLocalizationTextSource` |
| `Velsi.Typecraft.Dialogue` | `Runtime.Dialogue` | все | — | Модуль диалогов: ассет, ноды, раннер, плеер, выводы |
| `Velsi.Typecraft.Dialogue.InputSystem` | `Runtime.Dialogue.InputSystem` | все | `TYPECRAFT_INPUTSYSTEM` | `DialogueInputAdvance` |
| `Velsi.Typecraft.Editor` | `Editor` | Editor | — | Окно Typecraft, интеграция, таблицы, библиотека, шрифты, арт |
| `Velsi.Typecraft.Dialogue.Editor` | `Editor.Dialogue` | Editor | — | Вкладка «Диалоги», инспекторы, проверки, проигрывание |
| `Velsi.Typecraft.Dialogue.Editor.Graph` | `Editor.Dialogue.Graph` | Editor | отключается `TYPECRAFT_NO_DIALOGUE_GRAPH` | Граф на `UnityEditor.Experimental.GraphView` |
| `Velsi.Typecraft.Editor.Welcome` | `Editor.Welcome` | Editor | — | Окно Welcome и автоустановка зависимостей; ссылается только на `Unity.TextMeshPro` |

Сборок девять: `Velsi.Typecraft.InputSystem` и `Velsi.Typecraft.Localization` — отдельные рантайм-сборки с условной компиляцией.

Сборка `Velsi.Typecraft.Runtime` ссылается на `Unity.TextMeshPro` и `TMPEffects`; `Runtime` даёт `InternalsVisibleTo("Velsi.Typecraft.Editor")`.

### Define-символы

| Символ | Откуда берётся | Что включает |
| --- | --- | --- |
| `TYPECRAFT_INPUTSYSTEM` | `versionDefines`: `com.unity.inputsystem` ≥ 1.7.0 | Сборки `InputSystem` и `Dialogue.InputSystem`, ветки Input System в Editor |
| `TYPECRAFT_LOCALIZATION` | `versionDefines`: `com.unity.localization` ≥ 1.4.0 | Сборка `Localization`, `UnityLocalizationBackend` |
| `TYPECRAFT_TMPEFFECTS` | `versionDefines`: `com.luca3317.tmpeffects` ≥ 0.3.0 | Работа с `TMPWriter`/`TMPAnimator` в `TypecraftText`, `LineTypewriter`, редакторе эффектов |
| `TYPECRAFT_NO_DIALOGUE_GRAPH` | Вручную: Player Settings → Scripting Define Symbols | Выключает сборку `Dialogue.Editor.Graph`; вкладка «Диалоги» открывается в виде «Список» |

Граф — единственный код, использующий `UnityEditor.Experimental.GraphView`. Остальная вкладка от него не зависит: виды доски находятся через `IDialogueBoardView` (см. [10.11](#1011-editor-диалогов)).

---

## 3. Быстрый старт

1. `Tools/Typecraft/Create Project Settings` — создаёт в `Assets/Typecraft/` библиотеку стилей, профиль шрифтов (роли `Body` и `Header`), профиль глифов ввода (при Input System), встроенную таблицу строк `Main` с тремя стартовыми строками и сам `TypecraftSettings` в `Assets/Typecraft/Resources/`.
2. На объект с `TMP_Text` добавь `Typecraft Text`: режим **Inline** (разметка прямо в компоненте) или **Localized** (таблица + ключ).
3. `Window → Typecraft` (Ctrl+Alt+T) — окно редактора.

Из кода:

```csharp
using UnityEngine;
using Velsi.Typecraft;
using Velsi.Typecraft.Text;

public sealed class DoorHint : MonoBehaviour
{
    [SerializeField] private TypecraftText _text;

    private void Start()
    {
        _text.SetKey("Main", "ui.door.locked");
        Typecraft.SetLocale("ru");
        Typecraft.SetDevice(DeviceFamily.Xbox);
    }
}
```

Разовый рендер строки без компонента:

```csharp
string rendered = Typecraft.Render("Press {key:Interact} to [warn]open[/warn]");
```

Настройки загружаются лениво из `Resources/TypecraftSettings`. Если ассета нет, `Typecraft.Settings` возвращает `null`, а `Render` работает без стилей и иконок.

---

## 4. Текст и разметка

### 4.1. Синтаксис

| Запись | Что делает |
| --- | --- |
| `{key:Interact}` | Глиф кнопки действия для текущего устройства (встроенный обработчик с префиксом `key`) |
| `{candle}`, `{set/candle}` | Иконка из набора: просто по ключу или с идентификатором набора |
| `[warn]…[/warn]` | Стиль из `StyleLibrary` |
| `{player:name}` | Свой тег: `{префикс:аргумент}`, обработчик регистрируется `Typecraft.Register` |
| `{0}` | Цифровой плейсхолдер: проходит как есть, подставляется `string.Format` в `TypecraftText` (если у источника нет своего форматтера) |
| `<wave>…</wave>`, `<!wait=0.5>` | Теги TMP и TMPEffects — проходят как есть |
| `\{`, `\}`, `\[`, `\]`, `\<`, `\>`, `\\` | Экранирование (`\<` превращается в `<noparse><</noparse>`) |

Правила разбора (по `MarkupRenderer`):

- Скобки не могут пересекать перевод строки; внутри `{…}` и `[…]` повторная открывающая скобка того же вида обрывает разбор, и скобка остаётся текстом.
- `{…}`, начинающееся с цифры или пустое, остаётся как есть (плейсхолдер формата).
- `{prefix:arg}` — ищется обработчик по `prefix`. Нет обработчика — `MarkupIssue.UnknownTag`; обработчик вернул `false` — `UnresolvedTag`. В редакторе на месте тега выводится красная метка `[…]`, в билде — пусто.
- `{name}` без двоеточия — иконка. Не найдена — `UnknownIcon`. Заглушка набора без спрайта — `PlaceholderIcon`.
- `[id]` — стиль. Идентификатор: буква, затем буквы, цифры, `_`, `-`, `.`. Неизвестный стиль — `UnknownStyle`, текст остаётся как есть.
- Закрытие стиля `[/id]` закрывает и заново открывает вложенные стили, лежащие выше. Закрытие без открытия — `StrayClosingStyle` (тег удаляется), незакрытый стиль закрывается в конце — `UnclosedStyle`.
- Перед разбором применяются автотеги (см. [4.6](#46-автотеги)).

### 4.2. `Typecraft` — статический фасад

`namespace Velsi.Typecraft`, `public static class Typecraft`.

| Член | Описание |
| --- | --- |
| `static event Action Changed` | Срабатывает при смене языка, устройства, регистрации тега и `Refresh()`. На него подписаны `TypecraftText` и презентеры |
| `static TypecraftSettings Settings` | Настройки; при первом обращении грузятся из `Resources` по пути `TypecraftSettings.ResourcesPath` (может быть `null`) |
| `static string Locale` | Текущий язык; без `SetLocale` — `Settings.DefaultLocale`, при отсутствии настроек `"en"` |
| `static DeviceFamily Device` | Текущее устройство; без `SetDevice` — `Settings.DefaultDevice` |
| `static IconLibrary Icons` | Библиотека иконок: наборы из `Settings.IconSets` и наборы резолвера ввода |
| `static IReadOnlyDictionary<string, IMarkupTagHandler> TagHandlers` | Зарегистрированные обработчики по префиксу (всегда содержит `key`) |
| `static void Initialize(TypecraftSettings settings)` | Подменить настройки (сбрасывает кэши, пересобирает автотеги и реестр TMP-ассетов) |
| `static void Register(IMarkupTagHandler handler)` | Зарегистрировать или заменить обработчик по `Prefix`. `null` и пустой префикс игнорируются |
| `static void SetLocale(string locale)` | Сменить язык. Пустая строка или тот же код — ничего |
| `static void SetDevice(DeviceFamily device)` | Сменить устройство |
| `static void Refresh()` | Пересобрать кэши и вызвать `Changed` |
| `static string Render(string markup)` | Отрендерить в строку TMP rich text |
| `static string Render(string markup, List<MarkupDiagnostic> diagnostics)` | То же с диагностикой (кэш при чтении обходится) |
| `static string RenderPreview(string markup, string locale, DeviceFamily device, List<MarkupDiagnostic> diagnostics, List<int> sourceMap)` | Рендер для заданных языка и устройства без кэша; `sourceMap[i]` — индекс символа исходной разметки для символа результата `i` |

Кэш `Render` — до 1024 записей по тексту разметки, очищается при любом `Changed`. Статика сбрасывается на `RuntimeInitializeLoadType.SubsystemRegistration` (работает режим «Enter Play Mode без перезагрузки домена»).

### 4.3. `TypecraftText`

`namespace Velsi.Typecraft.Text`, `MonoBehaviour`, `[ExecuteAlways]`, `[DisallowMultipleComponent]`, `[RequireComponent(typeof(TMP_Text))]`, меню `Typecraft/Typecraft Text`.

| Член | Описание |
| --- | --- |
| `TextMode Mode` | `Inline` (разметка в поле) или `Localized` (таблица и ключ) |
| `string Markup`, `Table`, `Key` | Текущие значения полей |
| `TMP_Text Target` | Целевой `TMP_Text` |
| `string FontRole { get; set; }` | Роль шрифта; пусто — `Settings.DefaultFontRole`. Сеттер сразу обновляет текст |
| `void SetMarkup(string markup)` | Режим `Inline` и новая разметка |
| `void SetKey(string table, string key)` | Режим `Localized`, таблица и ключ. Пустая таблица: у `TableTextSource` берётся первая, где есть ключ; у Unity Localization — таблица по умолчанию |
| `void SetArguments(params object[] arguments)` | Аргументы для `{0}`; обновляет текст |
| `void Refresh()` | Перерисовать (ничего не делает у неактивного компонента) |

Что происходит при `Refresh`: подбирается шрифт и материал по роли и языку (`FontProfile.Resolve`), текст запрашивается (в режиме `Localized` через `LocalizedText.Request`), рендерится `Typecraft.Render`, затем подставляются аргументы (`string.Format` с `CurrentCulture`, только если источник не отформатировал строку сам), и, если на объекте нет `TMPWriter`, из результата вырезаются теги-команды (`EffectTags.StripWriterTags`). Асинхронные ответы устаревших запросов отбрасываются.

```csharp
_text.SetMarkup("Coins: {0}");
_text.SetArguments(12);
_text.FontRole = "Header";
```

### 4.4. Типы разметки (`Velsi.Typecraft.Markup`)

```csharp
public interface IMarkupTagHandler
{
    string Prefix { get; }
    bool TryRender(string argument, MarkupContext context, StringBuilder output);
}
```

`KeyTagHandler` (`public sealed`) — встроенный обработчик `key`: просит `Settings.InputGlyphs.TryAppendGlyph(argument, context.Device, output)`.

`MarkupContext` (`public sealed class`):

| Член | Описание |
| --- | --- |
| `MarkupContext(TypecraftSettings settings, IconLibrary icons, string locale, DeviceFamily device, List<MarkupDiagnostic> diagnostics)` | Конструктор |
| `TypecraftSettings Settings`, `string Locale`, `DeviceFamily Device`, `List<MarkupDiagnostic> Diagnostics` | Контекст рендера |
| `void Report(MarkupIssue issue, string value)` | Добавить диагностику в позиции разбора |
| `bool TryAppendIcon(string reference, StringBuilder output)` | Вставить иконку (`ключ` или `набор/ключ`); `false`, если иконки нет |
| `static void AppendIcon(IconSet set, IconEntry entry, StringBuilder output)` | Записать `<sprite="…" name="…">` (с `tint=1` для тонируемых) и зарегистрировать спрайт-ассет в реестре TMP |
| `static void AppendMissing(string reference, StringBuilder output)` | Красная метка `[reference]` (только в редакторе) |

`MarkupDiagnostic` — `readonly struct { MarkupIssue Issue; string Value; int Index }`, конструктор `(issue, value, index)`.

`enum MarkupIssue { UnknownIcon, PlaceholderIcon, UnknownStyle, UnknownTag, UnresolvedTag, StrayClosingStyle, UnclosedStyle }`.

`MarkupRenderer` (`public static`):

```csharp
static string Render(string markup, MarkupContext context, IReadOnlyDictionary<string, IMarkupTagHandler> handlers)
static string Render(string markup, MarkupContext context, IReadOnlyDictionary<string, IMarkupTagHandler> handlers, List<int> sourceMap)
```

Обычно вызывать `Typecraft.Render`; прямой `MarkupRenderer.Render` нужен, если контекст собирается вручную.

`EffectTags` (`public static`) — разбор тегов TMP-эффектов в уже отрендеренной строке:

| Метод | Описание |
| --- | --- |
| `bool IsWriterTag(string name)` | Тег-команда машинки: имя начинается с `!` или `#` (если это не HTML-цвет) |
| `bool IsShowHideTag(string name)` | Имя начинается с `+` или `-` (эффект появления/исчезания) |
| `void Scan(string rendered, Func<string, bool> isAnimation, out bool animations, out bool commands)` | Есть ли в строке анимации и/или команды |
| `string StripWriterTags(string rendered)` | Вырезать теги-команды (`<noparse>` пропускается) |

`TypecraftMarkupAttribute` (`[AttributeUsage(Field)]`, `PropertyAttribute`) — `TypecraftMarkup(bool preview = true)`, свойство `Preview`. Рисуется `TypecraftMarkupDrawer` (см. [10.3](#103-поле-разметки-в-инспекторе-typecraftmarkup)).

### 4.5. `DeviceFamily` и `LocaleCodes`

`enum DeviceFamily { KeyboardMouse, Xbox, PlayStation, Switch, SteamDeck, GenericGamepad, Touch }`.

`LocaleCodes.Language(string locale)` — часть до первого `-` или `_` (`"zh-Hans"` → `"zh"`), для пустой строки — `""`.

### 4.6. Автотеги

Автотеги оборачивают слова в стиль без ручной разметки. Источники правил:

- `TextStyle.AutoTagWords` — слова стиля (правится в «Библиотеке» и кнопкой «Автотег ▾» вкладки «Редактор»); шаблон `[id]$0[/id]`.
- `TypecraftSettings.AutoTags` — список `AutoTagRule` со своими словами и шаблоном (по умолчанию `[style]$0[/style]`).

```csharp
public sealed class AutoTagRule
{
    public AutoTagRule(IEnumerable<string> words, string template);
    public IReadOnlyList<string> Words { get; }
    public string Template { get; }
}
```

`AutoTagger` (`namespace Velsi.Typecraft.AutoTags`):

```csharp
static AutoTagger Build(IEnumerable<AutoTagRule> rules, StyleLibrary styles)
string Apply(string markup)
string Apply(string markup, List<int> sourceMap)
```

`Build` возвращает `null`, если слов нет. Совпадение — целое слово без учёта регистра; уже размеченные участки (`[x]…[/x]`, `{…}`, `<…>`, экранирование) не трогаются. В `Template` подстановка `$0` — найденное слово; правила `AutoTags` перекрывают слова стилей при совпадении слова.

Автотеги подключает `Typecraft` сам (при `Initialize`/`Refresh`); вручную `AutoTagger` нужен редко.

---

## 5. Стили, иконки и подсказки ввода

### 5.1. Стили (`Velsi.Typecraft.Styles`)

`StyleLibrary` (`ScriptableObject`, меню `Typecraft/Style Library`):

| Член | Описание |
| --- | --- |
| `IReadOnlyList<TextStyle> Styles` | Все стили |
| `bool TryGet(string id, out TextStyle style)` | Поиск по идентификатору |

`TextStyle` (`[Serializable] sealed class`):

| Член | Описание |
| --- | --- |
| `string Id`, `DisplayName` | Идентификатор (используется в `[id]`) и подпись (по умолчанию `Id`) |
| `string FontRole`, `MaterialPreset` | Роль шрифта и пресет материала (см. [6](#6-шрифты-по-локалям)) |
| `TMP_ColorGradient Gradient` | Градиент вместо цвета |
| `IReadOnlyList<string> AutoTagWords` | Слова автотега |
| `void AppendOpen(StringBuilder output, FontProfile fonts, string locale)` | Записать открывающие теги TMP |
| `void AppendClose(StringBuilder output, FontProfile fonts, string locale)` | Закрывающие теги в обратном порядке |

Остальное (цвет, `b/i/u`, регистр, размер в процентах, интервал в `em`, тег эффекта) хранится в приватных сериализуемых полях и правится в инспекторе или окне. Порядок открывающих тегов: `font`, `gradient`/`color`, `size`, `cspace`, `b`, `i`, `u`, регистр (`uppercase`/`lowercase`/`smallcaps`), тег эффекта.

`enum TextCase { None, Upper, Lower, SmallCaps }`.

### 5.2. Иконки (`Velsi.Typecraft.Library`)

`IconSet` (`ScriptableObject`, меню `Typecraft/Icon Set`): `string SetId`, `TMP_SpriteAsset SpriteAsset`, `IReadOnlyList<IconEntry> Entries`, `IconSetBuildSettings BuildSettings`, `bool TryGetEntry(string key, out IconEntry entry)`, `bool TryGetGlyphName(string key, out string glyphName)` (`false` для заглушек).

`IconEntry` (`[Serializable]`): `const float MinScale = 0.25f`, `MaxScale = 3f`, конструктор `(string key, string glyphName, bool tintable)`, свойства `Key`, `GlyphName`, `Tintable`, `IsPlaceholder` (нет имени глифа), `Offset` (`Vector2`), `Scale` (всегда > 0).

`IconSetBuildSettings` (`[Serializable]`): `SourceFolder` (get/set), `IncludeSubfolders`, `Padding` (0–8), `MaxAtlasSize`, `FilterMode`, `VerticalOffset`, `TintableByDefault`.

`IconLibrary` — поиск иконок:

```csharp
IconLibrary(IEnumerable<IconSet> sets)
void Add(IconSet set)
bool TryResolve(string reference, out IconSet set, out IconEntry entry)
IEnumerable<string> QualifiedKeys { get; }
```

`TryResolve` принимает `набор/ключ` или просто `ключ` (первый набор с таким ключом).

Иконки в тексте стоят на строке левым нижним углом холста; стандарт картинки — квадрат 128×128, рисунок в 104×104 (см. `Documentation~/ART_RULES.ru.md`, шаблон `IconTemplate_128.png`).

### 5.3. Подсказки ввода

`namespace Velsi.Typecraft.Input`:

```csharp
public interface IInputGlyphResolver
{
    IEnumerable<IconSet> IconSets { get; }
    void Activate();
    bool TryAppendGlyph(string actionName, DeviceFamily device, StringBuilder output);
}
```

Ассет-резолвер — `ScriptableObject`, назначается в `TypecraftSettings → Input Glyphs` (если ассет не реализует интерфейс, инспектор предупредит и сбросит поле). `Activate()` вызывается один раз при первом `Render`.

`namespace Velsi.Typecraft.InputSystem` (условие `TYPECRAFT_INPUTSYSTEM`):

`InputGlyphProfile` (`ScriptableObject`, `IInputGlyphResolver`, меню `Typecraft/Input Glyph Profile`):

| Член | Описание |
| --- | --- |
| `InputActionAsset Actions` | Ассет действий, где ищется `actionName` |
| `IReadOnlyList<FamilyGlyphs> Families` | Настройки по семействам устройств |
| `IEnumerable<IconSet> IconSets` | Наборы всех семейств |
| `void Activate()` | Запускает `DeviceTracker.Start()` |
| `bool TryAppendGlyph(string actionName, DeviceFamily device, StringBuilder output)` | Сначала оверрайд действия для семейства, затем иконки по биндингам, затем цепочка `Fallback` (не глубже 4) |
| `void Configure(InputActionAsset actions, List<FamilyGlyphs> families, List<ActionGlyphOverride> overrides)` | Задать данные из кода |

Для составных биндингов (WASD, стрелки) выводятся все части в порядке up/left/down/right. Если для какого-то контрола нет иконки, семейство отвергается целиком, и берётся `Fallback`.

`FamilyGlyphs`: конструктор `(DeviceFamily family, IconSet icons, DeviceFamily? fallback, List<ControlGlyph> controls)`, свойства `Family`, `Icons`, `Fallback`, `Controls`, метод `string FindIconKey(string control)`.

`ControlGlyph`: `(string control, string iconKey)`; `Control`, `IconKey`. Если сопоставления нет, ключом иконки служит имя контрола в нижнем регистре.

`ActionGlyphOverride`: `(DeviceFamily family, string action, string markup)`; `Family`, `Action`, `Markup` (разметка, например `{gestures/tap}`; рендерится через `Typecraft.Render`).

`DefaultControlGlyphs`: `static List<ControlGlyph> For(DeviceFamily family)` (клавиатура/мышь, Xbox, PlayStation, Switch, Steam Deck; для остальных пусто), `static List<ActionGlyphOverride> TouchOverrides()` (жесты для Interact, Attack, Jump, Crouch, Sprint, Move, Look, Previous, Next).

`DeviceTracker`:

```csharp
static void Start()
static DeviceFamily FamilyOf(InputDevice device)
```

`Start()` работает только в Play Mode: подписывается на нажатие любой кнопки и вызывает `Typecraft.SetDevice`, начальное семейство берёт по платформе. `FamilyOf`: клавиатура/мышь → `KeyboardMouse`, `Touchscreen` → `Touch`, геймпады — по layout и названию продукта (Steam Deck, DualShock/DualSense, Switch Pro, XInput/Xbox, иначе `GenericGamepad`).

Для Touch у действий нет touch-биндингов, поэтому глифы берутся из оверрайдов `ActionGlyphOverride`.

---

## 6. Шрифты по локалям

`namespace Velsi.Typecraft.Fonts`.

`FontProfile` (`ScriptableObject`, `Typecraft/Font Profile`): `IReadOnlyList<FontRole> Roles`, `bool TryGetRole(string roleId, out FontRole role)`, `FontVariant Resolve(string roleId, string locale)` (`null`, если роли нет).

`FontRole` (`[Serializable]`): `Id`, `Default` (`FontVariant`), `LocaleOverrides` (`IReadOnlyList<LocaleFontOverride>`), `FontVariant Resolve(string locale)`.

Порядок выбора в `Resolve`: точное совпадение кода локали без учёта регистра → первое совпадение по языку (`LocaleCodes.Language`) → `Default`. Оверрайды без шрифта пропускаются.

`LocaleFontOverride`: `Locale`, `Variant`.

`FontVariant`: `TMP_FontAsset Font`, `IReadOnlyList<FontMaterialPreset> Materials`, `Material FindMaterial(string presetId)`. `FontMaterialPreset`: `Id`, `Material`.

Стиль со своей `FontRole` вставляет `<font="…" material="…">` по шрифту текущего языка. Шрифты, пресеты материалов, спрайт-ассеты и градиенты Typecraft подставляет в TMP сам (`TMP_Text.OnFontAssetRequest`, `OnSpriteAssetRequest`), класть их в `Resources` не нужно.

Для CJK — статический шрифт под набор символов из строк (вкладка «Шрифты», `StaticFontBuilder`, см. [10.8](#108-шрифты-библиотека-и-арт)).

---

## 7. Источники текста, таблицы и слой разметки

### 7.1. `ITextSource` и `TextEntry`

`namespace Velsi.Typecraft.Text`.

```csharp
public interface ITextSource
{
    IReadOnlyList<string> Locales { get; }
    string BaseLocale { get; }
    void Activate();
    void Request(string table, string key, string locale, Action<TextEntry> onReady);
    void SelectLocale(string localeCode);
}
```

| Член | Контракт |
| --- | --- |
| `Locales` | Доступные языки (включая виртуальный `pseudo`, если он поддерживается) |
| `BaseLocale` | Основной язык, к которому идёт откат |
| `Activate()` | Один раз при первом `Render`: подписки, синхронизация текущего языка |
| `Request(...)` | Отдать **сырую** строку таблицы (без псевдолокали и без подстановки аргументов); результат передаётся в `onReady` синхронно или позже |
| `SelectLocale(code)` | Переключить язык источника (для встроенного — `Typecraft.SetLocale`) |

`TextEntry` — `readonly struct`:

```csharp
TextEntry(string table, string key, string locale, string text, Func<string, IList<object>, string> format = null)
string Table, Key, Locale, Text
Func<string, IList<object>, string> Format
bool Found
static TextEntry Missing(string table, string key, string locale)
```

В `TextEntry` указываются id таблицы и язык, из которых строка взята на самом деле (с учётом отката на основной язык) — по ним ищется запись слоя разметки. `Format` — свой форматтер источника (Smart Strings): получает уже оформленный текст и аргументы.

Источник назначается в `TypecraftSettings → Text Source` (поле хранится как `ScriptableObject`; если ассет не реализует `ITextSource`, он сбрасывается с предупреждением). Свойство `TypecraftSettings.TextSource` отдаёт `ITextSource`.

### 7.2. `LocalizedText`

```csharp
static void Request(string table, string key, IList<object> arguments, Action<string, bool> onReady)
static string Decorate(TextEntry entry)
```

`Request` — тот же путь, что у `TypecraftText`: источник → слой разметки → форматтер источника → псевдолокаль. `onReady(text, formatted)`; `formatted == false` означает, что `{0}` ещё не подставлены. Нет источника или ключ пуст — `onReady(key, true)`. Не найдено — `onReady(key, true)`. `Decorate` — текст с учётом слоя (`entry.Text`, если слоя нет).

```csharp
LocalizedText.Request("Main", "ui.door.locked", null, (markup, formatted) =>
{
    string rendered = Typecraft.Render(markup);
    Debug.Log(rendered);
});
```

### 7.3. Встроенные таблицы (`Velsi.Typecraft.Text.Tables`)

`TableTextSource` (`ScriptableObject`, `ITextSource`, меню `Typecraft/Text Sources/String Tables`):

| Член | Описание |
| --- | --- |
| `IReadOnlyList<string> Locales` | Языки проекта плюс `pseudo` (если включён) |
| `IReadOnlyList<string> ProjectLocales` | Только языки проекта |
| `string BaseLocale` | Основной язык (пустое поле — первый язык списка, иначе `"en"`) |
| `IReadOnlyList<StringTable> Tables` | Таблицы |
| `TextEntry Lookup(string table, string key, string locale)` | Строка на языке; нет — на основном (`TextEntry.Locale` отразит фактический); нет и там — `Missing` |
| `StringTable FindTable(string table, string key = null)` | Таблица по id; при пустом `table` — первая, содержащая ключ |
| `Request`, `Activate`, `SelectLocale` | Реализация `ITextSource`; `SelectLocale` вызывает `Typecraft.SetLocale` |

`StringTable` (`Typecraft/String Table`): `string TableId` (пустое поле — имя ассета), `IReadOnlyList<StringEntry> Entries`, `bool TryGet(string key, string locale, out string value)` (пустые значения считаются отсутствующими), `bool Contains(string key)`.

`StringEntry`: `Key`, `Comment` (get), `bool TryGet(string locale, out string value)`. `LocalizedValue`: `(string locale, string text)`, `Locale`, `Text`.

Запись в таблицы — через редактор (окно «Локализация», `ITableBackend`, см. [10.5](#105-таблицы-локализации)): изменяющие методы типов внутренние.

### 7.4. `MarkupLayer` — слой разметки

`MarkupLayer` (`ScriptableObject`, `Typecraft/Markup Layer`): оформление поверх таблицы.

| Член | Описание |
| --- | --- |
| `string TableId` | Id таблицы, к которой относится слой |
| `IReadOnlyList<MarkupLayerEntry> Entries` | Записи |
| `MarkupLayerEntry Find(string key, string locale)` | Запись или `null` |
| `string Resolve(string key, string locale, string tableText)` | Текст слоя, если снимок записи совпадает со строкой таблицы и разметка не пуста; иначе `tableText` |

`MarkupLayerEntry`: `(string key, string locale)`, `Key`, `Locale`, `Markup` (полный текст с оформлением), `Source` (снимок строки таблицы, на которую ставилось оформление). Сеттеры внутренние — пишет только редактор.

Слои перечислены в `TypecraftSettings → Markup Layers`; `TypecraftSettings.FindMarkupLayer(string tableId)` возвращает слой таблицы или `null`.

Инвариант слоя: `Decoration.Strip(entry.Markup) == entry.Source`. Строку таблицы изменили — `Source` перестаёт совпадать, `Resolve` отдаёт чистую строку таблицы (запись «устарела»).

### 7.5. Псевдолокаль

`Pseudolocalizer` (`public static`): `const string Locale = "pseudo"`, `bool IsPseudo(string locale)`, `string Transform(string markup)`. Оборачивает текст в `\[ … \]`, подменяет буквы на акцентированные, добавляет хвост из `·` (+35% длины); теги, плейсхолдеры и экранирование не трогаются. Псевдолокаль строится из оформленного текста основного языка.

### 7.6. Адаптер Unity Localization

`namespace Velsi.Typecraft.Localization`, условие `TYPECRAFT_LOCALIZATION`. `UnityLocalizationTextSource` (`ScriptableObject`, `ITextSource`, меню `Typecraft/Text Sources/Unity Localization`) — приватное поле `_defaultTable` (таблица по умолчанию, если запрос без таблицы).

- `Locales` — коды `LocalizationSettings.AvailableLocales` плюс `pseudo`; `BaseLocale` — `ProjectLocale` или `"en"`.
- `Activate()` — в Play Mode подписывается на `SelectedLocaleChanged` и синхронизирует `Typecraft.SetLocale`; в Edit Mode ставит язык проекта.
- `Request` — асинхронно через `StringDatabase.GetTableEntryAsync`; в `TextEntry` возвращает имя коллекции таблиц и фактический язык и `Format`, который для Smart-строк зовёт `SmartFormatter`, иначе `string.Format`.
- `SelectLocale(code)` — в Play Mode меняет `LocalizationSettings.SelectedLocale`; `pseudo` — только `Typecraft.SetLocale`.

### 7.7. Синхронизация, экспорт, поиск использований

Только редактор (рантайма у этих функций нет): CSV/TSV, `.xlsx`, Google Sheets, экспорт в `.xlsx`/CSV/TSV/JSON, поиск использований ключа. Подробное API — [10.5](#105-таблицы-локализации) и [10.6](#106-синхронизация-экспорт-и-поиск-использований).

---

## 8. Эффекты TMPEffects

Включаются символом `TYPECRAFT_TMPEFFECTS` (TMPEffects ≥ 0.3.0). Теги `<wave>…</wave>`, `<+fade>`, `<-fade>`, `<!wait=0.5>`, `<#cmd>` проходят разметку без изменений и обрабатываются компонентами TMPEffects на объекте с текстом.

- Анимации (`<x>`, `<+x>`, `<-x>`) требуют `TMPAnimator`, команды (`<!…>`, `<#…>`) и машинка — `TMPWriter`. Редактор добавляет их сам (`EffectComponents.Ensure`).
- `TypecraftText` вырезает теги-команды, если на объекте нет `TMPWriter` (иначе в текст попали бы сырые `<!wait>`).
- `TypecraftSettings.AnimationDatabase` — база анимаций проекта (`TypecraftAnimationDatabase`); её используют превью окна и `TMPAnimator`, которые ставит Typecraft. Первый свой эффект создаёт базу в `Effects/` рядом с `StyleLibrary`: туда ссылками входят все встроенные анимации TMPEffects плюс свои. `TMPAnimator`, добавленный вручную, по умолчанию берёт базу TMPEffects — для своих эффектов выбери в нём базу проекта.
- Стиль может нести тег эффекта (поле `effectTag`, например `wave`); закрывающий тег строится по имени до первого пробела или `=`.

Машинка для строк диалога — `LineTypewriter` (см. [9.10](#910-выводы)). Редакторское API эффектов — `EffectCatalog`, `EffectLibrary`, `EffectComponents` ([10.9](#109-эффекты-в-окне)).

---

## 9. Диалоги

Модуль диалогов не зависит от ECS и Zenject. Сборка `Velsi.Typecraft.Dialogue`, namespace `Velsi.Typecraft.Dialogue` (у `DialogueInputAdvance` — `Velsi.Typecraft.Dialogue.InputSystem`). Тексты реплик — ключи таблиц Typecraft, поэтому перевод, слой разметки, шрифты и проверки работают как для любого текста. Рабочий пример — сэмпл **Branching Dialogue**.

### 9.1. `DialogueAsset`

`ScriptableObject`, меню `Create → Typecraft → Dialogue`. Набор нод со ссылками друг на друга.

| Член | Описание |
| --- | --- |
| `string Id` | Стабильный идентификатор для сохранений (GUID без дефисов); выдаётся сам. У копии ассета в редакторе перевыдаётся (`DialogueIdGuard`) |
| `string DefaultTable { get; set; }` | Таблица по умолчанию для нод без своей |
| `string StartNodeId { get; set; }` | Основная стартовая нода; пусто — первая `StartNode`; `null`, если стартов нет |
| `IReadOnlyList<DialogueNode> Nodes` | Все ноды |
| `string ResolveTable(string tableOverride)` | `tableOverride`, а если пусто — `DefaultTable` |
| `bool TryGetNode(string id, out DialogueNode node)` | Поиск по `Id` ноды |
| `DialogueNode FindNode(string id)`, `T FindNode<T>(string id)` | То же, `null` при отсутствии |
| `T AddNode<T>(T node)` | Добавить ноду (пустой `Id` заполняется); первая `StartNode` становится основной |
| `bool RemoveNode(DialogueNode node)` | Удалить ноду |
| `void InvalidateLookup()` | Сбросить кэш поиска (после ручных правок списка) |
| `void RegenerateId()` | Выдать новый `Id` ассета |

Ноды хранятся через `[SerializeReference]`, поэтому тип ноды — часть данных ассета.

### 9.2. Ноды

Базовый класс:

```csharp
[Serializable]
public abstract class DialogueNode
{
    public string Id { get; set; }
    public Vector2 Position { get; set; }
    public virtual void CollectLinks(List<DialogueLink> links);
    public List<DialogueLink> GetLinks();
    public static string NewId();
}

public readonly struct DialogueLink
{
    public DialogueLink(string port, string target, bool required = true);
    public string Port { get; }
    public string Target { get; }
    public bool Required { get; }
    public bool IsEmpty { get; }
}
```

`Position` — положение на доске графа. Связь — `Id` ноды-цели в строковом поле.

| Нода | Поля | Порты (имена в `DialogueLink.Port`) | Поведение |
| --- | --- | --- | --- |
| `StartNode` | `Next` | `Next` | Точка входа. Стартов может быть несколько; основная выбирается кнопкой «Сделать основной» в инспекторе (`StartNodeId`) |
| `LineNode` | `Speaker` (`SpeakerProfile`), `Emotion`, `Table`, `Key`, `Variants` (`List<LineVariant>`), `Advance` (`LineAdvance`), `Seconds`, `Next` | `Next` | Реплика. `SelectVariant(variables, catalog)` — первый вариант с непустым ключом и выполненным условием |
| `ChoiceNode` | `Options` (`List<ChoiceOption>`) | `Option 0`, `Option 1`, … | Варианты ответа. У каждого условие и поведение, если оно не выполнено: `Hide` (скрыть) или `Disable` (показать неактивным) |
| `BranchNode` | `Cases` (`List<BranchCase>`), `Else` | `Case 0`, …, `Else` | `Evaluate(variables, catalog)` возвращает `Next` первого случая с выполненным условием, иначе `Else` |
| `SetVariablesNode` | `Assignments` (`List<VariableAssignment>`), `Next` | `Next` | `Apply(variables, catalog)` применяет присваивания |
| `EventNode` | `EventName`, `Argument`, `Next` | `Next` | Событие в игру |
| `WaitNode` | `Seconds` (по умолчанию 1), `Next` | `Next` | Пауза |
| `JumpNode` | `Target` (`DialogueAsset`), `TargetNode` | нет | Переход в другой диалог или к ноде этого. Пустой `TargetNode` — основной старт цели; пустой `Target` — этот же диалог |
| `EndNode` | — | нет | Конец |

Вспомогательные типы:

- `LineVariant` — `Condition`, `Key`, `Emotion`: другой ключ и эмоция по условию (первый подходящий).
- `LineAdvance { Input, AfterSeconds, AfterText }` — переход по вводу, через `Seconds` или через `Seconds` после того, как текст показан целиком.
- `ChoiceOption` — `Table`, `Key`, `Condition`, `WhenUnavailable` (`ChoiceUnavailable { Hide, Disable }`), `Next`.
- `BranchCase` — `Condition`, `Next`.
- `VariableAssignment` — `Name`, `Operation` (`VariableOperation { Set, Add, Toggle }`), `Value`; конструкторы `()` и `(string name, VariableOperation operation, DialogueValue value = default)`; `Apply(variables, catalog)`. `Add` — числа складываются, строки склеиваются; `Toggle` инвертирует bool (незаданное значение берётся из каталога).

Сборка диалога в коде:

```csharp
using UnityEngine;
using Velsi.Typecraft.Dialogue;

DialogueAsset asset = ScriptableObject.CreateInstance<DialogueAsset>();
asset.DefaultTable = "Main";

StartNode start = asset.AddNode(new StartNode());
LineNode hello = asset.AddNode(new LineNode { Key = "npc.hello" });
EndNode end = asset.AddNode(new EndNode());

start.Next = hello.Id;
hello.Next = end.Id;
```

### 9.3. Условия

```csharp
[Serializable]
public sealed class Condition
{
    public ConditionMode Mode { get; set; }
    public List<ConditionClause> Clauses { get; }
    public bool IsEmpty { get; }
    public Condition Add(string variable, ComparisonOperator comparison, DialogueValue value = default);
    public bool Evaluate(IDialogueVariables variables, DialogueVariableCatalog catalog = null);
    public static bool IsMet(Condition condition, IDialogueVariables variables, DialogueVariableCatalog catalog = null);
}

public sealed class ConditionClause
{
    public ConditionClause();
    public ConditionClause(string variable, ComparisonOperator comparison, DialogueValue value);
    public string Variable { get; set; }
    public ComparisonOperator Comparison { get; set; }
    public DialogueValue Value { get; set; }
    public bool UsesValue { get; }
    public bool Evaluate(IDialogueVariables variables, DialogueVariableCatalog catalog = null);
}

public enum ConditionMode { All, Any }
public enum ComparisonOperator { IsTrue, IsFalse, Equal, NotEqual, Greater, GreaterOrEqual, Less, LessOrEqual }
```

Пустое условие всегда выполнено; `null` в `Condition.IsMet` — тоже. Режим `All` — все сравнения, `Any` — хотя бы одно. Незаданный флаг берёт значение из каталога, а если и там нет — значение по умолчанию типа (`DialogueVariableLookup.Get`).

```csharp
var condition = new Condition()
    .Add("has_key", ComparisonOperator.IsTrue)
    .Add("visits", ComparisonOperator.GreaterOrEqual, DialogueValue.Int(2));

bool met = condition.Evaluate(DialogueGlobals.Variables, DialogueGlobals.Catalog);
```

### 9.4. Значения и флаги

`DialogueValue` — `[Serializable] struct`, `IEquatable<DialogueValue>`:

| Член | Описание |
| --- | --- |
| `DialogueValueType Type`, `bool IsNumber` | Тип (`Bool`, `Int`, `Float`, `String`) |
| `AsBool`, `AsInt`, `AsFloat`, `AsString` | Преобразования (строка в число разбирается инвариантной культурой, при неудаче 0) |
| `static Bool(bool)`, `Int(int)`, `Float(float)`, `String(string)` | Фабрики |
| `static DefaultOf(DialogueValueType)` | Значение по умолчанию типа |
| `ConvertTo(DialogueValueType)` | Преобразовать |
| `static Parse(string text)`, `Parse(string text, DialogueValueType type)` | Разбор строки (сначала bool, int, float, иначе строка) |
| `static Add(left, right)`, `static Compare(left, right)` | Сложение и сравнение (числа — численно, bool — по `AsBool`, иначе порядковое сравнение строк) |
| `==`, `!=`, `Equals`, `GetHashCode`, `ToString()` | Равенство — по `Compare == 0`, `ToString` — инвариантный текст |

```csharp
public interface IDialogueVariables
{
    event Action<string> Changed;
    bool TryGet(string name, out DialogueValue value);
    void Set(string name, DialogueValue value);
}
```

`DialogueVariables` — реализация в памяти:

| Член | Описание |
| --- | --- |
| `int Count`, `IEnumerable<string> Names` | Содержимое |
| `bool TryGet(...)`, `void Set(...)` | `Set` с тем же типом и значением события не вызывает |
| `bool Remove(string name)`, `void Clear()` | Удаление (оба вызывают `Changed` по каждому имени) |
| `event Action<string> Changed` | Имя изменённого флага |
| `string ToJson(bool prettyPrint = false)` | Снимок всех флагов (включая посещения) |
| `void FromJson(string json)` | Заменить содержимое снимком; `Changed` — по изменившимся именам |

`DialogueVariableCatalog` (`ScriptableObject`, меню `Typecraft/Dialogue Variables`) — объявление флагов:

```csharp
IReadOnlyList<DialogueVariableDefinition> Variables { get; }
bool TryGet(string name, out DialogueVariableDefinition definition)
bool Contains(string name)
void Add(DialogueVariableDefinition definition)
bool Remove(string name)
void ApplyDefaults(IDialogueVariables variables, bool overwrite = false)
void InvalidateLookup()
```

`DialogueVariableDefinition`: конструкторы `()` и `(string name, DialogueValue defaultValue, string description = null)`; `Name`, `Type`, `Default` (приводится к `Type`), `Description` — все с сеттерами.

`DialogueVariableLookup.Get(IDialogueVariables variables, DialogueVariableCatalog catalog, string name, DialogueValueType fallback)` — значение флага: из `variables`, иначе из каталога, иначе `DefaultOf(fallback)`.

`DialogueGlobals` (`static`) — общие флаги и каталог по умолчанию:

```csharp
static event Action VariablesReplaced
static IDialogueVariables Variables { get; }
static DialogueVariableCatalog Catalog { get; set; }
static void SetVariables(IDialogueVariables variables)
```

`Variables` создаётся лениво как `DialogueVariables`. Статика сбрасывается на `SubsystemRegistration`.

`DialogueVariablesSource` — абстрактный `MonoBehaviour` для своих флагов одного плеера: `abstract IDialogueVariables Variables { get; }`, `virtual DialogueVariableCatalog Catalog => null`.

Посещения нод считаются сами — флаги `visited:<id диалога>/<id ноды>` (`DialogueRunner.VisitedVariable(assetId, nodeId)`, префикс `DialogueRunner.VisitedPrefix`). Раннер считает посещения нод `Line` и `Choice`; посещение стартовой ноды записывает `DialoguePlayer.Play`.

### 9.5. `DialogueRunner`

Чистый C#, без `MonoBehaviour`: проходит граф и сообщает о шагах событиями.

```csharp
public sealed class DialogueRunner
{
    public const int DefaultMaxAutoSteps = 1000;
    public const string VisitedPrefix = "visited:";

    public DialogueRunner(IDialogueVariables variables, DialogueVariableCatalog catalog = null);

    public event Action<DialogueLine> LineStarted;
    public event Action<IReadOnlyList<DialogueChoice>> ChoicesPresented;
    public event Action<float> WaitRequested;
    public event Action<string, string> EventRaised;
    public event Action Ended;
    public event Action<DialogueAsset, DialogueNode> NodeEntered;

    public IDialogueVariables Variables { get; }
    public DialogueVariableCatalog Catalog { get; set; }
    public bool TrackVisits { get; set; }
    public int MaxAutoSteps { get; set; }
    public bool IsRunning { get; }
    public DialogueAsset Asset { get; }
    public DialogueStep Current { get; }
    public DialogueRunnerState State { get; }

    public static string VisitedVariable(string assetId, string nodeId);
    public int VisitCount(DialogueAsset asset, string nodeId);
    public void Start(DialogueAsset asset, string nodeId = null);
    public bool Continue();
    public bool Choose(int index);
    public void Stop();
    public bool Restore(DialogueRunnerState state, Func<string, DialogueAsset> resolver);
}
```

- `Start` идёт с `nodeId` или с основного старта; ноды без вывода (`Start`, `Branch`, `SetVariables`, `Event`, `Jump`) проходятся сразу, остановка — на `Line`, `Choice`, `Wait` и `End`. Нет старта, пустая связь, ссылка на несуществующую ноду — предупреждение в консоли и `Ended`. Более `MaxAutoSteps` шагов без вывода (цикл) — ошибка в консоли и `Ended`.
- `Continue()` работает на шагах `Line` и `Wait`; на `Choice` и вне разговора возвращает `false`.
- `Choose(index)` принимает `DialogueChoice.Index`; недоступный вариант возвращает `false`.
- `Choice` без единого доступного варианта завершает диалог с предупреждением.
- `EventRaised(name, argument)` и `NodeEntered` вызываются синхронно в момент прохода; обработчик может остановить диалог или запустить другой — раннер это переживает (счётчик поколений).
- Ожидание (`Wait`, `AfterSeconds`) таймером ведёт плеер; у голого раннера — вызывающий код, который по прошествии времени зовёт `Continue()`.
- `State` — `DialogueRunnerState` текущего места (`null`, если разговор не идёт).
- `Restore` возвращает разговор на сохранённую ноду без счёта повторного посещения.

```csharp
var variables = new DialogueVariables();
var runner = new DialogueRunner(variables);

runner.LineStarted += started => Debug.Log(started);
runner.ChoicesPresented += choices => runner.Choose(choices[0].Index);
runner.EventRaised += (name, argument) => Debug.Log($"{name}: {argument}");
runner.Ended += () => Debug.Log("done");

runner.Start(asset);
runner.Continue();
```

`DialogueRunnerState` — `[Serializable] sealed class` с публичными полями `string AssetId`, `string NodeId` и свойством `bool IsValid`.

### 9.6. Шаги, реплики, варианты

`DialogueStep` (`readonly struct`): `DialogueStepKind Kind`, `DialogueAsset Asset`, `string NodeId`, `DialogueLine Line`, `IReadOnlyList<DialogueChoice> Choices`, `float Seconds`. `enum DialogueStepKind { None, Line, Choice, Wait }`.

`DialogueLine` (`sealed class`): конструктор `(DialogueAsset asset, string nodeId, SpeakerProfile speaker, string emotion, Sprite portrait, string table, string key, LineAdvance advance, float seconds)`; свойства `Asset`, `NodeId`, `Speaker`, `Emotion`, `Portrait`, `Table`, `Key`, `Advance`, `Seconds`; `string ResolveTable()` (таблица с учётом `DefaultTable` диалога).

`DialogueChoice`: конструктор `(DialogueAsset asset, string nodeId, int index, string table, string key, bool available)`; свойства `Asset`, `NodeId`, `Index` (номер варианта в ноде, не в списке показанных), `Table`, `Key`, `Available`; `string ResolveTable()`.

### 9.7. Проверка диалога

`DialogueValidator` (`static`):

```csharp
static List<DialogueIssue> Validate(DialogueAsset asset, DialogueVariableCatalog catalog = null)
static void Validate(DialogueAsset asset, DialogueVariableCatalog catalog, List<DialogueIssue> issues)
```

Находит: нет старта, пустые и битые связи, недостижимые ноды, пустой ключ, пустые и необъявленные флаги, несовпадение типа флага, пустое имя события, битый переход, пустые и повторяющиеся `Id`, ноды с неизвестным типом. Проверки с таблицами (перевод, устаревшая разметка, символы шрифта, говорящий без имени, неизвестная эмоция) выполняет Editor-часть `DialogueChecks`.

`DialogueIssue` (`readonly struct`): конструктор `(DialogueIssueSeverity severity, DialogueIssueKind kind, string nodeId, string message, string subject = null, string detail = null)`; свойства `Severity`, `Kind`, `NodeId`, `Message`, `Subject`, `Detail`.

`enum DialogueIssueSeverity { Info, Warning, Error }`.

`enum DialogueIssueKind { NoStartNode, MissingNodeType, EmptyNodeId, DuplicateNodeId, EmptyLink, BrokenLink, Unreachable, EmptyKey, NoOptions, EmptyVariable, UndeclaredVariable, VariableTypeMismatch, EmptyEventName, BrokenJump, MissingKey, MissingTranslation, StaleMarkup, MissingGlyphs, SpeakerWithoutName, UnknownEmotion }`.

### 9.8. `SpeakerProfile`

`ScriptableObject`, меню `Create → Typecraft → Dialogue Speaker`.

| Член | Описание |
| --- | --- |
| `string Id` | Идентификатор; пусто — имя ассета |
| `string NameTable`, `NameKey` | Ключ имени (своя таблица; пустая — таблица реплики) |
| `string StyleId` | Стиль имени |
| `List<SpeakerPortrait> Portraits`, `Sprite DefaultPortrait` | Портреты по эмоциям и портрет по умолчанию |
| `AudioClip VoiceBlip` | Звук «бормотания» под машинку |
| `SpeakerSide Side` | `None`, `Left`, `Right` — левый или правый портрет окна |
| `string ResolveNameTable(string fallbackTable)` | Таблица имени |
| `Sprite PortraitFor(string emotion)` | Портрет эмоции (без учёта регистра) или портрет по умолчанию |

`SpeakerPortrait`: конструкторы `()` и `(string emotion, Sprite sprite)`, свойства `Emotion`, `Sprite`.

### 9.9. `DialoguePlayer`

`MonoBehaviour`, меню `Typecraft/Dialogue/Dialogue Player`. Фасад для сцены: диалог, вывод, флаги, события.

Свойства (с get/set, кроме указанных):

| Свойство | Описание |
| --- | --- |
| `Asset` | Диалог по умолчанию |
| `Presenter` | Основной вывод (`DialoguePresenter`) |
| `ChoicePresenter` | Отдельный вывод для выбора, если основной его не умеет |
| `StartNodeId` | Нода старта; пусто — основной старт |
| `PlayOnStart` | Запустить в `Start()` |
| `Once` | Не играть повторно (помнится во флагах: `visited` стартовой ноды) |
| `TimeMode` | `DialogueTimeMode { Scaled, Unscaled }` для таймеров |
| `VariablesSource` | Свои флаги; пусто — `DialogueGlobals` |
| `OnStarted`, `OnEnded` (get) | `UnityEvent` |
| `OnEvent` (get) | `DialogueEventUnityEvent : UnityEvent<string, string>` — метод с двумя `string` (динамический) |
| `Runner`, `IsPlaying`, `Current` (get) | Раннер, идёт ли разговор, текущий шаг |
| `Variables`, `Catalog` (get) | Эффективные флаги и каталог |
| `HasPlayed` (get) | Был ли пройден старт `Asset`/`StartNodeId` |
| `State` (get) | `DialogueRunnerState` или `null` |

События C#: `Started`, `Ended`, `EventRaised(string name, string argument)`, `LineStarted(DialogueLine)`, `ChoicesPresented(IReadOnlyList<DialogueChoice>)`.

Методы:

| Метод | Описание |
| --- | --- |
| `bool Play()` | Играть `Asset` со `StartNodeId` |
| `bool Play(DialogueAsset asset, string startNodeId = null)` | Играть другой диалог или с ноды. `false`, если ассета нет или `Once` и диалог уже проигран; идущий разговор останавливается |
| `bool HasPlayedFrom(DialogueAsset asset, string startNodeId = null)` | Посещался ли старт |
| `bool Advance()` | «Далее»: если вывод печатает реплику — допечатывает (`true`), иначе следующий шаг. `false` вне реплики |
| `bool Choose(int index)` | Выбор (`DialogueChoice.Index`); если вывод не показывает кнопки сам |
| `void Stop()` | Остановить |
| `bool Restore(DialogueRunnerState state, Func<string, DialogueAsset> resolver = null)` | Вернуть разговор на сохранённую ноду через выводы плеера; без `resolver` ищет только свой `Asset` |

Вызовы из кода (`nodeId` — id ноды, виден в инспекторе ноды):

```csharp
_player.Play();
_player.Play(_greeting);
_player.Play(_greeting, nodeId);
_player.Advance();
_player.Choose(choice.Index);
_player.Ended += OnDialogueEnded;
```

Поведение: ожидание (`Wait`, `AfterSeconds`, `AfterText`) ведёт таймер плеера в `Update` по `Time.time` или `Time.unscaledTime`. `OnDisable` останавливает разговор (вызывая `Ended`). Если нет вывода, поддерживающего выбор, плеер пишет предупреждение и выбирает первый доступный вариант. Раннер пересоздаётся, когда меняется ссылка на `Variables`; каталог обновляется на каждом `Play`.

```csharp
using UnityEngine;
using Velsi.Typecraft.Dialogue;

public sealed class NpcTalk : MonoBehaviour
{
    [SerializeField] private DialoguePlayer _player;
    [SerializeField] private DialogueAsset _greeting;

    private void OnEnable()
    {
        _player.EventRaised += OnEvent;
        _player.Ended += OnEnded;
    }

    private void OnDisable()
    {
        _player.EventRaised -= OnEvent;
        _player.Ended -= OnEnded;
    }

    public void Interact()
    {
        if (!_player.IsPlaying)
            _player.Play(_greeting);
        else
            _player.Advance();
    }

    private void OnEvent(string name, string argument) => Debug.Log($"{name}: {argument}");

    private void OnEnded() => Debug.Log("dialogue ended");
}
```

`DialogueTrigger` — `MonoBehaviour`, меню `Typecraft/Dialogue/Dialogue Trigger`:

| Член | Описание |
| --- | --- |
| `Player`, `Mode`, `Tag`, `Once` | Плеер, режим `DialogueTriggerMode { Manual, OnStart, OnTriggerEnter }`, тег коллайдера (пусто — любой), одноразовость |
| `bool HasFired` | Уже срабатывал |
| `bool Fire()` | Запустить `Play()` плеера. `false`, если плеер занят, одноразовый триггер уже сработал или `Play` отказал |
| `void ResetFired()` | Сбросить `HasFired` |

`OnTriggerEnter` и `OnTriggerEnter2D` работают в режиме `OnTriggerEnter`.

`DialogueInputAdvance` (`Velsi.Typecraft.Dialogue.InputSystem`, условие `TYPECRAFT_INPUTSYSTEM`, меню `Typecraft/Dialogue/Dialogue Input Advance`): `Player`, `InputActionReference Advance` (с автопереподпиской, если компонент включён); по `performed` зовёт `Player.Advance()`, пока идёт разговор. Включает действие, если оно выключено и стоит флаг «Enable Action» (выключает обратно только своё включение).

### 9.10. Выводы

`DialoguePresenter` — абстрактный `MonoBehaviour`, база любого вывода:

```csharp
public event Action LineCompleted;
public virtual bool SupportsChoices { get; }
public virtual bool IsLineComplete { get; }
public abstract void ShowLine(DialogueLine line);
public virtual void ShowChoices(IReadOnlyList<DialogueChoice> choices, Action<int> choose);
public abstract void Hide();
public virtual bool TryCompleteLine();
protected void RaiseLineCompleted();
```

Контракт: `RaiseLineCompleted()` — «реплика показана целиком», без него не сработает переход `AfterText`. Машинка: `IsLineComplete` возвращает `false`, пока печатается, `TryCompleteLine()` допечатывает и возвращает `true` — тогда первое «далее» допечатывает, второе идёт дальше. Выбор: `SupportsChoices => true` и `ShowChoices` (у недоступных `Available == false`, скрытые в список не попадают; по клику — `choose(choice.Index)`).

`TextDialoguePresenter` (абстрактный, наследник `DialoguePresenter`) — готовая база с машинкой, показом через `CanvasGroup` (или альфу текста) и автоскрытием:

| Член | Описание |
| --- | --- |
| `CanvasGroup Root`, `TypecraftText Text` | Корень и текст (заполняются в `Reset` из компонентов) |
| `float HideAfter { get; set; }` | Скрыть через N секунд после показа реплики; 0 — не скрывать |
| `bool IsVisible`, `DialogueLine CurrentLine` | Состояние |
| `virtual void ApplyLine(DialogueLine line)` | Записать реплику в текст (по умолчанию `Text.SetKey(line.ResolveTable(), line.Key)`) |
| `virtual void OnLineShowing(DialogueLine)` | Перед печатью |
| `virtual void OnLineShown()` | Реплика показана целиком |
| `virtual void OnCharacterShown(char)` | Показан символ |
| `virtual void OnHidden()` | После скрытия |
| `virtual void OnTypecraftChanged()` | Сменился язык или устройство при активной реплике |
| `virtual void SetVisible(bool)`, `Reset`, `Awake`, `OnEnable`, `OnDisable`, `OnDestroy`, `Update` | Жизненный цикл; при переопределении вызывай `base` |
| `protected LineTypewriter Typewriter`, `protected float Now` | Машинка и время (с учётом флага «Unscaled Time») |

Готовые выводы (ставятся из `GameObject → Typecraft`):

- `DialogueBoxPresenter` (`Typecraft/Dialogue/Dialogue Box Presenter`) — имя говорящего, портреты слева и справа (`SpeakerSide.Right` → правый), машинка (TMPEffects), индикатор «далее», кнопки выбора (`IReadOnlyList<Button> ChoiceButtons`), звук (`AudioSource`, интервал между «бормотаниями»). `SupportsChoices` истинно, когда заданы контейнер и префаб кнопки (кнопка с `TypecraftText` среди детей).
- `SubtitlePresenter` (`Typecraft/Dialogue/Subtitle Presenter`) — строка `{name}: реплика`; `ShowSpeakerName`, `NamePrefix` (подстановка `{name}`), скрывается через `HideAfter` (после `Reset` — 2 с).
- `WorldTextPresenter` (`Typecraft/Dialogue/World Text Presenter`) — текст в мире, разворот к камере: `FaceCamera`, `Camera` (пусто — `Camera.main`); `HideAfter` после `Reset` — 3 с.

`DialogueSpeakerText` (`static`) — получение текстов для выводов:

```csharp
static void RequestName(SpeakerProfile speaker, string fallbackTable, string fallbackStyle, Action<string> onReady)
static void RequestLine(DialogueLine line, Action<string> onReady)
static string Styled(string markup, string styleId)
```

`RequestName` отдаёт имя в стиле (`[style]имя[/style]`) или `null`, если у говорящего нет ключа имени.

`LineTypewriter` (`sealed class`, `IDisposable`) — обёртка над `TMPWriter` для одной строки:

```csharp
public LineTypewriter(TypecraftText text);
public event Action Completed;
public event Action<char> CharacterShown;
public TypecraftText Text { get; }
public bool IsActive { get; }
public bool IsComplete { get; }
public bool IsTyping { get; }
public void Begin(Action apply);
public bool TryComplete();
public void Cancel();
public void Dispose();
```

Без `TMPWriter` на объекте (или без символа `TYPECRAFT_TMPEFFECTS`, или вне Play Mode) реплика считается показанной сразу.

Свой вывод — [11.3](#113-свой-вывод-диалога).

---

## 10. Editor API

Типы Editor-сборок компилируются только в редакторе. Точки встраивания, рассчитанные на использование из своего кода: `[TypecraftMarkup]`, `TypecraftEditor`, `MarkupTextField`, `ITextEditTarget`, `ITypecraftTab`, `TypecraftWindow.ShowTab`, `ITableBackend` с `TableBackends`, `KeyUsages.RegisterSource`, `IDialogueBoardView`, а также статические помощники таблиц (`TableSync`, `TableExport`, `TableCsv`, `XlsxReader`, `GoogleSheets`, `MarkupLayers`, `Decoration`). Остальные `public`-типы — устройство окна и вкладок; они перечислены для полноты, и до версии 1.0 их сигнатуры могут меняться.

### 10.1. Пространства имён Editor-сборок

| Namespace | Содержимое |
| --- | --- |
| `Velsi.Typecraft.Editor` | `TypecraftEditor`, `MarkupTextField`, `TypecraftMarkupDrawer`, `TypecraftTextInspector`, `ProjectScan` |
| `Velsi.Typecraft.Editor.Window` | `TypecraftWindow`, `ITypecraftTab`, `EditorTab`, `LocalizationTab`, `LibraryTab`, `FontsTab` |
| `Velsi.Typecraft.Editor.Window.Targets` | `ITextEditTarget` и цели правки, `PlayModeEdits` |
| `Velsi.Typecraft.Editor.Window.Markup` | `MarkupField`, `MarkupHighlighter`, `MarkupCandidates`, `MarkupDocument`, `MarkupEdits`, `TagInspector` |
| `Velsi.Typecraft.Editor.Window.Preview` | `TextPreview`, `IconLinePreview`, `ThumbnailRenderer` |
| `Velsi.Typecraft.Editor.Window.Drawer` | `GalleryDrawer` |
| `Velsi.Typecraft.Editor.Window.Effects` | `EffectCatalog`, `EffectLibrary`, `EffectComponents`, `EffectComponentsWarning`, `EffectForm` |
| `Velsi.Typecraft.Editor.Window.Dialogs` | `FormOverlay`/`TypecraftForm`, `ExportDialog`, `KeyUsagesWindow`, `SyncSourceDialog`, `TableSyncDialog`, `TextChangeDialog` |
| `Velsi.Typecraft.Editor.Window.Layout` | `ColumnsLayout`, `SplitPane`, `TypecraftLayout` |
| `Velsi.Typecraft.Editor.Localization` | Таблицы: `ITableBackend`, `TableBackends`, синхронизация, экспорт, слои, проверки, поиск использований |
| `Velsi.Typecraft.Editor.Library` | Стили, наборы иконок, `MarkupRewriter` |
| `Velsi.Typecraft.Editor.Fonts` | Шрифты и пресеты материалов |
| `Velsi.Typecraft.Editor.Art` | Сборка наборов иконок и атласов |
| `Velsi.Typecraft.Editor.Setup` | `TypecraftSetup` |
| `Velsi.Typecraft.Dialogue.Editor` | `DialoguesTab`, `DialogueMenu`, `DialogueIdGuard` |
| `Velsi.Typecraft.Dialogue.Editor.Board` | `IDialogueBoardView`, `DialogueBoards`, `DialogueListBoard` |
| `Velsi.Typecraft.Dialogue.Editor.Inspector` | Инспекторы нод, говорящих, флагов и поля выбора |
| `Velsi.Typecraft.Dialogue.Editor.Model` | `DialogueEdits`, `DialogueChecks`, `DialogueProject`, `DialogueTexts` и др. |
| `Velsi.Typecraft.Dialogue.Editor.Playback` | `DialoguePlaybackPanel`, `DialogueIssuesPanel` |
| `Velsi.Typecraft.Dialogue.Editor.Graph` | Граф на GraphView |

Внутренние типы (`TypecraftLog`, `TypecraftContextMenu`, `DialogueAssetInspector`, `DialogueIdPostprocessor`, `DialogueKeyUsages`, `DialogueOpenAsset`, `DialogueValueDrawer`) в API не входят.

### 10.2. Окно и `TypecraftEditor`

`TypecraftEditor` (`namespace Velsi.Typecraft.Editor`, `public static`) — главный вход для встраивания:

```csharp
static void Edit(string markup, Action<string> onApply, string title = null, TMP_Text reference = null)
static void Edit(ITextEditTarget target)
static void Edit(TMP_Text text)
static void Edit(SerializedProperty property)
static void RegisterTableBackend(Func<ITextSource, ITableBackend> factory)
```

- `Edit(string, Action<string>, …)` — открыть окно на произвольном тексте. «Применить» вызывает колбэк; `reference` задаёт `TMP_Text` для размеров превью.
- `Edit(ITextEditTarget)` — открыть окно на своей цели ([10.4](#104-цели-правки)).
- `Edit(TMP_Text)` — подберёт цель: `TypecraftText` в режиме `Localized` → `LocalizedTextTarget`, в режиме `Inline` → `TypecraftTextTarget`, голый `TMP_Text` → `TmpTextTarget`.
- `Edit(SerializedProperty)` — только строковые свойства; `m_text` у `TMP_Text` и `_markup` у `TypecraftText` ведут на соответствующие цели, остальное — `PropertyTarget`.
- `RegisterTableBackend` — зарегистрировать свой `ITableBackend` для своего `ITextSource` ([10.5](#105-таблицы-локализации)).

```csharp
TypecraftEditor.Edit(node.Text, text =>
{
    Undo.RecordObject(graph, "Edit line");
    node.Text = text;
}, title: $"Реплика {node.Id}");

TypecraftEditor.Edit(serializedObject.FindProperty("_description"));
TypecraftEditor.Edit(someTmpText);
```

Окно при этом закрепляется на тексте (🔒) и не переключается на выделение в сцене.

`TypecraftWindow` (`EditorWindow`, `namespace Velsi.Typecraft.Editor.Window`, пункты `Window/Typecraft` Ctrl+Alt+T и `Tools/Typecraft/Open Window`):

```csharp
static TypecraftWindow Open()
static void Edit(ITextEditTarget target)
static void ShowTab<T>(Action<T> then = null) where T : class, ITypecraftTab
override void SaveChanges()
```

`ShowTab<T>` открывает окно на дополнительной вкладке типа `T` и передаёт её в `then` (если окно ещё строится — выполнит после построения). `SaveChanges` предлагает записать несохранённую правку слов строки локализации.

### 10.3. Поле разметки в инспекторе (`[TypecraftMarkup]`)

```csharp
using UnityEngine;
using Velsi.Typecraft;

public sealed class QuestStep : ScriptableObject
{
    [SerializeField, TypecraftMarkup] private string _description;
    [SerializeField, TypecraftMarkup(preview: false)] private System.Collections.Generic.List<string> _hints;
}
```

Поле подсвечивает разметку, подсказывает теги после `{`, `[`, `<`, показывает ошибки; 👁 включает превью, ✎ открывает текст в окне Typecraft. Правка пишется в поле с Undo. Работает и в старых IMGUI-инспекторах (упрощённый вид, `OnGUI`). Только `string` — для других типов выводится сообщение об ошибке.

Без атрибута: правый клик по любому string-полю → **Edit in Typecraft**; у `TMP_Text` — пункт `Edit in Typecraft` в меню компонента (`CONTEXT/TMP_Text`).

`TypecraftMarkupDrawer` (`PropertyDrawer` для `TypecraftMarkupAttribute`): `CreatePropertyGUI(SerializedProperty)` (UI Toolkit, строится на `MarkupTextField`), `GetPropertyHeight`, `OnGUI` (IMGUI). `TypecraftTextInspector` (`Editor` для `TypecraftText`, `CreateInspectorGUI`) — стандартный инспектор плюс плашка `EffectComponentsWarning`, если разметка требует `TMPAnimator` или `TMPWriter`, а их на объекте нет.

**`MarkupTextField`** (`namespace Velsi.Typecraft.Editor`, `VisualElement`, `INotifyValueChanged<string>`) — элемент UI Toolkit для своих редакторов: поле разметки с подсветкой, кнопкой превью 👁 и кнопкой ✎.

| Член | Описание |
| --- | --- |
| `MarkupTextField()` | Без подписи |
| `MarkupTextField(string label, bool showPreview = false)` | С подписью; превью включается сразу, если `showPreview` |
| `string label` | Подпись |
| `bool ShowPreview` | Показать или скрыть превью |
| `string value` | Значение; изменение шлёт `ChangeEvent<string>` |
| `void SetValueWithoutNotify(string newValue)` | Задать значение без события |
| `event Action EditRequested` | Нажата кнопка ✎. Без подписки кнопка сама откроет окно (`TypecraftEditor.Edit`) и вернёт текст в поле |

```csharp
var field = new MarkupTextField("Реплика", showPreview: true);
field.SetValueWithoutNotify(node.Text);
field.RegisterValueChangedCallback(evt => node.Text = evt.newValue);
field.EditRequested += () => TypecraftEditor.Edit(node.Text, text => field.value = text);
root.Add(field);
```

Поле проверяет разметку через `Typecraft.RenderPreview` с текущими языком и устройством и подсвечивает ошибки.

### 10.4. Цели правки

`ITextEditTarget` (`namespace Velsi.Typecraft.Editor.Window.Targets`) описывает, откуда окно берёт текст и куда пишет:

```csharp
public interface ITextEditTarget
{
    string Title { get; }
    string Markup { get; }
    bool IsValid { get; }
    bool CanWrite { get; }
    string ReadOnlyReason { get; }
    string FontRole { get; }
    TMP_Text Reference { get; }
    void Write(string markup);
    void SetFontRole(string role);
}
```

| Член | Назначение |
| --- | --- |
| `Title` | Заголовок во вкладке |
| `Markup` | Исходная разметка |
| `IsValid` | Цель ещё существует |
| `CanWrite`, `ReadOnlyReason` | Можно ли писать; причина запрета |
| `FontRole` / `SetFontRole` | Роль шрифта превью и её смена |
| `Reference` | `TMP_Text` для размеров превью (может быть `null`) |
| `Write(markup)` | Записать результат («Применить») |

Готовые реализации: `CallbackTarget(string markup, Action<string> onApply, string title, TMP_Text reference)`, `PropertyTarget(SerializedProperty)`, `TmpTextTarget(TMP_Text)`, `TypecraftTextTarget(TypecraftText)` (+ `Component`), `LocalizedTextTarget(TypecraftText text, Func<string> locale)` (+ `TableText`, `Subject`, `bool ChangesWords(string markup)`, `TransferResult RestoreWords(string markup)`, `IEnumerable<string> AllMarkups()`), `ScratchTarget` (пустая рабочая область).

`PlayModeEdits` (`static`): `bool KeepAfterPlay { get; set; }` (EditorPrefs, по умолчанию включено), `static void Record(TMP_Text text, string markup)` — запомнить правку, сделанную в Play Mode, и применить после выхода из него (по `GlobalObjectId`; объекты, созданные в рантайме, не переживают выход).

Свой объект правки (узел графа, запись базы данных):

```csharp
using TMPro;
using UnityEngine;
using Velsi.Typecraft.Editor;
using Velsi.Typecraft.Editor.Window.Targets;

public sealed class QuestNoteTarget : ITextEditTarget
{
    private readonly QuestNote _note;

    public QuestNoteTarget(QuestNote note) => _note = note;

    public string Title => _note.name;
    public string Markup => _note.Text;
    public bool IsValid => _note != null;
    public bool CanWrite => true;
    public string ReadOnlyReason => null;
    public string FontRole => null;
    public TMP_Text Reference => null;

    public void Write(string markup)
    {
        UnityEditor.Undo.RecordObject(_note, "Edit note");
        _note.Text = markup;
        UnityEditor.EditorUtility.SetDirty(_note);
    }

    public void SetFontRole(string role)
    {
    }
}

public sealed class QuestNote : ScriptableObject
{
    public string Text;
}
```

Открыть окно на этой цели:

```csharp
TypecraftEditor.Edit(new QuestNoteTarget(note));
```

`FontRole == null` окно трактует как «роль не задана» и берёт `TypecraftSettings.DefaultFontRole`; `SetFontRole` вызывается, когда пользователь выбирает роль в галерее.

### 10.5. Таблицы локализации

`namespace Velsi.Typecraft.Editor.Localization`.

`ITableBackend` — всё, что вкладке «Локализация», синхронизации, экспорту и проверкам нужно знать о хранилище строк. Поэтому они одинаково работают для встроенных таблиц, Unity Localization и своего бэкенда.

```csharp
public interface ITableBackend
{
    string Name { get; }
    IReadOnlyList<string> Locales { get; }
    string BaseLocale { get; }
    IReadOnlyList<string> Tables { get; }
    IReadOnlyList<string> Keys(string table);
    string Get(string table, string key, string locale);
    string Comment(string table, string key);
    void Set(string table, string key, string locale, string text);
    void SetComment(string table, string key, string comment);
    bool AddKey(string table, string key);
    bool RemoveKey(string table, string key);
    bool RenameKey(string table, string key, string newKey);
    bool AddLocale(string locale);
    string CreateTable(string name);
    UnityEngine.Object Asset(string table);
}
```

| Член | Контракт |
| --- | --- |
| `Name` | Название бэкенда в интерфейсе |
| `Locales`, `BaseLocale`, `Tables` | Языки, основной язык, идентификаторы таблиц |
| `Keys(table)` | Ключи таблицы |
| `Get(table, key, locale)` | Строка таблицы (без слоя разметки) |
| `Comment`/`SetComment` | Комментарий переводчику |
| `Set` | Записать строку (дальше о слое позаботится вызывающий) |
| `AddKey`, `RemoveKey`, `RenameKey`, `AddLocale` | Операции над таблицей; `false` — не удалось |
| `CreateTable(name)` | Создать таблицу и вернуть её id |
| `Asset(table)` | Ассет таблицы (для навигации и выбора папки слоя) |

`TableBackends` (`static`): `void Register(Func<ITextSource, ITableBackend> factory)`, `ITableBackend Current` — бэкенд источника из `TypecraftSettings` (сначала зарегистрированные фабрики, затем встроенный для `TableTextSource` и `UnityLocalizationBackend` для адаптера; иначе `null`). Реализации: `BuiltInTableBackend(TableTextSource source)` и `UnityLocalizationBackend` (при `TYPECRAFT_LOCALIZATION`).

`MarkupLayers` (`static`) — слой разметки с точки зрения редактора:

| Метод | Описание |
| --- | --- |
| `MarkupLayer Find(string table)` | Слой таблицы |
| `MarkupLayerEntry Entry(string table, string key, string locale)` | Запись с непустой разметкой |
| `LayerState State(ITableBackend backend, string table, string key, string locale)` | `None`, `Applied`, `Stale` (снимок не совпадает со строкой таблицы) |
| `string Display(ITableBackend backend, string table, string key, string locale)` | Текст для показа (слой, если применён, иначе таблица) |
| `void Write(ITableBackend, table, key, locale, string markup)` | Записать слова в таблицу (если изменились) и оформление в слой |
| `bool Set(ITableBackend, table, key, locale, string markup)` | Только слой; `false`, если слова разметки не совпадают со строкой таблицы. Разметка без оформления удаляет запись |
| `void Remove(table, key, locale)`, `RemoveKey(table, key)`, `RenameKey(table, key, newKey)` | Правка слоя (с Undo) |
| `MarkupLayer GetOrCreate(ITableBackend backend, string table)` | Найти слой или создать `MarkupLayer_<таблица>.asset` рядом с таблицей и внести в настройки |

`Decoration` (`static`): `Strip(markup)` — текст без оформления (инвариант слоя: `Strip(markup) == source`), `Has(markup)` — есть ли оформление, `Tags(markup)`, `Diff(before, after)` → `List<DiffPart>` (`DiffPart(DiffKind kind, string text)`, `DiffKind { Same, Removed, Added }`), `Transfer(markup, text)` → `TransferResult` (перенос оформления на новый текст по совпадающим словам: `Markup`, `Approximate`, `Dropped`, `Broken`, `Exact`, `Describe()`), `RichDiff(IEnumerable<DiffPart>, bool before)` — подсветка различий для показа.

`MarkupLayerMigration` — перенос оформления из ячеек таблиц в слои (меню `Tools/Typecraft/Move Markup To Layers`): `static MarkupLayerMigration Plan(ITableBackend backend)`, `Cells` (`IReadOnlyList<Cell>` — `Cell(table, key, locale, markup, text)`), `string Describe()`, `void Apply(Func<string, MarkupLayer> layerFor)`.

`LocalizationChecks` (`static`): `RowReport Check(ITableBackend backend, string table, string key)`, `string Signature(string markup)` (набор тегов для сравнения языков), `string MissingCharacters(string locale, string markup)` (символы, которых нет в шрифте языка). `RowReport` — `Cells` (`Dictionary<string, CellIssue>`), `Messages`, `All`, индексатор по языку. `[Flags] enum CellIssue { None = 0, Missing = 1, Overflow = 2, MissingGlyph = 4, MarkupMismatch = 8, StaleMarkup = 16 }`.

`SceneTexts` (`static`) — `TypecraftText` в открытых сценах: `Localized(table, key)`, `Uses(TypecraftText text, string table, string key)` (пустая таблица считается совпадением, если ключ первым находится в этой таблице), `ResolvesTo(table, key)`, `RefreshAll()`, `Batch()` → `IDisposable` (отложить обновление текстов до конца блока; вложенные блоки допустимы).

`TextPrompt` (`EditorWindow`): `static string Show(string title, string message, string initial)` — модальный ввод строки (`ShowModalUtility`).

Регистрация своего бэкенда:

```csharp
using UnityEditor;
using Velsi.Typecraft.Editor;

public static class MyBackendRegistration
{
    [InitializeOnLoadMethod]
    private static void Register() =>
        TypecraftEditor.RegisterTableBackend(source => source is MyTextSource my ? new MyBackend(my) : null);
}
```

### 10.6. Синхронизация, экспорт и поиск использований

Всё работает через `ITableBackend`, поэтому одинаково для встроенных таблиц, Unity Localization и своего бэкенда. Только редактор.

```csharp
using Velsi.Typecraft.Editor.Localization;

ITableBackend backend = TableBackends.Current;

SheetImport data = TableCsv.Read("Assets/Strings.csv");
data = SheetImport.FromRows(XlsxReader.Read("Assets/Strings.xlsx", "Strings"));

TableSyncPlan plan = TableSync.Plan(backend, "Main", data, "my source");
TableSync.Apply(backend, plan, new TableSyncOptions { RemoveMissing = false, ClearEmpty = false });
```

`SheetImport` — разобранная таблица: `const string KeyColumn = "key"`, `CommentColumn = "comment"`; `Locales`, `Rows` (`SheetImport.Row`: `Key`, `Comment`, `Line`, `Values`), `IgnoredColumns`, `DuplicateKeys`, `HasComments`; `static SheetImport FromRows(IReadOnlyList<IReadOnlyList<string>> rows)` принимает строки ячеек из любого места: первая непустая строка — заголовки `key` / `comment` / коды языков.

Чтение файлов: `TableCsv.Read(string path)`, `Read(string path, char delimiter)`, `Parse(string text, char delimiter)`, `char DelimiterFor(string path)` (по расширению); `XlsxReader.SheetNames(string path)`, `Read(string path, string sheet = null)` (`null` — первый лист), `ColumnIndex(string reference)`, `RowIndex(string reference)`. Читается без сторонних библиотек; старый `.xls` не поддерживается.

`TableSync` (`static`): `TableSyncPlan Plan(ITableBackend backend, string table, SheetImport data, string origin = null)`, `void Apply(ITableBackend backend, TableSyncPlan plan, TableSyncOptions options)` (одна группа Undo, обновление текстов в сцене один раз в конце), `string Summary(TableSyncPlan, TableSyncOptions)`.

`TableSyncOptions`: `AddLocales = true`, `ClearEmpty` (пустая ячейка источника стирает перевод), `RemoveMissing` (удалять ключи, которых нет в источнике) — поля.

`TableSyncPlan` — отчёт без записи:

| Член | Описание |
| --- | --- |
| `Table`, `Origin` | Таблица и подпись источника |
| `NewLocales` | Языки, которых нет в проекте |
| `AddedKeys` | Новые ключи (`List<SheetImport.Row>`) |
| `Changes` | `List<CellChange>`: `Key`, `Locale`, `Before`, `After`, `NewLocale`, `LosesMarkup` (у строки есть применённая разметка, после записи она станет ⚠), `Clears` |
| `Comments` | `List<CommentChange>`: `Key`, `Before`, `After`, `Clears` |
| `MissingKeys`, `MissingKeysWithMarkup` | Ключи, которых нет в источнике (и среди них с разметкой) |
| `DuplicateKeys`, `IgnoredColumns` | Предупреждения разбора |
| `IsEmpty` | Нечего применять |
| `EffectiveChanges(options)`, `EffectiveComments(options)`, `AddedValues(row, options)`, `int MarkupLost(options)` | Что реально будет записано с учётом опций |

Источники и их хранение: `TableSyncSource(string table, TableSyncKind kind, string path, string sheet, string url)` (`enum TableSyncKind { Csv, Tsv, Xlsx, GoogleSheets }`, свойства `Table`, `Kind`, `Path`, `Sheet`, `Url`, `IsFile`, `FullPath`, `Describe()`, `SheetImport ReadFile()`, `static TableSyncKind KindFor(string path)`); `TableSyncSources` (`ScriptableSingleton`, файл `ProjectSettings/TypecraftTableSync.asset`): `static TableSyncSource Find(string table)`, `static void Set(TableSyncSource source)`, `static void Remove(string table)`. Пути внутри проекта пишутся относительными.

Google Sheets (по открытой ссылке, без ключей и OAuth):

```csharp
using UnityEngine;
using Velsi.Typecraft.Editor.Localization;

if (GoogleSheets.TryParse(link, out GoogleSheetLink sheet))
{
    GoogleSheetsDownload download = GoogleSheetsDownload.Start(
        sheet,
        csv => Debug.Log(csv.Length),
        error => Debug.LogWarning(error));
}
```

- `GoogleSheets.TryParse(string link, out GoogleSheetLink parsed)` — обычная ссылка (`…/d/<SHEET_ID>/edit#gid=<GID>`, `?gid=`, `/u/0/d/…`, опубликованные `/d/e/…`). `GoogleSheetLink` — `Id`, `Gid`, `Published`, `ExportUrl`, конструктор `(string id, string gid, bool published)`.
- `GoogleSheetsDownload.Start(GoogleSheetLink link, Action<string> completed, Action<string> failed)` — загрузка CSV в фоне; `IsRunning`, `DownloadedBytes`, `Progress`, `Cancel()`.
- `GoogleSheets.Interpret(long status, string contentType, string body)` — превращает HTML логина и 401/403/404 в понятную ошибку; `const string ClosedMessage`. `TableSyncException : Exception` — исключение разбора и скачивания.

Экспорт:

```csharp
TableExport.Export(backend, backend.Tables, TableExportFormat.Xlsx, "Strings.xlsx");

ExportTable table = TableExport.Collect(backend, "Main");
XlsxWriter.Write("Strings.xlsx", new[] { table });
TableJson.Write("Strings.json", new[] { table });
TableCsv.Write(table, "Main.csv", ',');
```

- `enum TableExportFormat { Xlsx, Csv, Tsv, Json }`; `TableExport.Extension(format)`, `WritesFolder(format, tables)` (CSV/TSV при нескольких таблицах пишут папку), `Collect(backend, table)` → `ExportTable`, `Export(backend, IReadOnlyList<string> tables, format, path)` → `List<string>` (записанные пути).
- `ExportTable`: `Id`, `Locales`, `Rows`, `Header`, `Cells(row)`. `ExportRow`: `Key`, `Comment`, `StaleMarkup`, `Values`.
- `XlsxWriter.Write(path, tables)`, `SheetNames(ids)`, `Reference(column, row)`; `TableJson.Write(path, tables)`, `Serialize(tables)`, `Quote(text)`; `TableCsv.Export(backend, table, path[, delimiter])`, `Write(ExportTable table, string path, char delimiter)`.
- Формат JSON: `{ "tables": [ { "id", "locales", "entries": [ { "key", "comment", "values" } ] } ] }`, UTF-8 без BOM. Оформление из слоя разметки в экспорт не попадает.

Поиск использований ключа:

```csharp
List<KeyUsage> usages = KeyUsages.Find("Main", "ui.door.locked");
if (usages != null && usages.Count > 0)
    KeyUsages.Reveal(usages[0]);
```

- `KeyUsages.Find(string table, string key)` → `List<KeyUsage>` (`null` — пользователь отменил прогресс): `TypecraftText` в режиме Localized в открытых и закрытых сценах и префабах `Assets/`, строка `"key"` в `.cs` из `Assets/`, плюс свои источники.
- `KeyUsages.Reveal(KeyUsage usage)` — переход: объект, сцена, префаб или строка скрипта.
- `KeyUsages.RegisterSource(Func<string, string, IEnumerable<KeyUsage>> source)` — добавить свой поиск (аргументы — таблица и ключ). Исключения источника логируются и не прерывают поиск.
- `KeyUsage`: конструкторы `(KeyUsageKind kind, string assetPath, string objectPath, int[] siblings, int line, GameObject live)` и `(string assetPath, string title, string location, Action reveal)` (свой вид использования); `Kind` (`KeyUsageKind { Scene, Prefab, Script, Asset }`), `AssetPath`, `ObjectPath`, `Siblings`, `Line`, `Live`, `KindTitle`, `Location`.

`ProjectScan` (`static`) — общий обход проекта; на нём же работает переписывание разметки при переименовании стилей и иконок: `const string ProjectRoot = "Assets"`, `LoadedScenes()`, `OpenPrefabRoot()`, `OpenPrefabPath()`, `PrefabPaths()`, `ClosedScenePaths()`, `InPrefab<T>(path, Func<GameObject, T>)`, `InClosedScene<T>(path, Func<Scene, T>)`, `MayContain(assetPath, Regex)` (YAML-префильтр), `AssetPaths(filter)`, `IsInProject(path)`, `HierarchyPath(Transform, Transform top = null)`, `SiblingPath(...)`, `Find(IReadOnlyList<GameObject> roots, int[] siblings, string hierarchyPath)`.

Диалоги-окна: `ExportDialog.Show(ITableBackend backend, string table)`, `KeyUsagesWindow.Show(string table, string key, IReadOnlyList<KeyUsage> usages)`, `SyncSourceDialog.Show(string table)` → `SyncSourceChoice`, `TableSyncDialog.Show(TableSyncPlan plan)` → `TableSyncOptions` (или `null` при отказе), `TextChangeDialog.Show(string subject, string before, string after, string restoreRefusal)` → `TextChangeChoice`.

### 10.7. Разметка в редакторе

- `MarkupField` (`VisualElement`, `Window.Markup`) — многострочное поле разметки с подсветкой и автодополнением: конструктор `(Func<char, IEnumerable<string>> candidates)`, события `MarkupChanged(string)`, `SelectionChanged(TextRange)`, `ApplyRequested`, свойства `Value`, `Selection`, `Caret`, методы `SetValue(string value, bool notify)`, `SetDiagnostics(IReadOnlyList<MarkupDiagnostic>)`, `Select(TextRange)`, `SetReadOnly(bool)`.
- `MarkupHighlighter` — `static string ToRichText(string markup, IReadOnlyList<MarkupDiagnostic> diagnostics)` и константы цветов (`InputColor`, `IconColor`, `StyleColor`, `TmpColor`, `CommandColor`, `ErrorColor`, `EscapeColor`).
- `MarkupCandidates` — подсказки автодополнения: `For(char opener)`, `AllIconSets()`.
- `MarkupEdits`, `TextRange` (`readonly struct`: `TextRange(int start, int end)`, `Start`, `End`, `Length`, `IsEmpty`) — правки по диапазону: `Snap(markup, range)`, `Wrap(markup, range, open, close, out result)`, `Insert(markup, index, text, out caret)`, `Replace(markup, range, text, out result)`, `WordAt(markup, range)`.
- `MarkupDocument` (`Parse(markup)`), `MarkupModule` (`Slot`, `Open`, `Close`, `Label`, `IsStyle`, `TryParse`, `IsInlineTag`), `ModuleCoverage { None, Partial, Full }` — модель тегов вокруг выделения (используется кнопками стиля и «Стиль из выделения»): `Coverage`, `ModulesIn`, `Toggle`, `RemoveSlot`, `Clear`.
- `TagInspector` (`VisualElement`) — правка параметров тега под курсором: `TryFindTag(markup, caret, out start)`, `Edit(markup, start)`, `Hide()`, событие `Changed(int, int, string)`.
- `TextPreview` (`IDisposable`) — живое превью с машинкой: события `SelectionChanged`, `SelectionCleared`, `CommandClicked`; свойства `Element` (`IMGUIContainer`), `Animate`, `CharactersPerSecond`, `IsTypewriting`, `ShowMarkers`, `SelectionBounds`, `VisibleText`, `ExtraEffectTag`; методы `Configure(TMP_Text reference, TMP_FontAsset font, Material material)`, `SetMarkup(string markup, string locale, DeviceFamily device, List<MarkupDiagnostic> diagnostics)`, `RestartTypewriter()`, `ShowAll()`, `HighlightMarkupRange(TextRange)`, `OverrideDatabase(TMPAnimationDatabase)`, `ReplayVisibility(bool)`, `bool Tick(float deltaTime)`.
- `IconLinePreview` — превью иконки в строке: `Element`, `Show(TMP_SpriteAsset, string glyphName, bool tint)`, `Refresh()`. `ThumbnailRenderer` — миниатюры `Render(string key, string richText, TMP_FontAsset font, Material material, Vector2Int size)`, `Clear()`.
- `GalleryDrawer` — галерея стилей, иконок и эффектов: события `WrapRequested`, `InsertRequested`, `FontRoleRequested`, `CreateStyleRequested`, `CreateMaterialRequested`, `CreateEffectRequested`; `Refresh(string locale, DeviceFamily device)`, `ShowApplied(Func<string, ModuleCoverage>)`.

Простейшая встройка превью в свой UI:

```csharp
using UnityEngine.UIElements;
using Velsi.Typecraft;
using Velsi.Typecraft.Editor.Window.Preview;

public static class PreviewExample
{
    public static TextPreview AddPreview(VisualElement root)
    {
        var preview = new TextPreview { Animate = false };
        preview.Configure(null, null, null);
        preview.SetMarkup("[warn]Hello[/warn] {key:Interact}", Typecraft.Locale, Typecraft.Device, null);
        root.Add(preview.Element);
        return preview;
    }
}
```

(`MarkupTextField` с `showPreview: true` делает ровно это сам.)

### 10.8. Шрифты, библиотека и арт

Шрифты (`Editor.Fonts`):

- `FontCharset` — `Collect(string locale, string extra)` (набор символов: ASCII, расширенная пунктуация, для CJK пунктуация CJK, плюс всё из строк таблиц языка), `Used(string locale)` (только из строк), `Missing(TMP_FontAsset font, string characters)`.
- `StaticFontBuilder` — статический шрифт под набор символов (для CJK): `DefaultPath(Font source, string locale)`, `Create(Font source, string locale, TextAsset extra, string assetPath)` → `Result` (`Font`, `Characters`, `NotInSource`), `IsBuilt(font)`, `LocaleOf(font)`, `Rebuild(font)`. Рецепт (исходный шрифт, язык, дополнительные символы) сохраняется для `Rebuild`.
- `MaterialPresets` — пресеты SDF-материалов: `const string FolderName = "Materials"`, `Folder(FontProfile)`, `Variants(FontRole)`, `MaterialName(font, roleId, presetId)`, `Validate(fonts, roleId, presetId)`, `CreatePreviewMaterial(font, settings)`, `Create(fonts, roleId, presetId, settings)` (копия материала для каждого шрифта роли), `PresetIds(fonts, roleId)`, `IsOwned(fonts, material)`, `Remove(fonts, roleId, presetId)`, `SyncAtlases(fonts)`. `SdfMaterialSettings` — поля `FaceDilate`, `Softness`, `OutlineColor`, `OutlineWidth`, `Underlay*`, `Glow*`, `const string FullShader`, `From(Material)`, `SupportsGlow(Material)`, `ApplyTo(Material)`. `MaterialPresetForm` — форма пресета ([10.10](#1010-вкладки-формы-раскладка)).

Библиотека (`Editor.Library`):

- `StyleEdits` — `ValidateId(StyleLibrary, string id)`, `Add(StyleLibrary, SerializedProperty draft)`, `Remove(StyleLibrary, string id)`, `IndexOf(SerializedProperty styles, string id)`.
- `StyleFromText` — `Prefill(SerializedProperty style, IEnumerable<MarkupModule> modules, FontProfile fonts)`, `Create(StyleLibrary, FontProfile, TMP_Text)` — стиль из `TMP_Text`.
- `StylePanel` (`StylePanel(StyleLibrary library, int index)`, `static readonly Regex ValidId = ^[A-Za-z][\w.-]*$`, событие `Selected`, `Tick`, `Dispose`), `StyleForm`, `StyleFields` — панель и форма стилей.
- `IconSetPanel(IconSet set)` — панель набора иконок; `IconCheckDialog.Show(Texture2D, string fileName, IReadOnlyList<string> problems)` → `IconCheckChoice`.
- `MarkupRewriter` — переписывание тегов в таблицах, слоях, открытых и закрытых сценах, префабах, полях `[TypecraftMarkup]` и правилах автотегов при переименовании стиля или иконки: `Prepare(Regex pattern, string replacement)`, `StylePattern(string id)`, `StyleReplacement(string id)`, `IconPattern(string setId, string key, bool plain)`, `Describe()`, `Apply(string undoName)`, `Cells`, `LayerEntries`, `AutoTags`, `IsEmpty`, `HasIrreversible`, `UndoNote`. Префабы и закрытые сцены сохраняются сразу, без Undo.

Арт (`Editor.Art`):

- `IconSetBuilder.Build(IconSet set)` → `bool` — собрать атлас и `TMP_SpriteAsset` из PNG папки набора.
- `SpriteAtlasBuilder.Build(IReadOnlyList<string> sourceFiles, string atlasAssetPath, int padding, int maxAtlasSize, FilterMode filterMode)` → `Dictionary<string, Sprite>`.
- `TmpSpriteAssetSync` — `CreateSpriteAsset(path)`, `CreateTransient(Texture2D, string glyphName)`, `DestroyTransient`, `SetPlacement(target, glyphName, Vector2 offset, float scale)`, `ApplyPlacement(IconSet)`, `AssignSpriteSheet`, `Sync`.
- `IconImageCheck` — проверка картинок (`StandardSize = 128`, `StandardMargin = 12`, `Load`, `PrevailingSize`, `Inspect`, `Report`); `IconKeyNaming` — `KeysFor(IReadOnlyCollection<string> glyphNames)`, `ToKey(string raw)`; `BasePromptSets` — `IsBaseSet(IconSet)`, `RebuildAll()` (меню `Tools/Typecraft/Rebuild Base Prompt Sets`; контекстное меню `IconSet → Rebuild Atlas`).

Начальная настройка (`Editor.Setup.TypecraftSetup`): `CreateStringTables()` (меню `Tools/Typecraft/Create String Tables`, создаёт `TableTextSource` + `StringTable_Main` и назначает источником), `CreateDefaults(string configFolder, string resourcesFolder)` → `TypecraftSettings` (меню `Tools/Typecraft/Create Project Settings`; если настройки уже есть — возвращает их), `DefaultFamilies()` (при Input System).

### 10.9. Эффекты в окне

`namespace Velsi.Typecraft.Editor.Window.Effects`; нужен `TYPECRAFT_TMPEFFECTS` для работы с TMPEffects.

- `EffectCatalog` — каталог эффектов: `Entries` (`IReadOnlyList<EffectEntry>`), `IsEffectTag(string)`, `IsWriterTag(string)`, `DefaultAnimationDatabase`, `DefaultCommandDatabase`, `Invalidate()`. `EffectEntry(EffectKind kind, string name)` — `Kind`, `Name`, `TagName`, `Wraps`, `Open`, `Close`. `enum EffectKind { Basic, Show, Hide, Command }`.
- `EffectLibrary` — свои эффекты и база анимаций проекта: `const DatabaseName = "TypecraftAnimationDatabase"`, `FolderName = "Effects"`, `Available`, `Folder(TypecraftSettings)`, `ValidateName(kind, name)`, `TagOf(kind, name)`, `Project`, `Current`, `IsCustom`, `AssetOf`, `Bases(kind)` (`EffectBase(string label, Object source, Type type)`), `CreateDraft(EffectBase)`, `EnsureProject()`, `Save(kind, name, draft)`, `Remove(kind, name)`, `CreatePreviewDatabase`, `DestroyPreviewDatabase`, `Configure(TMPAnimator)`, `UsesDefault(TMPAnimator)`.
- `EffectComponents` — `Ensure(TMP_Text text, string markup)` / `Ensure(TMP_Text, IEnumerable<string>)` (добавляет `TMPAnimator`/`TMPWriter`, если разметка их требует), `Missing(text, markups, out bool writer, out bool animator)`, `Markups(TypecraftText)`. `EffectComponentsWarning` (`VisualElement`, событие `Added`, `Refresh(TMP_Text[, IEnumerable<string>])`) — плашка «нет компонента».
- `EffectForm` (`TypecraftForm`) — форма нового эффекта.

### 10.10. Вкладки, формы, раскладка

- `ITypecraftTab` — дополнительная вкладка окна (см. [11.4](#114-своя-вкладка-окна)). Встроенные вкладки: `EditorTab`, `LocalizationTab`, `LibraryTab`, `FontsTab` (`VisualElement`, у каждой `Reload`/`Tick`/`Dispose` и свои методы; публичные члены `EditorTab`: `Overlay`, `Target`, `SetTarget(ITextEditTarget)`, `Pin(ITextEditTarget)`, `FollowSelection()`, `HasPendingWordChange`, `ResolvePendingWordChange()`, `OpenStyleForm(bool fromSelection)`, `OpenMaterialForm(string roleId)`, `OpenEffectForm()`, `Tick`, `RefreshAll`, `Dispose`).
- `FormOverlay` (`VisualElement`) — слой форм поверх вкладки: `IsOpen`, `Form`, `Show(TypecraftForm)`, `Close()`, `Tick`. `TypecraftForm` — абстрактная форма: `protected TypecraftForm(string title)`, событие `Closed`, `Tick`, `Dispose`, `bool Submit()`, защищённые `Fields`, `Side`, `SaveButton`, `abstract bool Save()`, `SetError(string)`, `Close()`, `static Section(string title)`. Наследники: `StyleForm`, `MaterialPresetForm`, `EffectForm`.
- `ColumnsLayout`, `SplitPane`, `TypecraftLayout` — помощники адаптивной раскладки (узкая ширина — `TypecraftLayout.NarrowWidth = 700f`): `ColumnsLayout(string key, VisualElement list, VisualElement center, VisualElement side, float listWidth, float sideWidth, float sideHeight, float narrowWidth)` (`MenuButton`, `IsNarrow`, `ListShown`, `ShowList()`, `HideList()`); `SplitPane(string key, VisualElement first, VisualElement second, bool fixedFirst, float defaultWidth, float defaultHeight, float minFixed, float minFlex)` (`Vertical`, `NarrowWidth`, `FixedPane`, `ReleaseFixed()`, `RestoreFixed()`, `SetFixedVisible(bool)`, `SetFixedAuto(bool)`); `TypecraftLayout.VerticalScroll(string className = null)`, `FitScroll`, `Responsive`, `Dialog`, `ButtonRow`, `TruncatedTooltips`, константа `FitClass`.

### 10.11. Editor диалогов

Namespace'ы `Velsi.Typecraft.Dialogue.Editor.*` ([10.1](#101-пространства-имён-editor-сборок)).

**Вкладка.** `DialoguesTab` (`VisualElement`, `ITypecraftTab`, `IDialogueBoardHost`, `IDialogueEditorContext`, `IDialoguePlaybackHost`; `Title` = «☰ Диалоги», `Order` = 100). Открыть из кода:

```csharp
using Velsi.Typecraft.Dialogue.Editor;

DialoguesTab.Open(asset);
DialoguesTab.Open(asset, nodeId);
DialoguesTab.OpenSpeaker(speaker);
```

Публичные члены: `Asset`, `Locale`, `Root`, `Board`, `SelectedNodeId`, `Issues`, `InspectorElement`, `Playback`, `Problems`, `DockOpen`, `Page` (`DockPage { Playback, Problems }`), `CurrentNodeId`; методы `Select(asset, nodeId)`, `SelectNode(id[, bool frame])`, `SelectSpeaker`, `SelectFlag(catalog, name)`, `Edit(Object target, string undoName, Action change, bool rebuildInspector = false)`, `AddNode(Type, Vector2? position = null)`, `RemoveNode(id)`, `CreateDialogue(path)`, `CreateSpeaker(name)`, `CreateFlag()`, `ShowCurrent`, `Reveal`, `OpenDock(DockPage)`, `PlayFromStart()`, `ShowProblems()`, `RenameFlag(catalog, from, to, bool rewriteDialogues)`, `OnNodeSelected`, `OnBoardEdited`, `OnShow`, `Tick`, `OnTypecraftChanged`, `Dispose`.

`DialogueMenu` (`static`): `CreateDialogueBox(GameObject context)`, `CreateSubtitles(GameObject context)`, `CreateWorldText(GameObject context)` — меню `GameObject/Typecraft/…`. `DialogueIdGuard.FixDuplicates()` → `int` — перевыдаёт `Id` у копий диалогов (вызывается при импорте), `DialogueIdGuard.Find(table, key)` — источник для `KeyUsages` (использования ключа в диалогах).

**Виды доски.** `IDialogueBoardView : IDisposable`:

```csharp
VisualElement Element { get; }
void Bind(IDialogueBoardHost host);
void Show(DialogueAsset asset);
void Refresh();
void RefreshNode(string nodeId);
void SelectNode(string nodeId, bool frame);
void SetIssues(IReadOnlyList<DialogueIssue> issues);
void SetCurrent(string nodeId);
Vector2 SuggestPosition();
```

`IDialogueBoardHost`: `string Locale`, `void OnNodeSelected(string nodeId)`, `void OnBoardEdited(IReadOnlyList<string> selectNodes = null)`. Вид с публичным конструктором без параметров находится автоматически (`TypeCache`); `[DialogueBoardPriority(int priority, string title)]` задаёт заголовок и порядок (больше — раньше): граф — 100 («Граф»), список — 0 («Список»). `DialogueBoards`: `Types`, `Create(Type preferred = null)` (первый успешно созданный; запасной — `DialogueListBoard`), `Title(Type)`.

**Контекст инспекторов.** `IDialogueEditorContext`: `Asset`, `Locale`, `Root`, `Edit(Object target, string undoName, Action change, bool rebuildInspector = false)`, `SelectNode(string)`, `RemoveNode(string)`.

**Инспекторы и поля.** `NodeInspector(IDialogueEditorContext context, DialogueNode node, IReadOnlyList<DialogueIssue> issues)` (+ `static PickField EmotionField(...)`), `SpeakerInspector(context, SpeakerProfile speaker, Action delete)`, `FlagInspector(context, DialogueVariableCatalog catalog, DialogueVariableDefinition definition, Action delete, Func<string, bool> rename)`, `ConditionEditor(context, Condition condition, string title = "Условие")` (+ `VariableItems()`, `TypeChooser(...)`), `DialogueValueField(string label)` (событие `Changed`, `Value`, `SetValue`), `KeyField(string label, IDialogueEditorContext context, Func<string> table, Func<string> prefix, bool preview)` (`Changed`, `Value`, `NewText`, `Create()`, `Refresh`, `Tick`), `PickField(string label, string title, Func<IEnumerable<PickItem>> items)` (+ `PickItem(string value, string label, string group = null)`, `PickDropdown.Show(Rect, string title, IEnumerable<PickItem>, Action<string>)`).

**Модель и проверки.**

- `DialogueEdits` (`static`) — правки с Undo: событие `Edited`, `Change(Object target, string name, Action change)`, `Commit(Object)`, `Create(Type)`, `AddNode(asset, type, position, linkFrom, linkPort)`, `RemoveNodes(asset, ids)`, `RemoveWithoutUndo`, `Link(asset, nodeId, port, target)`, `GetLink(node, port)`, `SetLink(node, port, target)`, `Move(asset, moves)`, `SetStart(asset, nodeId)`, `Copy(asset, ids)`, `CanPaste(data)`, `Paste(asset, data, offset)`, `FlowOrder(asset, out depth)`.
- `DialogueChecks` — `Run(DialogueAsset asset)`, `Run(asset, catalog, List<DialogueIssue> issues)`, `Errors(issues)`, `Warnings(issues)`. К проверкам рантайма (`DialogueValidator`) добавляет проверки таблиц: ключ есть, перевод есть, разметка устарела, символы есть в шрифте, у говорящего есть имя, эмоция известна.
- `DialogueProject` — поиск и создание ассетов: `DefaultFolder = "Assets/Game/Dialogues"`, `Folder { get; set; }` (хранится в `EditorUserSettings`), `FindAll<T>()`, `Dialogues()`, `Speakers()`, `Catalogs()`, `Variables()`, `TryVariable(name, out definition)`, `MergedCatalog()`, `FolderOf`, `EnsureFolder`, `CreateDialogue(path, defaultTable)`, `CreateSpeaker(folder, name)`, `CatalogForNewFlag(folder)`, `UniqueVariableName(prefix)`.
- `DialogueTexts` — тексты для показа в интерфейсе: `Backend`, `Locale(string preferred)`, `HasKey`, `Markup`, `Plain`, `SpeakerName`, `Summary`, `ShortId`, `NodeLabel`, `Shorten`, `Condition`, `Clause`, `Assignment`, `Operator`, `Value`, `SnakeCase`, `NextKey(table, prefix)`.
- `DialogueNodeInfo` — сведения о типах нод: константы портов `NextPort`, `ElsePort`, `OptionPort`, `CasePort`, `Types`, `Title`, `Glyph`, `Category` (`NodeCategory { Flow, Speech, Logic }`), `CategoryClass`, `HasInput`, `PortTitle`, `TryIndex`.
- `DialogueIssueText` — `Describe(DialogueIssue)`, `Port(string)`, `Mark(DialogueIssueSeverity)`. `DialogueFlagRename` — переименование флага: `Prepare(from, to)`, `Count(asset, name)`, `Describe()`, `Apply(catalog, definition, bool rewriteDialogues)`, свойства `From`, `To`, `Dialogues`, `Places`.

**Проигрывание и проверки в окне.** `DialoguePlaybackPanel(IDialoguePlaybackHost host)` — проигрывание без запуска игры: `Runner`, `Variables`, `IsPlaying`, `Current`, `Observed`, `Log` (`PlaybackLogEntry(text, asset, nodeId, kind)`), `StatusText`, `WaitRemaining`, `ChoiceButtons`, `HiddenText`; `Play(bool fromSelected)`, `Stop()`, `Next()`, `Choose(int)`, `SkipWait()`, `SetFlag`, `ResetFlags`, `RefreshFlags`, `OnLocaleChanged`, `Observe(DialoguePlayer)`, `Tick`, `Dispose`. `IDialoguePlaybackHost`: `Asset`, `Locale`, `SelectedNodeId`, `ShowCurrent(asset, nodeId)`, `Reveal(asset, nodeId)`. `DialogueIssuesPanel(Action<DialogueAsset, string> reveal)` — `ShowsAll`, `AllResults`, `Summary`, `ShowCurrent`, `Refresh`, `CheckAll()`.

**Граф** (`Dialogue.Editor.Graph`, `TYPECRAFT_NO_DIALOGUE_GRAPH` отключает): `DialogueGraphBoard : GraphView, IDialogueBoardView` (приоритет 100), `DialogueGraphNode : Node`, `DialoguePort : Port`, `DialogueEdgeListener : IEdgeConnectorListener`, `DialogueNodeSearch : ScriptableObject, ISearchWindowProvider` (`Create(Action<Type, Vector2> picked)`). `DialogueListBoard` — запасной список (в основной сборке).

### 10.12. Поведение окна и вкладок (справка)

Краткая сводка того, что делает интерфейс поверх API выше. Пошаговое описание — в `Manual.ru.md`.

**Вкладки окна `Window → Typecraft`.**

- **Редактор** (`EditorTab`) — разметка с подсветкой и автодополнением, живое превью с машинкой, галерея стилей, иконок и эффектов, выделение мышью прямо в превью; правки в Play Mode переживают выход из него (`PlayModeEdits`).
- **Локализация** (`LocalizationTab`) — сетка «ключ × язык» со строками таблицы; флаги `CellIssue`: «нет перевода», «не помещается», «нет символа», «разметка расходится»; ✦ — есть разметка в слое, ⚠ — строка изменилась и разметка не применяется (фильтр «Только изменившиеся»); синхронизация с CSV/TSV, Excel и Google Sheets; экспорт в `.xlsx`/CSV/TSV/JSON; поиск использований ключа; псевдолокаль `pseudo`.
- **Библиотека** (`LibraryTab`) — наборы иконок (перетаскивание PNG), редактор стилей, «стиль из `TMP_Text`», пресеты материалов и свои эффекты. Переименование стиля или иконки переписывает теги в таблицах и сценах (`MarkupRewriter`).
- **Шрифты** (`FontsTab`) — роли × языки, покрытие символов, сборщик статического шрифта по набору символов из строк (для CJK).
- **Диалоги** (`DialoguesTab`) — граф или список нод, инспектор ноды, проигрывание и проверка.

**Что где хранится: таблица и слой.**

| Где | Что лежит |
| --- | --- |
| Таблица (`StringTable` или Unity Localization) | Слова и содержательные токены: `{key:Interact}`, `{candle}`, `{0}`, `{player:name}` — порядок слов в языках разный, переводчик ставит их сам. Экранирование `\[`, `\<` — тоже текст |
| Слой (`MarkupLayer`, один на таблицу) | Оформление: стили `[warn]…[/warn]` и теги TMP/TMPEffects `<…>`, включая `<!wait=…>`. Запись = ключ + язык + полный текст с оформлением + снимок строки таблицы, на которую оформление ставилось |

- Слои перечислены в `TypecraftSettings → Markup Layers` и создаются сами при первой разметке рядом с таблицей. Таблица находится по id (у Unity Localization — по имени коллекции) и коду языка.
- В игре: если снимок совпадает с текущей строкой таблицы — показывается текст из слоя, иначе чистая строка таблицы. Так работает и встроенный источник, и Unity Localization; псевдолокаль `pseudo` строится из оформленного текста основного языка.
- Строку в таблице поменяли (переводчик, импорт CSV) — оформление перестаёт применяться, ячейка во вкладке «Локализация» помечена ⚠. «Перенести теги» переставляет оформление на новый текст по совпадающим словам (что не удалось привязать точно — покажет и спросит; `Decoration.Transfer`), «Сбросить разметку» удаляет запись слоя.
- Вкладка «Редактор» показывает текст из слоя. «Применить» без изменения слов пишет только в слой; если слова изменились — окно «было / стало» (`TextChangeDialog`) с выбором «Записать в таблицу» или «Вернуть слова». Оно же появляется при уходе с объекта и при закрытии окна с незаписанными словами.
- Старые таблицы с оформлением прямо в ячейках: `Tools/Typecraft/Move Markup To Layers`.
- Переименование стиля во вкладке «Библиотека» переписывает слои; иконки `{…}` живут в таблице и переписываются там (и в снимках слоя, чтобы оформление не устарело).

**Синхронизация таблиц (вкладка «Локализация»).** Строки можно вести во внешней таблице и подтягивать в редакторе (в рантайме синхронизации нет). **Источник…** настраивает источник для выбранной таблицы, **Синхронизировать** загружает одним кликом, **Импорт…** — разовая загрузка файла без сохранения источника.

| Источник | Что указать |
| --- | --- |
| CSV / TSV | Путь к файлу (UTF-8; кавычки и переносы строк внутри ячеек поддерживаются) |
| Excel `.xlsx` | Путь к файлу и лист (по умолчанию первый). Читается без сторонних библиотек; старый `.xls` — нет |
| Google Sheets | Обычная ссылка `https://docs.google.com/spreadsheets/d/<SHEET_ID>/edit#gid=<GID>` — лист берётся по `gid` |

- Формат: первая строка — заголовки. `key` — обязательная колонка, `comment` — необязательная, остальные — коды языков (`en`, `ru`, `zh-Hans`…). Колонки, не похожие на код языка (`notes`), пропускаются. Языков, которых нет в проекте, предложат добавить.
- Google Sheets: нужен доступ «Все, у кого есть ссылка — читатель» (или «Опубликовать в интернете»). Без ключей и OAuth; закрытая таблица даёт ошибку «Таблица закрыта» — тогда открой доступ или выгрузи `.xlsx`. Загрузка идёт в фоне, её можно отменить.
- Источник хранится в `ProjectSettings/TypecraftTableSync.asset` (только редактор, в билд не попадает). Пути внутри проекта пишутся относительными. Ссылка на таблицу попадает в репозиторий — по ней таблицу может прочитать любой, у кого есть ссылка.
- Перед применением — отчёт (`TableSyncPlan`, `TableSyncDialog`): новые ключи, изменённые строки по языкам («было / стало»), комментарии, ключи, которых нет в источнике. Отсутствующие ключи по умолчанию не удаляются (флажок «Удалить ключи…»); пустая ячейка в источнике по умолчанию не стирает перевод (флажок «Стирать строки…»). «Применить» — одна операция Undo.
- Разметка: если строка изменилась, а у неё есть оформление в слое, оно перестаёт применяться (снимок не совпадает) — ячейка получит ⚠. Отчёт заранее показывает «Разметка перестанет применяться: N строк»; после синхронизации — фильтр «Только изменившиеся» и «Перенести теги». При удалении ключа удаляется и его запись в слое.

**Экспорт и поиск использований.**

- **Экспорт…** — формат (Excel `.xlsx`, CSV, TSV, JSON) и охват (текущая таблица или все). Колонки `key`, `comment`, языки (основной первым) — файл читается обратно синхронизацией. Все таблицы: `.xlsx` — одна книга, лист на таблицу (имя листа — id таблицы, с поправкой на ограничения Excel); CSV/TSV — папка, файл на таблицу; JSON — один файл. В `.xlsx` шапка закреплена, пустой перевод — красная заливка, строка с ⚠ — оранжевая. Оформление из слоя разметки в файл не попадает, только текст таблицы.
- **Двойной клик по ключу** — где он используется: `TypecraftText` в режиме Localized (с этой таблицей или без таблицы, если ключ находится в ней первым) в открытых и закрытых сценах и префабах `Assets/`, плюс строка `"key"` в `.cs` из `Assets/`. Одно место — сразу переход (объект выделяется и наводится камера; закрытая сцена открывается после вопроса о сохранении, префаб — в Prefab Mode, скрипт — на нужной строке), несколько — список (`KeyUsagesWindow`). Переименование ключа — F2 или контекстное меню ячейки.

**Свои стили, материалы и эффекты.** Создаются в окне — в галерее вкладки «Редактор» (карточки «+ стиль», «+ материал», «+ эффект») или кнопками на панели вкладки «Библиотека». Форма (`TypecraftForm` в `FormOverlay`) открывается поверх окна, превью живое, «Сохранить» пишет в ассеты (Undo там, где Unity его поддерживает).

| Что | Как работает | Где хранится |
| --- | --- | --- |
| Стиль | id, цвет/градиент, b/i/u, регистр, интервал, роль шрифта, пресет материала, эффект. Превью — на выделенном тексте или на образце; после сохранения выделение оборачивается в `[id]…[/id]`. «Стиль из выделения» (плавающая панель, меню «Стиль ▾») берёт теги вокруг выделения — `<color>`, `<b>`, `<i>`, `<u>`, `<cspace=…em>`, `<size=…%>`, `<font … material=…>`, `<material>`, эффект — в форму и заменяет их на `[id]…[/id]` | `StyleLibrary` |
| Материал | Пресет SDF-материала для роли шрифта: Face Dilate, Softness, обводка, подложка (`UNDERLAY_ON`), свечение (`GLOW_ON`). Создаётся копия материала для каждого шрифта роли (у каждого языка свой атлас) с одним id пресета, так что стиль работает на всех языках. Свечение есть только в полном шейдере — пресет со свечением переводится на `TextMeshPro/Distance Field` | `Materials/` рядом с `FontProfile`, запись в `FontProfile` |
| Эффект | Тип (постоянный `<x>`, появление `<+x>`, исчезание `<-x>`), основа — копия встроенной анимации TMPEffects, `Generic` (дорожки) или `Stack` (несколько анимаций), параметры — инспектор ассета внутри формы. Имя: строчные латинские буквы, цифры, `_`; не может совпадать со встроенным эффектом или тегом TMP | `Effects/` рядом со `StyleLibrary` |

- База анимаций проекта: первый свой эффект создаёт `Effects/TypecraftAnimationDatabase.asset`; ссылка на неё — `TypecraftSettings → Animation Database` (подробности — [раздел 8](#8-эффекты-tmpeffects)). Аниматор на базе TMPEffects по умолчанию при «Применить» переводится на базу проекта; глобальные настройки TMPEffects не меняются.
- Свои эффекты сразу появляются в автодополнении, галерее (подпись «свой»), меню «Эффект ▾» и в выпадающем списке эффектов стиля.
- Удаление: правый клик по карточке материала/эффекта в галерее или кнопка «Удалить» во вкладке «Библиотека». Материалы и ассеты эффектов уходят в корзину ОС; теги в тексте остаются.
- После «Пересобрать» статического шрифта атлас в материалах пресетов обновляется сам (`MaterialPresets.SyncAtlases`).

**Вкладка «Диалоги».**

- Слева — диалоги, говорящие и флаги («+» у раздела создаёт новый; новый флаг заводит каталог, если его нет).
- В центре — доска: граф (порты, перетаскивание связей, пробел или правый клик — новая нода, мини-карта) или список нод; переключатель вида и язык текстов — на панели.
- Справа — инспектор выделенной ноды: говорящий и эмоция из списков, ключ с поиском и превью текста, «Создать ключ» — строка в таблице прямо из инспектора (текст на основном языке, оформление `[стиль]` и `<тег>` уходит в слой разметки), условия с выбором флага.
- Внизу — «▶ Проигрывание» без запуска игры: с начала или с выделенной ноды, печать, выбор, флаги проигрывания (правятся на лету, «Сбросить»), лог; текущая нода подсвечивается на доске.
- «Проверить» — проблемы диалога (нет старта, пустые и битые связи, недостижимые ноды, пустой или отсутствующий ключ, нет перевода, устаревшая разметка, нет символов в шрифте, необъявленный флаг или не тот тип, неизвестная эмоция…), клик — к ноде; «Проверить все» — по всему проекту. Переименование флага во вкладке переписывает его во всех диалогах (`DialogueFlagRename`).

---

## 11. Расширение

### 11.1. Свой тег `{prefix:argument}`

```csharp
using System.Text;
using UnityEngine;
using Velsi.Typecraft;
using Velsi.Typecraft.Markup;

public sealed class PlayerTag : IMarkupTagHandler
{
    public static string Name { get; set; } = "Alex";

    public string Prefix => "player";

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register() => Typecraft.Register(new PlayerTag());

    public bool TryRender(string argument, MarkupContext context, StringBuilder output)
    {
        if (argument != "name")
            return false;

        output.Append(Name);
        return true;
    }
}
```

- `TryRender` вернул `false` — тег не распознан: рендер даёт `MarkupIssue.UnresolvedTag`, а в редакторе тег подсвечивается ошибкой.
- Через `context` доступны язык (`Locale`), устройство (`Device`), `Settings` и `TryAppendIcon(reference, output)` для вставки иконки.
- Чтобы тег работал и в превью окна, регистрируй его и в редакторе (`[InitializeOnLoadMethod]`), как в примере и в сэмпле Simple Dialogue.
- Результат `Typecraft.Render` кэшируется по тексту разметки: после смены данных, от которых зависит тег (`PlayerTag.Name`), вызови `Typecraft.Refresh()`.
- Регистрацию делай на `BeforeSceneLoad` (или позже): на `SubsystemRegistration` пакет сам сбрасывает свои статические обработчики, и порядок двух методов одной стадии не определён.

Тег с иконкой:

```csharp
using System.Text;
using Velsi.Typecraft.Markup;

public sealed class RankTag : IMarkupTagHandler
{
    public string Prefix => "rank";

    public bool TryRender(string argument, MarkupContext context, StringBuilder output) =>
        context.TryAppendIcon("rank_" + argument, output);
}
```

### 11.2. Свой источник строк

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Velsi.Typecraft;
using Velsi.Typecraft.Text;

[CreateAssetMenu(menuName = "Game/Dictionary Text Source")]
public sealed class DictionaryTextSource : ScriptableObject, ITextSource
{
    [SerializeField] private List<string> _locales = new List<string> { "en", "ru" };
    [SerializeField] private List<Row> _rows = new List<Row>();

    public IReadOnlyList<string> Locales => _locales;

    public string BaseLocale => _locales.Count > 0 ? _locales[0] : "en";

    public void Activate()
    {
    }

    public void Request(string table, string key, string locale, Action<TextEntry> onReady)
    {
        foreach (string candidate in new[] { locale, BaseLocale })
        {
            foreach (Row row in _rows)
            {
                if (row.Key == key && row.Locale == candidate)
                {
                    onReady(new TextEntry("Game", key, candidate, row.Text));
                    return;
                }
            }
        }

        onReady(TextEntry.Missing(table, key, locale));
    }

    public void SelectLocale(string localeCode) => Typecraft.SetLocale(localeCode);

    [Serializable]
    private struct Row
    {
        public string Key;
        public string Locale;
        [TextArea] public string Text;
    }
}
```

Назначь ассет в `TypecraftSettings → Text Source`.

- `Request` отдаёт **сырую** строку таблицы: без псевдолокали и без подстановки аргументов. В `TextEntry` укажи id таблицы и язык, из которых строка взята на самом деле (с учётом отката на основной язык) — по ним Typecraft находит запись в слое разметки.
- Слой, псевдолокаль `pseudo` и `{0}` из `SetArguments` Typecraft применяет сам. Если у источника свой форматтер (как Smart Strings у Unity Localization), передай его пятым аргументом `TextEntry` — `(text, arguments) => …`; он получит уже оформленный текст.
- Чтобы таблицы источника правились во вкладке «Локализация», реализуй `ITableBackend` и зарегистрируй его (`TypecraftEditor.RegisterTableBackend`, пример в [10.5](#105-таблицы-локализации)). Пока бэкенда нет, рантайм работает, а вкладка «Локализация», синхронизация, экспорт и слой разметки для этого источника недоступны.

Слой разметки в своём коде:

```csharp
using Velsi.Typecraft;
using Velsi.Typecraft.Text;

LocalizedText.Request(table, key, null, (markup, formatted) => Debug.Log(Typecraft.Render(markup)));

MarkupLayer layer = Typecraft.Settings.FindMarkupLayer(tableId);
string shown = layer != null ? layer.Resolve(key, locale, tableText) : tableText;
```

`Resolve` отдаёт текст слоя, только если снимок записи совпадает со строкой таблицы.

### 11.3. Свой вывод диалога

Наследник `DialoguePresenter`; плееру его даёт поле `Presenter`.

```csharp
using UnityEngine;
using Velsi.Typecraft.Dialogue;
using Velsi.Typecraft.Text;

public sealed class BubblePresenter : DialoguePresenter
{
    [SerializeField] private TypecraftText _text;

    public override void ShowLine(DialogueLine line)
    {
        gameObject.SetActive(true);
        DialogueSpeakerText.RequestLine(line, markup =>
        {
            _text.SetMarkup(markup);
            RaiseLineCompleted();
        });
    }

    public override void Hide() => gameObject.SetActive(false);
}
```

- `RaiseLineCompleted()` — «реплика показана целиком». Без него не сработает переход `AfterText`.
- Машинка: верни `false` из `IsLineComplete`, пока печатаешь, а `TryCompleteLine()` пусть допечатывает и возвращает `true` — тогда первое «далее» допечатает, второе перейдёт дальше.
- Выбор: `SupportsChoices => true` и `ShowChoices(choices, choose)`; у недоступных `Available == false` (скрытые в список не попадают), по клику — `choose(choice.Index)`.
- Готовая база с машинкой, показом через `CanvasGroup` и автоскрытием — `TextDialoguePresenter`: переопредели `ApplyLine`, `OnLineShowing`, `OnHidden`. Имя говорящего — `DialogueSpeakerText.RequestName(speaker, table, style, onReady)`, портрет — `line.Portrait`.

```csharp
using UnityEngine;
using Velsi.Typecraft.Dialogue;

public sealed class ShoutPresenter : TextDialoguePresenter
{
    protected override void OnLineShowing(DialogueLine line)
    {
        if (Text != null)
            Text.Target.color = line.Emotion == "angry" ? Color.red : Color.white;
    }
}
```

### 11.4. Своя вкладка окна

Любой не абстрактный класс с публичным конструктором без параметров, реализующий `ITypecraftTab`, находится автоматически (`TypeCache`) и появляется вкладкой окна; вкладки сортируются по `Order` (встроенная «Диалоги» — 100).

```csharp
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Window;

public sealed class StatsTab : VisualElement, ITypecraftTab
{
    private readonly Label _label = new Label();

    public StatsTab() => Add(_label);

    public string Title => "Stats";
    public int Order => 200;
    public VisualElement Element => this;

    public void OnShow() => _label.text = "Opened";

    public void Tick(float deltaTime)
    {
    }

    public void OnTypecraftChanged()
    {
    }

    public void Dispose()
    {
    }
}
```

Открыть вкладку из кода:

```csharp
TypecraftWindow.ShowTab<StatsTab>(tab => tab.Focus());
```

| Метод | Когда вызывается |
| --- | --- |
| `Title`, `Order`, `Element` | При построении окна; `Element` добавляется в тело при выборе вкладки |
| `OnShow()` | Каждый раз, когда вкладку выбрали |
| `Tick(deltaTime)` | Раз в кадр (не чаще 30 раз в секунду, `deltaTime` не больше 0.1) — только пока вкладка активна |
| `OnTypecraftChanged()` | При любой смене языка, устройства или настроек — у всех вкладок, активна она или нет |
| `Dispose()` | При закрытии окна и перестроении вкладок |

Исключение в конструкторе вкладки логируется, остальные вкладки создаются.

### 11.5. Свои подсказки ввода

`IInputGlyphResolver` (`ScriptableObject`) превращает `{key:Action}` в иконку для устройства. Встроенный `InputGlyphProfile` работает с Input System; для Rewired или своей системы ввода реализуй интерфейс сам:

```csharp
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Velsi.Typecraft;
using Velsi.Typecraft.Input;
using Velsi.Typecraft.Library;
using Velsi.Typecraft.Markup;

[CreateAssetMenu(menuName = "Game/Simple Glyph Resolver")]
public sealed class SimpleGlyphResolver : ScriptableObject, IInputGlyphResolver
{
    [SerializeField] private List<IconSet> _iconSets = new List<IconSet>();

    public IEnumerable<IconSet> IconSets => _iconSets;

    public void Activate()
    {
    }

    public bool TryAppendGlyph(string actionName, DeviceFamily device, StringBuilder output)
    {
        string reference = (device + "_" + actionName).ToLowerInvariant();
        if (!Typecraft.Icons.TryResolve(reference, out IconSet set, out IconEntry entry) || entry.IsPlaceholder || set.SpriteAsset == null)
            return false;

        MarkupContext.AppendIcon(set, entry, output);
        return true;
    }
}
```

Назначь в `TypecraftSettings → Input Glyphs`. Наборы из `IconSets` попадают в `Typecraft.Icons`. Устройство в рантайме нужно сообщать самому: `Typecraft.SetDevice(...)` (у `InputGlyphProfile` это делает `DeviceTracker`).

### 11.6. Свои флаги диалогов

Флаги — `IDialogueVariables` (`TryGet`, `Set`, событие `Changed`). По умолчанию у всех плееров общий `DialogueGlobals.Variables` (`DialogueVariables` в памяти). Пример — флаги из своей системы квестов:

```csharp
using System;
using System.Collections.Generic;
using Velsi.Typecraft.Dialogue;

public sealed class QuestLog
{
    private readonly Dictionary<string, int> _stages = new Dictionary<string, int>();

    public event Action<string> StageChanged;

    public int Stage(string id) => _stages.TryGetValue(id, out int stage) ? stage : 0;

    public void SetStage(string id, int stage)
    {
        _stages[id] = stage;
        StageChanged?.Invoke(id);
    }
}

public sealed class QuestFlags : IDialogueVariables
{
    private const string Prefix = "quest.";

    private readonly QuestLog _quests;
    private readonly DialogueVariables _own = new DialogueVariables();

    public QuestFlags(QuestLog quests)
    {
        _quests = quests;
        _quests.StageChanged += id => Changed?.Invoke(Prefix + id);
        _own.Changed += name => Changed?.Invoke(name);
    }

    public event Action<string> Changed;

    public bool TryGet(string name, out DialogueValue value)
    {
        if (!name.StartsWith(Prefix, StringComparison.Ordinal))
            return _own.TryGet(name, out value);

        value = DialogueValue.Int(_quests.Stage(name.Substring(Prefix.Length)));
        return true;
    }

    public void Set(string name, DialogueValue value)
    {
        if (name.StartsWith(Prefix, StringComparison.Ordinal))
            _quests.SetStage(name.Substring(Prefix.Length), value.AsInt);
        else
            _own.Set(name, value);
    }
}
```

Подключение:

```csharp
DialogueGlobals.SetVariables(new QuestFlags(questLog));
DialogueGlobals.Catalog = flagsCatalog;
```

- Всё, что не твоё (посещения `visited:…`, флаги `Once`, локальные флаги диалогов), складывай в обычный `DialogueVariables` — иначе `Once` и счётчики посещений перестанут работать.
- Объяви `quest.lighthouse` (int) в каталоге — тогда вкладка «Диалоги» предложит его в условиях, проверит тип, а проигрывание в редакторе покажет и даст поменять.
- `SetVariables` вызывай до первого `Play`: плеер пересоздаёт раннер на следующем `Play`, уже идущий разговор остаётся на старых флагах. Подписчикам — событие `DialogueGlobals.VariablesReplaced`.
- Свои флаги для одного плеера — компонент-наследник `DialogueVariablesSource` (свойства `Variables` и `Catalog`) в поле `Variables Source`.

### 11.7. События диалога в игру

Нода Event отдаёт `(имя, аргумент)` синхронно, в момент прохода:

- в инспекторе — `DialoguePlayer → On Event (String, String)`, метод с двумя string (dynamic);
- из кода — `_player.EventRaised += (name, argument) => …`.

Внутри обработчика можно остановить диалог или запустить другой — раннер это переживает. Если событие должно подождать (анимация двери), поставь после Event ноду Wait или Line.

### 11.8. Диалоги и ECS-игра

Диалог — презентация и сценарий, симуляции он не нужен; граница проходит по мостам, как у любого MonoBehaviour-мира:

- **Запуск.** На объекте собеседника — authoring, который при рождении сущности вешает на неё компонент-мост со ссылкой на `DialoguePlayer` (или на `DialogueTrigger`). Система взаимодействия, получив запрос «взаимодействовать» на цели с этим мостом, зовёт `player.Play()`. Пока идёт разговор, ввод движения можно гасить тегом на игроке, который вешается на `Started` и снимается на `Ended`.
- **События обратно.** Обработчик `EventRaised` не трогает игру напрямую, а превращается в данные: вешает на нужную сущность однокадровый компонент-запрос (`OpenDoorRequest`, `GiveItemRequest { Id }`), дальше его обрабатывает обычная система в своей стадии конвейера, а уборщик снимает в конце тика. Аргумент события — id, а не ссылка на объект.
- **Флаги.** Источник правды — состояние игры: `IDialogueVariables` поверх компонентов или сервиса квестов (пример выше), а не копия, которую надо синхронизировать. Посещения и мелкие флаги диалогов — в его встроенном `DialogueVariables`.
- **Сохранение.** Флаги диалогов — отдельная секция сейва (`ToJson`), позиция в разговоре — `DialogueRunnerState` рядом с сущностью собеседника, если нужна.

### 11.9. Свой вид доски диалогов

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Dialogue;
using Velsi.Typecraft.Dialogue.Editor.Board;

[DialogueBoardPriority(50, "Compact")]
public sealed class CompactBoard : IDialogueBoardView
{
    private readonly ScrollView _root = new ScrollView();
    private IDialogueBoardHost _host;
    private DialogueAsset _asset;

    public VisualElement Element => _root;

    public void Bind(IDialogueBoardHost host) => _host = host;

    public void Show(DialogueAsset asset)
    {
        _asset = asset;
        Refresh();
    }

    public void Refresh()
    {
        _root.Clear();
        if (_asset == null)
            return;

        foreach (DialogueNode node in _asset.Nodes)
        {
            if (node == null)
                continue;

            string id = node.Id;
            _root.Add(new Button(() => _host.OnNodeSelected(id)) { text = node.ToString() });
        }
    }

    public void RefreshNode(string nodeId) => Refresh();

    public void SelectNode(string nodeId, bool frame)
    {
    }

    public void SetIssues(IReadOnlyList<DialogueIssue> issues)
    {
    }

    public void SetCurrent(string nodeId)
    {
    }

    public Vector2 SuggestPosition() => Vector2.zero;

    public void Dispose()
    {
    }
}
```

Вид находится автоматически; выбирается переключателем вида на панели вкладки «Диалоги».

### 11.10. Свой источник использований ключа

```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Velsi.Typecraft.Editor.Localization;

public static class QuestKeyUsages
{
    [InitializeOnLoadMethod]
    private static void Register() => KeyUsages.RegisterSource(Find);

    private static IEnumerable<KeyUsage> Find(string table, string key)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:QuestEntry"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var entry = AssetDatabase.LoadAssetAtPath<QuestEntry>(path);
            if (entry != null && entry.TextKey == key)
                yield return new KeyUsage(path, "Quest", entry.name, () => Selection.activeObject = entry);
        }
    }
}

public sealed class QuestEntry : ScriptableObject
{
    public string TextKey;
}
```

---

## 12. Сохранение и загрузка

### 12.1. Флаги диалогов

`DialogueVariables` сериализуется в JSON (посещения нод входят в снимок):

```csharp
string json = ((DialogueVariables)DialogueGlobals.Variables).ToJson();
((DialogueVariables)DialogueGlobals.Variables).FromJson(json);
```

Каталог лучше применить после загрузки: `catalog.ApplyDefaults(variables)` дозаполнит флаги, которых не было в старом сейве. Свой `IDialogueVariables` сохраняется твоей системой; JSON есть только у `DialogueVariables`.

### 12.2. Место в разговоре

```csharp
using System;
using System.Linq;
using UnityEngine;
using Velsi.Typecraft.Dialogue;

[Serializable]
public sealed class DialogueSave
{
    public string Flags;
    public DialogueRunnerState Conversation;
}

public sealed class DialogueSaver : MonoBehaviour
{
    [SerializeField] private DialoguePlayer _player;
    [SerializeField] private DialogueAsset[] _dialogues;

    public string Save()
    {
        var save = new DialogueSave
        {
            Flags = ((DialogueVariables)DialogueGlobals.Variables).ToJson(),
            Conversation = _player.State
        };

        return JsonUtility.ToJson(save);
    }

    public void Load(string json)
    {
        var loaded = JsonUtility.FromJson<DialogueSave>(json);
        ((DialogueVariables)DialogueGlobals.Variables).FromJson(loaded.Flags);

        if (loaded.Conversation != null && loaded.Conversation.IsValid)
            _player.Restore(loaded.Conversation, id => _dialogues.FirstOrDefault(asset => asset.Id == id));
    }
}
```

- `State` — диалог и нода, на которой стоит разговор (реплика, выбор или ожидание); `null`, если разговор не идёт. Сохранять его стоит, только если сейв возможен посреди разговора; чаще хватает флагов.
- `Restore` возвращает разговор на сохранённую ноду через выводы плеера, не считает повторное посещение и работает и у плеера с `Once`. Без `resolver` плеер ищет только свой `Asset`.
- Голый раннер без выводов: `runner.Restore(state, id => DialogueAsset)`.
- Идентификатор диалога — `DialogueAsset.Id`, он не меняется при переименовании и перемещении ассета.

### 12.3. Язык и устройство

Typecraft не сохраняет выбранный язык и устройство. Сохраняй их в своих настройках и восстанавливай через `Typecraft.SetLocale(code)` (или `ITextSource.SelectLocale`, для Unity Localization — `LocalizationSettings.SelectedLocale`) и `Typecraft.SetDevice(device)`.

### 12.4. Что пакет хранит в проекте

См. [14.3](#143-хранилища-данных).

---

## 13. Ограничения и подводные камни

**Рендер и разметка**

- `Typecraft` — и namespace `Velsi.Typecraft`, и статический класс в нём. Из кода в namespace, начинающемся с `Velsi.` (кроме `Velsi.Typecraft.*`), имя `Typecraft` по правилам C# разрешается в namespace, а не в класс: пиши `Velsi.Typecraft.Typecraft.Render(...)` или `using Typecraft = Velsi.Typecraft.Typecraft;`. В глобальном namespace с `using Velsi.Typecraft;`, как и внутри `Velsi.Typecraft.*`, проблемы нет.
- `Render` кэшируется по тексту разметки. Если тег зависит от внешних данных (`{player:name}`), после их смены вызови `Typecraft.Refresh()`.
- Регистрация обработчиков тегов пропадает при сбросе статики на `SubsystemRegistration`; регистрируй на `BeforeSceneLoad` и позже.
- `TypecraftText` с аргументами: если источник не отформатировал строку сам (встроенные таблицы), результат рендера пропускается через `string.Format`. Литерал `{` или `}` в готовом тексте (после `\{`) вызовет `FormatException` — он перехватывается и пишется предупреждением, текст остаётся без подстановки. Именованные плейсхолдеры (`{coin}`) встроенными таблицами не подставляются: для Typecraft это иконка `coin`. Именованные аргументы работают только через Smart Strings Unity Localization.
- Теги-команды TMPEffects (`<!wait>`, `<#…>`) вырезаются, если на объекте нет `TMPWriter`.
- Незакрытые и неизвестные стили не бросают исключений: они дают диагностику `MarkupDiagnostic` (видна в редакторе) и остаются в тексте как есть.
- Иконки стоят на строке левым нижним углом холста; пивот спрайта и смещение внутри холста на положение не влияют — подгонка делается у набора или иконки.
- Строки со стилями и `<…>` в таблице не хранятся: оформление живёт в слое разметки. Строка таблицы изменилась — слой перестаёт применяться (`LayerState.Stale`), в игре показывается чистая строка.

**Источники и локали**

- Для `zh-Hans` и других CJK нужен шрифт с нужными символами; после новых строк пересобери статический шрифт (вкладка «Шрифты»), иначе флаг «нет символа».
- `Typecraft.SetLocale` не переключает Unity Localization; для него используй `ITextSource.SelectLocale` или `LocalizationSettings.SelectedLocale`. Адаптер сам подтягивает смену языка Unity Localization в Typecraft.
- Псевдолокаль `pseudo` строится из основного языка и существует только в списках `Locales` (у Unity Localization — когда есть хотя бы один язык).
- Синхронизация, экспорт и поиск использований работают только в редакторе.
- Все `TypecraftText` подписаны на `Typecraft.Changed` и обновляются при каждой смене языка, устройства, регистрации тега и `Refresh()`; на сценах с тысячами строк не вызывай `Refresh()` каждый кадр.
- Ключ, найденный в нескольких таблицах `TableTextSource`, при пустом `table` берётся из первой по порядку.

**Ввод**

- `DeviceTracker` работает только в Play Mode. У Touch нет биндингов — нужны оверрайды в `InputGlyphProfile`.
- Ловушка Input System: у действия из шаблона Unity может стоять интеракция `Hold` — тогда `performed` приходит только после удержания, и «далее» `DialogueInputAdvance` на быстрое нажатие не срабатывает.

**Диалоги**

- `DialogueAsset.Id` попадает в сейвы и в флаги посещений. Копия ассета получает новый `Id` (при импорте срабатывает `DialogueIdGuard`); не полагайся на `Id` копии, пока он не выдан.
- `DialoguePlayer.Once` помнит посещение стартовой ноды в `Variables`; сбросили флаги — диалог можно играть снова.
- `SetVariables` вызывай до первого `Play`; уже идущий разговор остаётся на старых флагах. Каталог (`DialogueGlobals.Catalog`) читается на каждом `Play`.
- События `EventRaised` и `NodeEntered` синхронны. Долгое действие в обработчике (анимация) блокирует поток диалога — после ноды Event ставь Wait или Line.
- `MaxAutoSteps` (1000) защищает от циклов без вывода (Branch ↔ SetVariables); срабатывание — ошибка в консоли и `Ended`.
- `Choose` с недоступным вариантом возвращает `false`. Если ни один вывод не умеет показывать выбор, плеер сам берёт первый доступный вариант.
- `OnDisable` у `DialoguePlayer` останавливает разговор (с `Ended`).
- Таймеры плеера идут по `Time.time` (`Scaled`) или `Time.unscaledTime`; на паузе с `Time.timeScale = 0` выбери `Unscaled`.
- `TextDialoguePresenter` по умолчанию в `Awake` (поле `Hide On Awake`, только в Play Mode) вызывает `Hide()`: вывод становится невидимым (альфа 0), но объект остаётся активным.
- Граф (`GraphView`) помечен в Unity как Experimental; при проблемах в новой версии Unity включи `TYPECRAFT_NO_DIALOGUE_GRAPH` — вкладка работает в виде «Список».

**Редактор**

- Переименование стиля или иконки переписывает теги в таблицах, слоях, открытых и закрытых сценах, префабах, полях `[TypecraftMarkup]` и правилах автотегов. Префабы и закрытые сцены сохраняются сразу, без Undo.
- Google Sheets: нужен доступ «Все, у кого есть ссылка — читатель»; закрытая таблица даёт ошибку «Таблица закрыта». Ссылка на таблицу попадает в репозиторий (`ProjectSettings/TypecraftTableSync.asset`).
- Источники синхронизации и настройки хранятся в `ProjectSettings`/EditorPrefs/SessionState и в билд не попадают.
- Вручную добавленный `TMPAnimator` по умолчанию берёт базу анимаций TMPEffects; для своих эффектов выбери в нём базу проекта.
- До версии 1.0 `public`-типы Editor, не перечисленные в начале [раздела 10](#10-editor-api) как точки встраивания, могут менять сигнатуры.

---

## 14. Приложение: меню, ассеты, хранилища

### 14.1. Пункты меню

| Меню | Действие |
| --- | --- |
| `Window/Typecraft` (Ctrl+Alt+T), `Tools/Typecraft/Open Window` | Окно Typecraft |
| `Tools/Typecraft/Create Project Settings` | `TypecraftSetup.CreateDefaults` |
| `Tools/Typecraft/Create String Tables` | `TypecraftSetup.CreateStringTables` |
| `Tools/Typecraft/Move Markup To Layers` | `MarkupLayerMigration` |
| `Tools/Typecraft/Rebuild Base Prompt Sets` | `BasePromptSets.RebuildAll` |
| `GameObject/Typecraft/Dialogue Box`, `Subtitles`, `World Text` | `DialogueMenu` |
| `CONTEXT/TMP_Text/Edit in Typecraft`, правый клик по string-полю → `Edit in Typecraft` | `TypecraftEditor.Edit` |
| `CONTEXT/IconSet/Rebuild Atlas` | `IconSetBuilder.Build` |

### 14.2. Ассеты и компоненты

`Create → Typecraft`: `Settings`, `Style Library`, `Font Profile`, `Icon Set`, `Input Glyph Profile`, `Markup Layer`, `String Table`, `Text Sources/String Tables`, `Text Sources/Unity Localization`, `Dialogue`, `Dialogue Speaker`, `Dialogue Variables`.

`Add Component → Typecraft`: `Typecraft Text`, `Dialogue/Dialogue Player`, `Dialogue/Dialogue Trigger`, `Dialogue/Dialogue Input Advance`, `Dialogue/Dialogue Box Presenter`, `Dialogue/Subtitle Presenter`, `Dialogue/World Text Presenter`.

### 14.3. Хранилища данных

| Что | Где |
| --- | --- |
| Настройки пакета | `Resources/TypecraftSettings.asset` (`TypecraftSettings.ResourcesPath`) |
| Источники синхронизации таблиц | `ProjectSettings/TypecraftTableSync.asset` (только редактор) |
| Папка диалогов | `EditorUserSettings` (ключ `Typecraft.Dialogues.Folder`), по умолчанию `Assets/Game/Dialogues` |
| Правки Play Mode | `SessionState` (`Typecraft.PlayModeEdits`), флаг сохранения — `EditorPrefs` (`Typecraft.KeepPlayModeEdits`) |
| Черновик окна | `SessionState` (`Typecraft.Scratch.*`) |
| Конфиги Typecraft (по умолчанию) | `Assets/Typecraft/`, настройки в `Assets/Typecraft/Resources/` |
| Слои разметки | Рядом с таблицей: `MarkupLayer_<таблица>.asset` |
| Материалы пресетов, свои эффекты | `Materials/` рядом с `FontProfile`, `Effects/` рядом с `StyleLibrary` |
