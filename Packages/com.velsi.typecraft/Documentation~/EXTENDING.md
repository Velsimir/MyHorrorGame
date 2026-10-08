# Встраивание и расширение Typecraft

[Русский](#русский) · [English](#english)

---

## Русский

### Поле разметки в инспекторе

```csharp
using Velsi.Typecraft;

public sealed class QuestStep : ScriptableObject
{
    [SerializeField, TypecraftMarkup] private string _description;
    [SerializeField, TypecraftMarkup(preview: false)] private List<string> _hints;
}
```

Поле подсвечивает разметку, подсказывает теги после `{`, `[`, `<`, показывает ошибки,
а 👁 включает превью. ✎ открывает текст в окне Typecraft. Правка пишется в поле с Undo.
Работает и в старых IMGUI-инспекторах (упрощённый вид).

Без атрибута: правый клик по любому string-полю → **Edit in Typecraft**.

### Открыть окно из своего кода

```csharp
using Velsi.Typecraft.Editor;

TypecraftEditor.Edit(node.Text, text =>
{
    Undo.RecordObject(graph, "Edit line");
    node.Text = text;
}, title: $"Реплика {node.Id}");

TypecraftEditor.Edit(serializedObject.FindProperty("_description"));
TypecraftEditor.Edit(someTmpText);
```

Окно закрепляется на этом тексте (🔒) и не переключается на выделение в сцене. «Применить»
вызывает колбэк.

### Элемент для своих редакторов (UI Toolkit)

```csharp
var field = new MarkupTextField("Реплика", showPreview: true);
field.SetValueWithoutNotify(node.Text);
field.RegisterValueChangedCallback(evt => node.Text = evt.newValue);
field.EditRequested += () => TypecraftEditor.Edit(node.Text, text => field.value = text);
root.Add(field);
```

Без подписки на `EditRequested` кнопка ✎ сама откроет окно и вернёт текст в поле.

### Свой тег `{prefix:argument}`

```csharp
public sealed class PlayerTag : IMarkupTagHandler
{
    public string Prefix => "player";

    public bool TryRender(string argument, MarkupContext context, StringBuilder output)
    {
        if (argument != "name")
            return false;

        output.Append(PlayerProfile.Name);
        return true;
    }
}

Typecraft.Register(new PlayerTag());
```

`false` — тег не распознан: в редакторе он подсветится ошибкой. Через `context` доступны
язык, устройство и `TryAppendIcon` для вставки иконки. Чтобы тег работал и в превью окна,
регистрируй его и в редакторе (`[InitializeOnLoadMethod]`), как в сэмпле Simple Dialogue.

### Свой источник строк

```csharp
public sealed class MyTextSource : ScriptableObject, ITextSource
{
    public IReadOnlyList<string> Locales => ...;
    public string BaseLocale => "en";
    public void Activate() { }
    public void Request(string table, string key, string locale, Action<TextEntry> onReady) =>
        onReady(Find(table, key, locale, out string tableId, out string foundLocale, out string text)
            ? new TextEntry(tableId, key, foundLocale, text)
            : TextEntry.Missing(table, key, locale));
    public void SelectLocale(string localeCode) => Typecraft.SetLocale(localeCode);
}
```

Назначь ассет в `TypecraftSettings → Text Source`. `Request` отдаёт **сырую** строку таблицы:
без псевдолокали и без подстановки аргументов. В `TextEntry` укажи id таблицы и язык, из которых
строка взята на самом деле (с учётом фолбэка на основной язык) — по ним Typecraft находит запись
в слое разметки. Слой, псевдолокаль `pseudo` и `{0}` из `SetArguments` Typecraft применяет сам.
Если у источника свой форматтер (как Smart Strings у Unity Localization), передай его пятым
аргументом `TextEntry` — `(text, arguments) => …`, он получит уже оформленный текст.

### Слой разметки в своём коде

```csharp
LocalizedText.Request(table, key, arguments, (markup, formatted) => ...);
MarkupLayer layer = Typecraft.Settings.FindMarkupLayer(tableId);
string shown = layer != null ? layer.Resolve(key, locale, tableText) : tableText;
```

`LocalizedText.Request` — тот же путь, что у `TypecraftText`: источник → слой → форматтер →
псевдолокаль; `formatted = false` значит, что `{0}` ещё не подставлены. `Resolve` отдаёт текст
слоя, только если снимок записи совпадает со строкой таблицы. В редакторе: `Decoration.Strip`
(текст без оформления), `Decoration.Transfer` (перенос оформления на новый текст),
`MarkupLayers.Write/Set/State` — запись и состояние слоя через текущий `ITableBackend`.
Инвариант слоя: `Decoration.Strip(markup) == source`.

Чтобы его таблицы правились во вкладке «Локализация», реализуй `ITableBackend` и зарегистрируй:

```csharp
[InitializeOnLoadMethod]
private static void RegisterBackend() =>
    TypecraftEditor.RegisterTableBackend(source => source is MyTextSource my ? new MyBackend(my) : null);
```

### Синхронизация таблиц

Всё работает через `ITableBackend`, поэтому одинаково для встроенных таблиц и Unity Localization
(и для своего бэкенда). Только редактор.

```csharp
SheetImport data = TableCsv.Read(path);                                  // CSV/TSV по расширению
data = SheetImport.FromRows(XlsxReader.Read(path, "Strings")); // лист .xlsx, null — первый
TableSyncPlan plan = TableSync.Plan(backend, "Main", data, "мой источник");
TableSync.Apply(backend, plan, new TableSyncOptions { RemoveMissing = false, ClearEmpty = false });
```

- `SheetImport.FromRows` принимает строки ячеек из любого места (своя БД, свой формат): первая
  непустая строка — заголовки `key` / `comment` / коды языков.
- `TableSyncPlan` — отчёт без записи: `AddedKeys`, `Changes` (`Before`/`After`, `LosesMarkup` —
  у строки есть применённая разметка в слое и после записи она станет ⚠), `Comments`, `MissingKeys`,
  `MissingKeysWithMarkup`, `NewLocales`, `DuplicateKeys`, `IgnoredColumns`; `MarkupLost(options)`.
- `TableSync.Apply` — одна группа Undo, обновление текстов в сцене один раз в конце.
- Google Sheets: `GoogleSheets.TryParse(link, out GoogleSheetLink sheet)` разбирает обычную ссылку
  (`.../d/<SHEET_ID>/edit#gid=<GID>`, `?gid=`, `/u/0/d/…`, опубликованные `/d/e/…`),
  `GoogleSheetsDownload.Start(sheet, csv => …, error => …)` качает CSV в фоне (`Cancel()` — отмена),
  `GoogleSheets.Interpret(status, contentType, body)` превращает HTML логина и 401/403/404 в понятную
  ошибку `TableSyncException`.
- Настроенные источники: `TableSyncSources.Find(table)` / `Set(source)` / `Remove(table)`
  (`ProjectSettings/TypecraftTableSync.asset`).

### Экспорт и поиск использований

```csharp
TableExport.Export(backend, backend.Tables, TableExportFormat.Xlsx, "Strings.xlsx"); // CSV/TSV при нескольких таблицах — путь к папке
ExportTable data = TableExport.Collect(backend, "Main");   // key, comment, значения по языкам, флаг StaleMarkup
XlsxWriter.Write(path, new[] { data });  TableJson.Write(path, new[] { data });  TableCsv.Write(data, path, ',');

List<KeyUsage> usages = KeyUsages.Find("Main", "ui.door.locked"); // null — пользователь отменил прогресс
KeyUsages.Reveal(usages[0]);                                       // переход: объект, сцена, префаб или строка скрипта
```

- `ProjectScan` — общий обход проекта (открытые сцены, открытый префаб, префабы и закрытые сцены
  `Assets/`, YAML-префильтр `MayContain`); на нём же работает переписывание разметки при
  переименовании стилей и иконок.
- `SceneTexts.Uses(text, table, key)` — использует ли `TypecraftText` ключ: пустая таблица считается
  совпадением, если ключ находится в этой таблице первым (`TableTextSource.FindTable`).

### Свой объект правки

`ITextEditTarget` описывает, откуда окно берёт текст и куда пишет: заголовок, разметка,
можно ли писать, роль шрифта и `TMP_Text` для размеров превью. Реализуй его для узла
диалогового графа или записи базы данных и открывай через `TypecraftEditor.Edit(target)`.

### Свои подсказки ввода

`IInputGlyphResolver` (ScriptableObject в `TypecraftSettings → Input Glyphs`) превращает
`{key:Action}` в иконку для устройства. Встроенный `InputGlyphProfile` работает с Input System;
для Rewired или своей системы ввода реализуй интерфейс сам.

### Диалоги в своей игре

Рабочий пример всего, что ниже, — сэмпл **Branching Dialogue**.

**Запуск из кода.** `DialoguePlayer` — фасад для сцены: диалог, вывод, события.

```csharp
using Velsi.Typecraft.Dialogue;

[SerializeField] private DialoguePlayer _player;
[SerializeField] private DialogueAsset _greeting;

_player.Play();                       // диалог и стартовая нода из инспектора
_player.Play(_greeting);              // другой диалог
_player.Play(_greeting, nodeId);      // с конкретной ноды (id видно в инспекторе ноды)
_player.Advance();                    // «далее»: сначала допечатать, потом следующая реплика
_player.Choose(choice.Index);         // выбор, если вывод не показывает кнопки сам
_player.Ended += OnDialogueEnded;
```

`Play` возвращает `false`, если ассета нет или `Once` и диалог уже проигран. Без сцены и
вывода — `DialogueRunner`: `new DialogueRunner(variables, catalog)`, события `LineStarted`,
`ChoicesPresented`, `WaitRequested`, `EventRaised`, `Ended`, команды `Start`, `Continue`,
`Choose`, `Stop`. Ожидание (`Wait`, `AfterSeconds`) таймером ведёт плеер, у голого раннера —
вызывающий код.

**Свой вывод.** Наследник `DialoguePresenter`; плееру его даёт поле `Presenter`.

```csharp
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
- Машинка: верни `false` из `IsLineComplete`, пока печатаешь, а `TryCompleteLine()` пусть
  допечатывает и возвращает `true` — тогда первое «далее» допечатает, второе перейдёт дальше.
- Выбор: `SupportsChoices => true` и `ShowChoices(choices, choose)`; у недоступных `Available == false`
  (скрытые в список не попадают), по клику — `choose(choice.Index)`.
- Готовая база с машинкой, показом через `CanvasGroup` и автоскрытием — `TextDialoguePresenter`:
  переопредели `ApplyLine`, `OnLineShowing`, `OnHidden`. Имя говорящего —
  `DialogueSpeakerText.RequestName(speaker, table, style, onReady)`, портрет — `line.Portrait`.

**Свои флаги.** Флаги — `IDialogueVariables` (`TryGet`, `Set`, событие `Changed`). По умолчанию
у всех плееров общий `DialogueGlobals.Variables` (`DialogueVariables` в памяти). Пример — флаги
из своей системы квестов:

```csharp
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
        if (!name.StartsWith(Prefix))
            return _own.TryGet(name, out value);

        value = DialogueValue.Int(_quests.Stage(name.Substring(Prefix.Length)));
        return true;
    }

    public void Set(string name, DialogueValue value)
    {
        if (name.StartsWith(Prefix))
            _quests.SetStage(name.Substring(Prefix.Length), value.AsInt);
        else
            _own.Set(name, value);
    }
}

DialogueGlobals.SetVariables(new QuestFlags(questLog));
DialogueGlobals.Catalog = flagsCatalog;
```

- Всё, что не твоё (посещения `visited:…`, флаги `Once`, локальные флаги диалогов), складывай
  в обычный `DialogueVariables` — иначе `Once` и счётчики посещений перестанут работать.
- Объяви `quest.lighthouse` (int) в каталоге — тогда вкладка «Диалоги» предложит его в условиях,
  проверит тип, а проигрывание в редакторе покажет и даст поменять.
- `SetVariables` вызывай до первого `Play`: плеер пересоздаёт раннер на следующем `Play`,
  уже идущий разговор остаётся на старых флагах. Подписчикам — событие `DialogueGlobals.VariablesReplaced`.
- Свои флаги для одного плеера — компонент-наследник `DialogueVariablesSource` (свойства
  `Variables` и `Catalog`) в поле `Variables Source`.

**События в игру.** Нода Event отдаёт `(имя, аргумент)` синхронно, в момент прохода:

- в инспекторе — `DialoguePlayer → On Event (String, String)`, метод с двумя string (dynamic);
- из кода — `_player.EventRaised += (name, argument) => …`.

Внутри обработчика можно остановить диалог или запустить другой — раннер это переживает.
Если событие должно подождать (анимация двери), поставь после Event ноду Wait или Line.

**Сохранение и восстановление.**

```csharp
[Serializable]
private sealed class DialogueSave
{
    public string Flags;
    public DialogueRunnerState Conversation;
}

var save = new DialogueSave
{
    Flags = ((DialogueVariables)DialogueGlobals.Variables).ToJson(),
    Conversation = _player.State
};
string json = JsonUtility.ToJson(save);

var loaded = JsonUtility.FromJson<DialogueSave>(json);
((DialogueVariables)DialogueGlobals.Variables).FromJson(loaded.Flags);
if (loaded.Conversation != null && loaded.Conversation.IsValid)
    _player.Restore(loaded.Conversation, id => _dialogues.FirstOrDefault(asset => asset.Id == id));
```

- `State` — диалог и нода, на которой стоит разговор (реплика, выбор или ожидание). Сохранять
  его стоит только если сейв возможен посреди разговора; чаще хватает флагов.
- `Restore` возвращает разговор на сохранённую ноду через выводы плеера, не считает повторное
  посещение и работает и у плеера с `Once`. Без resolver плеер ищет только свой `Asset`.
- Голый раннер без выводов: `runner.Restore(state, id => DialogueAsset)`.
- Каталог лучше применить после загрузки: `catalog.ApplyDefaults(variables)` дозаполнит флаги,
  которых не было в старом сейве.
- Свой `IDialogueVariables` сохраняется твоей системой; JSON есть только у `DialogueVariables`.

**Интеграция с ECS-игрой.** Диалог — презентация и сценарий, симуляции он не нужен; граница
проходит по мостам, как у любого MonoBehaviour-мира:

- **Запуск.** На объекте собеседника — authoring, который при рождении сущности вешает на неё
  компонент-мост со ссылкой на `DialoguePlayer` (или на `DialogueTrigger`). Система
  взаимодействия, получив запрос «взаимодействовать» на цели с этим мостом, зовёт
  `player.Play()`. Пока идёт разговор, ввод движения можно гасить тегом на игроке, который
  вешается на `Started` и снимается на `Ended`.
- **События обратно.** Обработчик `EventRaised` не трогает игру напрямую, а превращается в
  данные: вешает на нужную сущность однокадровый компонент-запрос (`OpenDoorRequest`,
  `GiveItemRequest { Id }`), дальше его обрабатывает обычная система в своей стадии конвейера,
  а уборщик снимает в конце тика. Аргумент события — id, а не ссылка на объект.
- **Флаги.** Источник правды — состояние игры: `IDialogueVariables` поверх компонентов или
  сервиса квестов (пример выше), а не копия, которую надо синхронизировать. Посещения и мелкие
  флаги диалогов — в его встроенном `DialogueVariables`.
- **Сохранение.** Флаги диалогов — отдельная секция сейва (`ToJson`), позиция в разговоре —
  `DialogueRunnerState` рядом с сущностью собеседника, если нужна.

---

## English

### Markup field in the Inspector

```csharp
using Velsi.Typecraft;

public sealed class QuestStep : ScriptableObject
{
    [SerializeField, TypecraftMarkup] private string _description;
    [SerializeField, TypecraftMarkup(preview: false)] private List<string> _hints;
}
```

The field highlights markup, suggests tags after `{`, `[`, `<`, shows errors, and 👁 toggles
the preview. ✎ opens the text in the Typecraft window. Edits are written back with Undo.
Legacy IMGUI inspectors get a simplified version.

Without the attribute: right-click any string field → **Edit in Typecraft**.

### Opening the window from code

```csharp
using Velsi.Typecraft.Editor;

TypecraftEditor.Edit(node.Text, text =>
{
    Undo.RecordObject(graph, "Edit line");
    node.Text = text;
}, title: $"Line {node.Id}");

TypecraftEditor.Edit(serializedObject.FindProperty("_description"));
TypecraftEditor.Edit(someTmpText);
```

The window pins itself to that text (🔒) and ignores scene selection. "Apply" invokes the callback.

### Element for your own editors (UI Toolkit)

```csharp
var field = new MarkupTextField("Line", showPreview: true);
field.SetValueWithoutNotify(node.Text);
field.RegisterValueChangedCallback(evt => node.Text = evt.newValue);
field.EditRequested += () => TypecraftEditor.Edit(node.Text, text => field.value = text);
root.Add(field);
```

Without an `EditRequested` subscriber, ✎ opens the window and writes the result back into the field.

### Custom tag `{prefix:argument}`

```csharp
public sealed class PlayerTag : IMarkupTagHandler
{
    public string Prefix => "player";

    public bool TryRender(string argument, MarkupContext context, StringBuilder output)
    {
        if (argument != "name")
            return false;

        output.Append(PlayerProfile.Name);
        return true;
    }
}

Typecraft.Register(new PlayerTag());
```

Return `false` when the tag is not recognised — the editor highlights it as an error. `context`
gives the language, the device and `TryAppendIcon` for inserting icons. To see the tag in the
window preview, register it in the editor too (`[InitializeOnLoadMethod]`), as the Simple
Dialogue sample does.

### Custom text source

```csharp
public sealed class MyTextSource : ScriptableObject, ITextSource
{
    public IReadOnlyList<string> Locales => ...;
    public string BaseLocale => "en";
    public void Activate() { }
    public void Request(string table, string key, string locale, Action<TextEntry> onReady) =>
        onReady(Find(table, key, locale, out string tableId, out string foundLocale, out string text)
            ? new TextEntry(tableId, key, foundLocale, text)
            : TextEntry.Missing(table, key, locale));
    public void SelectLocale(string localeCode) => Typecraft.SetLocale(localeCode);
}
```

Assign the asset to `TypecraftSettings → Text Source`. `Request` returns the **raw** table
string: no pseudo-locale, no argument substitution. Put the table id and the locale the string
actually came from (after falling back to the base language) into `TextEntry` — Typecraft uses
them to find the markup layer entry. The layer, the `pseudo` locale and `{0}` from
`SetArguments` are applied by Typecraft. If the source has its own formatter (like Unity
Localization Smart Strings), pass it as the fifth `TextEntry` argument, `(text, arguments) => …`;
it receives the already decorated text.

### Markup layer in your code

```csharp
LocalizedText.Request(table, key, arguments, (markup, formatted) => ...);
MarkupLayer layer = Typecraft.Settings.FindMarkupLayer(tableId);
string shown = layer != null ? layer.Resolve(key, locale, tableText) : tableText;
```

`LocalizedText.Request` is the same path `TypecraftText` takes: source → layer → formatter →
pseudo-locale; `formatted = false` means `{0}` is not substituted yet. `Resolve` returns the
layer text only when the entry snapshot equals the table string. In the editor:
`Decoration.Strip` (text without decoration), `Decoration.Transfer` (move decoration onto new
text), `MarkupLayers.Write/Set/State` — writing and state of the layer through the current
`ITableBackend`. Layer invariant: `Decoration.Strip(markup) == source`.

To edit its tables in the Localization tab, implement `ITableBackend` and register it:

```csharp
[InitializeOnLoadMethod]
private static void RegisterBackend() =>
    TypecraftEditor.RegisterTableBackend(source => source is MyTextSource my ? new MyBackend(my) : null);
```

### Table sync

Everything goes through `ITableBackend`, so it works the same for built-in tables, Unity
Localization and your own backend. Editor only.

```csharp
SheetImport data = TableCsv.Read(path);                                  // CSV/TSV by extension
data = SheetImport.FromRows(XlsxReader.Read(path, "Strings")); // .xlsx sheet, null = first
TableSyncPlan plan = TableSync.Plan(backend, "Main", data, "my source");
TableSync.Apply(backend, plan, new TableSyncOptions { RemoveMissing = false, ClearEmpty = false });
```

- `SheetImport.FromRows` takes rows of cells from anywhere (your database, your format): the first
  non-empty row is the header `key` / `comment` / language codes.
- `TableSyncPlan` is a report that writes nothing: `AddedKeys`, `Changes` (`Before`/`After`,
  `LosesMarkup` — the string has applied markup in the layer and will turn ⚠ after writing),
  `Comments`, `MissingKeys`, `MissingKeysWithMarkup`, `NewLocales`, `DuplicateKeys`,
  `IgnoredColumns`; `MarkupLost(options)`.
- `TableSync.Apply` is one Undo group; scene texts refresh once at the end.
- Google Sheets: `GoogleSheets.TryParse(link, out GoogleSheetLink sheet)` parses a regular link
  (`.../d/<SHEET_ID>/edit#gid=<GID>`, `?gid=`, `/u/0/d/…`, published `/d/e/…`),
  `GoogleSheetsDownload.Start(sheet, csv => …, error => …)` downloads CSV in the background
  (`Cancel()` to stop), `GoogleSheets.Interpret(status, contentType, body)` turns a login page and
  401/403/404 into a readable `TableSyncException`.
- Configured sources: `TableSyncSources.Find(table)` / `Set(source)` / `Remove(table)`
  (`ProjectSettings/TypecraftTableSync.asset`).

### Export and key usages

```csharp
TableExport.Export(backend, backend.Tables, TableExportFormat.Xlsx, "Strings.xlsx"); // CSV/TSV with several tables takes a folder
ExportTable data = TableExport.Collect(backend, "Main");   // key, comment, values per language, StaleMarkup flag
XlsxWriter.Write(path, new[] { data });  TableJson.Write(path, new[] { data });  TableCsv.Write(data, path, ',');

List<KeyUsage> usages = KeyUsages.Find("Main", "ui.door.locked"); // null when the user cancelled the progress bar
KeyUsages.Reveal(usages[0]);                                       // jump: object, scene, prefab or script line
```

- `ProjectScan` is the shared project walk (open scenes, the open prefab, prefabs and closed scenes
  under `Assets/`, the `MayContain` YAML prefilter); markup rewriting on style and icon renames uses it too.
- `SceneTexts.Uses(text, table, key)` tells whether a `TypecraftText` uses a key: an empty table
  matches when the key resolves to this table first (`TableTextSource.FindTable`).

### Custom edit target

`ITextEditTarget` tells the window where text comes from and where it goes: title, markup,
writability, font role and a `TMP_Text` for preview sizing. Implement it for a dialogue graph
node or a database record and open it with `TypecraftEditor.Edit(target)`.

### Custom input prompts

`IInputGlyphResolver` (a ScriptableObject in `TypecraftSettings → Input Glyphs`) turns
`{key:Action}` into an icon for the device. The built-in `InputGlyphProfile` uses the Input
System; implement the interface yourself for Rewired or a custom input layer.

### Dialogues in your game

The **Branching Dialogue** sample is a working example of everything below.

**Starting from code.** `DialoguePlayer` is the scene-side facade: dialogue, output, events.

```csharp
using Velsi.Typecraft.Dialogue;

[SerializeField] private DialoguePlayer _player;
[SerializeField] private DialogueAsset _greeting;

_player.Play();                       // dialogue and start node from the Inspector
_player.Play(_greeting);              // another dialogue
_player.Play(_greeting, nodeId);      // from a specific node (the id is shown in the node inspector)
_player.Advance();                    // "next": finish typing first, then the next line
_player.Choose(choice.Index);         // a choice, when the output does not show buttons itself
_player.Ended += OnDialogueEnded;
```

`Play` returns `false` when there is no asset, or with `Once` when the dialogue was already
played. Without a scene or an output use `DialogueRunner`: `new DialogueRunner(variables,
catalog)`, events `LineStarted`, `ChoicesPresented`, `WaitRequested`, `EventRaised`, `Ended`,
commands `Start`, `Continue`, `Choose`, `Stop`. Timed waits (`Wait`, `AfterSeconds`) are run by
the player; with a bare runner the calling code runs them.

**A custom output.** Derive from `DialoguePresenter`; the player gets it through `Presenter`.

```csharp
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

- `RaiseLineCompleted()` means "the line is fully shown". Without it `AfterText` never advances.
- Typewriter: return `false` from `IsLineComplete` while typing and make `TryCompleteLine()`
  finish the text and return `true` — then the first "next" finishes the line, the second moves on.
- Choices: `SupportsChoices => true` and `ShowChoices(choices, choose)`; unavailable options have
  `Available == false` (hidden ones are not in the list); on click call `choose(choice.Index)`.
- A ready base with typewriter, `CanvasGroup` show/hide and auto-hide is `TextDialoguePresenter`:
  override `ApplyLine`, `OnLineShowing`, `OnHidden`. The speaker name comes from
  `DialogueSpeakerText.RequestName(speaker, table, style, onReady)`, the portrait from `line.Portrait`.

**Your own flags.** Flags are an `IDialogueVariables` (`TryGet`, `Set`, the `Changed` event). By
default every player shares `DialogueGlobals.Variables` (an in-memory `DialogueVariables`).
Example — flags backed by your quest system:

```csharp
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
        if (!name.StartsWith(Prefix))
            return _own.TryGet(name, out value);

        value = DialogueValue.Int(_quests.Stage(name.Substring(Prefix.Length)));
        return true;
    }

    public void Set(string name, DialogueValue value)
    {
        if (name.StartsWith(Prefix))
            _quests.SetStage(name.Substring(Prefix.Length), value.AsInt);
        else
            _own.Set(name, value);
    }
}

DialogueGlobals.SetVariables(new QuestFlags(questLog));
DialogueGlobals.Catalog = flagsCatalog;
```

- Keep everything that is not yours (`visited:…` counters, `Once`, local dialogue flags) in a
  plain `DialogueVariables`, otherwise `Once` and visit counts stop working.
- Declare `quest.lighthouse` (int) in a catalog — the Dialogues tab then offers it in
  conditions, checks its type, and editor playback shows it and lets you change it.
- Call `SetVariables` before the first `Play`: a player recreates its runner on the next `Play`,
  a conversation in progress keeps the old flags. Listeners get `DialogueGlobals.VariablesReplaced`.
- Per-player flags: a component deriving from `DialogueVariablesSource` (`Variables` and
  `Catalog` properties) in the player's `Variables Source` field.

**Events into the game.** An Event node sends `(name, argument)` synchronously as it is passed:

- in the Inspector — `DialoguePlayer → On Event (String, String)`, a method with two strings (dynamic);
- from code — `_player.EventRaised += (name, argument) => …`.

A handler may stop the dialogue or start another one — the runner handles that. If the game
needs time (a door animation), put a Wait or a Line after the Event node.

**Saving and restoring.**

```csharp
[Serializable]
private sealed class DialogueSave
{
    public string Flags;
    public DialogueRunnerState Conversation;
}

var save = new DialogueSave
{
    Flags = ((DialogueVariables)DialogueGlobals.Variables).ToJson(),
    Conversation = _player.State
};
string json = JsonUtility.ToJson(save);

var loaded = JsonUtility.FromJson<DialogueSave>(json);
((DialogueVariables)DialogueGlobals.Variables).FromJson(loaded.Flags);
if (loaded.Conversation != null && loaded.Conversation.IsValid)
    _player.Restore(loaded.Conversation, id => _dialogues.FirstOrDefault(asset => asset.Id == id));
```

- `State` is the dialogue and the node the conversation stands on (a line, a choice or a wait).
  Save it only if saving mid-conversation is possible; usually the flags are enough.
- `Restore` puts the conversation back on the saved node through the player's presenters, counts
  no extra visit and works with `Once` too. Without a resolver the player only looks at its own `Asset`.
- A bare runner without presenters: `runner.Restore(state, id => DialogueAsset)`.
- After loading you may call `catalog.ApplyDefaults(variables)` to fill flags missing from an
  older save.
- A custom `IDialogueVariables` is saved by your own system; JSON exists only on `DialogueVariables`.

**Integrating with an ECS game.** A dialogue is presentation and script; the simulation does
not need it. The boundary goes through bridges, like any MonoBehaviour world:

- **Starting.** On the NPC object an authoring component, when it creates the entity, adds a
  bridge component referencing the `DialoguePlayer` (or a `DialogueTrigger`). The interaction
  system, receiving an "interact" request on a target with that bridge, calls `player.Play()`.
  While a conversation runs, movement input can be muted with a tag on the player entity added
  on `Started` and removed on `Ended`.
- **Events back.** An `EventRaised` handler does not touch the game directly; it turns into data:
  it adds a one-frame request component to the relevant entity (`OpenDoorRequest`,
  `GiveItemRequest { Id }`), a regular system handles it in its pipeline stage, and a cleanup
  system removes it at the end of the tick. Event arguments are ids, not object references.
- **Flags.** The game state is the source of truth: an `IDialogueVariables` over components or a
  quest service (example above), not a copy that has to be kept in sync. Visits and small dialogue
  flags live in its embedded `DialogueVariables`.
- **Saving.** Dialogue flags are their own save section (`ToJson`); the conversation position is
  a `DialogueRunnerState` next to the NPC entity, if needed.
