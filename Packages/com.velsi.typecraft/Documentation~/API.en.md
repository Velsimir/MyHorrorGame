# Typecraft — API reference

[Русский](API.ru.md) · [English](API.en.md) · [Руководство](Manual.ru.md) · [Manual](Manual.en.md) · [README](../README.md)

Package version `0.2.0` (the public API may change before 1.0). Unity 6000.0+, TextMeshPro (`com.unity.ugui` 2.0), namespace `Velsi.Typecraft.*`.

This document describes the whole public API of the package: the runtime, the dialogue module and the Editor part, including the embedding points (`[TypecraftMarkup]`, `TypecraftEditor.Edit`, `MarkupTextField`, `ITypecraftTab`). Examples were checked against the package sources; the code has no comments on purpose. Fragments without an enclosing type (standalone calls and assignments) are meant to run inside a method.

## Contents

1. [Installation and dependencies](#1-installation-and-dependencies)
2. [Assemblies and define symbols](#2-assemblies-and-define-symbols)
3. [Quick start](#3-quick-start)
4. [Text and markup](#4-text-and-markup)
5. [Styles, icons and input prompts](#5-styles-icons-and-input-prompts)
6. [Fonts per locale](#6-fonts-per-locale)
7. [Text sources, tables and the markup layer](#7-text-sources-tables-and-the-markup-layer)
8. [TMPEffects](#8-tmpeffects)
9. [Dialogues](#9-dialogues)
10. [Editor API](#10-editor-api)
11. [Extending](#11-extending)
12. [Saving and loading](#12-saving-and-loading)
13. [Limitations and pitfalls](#13-limitations-and-pitfalls)
14. [Appendix: menus, assets, storage](#14-appendix-menus-assets-storage)

---

## 1. Installation and dependencies

The main way is the `com.velsi.typecraft-0.2.0.tgz` archive: put it into the project's `Packages/` and install with `Package Manager → + → Install package from tarball…`. Embedded (a folder in `Packages/`) and git URL work too.

On first load the `Velsi.Typecraft.Editor.Welcome` assembly (no dependencies, always compiles) installs the required parts and opens the Welcome window:

| What | How it is installed | Presence check |
| --- | --- | --- |
| TMP Essential Resources | `TMP_PackageResourceImporter.ImportResources(true, false, false)` | a `TMP_Settings` asset exists |
| TMPEffects 0.3.2 | `Vendor~/com.luca3317.tmpeffects-0.3.2.tgz` is copied into the project's `Packages/`, `Client.Add("file:com.luca3317.tmpeffects-0.3.2.tgz")`; without the copy — the git URL | type `TMPEffects.Components.TMPAnimator` |
| TMPEffects Required Resources | `AssetDatabase.ImportPackage(".../TMPEffects Required Resources.unitypackage", false)` | a `TMPEffectsSettings` asset exists |

Each step is tried once per editor session (`SessionState`) and never runs in batch mode. Input System and Localization install with the window's buttons (`Client.Add`). The window shows on first install and on a version change (per project, keyed by `PlayerSettings.productGUID`); manually — `Help → Typecraft Welcome`.

All code that uses TMPEffects, Input System and Localization is behind define symbols (section 2), so the package compiles before the dependencies are installed.

Required `package.json` dependencies: `com.unity.ugui` 2.0.0, `com.unity.2d.sprite` 1.0.0, `com.unity.modules.audio`, `com.unity.modules.physics`, `com.unity.modules.physics2d`.

| Package | What it provides | Without it |
| --- | --- | --- |
| TMPEffects (`com.luca3317.tmpeffects`, 0.3.0 or newer, tested on 0.3.2) | `<wave>` animations, typewriter, `<!wait>` commands | Effect tags do not work |
| Input System (`com.unity.inputsystem`, 1.7.0 or newer) | `{key:Action}` glyphs from bindings, `DialogueInputAdvance` | Input glyphs are not substituted |
| Unity Localization (`com.unity.localization`, 1.4.0 or newer) | The `UnityLocalizationTextSource` adapter | Built-in tables work |

Manual TMPEffects install from git, if you need another version:

```json
"com.luca3317.tmpeffects": "https://github.com/Luca3317/TMPEffects.git?path=/Package#0.3.2"
```

Samples (Package Manager → Typecraft → Samples): **Simple Dialogue**, **Branching Dialogue**, **Showcase**, **Extra Input Prompts** (see the README).

---

## 2. Assemblies and define symbols

| Assembly (asmdef) | Folder | Platforms | Compile condition | Contents |
| --- | --- | --- | --- | --- |
| `Velsi.Typecraft.Runtime` | `Runtime` | all | — | Core: `Typecraft`, `TypecraftText`, markup, styles, icons, fonts, text sources, markup layer |
| `Velsi.Typecraft.InputSystem` | `Runtime.InputSystem` | all | `TYPECRAFT_INPUTSYSTEM` | `InputGlyphProfile`, `DeviceTracker`, `ControlGlyph` and related types |
| `Velsi.Typecraft.Localization` | `Runtime.Localization` | all | `TYPECRAFT_LOCALIZATION` | `UnityLocalizationTextSource` |
| `Velsi.Typecraft.Dialogue` | `Runtime.Dialogue` | all | — | Dialogue module: asset, nodes, runner, player, presenters |
| `Velsi.Typecraft.Dialogue.InputSystem` | `Runtime.Dialogue.InputSystem` | all | `TYPECRAFT_INPUTSYSTEM` | `DialogueInputAdvance` |
| `Velsi.Typecraft.Editor` | `Editor` | Editor | — | The Typecraft window, integration, tables, library, fonts, art |
| `Velsi.Typecraft.Dialogue.Editor` | `Editor.Dialogue` | Editor | — | The "Dialogues" tab, inspectors, checks, playback |
| `Velsi.Typecraft.Dialogue.Editor.Graph` | `Editor.Dialogue.Graph` | Editor | disabled by `TYPECRAFT_NO_DIALOGUE_GRAPH` | The graph on `UnityEditor.Experimental.GraphView` |
| `Velsi.Typecraft.Editor.Welcome` | `Editor.Welcome` | Editor | — | The Welcome window and automatic dependency install; references only `Unity.TextMeshPro` |

There are nine assemblies: `Velsi.Typecraft.InputSystem` and `Velsi.Typecraft.Localization` are separate runtime assemblies with conditional compilation.

`Velsi.Typecraft.Runtime` references `Unity.TextMeshPro` and `TMPEffects`; `Runtime` grants `InternalsVisibleTo("Velsi.Typecraft.Editor")`.

### Define symbols

| Symbol | Where it comes from | What it enables |
| --- | --- | --- |
| `TYPECRAFT_INPUTSYSTEM` | `versionDefines`: `com.unity.inputsystem` ≥ 1.7.0 | The `InputSystem` and `Dialogue.InputSystem` assemblies, Input System branches in the Editor |
| `TYPECRAFT_LOCALIZATION` | `versionDefines`: `com.unity.localization` ≥ 1.4.0 | The `Localization` assembly, `UnityLocalizationBackend` |
| `TYPECRAFT_TMPEFFECTS` | `versionDefines`: `com.luca3317.tmpeffects` ≥ 0.3.0 | `TMPWriter`/`TMPAnimator` handling in `TypecraftText`, `LineTypewriter`, the effect editor |
| `TYPECRAFT_NO_DIALOGUE_GRAPH` | Manually: Player Settings → Scripting Define Symbols | Disables the `Dialogue.Editor.Graph` assembly; the "Dialogues" tab opens in list view |

The graph is the only code that uses `UnityEditor.Experimental.GraphView`. The rest of the tab does not depend on it: board views are discovered through `IDialogueBoardView` (see [10.11](#1011-dialogue-editor)).

---

## 3. Quick start

1. `Tools/Typecraft/Create Project Settings` creates in `Assets/Typecraft/` a style library, a font profile (roles `Body` and `Header`), an input glyph profile (with Input System), a built-in string table `Main` with three starter strings, and `TypecraftSettings` itself in `Assets/Typecraft/Resources/`.
2. Add `Typecraft Text` to an object with a `TMP_Text`: mode **Inline** (markup right in the component) or **Localized** (table + key).
3. `Window → Typecraft` (Ctrl+Alt+T) opens the editor window.

From code:

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

One-off rendering of a string without a component:

```csharp
string rendered = Typecraft.Render("Press {key:Interact} to [warn]open[/warn]");
```

Settings are loaded lazily from `Resources/TypecraftSettings`. Without the asset `Typecraft.Settings` returns `null` and `Render` works without styles and icons.

---

## 4. Text and markup

### 4.1. Syntax

| Form | What it does |
| --- | --- |
| `{key:Interact}` | The glyph of an action button for the current device (built-in handler with the `key` prefix) |
| `{candle}`, `{set/candle}` | An icon from a set: by key alone or with the set id |
| `[warn]…[/warn]` | A style from the `StyleLibrary` |
| `{player:name}` | A custom tag: `{prefix:argument}`, the handler is registered with `Typecraft.Register` |
| `{0}` | A numeric placeholder: passes through as is and is filled by `string.Format` in `TypecraftText` (when the source has no formatter of its own) |
| `<wave>…</wave>`, `<!wait=0.5>` | TMP and TMPEffects tags — pass through as is |
| `\{`, `\}`, `\[`, `\]`, `\<`, `\>`, `\\` | Escaping (`\<` becomes `<noparse><</noparse>`) |

Parsing rules (from `MarkupRenderer`):

- Spans cannot cross a line break; inside `{…}` and `[…]` a second opening bracket of the same kind aborts the parse and the bracket stays text.
- A `{…}` that starts with a digit, or is empty, is kept as is (a format placeholder).
- `{prefix:arg}` looks up a handler by `prefix`. No handler — `MarkupIssue.UnknownTag`; the handler returned `false` — `UnresolvedTag`. In the Editor a red `[…]` marker is printed in place of the tag, in a build nothing is printed.
- `{name}` without a colon is an icon. Not found — `UnknownIcon`. A set placeholder without a sprite — `PlaceholderIcon`.
- `[id]` is a style. Identifier: a letter, then letters, digits, `_`, `-`, `.`. Unknown style — `UnknownStyle`, the text stays as is.
- Closing a style with `[/id]` closes and re-opens the nested styles above it. A closing tag with no opening one — `StrayClosingStyle` (the tag is removed); an unclosed style is closed at the end — `UnclosedStyle`.
- Auto-tags are applied before parsing (see [4.6](#46-auto-tags)).

### 4.2. `Typecraft` — the static facade

`namespace Velsi.Typecraft`, `public static class Typecraft`.

| Member | Description |
| --- | --- |
| `static event Action Changed` | Fires on locale or device change, tag registration and `Refresh()`. `TypecraftText` and presenters subscribe to it |
| `static TypecraftSettings Settings` | The settings; on first access loaded from `Resources` at `TypecraftSettings.ResourcesPath` (may be `null`) |
| `static string Locale` | The current locale; without `SetLocale` — `Settings.DefaultLocale`, `"en"` if there are no settings |
| `static DeviceFamily Device` | The current device; without `SetDevice` — `Settings.DefaultDevice` |
| `static IconLibrary Icons` | The icon library: sets from `Settings.IconSets` and the sets of the input resolver |
| `static IReadOnlyDictionary<string, IMarkupTagHandler> TagHandlers` | Registered handlers by prefix (always contains `key`) |
| `static void Initialize(TypecraftSettings settings)` | Replace the settings (resets caches, rebuilds auto-tags and the TMP asset registry) |
| `static void Register(IMarkupTagHandler handler)` | Register or replace a handler by `Prefix`. `null` and an empty prefix are ignored |
| `static void SetLocale(string locale)` | Change the locale. An empty string or the same code does nothing |
| `static void SetDevice(DeviceFamily device)` | Change the device |
| `static void Refresh()` | Rebuild caches and raise `Changed` |
| `static string Render(string markup)` | Render to a TMP rich-text string |
| `static string Render(string markup, List<MarkupDiagnostic> diagnostics)` | The same with diagnostics (the cache is bypassed on read) |
| `static string RenderPreview(string markup, string locale, DeviceFamily device, List<MarkupDiagnostic> diagnostics, List<int> sourceMap)` | Render for a given locale and device without the cache; `sourceMap[i]` is the index in the source markup of result character `i` |

The `Render` cache holds up to 1024 entries keyed by the markup text and is cleared on every `Changed`. Statics are reset on `RuntimeInitializeLoadType.SubsystemRegistration` (so "Enter Play Mode without domain reload" works).

### 4.3. `TypecraftText`

`namespace Velsi.Typecraft.Text`, `MonoBehaviour`, `[ExecuteAlways]`, `[DisallowMultipleComponent]`, `[RequireComponent(typeof(TMP_Text))]`, menu `Typecraft/Typecraft Text`.

| Member | Description |
| --- | --- |
| `TextMode Mode` | `Inline` (markup in the field) or `Localized` (table and key) |
| `string Markup`, `Table`, `Key` | Current field values |
| `TMP_Text Target` | The target `TMP_Text` |
| `string FontRole { get; set; }` | Font role; empty — `Settings.DefaultFontRole`. The setter refreshes the text right away |
| `void SetMarkup(string markup)` | `Inline` mode and new markup |
| `void SetKey(string table, string key)` | `Localized` mode, table and key. Empty table: `TableTextSource` takes the first table that has the key; Unity Localization uses its default table |
| `void SetArguments(params object[] arguments)` | Arguments for `{0}`; refreshes the text |
| `void Refresh()` | Redraw (does nothing on an inactive component) |

What `Refresh` does: it picks a font and material by role and locale (`FontProfile.Resolve`), requests the text (in `Localized` mode through `LocalizedText.Request`), renders it with `Typecraft.Render`, then fills in the arguments (`string.Format` with `CurrentCulture`, only if the source did not format the string itself) and, if the object has no `TMPWriter`, cuts command tags out of the result (`EffectTags.StripWriterTags`). Stale asynchronous answers are discarded.

```csharp
_text.SetMarkup("Coins: {0}");
_text.SetArguments(12);
_text.FontRole = "Header";
```

### 4.4. Markup types (`Velsi.Typecraft.Markup`)

```csharp
public interface IMarkupTagHandler
{
    string Prefix { get; }
    bool TryRender(string argument, MarkupContext context, StringBuilder output);
}
```

`KeyTagHandler` (`public sealed`) is the built-in `key` handler: it asks `Settings.InputGlyphs.TryAppendGlyph(argument, context.Device, output)`.

`MarkupContext` (`public sealed class`):

| Member | Description |
| --- | --- |
| `MarkupContext(TypecraftSettings settings, IconLibrary icons, string locale, DeviceFamily device, List<MarkupDiagnostic> diagnostics)` | Constructor |
| `TypecraftSettings Settings`, `string Locale`, `DeviceFamily Device`, `List<MarkupDiagnostic> Diagnostics` | Render context |
| `void Report(MarkupIssue issue, string value)` | Add a diagnostic at the current parse position |
| `bool TryAppendIcon(string reference, StringBuilder output)` | Insert an icon (`key` or `set/key`); `false` if there is no such icon |
| `static void AppendIcon(IconSet set, IconEntry entry, StringBuilder output)` | Write `<sprite="…" name="…">` (with `tint=1` for tintable icons) and register the sprite asset in the TMP registry |
| `static void AppendMissing(string reference, StringBuilder output)` | A red `[reference]` marker (Editor only) |

`MarkupDiagnostic` is a `readonly struct { MarkupIssue Issue; string Value; int Index }` with constructor `(issue, value, index)`.

`enum MarkupIssue { UnknownIcon, PlaceholderIcon, UnknownStyle, UnknownTag, UnresolvedTag, StrayClosingStyle, UnclosedStyle }`.

`MarkupRenderer` (`public static`):

```csharp
static string Render(string markup, MarkupContext context, IReadOnlyDictionary<string, IMarkupTagHandler> handlers)
static string Render(string markup, MarkupContext context, IReadOnlyDictionary<string, IMarkupTagHandler> handlers, List<int> sourceMap)
```

Normally call `Typecraft.Render`; calling `MarkupRenderer.Render` directly is only needed when you build the context by hand.

`EffectTags` (`public static`) parses TMP effect tags in an already rendered string:

| Method | Description |
| --- | --- |
| `bool IsWriterTag(string name)` | A typewriter command tag: the name starts with `!` or `#` (unless it is an HTML colour) |
| `bool IsShowHideTag(string name)` | The name starts with `+` or `-` (appear/disappear effect) |
| `void Scan(string rendered, Func<string, bool> isAnimation, out bool animations, out bool commands)` | Whether the string contains animations and/or commands |
| `string StripWriterTags(string rendered)` | Cut command tags out (`<noparse>` is skipped) |

`TypecraftMarkupAttribute` (`[AttributeUsage(Field)]`, a `PropertyAttribute`) — `TypecraftMarkup(bool preview = true)`, property `Preview`. Drawn by `TypecraftMarkupDrawer` (see [10.3](#103-the-markup-field-in-the-inspector-typecraftmarkup)).

### 4.5. `DeviceFamily` and `LocaleCodes`

`enum DeviceFamily { KeyboardMouse, Xbox, PlayStation, Switch, SteamDeck, GenericGamepad, Touch }`.

`LocaleCodes.Language(string locale)` returns the part before the first `-` or `_` (`"zh-Hans"` → `"zh"`), and `""` for an empty string.

### 4.6. Auto-tags

Auto-tags wrap words in a style without manual markup. Rule sources:

- `TextStyle.AutoTagWords` — the style's words (edited in the Library and with the "Auto-tag ▾" button in the Editor tab); template `[id]$0[/id]`.
- `TypecraftSettings.AutoTags` — a list of `AutoTagRule` with their own words and template (default `[style]$0[/style]`).

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

`Build` returns `null` when there are no words. A match is a whole word, case-insensitive; already marked-up spans (`[x]…[/x]`, `{…}`, `<…>`, escapes) are left alone. In `Template`, `$0` is the found word; `AutoTags` rules override style words when the word is the same.

`Typecraft` wires auto-tags itself (on `Initialize`/`Refresh`); you rarely need `AutoTagger` by hand.

---

## 5. Styles, icons and input prompts

### 5.1. Styles (`Velsi.Typecraft.Styles`)

`StyleLibrary` (`ScriptableObject`, menu `Typecraft/Style Library`):

| Member | Description |
| --- | --- |
| `IReadOnlyList<TextStyle> Styles` | All styles |
| `bool TryGet(string id, out TextStyle style)` | Look up by id |

`TextStyle` (`[Serializable] sealed class`):

| Member | Description |
| --- | --- |
| `string Id`, `DisplayName` | The id (used in `[id]`) and a label (defaults to `Id`) |
| `string FontRole`, `MaterialPreset` | Font role and material preset (see [6](#6-fonts-per-locale)) |
| `TMP_ColorGradient Gradient` | A gradient instead of a colour |
| `IReadOnlyList<string> AutoTagWords` | Auto-tag words |
| `void AppendOpen(StringBuilder output, FontProfile fonts, string locale)` | Write the opening TMP tags |
| `void AppendClose(StringBuilder output, FontProfile fonts, string locale)` | The closing tags in reverse order |

The rest (colour, `b/i/u`, case, size in percent, spacing in `em`, effect tag) is kept in private serialized fields and edited in the Inspector or the window. Opening tag order: `font`, `gradient`/`color`, `size`, `cspace`, `b`, `i`, `u`, case (`uppercase`/`lowercase`/`smallcaps`), effect tag.

`enum TextCase { None, Upper, Lower, SmallCaps }`.

### 5.2. Icons (`Velsi.Typecraft.Library`)

`IconSet` (`ScriptableObject`, menu `Typecraft/Icon Set`): `string SetId`, `TMP_SpriteAsset SpriteAsset`, `IReadOnlyList<IconEntry> Entries`, `IconSetBuildSettings BuildSettings`, `bool TryGetEntry(string key, out IconEntry entry)`, `bool TryGetGlyphName(string key, out string glyphName)` (`false` for placeholders).

`IconEntry` (`[Serializable]`): `const float MinScale = 0.25f`, `MaxScale = 3f`, constructor `(string key, string glyphName, bool tintable)`, properties `Key`, `GlyphName`, `Tintable`, `IsPlaceholder` (no glyph name), `Offset` (`Vector2`), `Scale` (always > 0).

`IconSetBuildSettings` (`[Serializable]`): `SourceFolder` (get/set), `IncludeSubfolders`, `Padding` (0–8), `MaxAtlasSize`, `FilterMode`, `VerticalOffset`, `TintableByDefault`.

`IconLibrary` — icon lookup:

```csharp
IconLibrary(IEnumerable<IconSet> sets)
void Add(IconSet set)
bool TryResolve(string reference, out IconSet set, out IconEntry entry)
IEnumerable<string> QualifiedKeys { get; }
```

`TryResolve` accepts `set/key` or just `key` (the first set with that key).

Icons in text sit on the line by the lower-left corner of the canvas; the image standard is a 128×128 square with the artwork inside 104×104 (see `Documentation~/ART_RULES.en.md`, template `IconTemplate_128.png`).

### 5.3. Input prompts

`namespace Velsi.Typecraft.Input`:

```csharp
public interface IInputGlyphResolver
{
    IEnumerable<IconSet> IconSets { get; }
    void Activate();
    bool TryAppendGlyph(string actionName, DeviceFamily device, StringBuilder output);
}
```

An asset resolver is a `ScriptableObject` assigned in `TypecraftSettings → Input Glyphs` (if the asset does not implement the interface, the Inspector warns and clears the field). `Activate()` is called once, on the first `Render`.

`namespace Velsi.Typecraft.InputSystem` (condition `TYPECRAFT_INPUTSYSTEM`):

`InputGlyphProfile` (`ScriptableObject`, `IInputGlyphResolver`, menu `Typecraft/Input Glyph Profile`):

| Member | Description |
| --- | --- |
| `InputActionAsset Actions` | The actions asset in which `actionName` is looked up |
| `IReadOnlyList<FamilyGlyphs> Families` | Per-device-family settings |
| `IEnumerable<IconSet> IconSets` | The sets of all families |
| `void Activate()` | Starts `DeviceTracker.Start()` |
| `bool TryAppendGlyph(string actionName, DeviceFamily device, StringBuilder output)` | First the family's override for the action, then icons from bindings, then the `Fallback` chain (no deeper than 4) |
| `void Configure(InputActionAsset actions, List<FamilyGlyphs> families, List<ActionGlyphOverride> overrides)` | Set the data from code |

For composite bindings (WASD, arrows) all parts are printed in up/left/down/right order. If any control has no icon the whole family is rejected and the `Fallback` is used.

`FamilyGlyphs`: constructor `(DeviceFamily family, IconSet icons, DeviceFamily? fallback, List<ControlGlyph> controls)`, properties `Family`, `Icons`, `Fallback`, `Controls`, method `string FindIconKey(string control)`.

`ControlGlyph`: `(string control, string iconKey)`; `Control`, `IconKey`. When there is no mapping, the lower-cased control name is used as the icon key.

`ActionGlyphOverride`: `(DeviceFamily family, string action, string markup)`; `Family`, `Action`, `Markup` (markup such as `{gestures/tap}`; rendered through `Typecraft.Render`).

`DefaultControlGlyphs`: `static List<ControlGlyph> For(DeviceFamily family)` (keyboard/mouse, Xbox, PlayStation, Switch, Steam Deck; empty for the others), `static List<ActionGlyphOverride> TouchOverrides()` (gestures for Interact, Attack, Jump, Crouch, Sprint, Move, Look, Previous, Next).

`DeviceTracker`:

```csharp
static void Start()
static DeviceFamily FamilyOf(InputDevice device)
```

`Start()` works only in Play Mode: it subscribes to any button press and calls `Typecraft.SetDevice`, and picks the initial family by platform. `FamilyOf`: keyboard/mouse → `KeyboardMouse`, `Touchscreen` → `Touch`, gamepads by layout and product name (Steam Deck, DualShock/DualSense, Switch Pro, XInput/Xbox, otherwise `GenericGamepad`).

Actions have no touch bindings, so for Touch the glyphs come from `ActionGlyphOverride` entries.

---

## 6. Fonts per locale

`namespace Velsi.Typecraft.Fonts`.

`FontProfile` (`ScriptableObject`, `Typecraft/Font Profile`): `IReadOnlyList<FontRole> Roles`, `bool TryGetRole(string roleId, out FontRole role)`, `FontVariant Resolve(string roleId, string locale)` (`null` if there is no such role).

`FontRole` (`[Serializable]`): `Id`, `Default` (`FontVariant`), `LocaleOverrides` (`IReadOnlyList<LocaleFontOverride>`), `FontVariant Resolve(string locale)`.

`Resolve` order: exact locale code match (case-insensitive) → the first match by language (`LocaleCodes.Language`) → `Default`. Overrides without a font are skipped.

`LocaleFontOverride`: `Locale`, `Variant`.

`FontVariant`: `TMP_FontAsset Font`, `IReadOnlyList<FontMaterialPreset> Materials`, `Material FindMaterial(string presetId)`. `FontMaterialPreset`: `Id`, `Material`.

A style with its own `FontRole` inserts `<font="…" material="…">` for the current locale's font. Typecraft registers fonts, material presets, sprite assets and gradients with TMP itself (`TMP_Text.OnFontAssetRequest`, `OnSpriteAssetRequest`); you do not need to put them in `Resources`.

For CJK, build a static font for the character set used by the strings (the "Fonts" tab, `StaticFontBuilder`, see [10.8](#108-fonts-library-and-art)).

---

## 7. Text sources, tables and the markup layer

### 7.1. `ITextSource` and `TextEntry`

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

| Member | Contract |
| --- | --- |
| `Locales` | Available locales (including the virtual `pseudo` if supported) |
| `BaseLocale` | The base locale used as fallback |
| `Activate()` | Once, on the first `Render`: subscriptions, syncing the current locale |
| `Request(...)` | Return the **raw** table string (no pseudo-localisation, no argument substitution); the result goes to `onReady`, synchronously or later |
| `SelectLocale(code)` | Switch the source's locale (for the built-in one — `Typecraft.SetLocale`) |

`TextEntry` is a `readonly struct`:

```csharp
TextEntry(string table, string key, string locale, string text, Func<string, IList<object>, string> format = null)
string Table, Key, Locale, Text
Func<string, IList<object>, string> Format
bool Found
static TextEntry Missing(string table, string key, string locale)
```

In `TextEntry` put the table id and locale the string was actually taken from (after falling back to the base locale) — they are used to find the markup-layer entry. `Format` is the source's own formatter (Smart Strings): it receives the already decorated text and the arguments.

The source is assigned in `TypecraftSettings → Text Source` (the field is stored as a `ScriptableObject`; an asset that does not implement `ITextSource` is cleared with a warning). `TypecraftSettings.TextSource` returns it as an `ITextSource`.

### 7.2. `LocalizedText`

```csharp
static void Request(string table, string key, IList<object> arguments, Action<string, bool> onReady)
static string Decorate(TextEntry entry)
```

`Request` is the same path as `TypecraftText` uses: source → markup layer → the source's formatter → pseudo-locale. `onReady(text, formatted)`; `formatted == false` means `{0}` has not been filled yet. No source or an empty key — `onReady(key, true)`. Not found — `onReady(key, true)`. `Decorate` returns the text with the layer applied (`entry.Text` if there is no layer).

```csharp
LocalizedText.Request("Main", "ui.door.locked", null, (markup, formatted) =>
{
    string rendered = Typecraft.Render(markup);
    Debug.Log(rendered);
});
```

### 7.3. Built-in tables (`Velsi.Typecraft.Text.Tables`)

`TableTextSource` (`ScriptableObject`, `ITextSource`, menu `Typecraft/Text Sources/String Tables`):

| Member | Description |
| --- | --- |
| `IReadOnlyList<string> Locales` | Project locales plus `pseudo` (if enabled) |
| `IReadOnlyList<string> ProjectLocales` | Project locales only |
| `string BaseLocale` | The base locale (an empty field — the first locale in the list, otherwise `"en"`) |
| `IReadOnlyList<StringTable> Tables` | The tables |
| `TextEntry Lookup(string table, string key, string locale)` | The string in the locale; if missing — in the base locale (`TextEntry.Locale` reflects the real one); missing there too — `Missing` |
| `StringTable FindTable(string table, string key = null)` | A table by id; with an empty `table` — the first one that contains the key |
| `Request`, `Activate`, `SelectLocale` | `ITextSource` implementation; `SelectLocale` calls `Typecraft.SetLocale` |

`StringTable` (`Typecraft/String Table`): `string TableId` (empty field — the asset name), `IReadOnlyList<StringEntry> Entries`, `bool TryGet(string key, string locale, out string value)` (empty values count as missing), `bool Contains(string key)`.

`StringEntry`: `Key`, `Comment` (get), `bool TryGet(string locale, out string value)`. `LocalizedValue`: `(string locale, string text)`, `Locale`, `Text`.

Writing to the tables goes through the Editor (the "Localization" tab, `ITableBackend`, see [10.5](#105-localization-tables)): the mutating methods of these types are internal.

### 7.4. `MarkupLayer` — the markup layer

`MarkupLayer` (`ScriptableObject`, `Typecraft/Markup Layer`): decoration on top of a table.

| Member | Description |
| --- | --- |
| `string TableId` | The id of the table the layer belongs to |
| `IReadOnlyList<MarkupLayerEntry> Entries` | The entries |
| `MarkupLayerEntry Find(string key, string locale)` | An entry or `null` |
| `string Resolve(string key, string locale, string tableText)` | The layer text if the entry's snapshot matches the table string and the markup is not empty; otherwise `tableText` |

`MarkupLayerEntry`: `(string key, string locale)`, `Key`, `Locale`, `Markup` (full text with decoration), `Source` (a snapshot of the table string the decoration was applied to). The setters are internal — only the Editor writes.

Layers are listed in `TypecraftSettings → Markup Layers`; `TypecraftSettings.FindMarkupLayer(string tableId)` returns a table's layer or `null`.

Layer invariant: `Decoration.Strip(entry.Markup) == entry.Source`. When the table string changes, `Source` stops matching and `Resolve` returns the plain table string (the entry is "stale").

### 7.5. Pseudo-locale

`Pseudolocalizer` (`public static`): `const string Locale = "pseudo"`, `bool IsPseudo(string locale)`, `string Transform(string markup)`. It wraps the text in `\[ … \]`, replaces letters with accented ones and appends a tail of `·` (+35% of the length); tags, placeholders and escapes are left alone. The pseudo-locale is built from the decorated text of the base locale.

### 7.6. The Unity Localization adapter

`namespace Velsi.Typecraft.Localization`, condition `TYPECRAFT_LOCALIZATION`. `UnityLocalizationTextSource` (`ScriptableObject`, `ITextSource`, menu `Typecraft/Text Sources/Unity Localization`) has a private field `_defaultTable` (the table to use when a request names none).

- `Locales` — the codes from `LocalizationSettings.AvailableLocales` plus `pseudo`; `BaseLocale` — `ProjectLocale` or `"en"`.
- `Activate()` — in Play Mode subscribes to `SelectedLocaleChanged` and syncs `Typecraft.SetLocale`; in Edit Mode sets the project locale.
- `Request` — asynchronous via `StringDatabase.GetTableEntryAsync`; the `TextEntry` carries the table collection name, the real locale and a `Format` that uses `SmartFormatter` for Smart strings and `string.Format` otherwise.
- `SelectLocale(code)` — in Play Mode changes `LocalizationSettings.SelectedLocale`; `pseudo` goes only to `Typecraft.SetLocale`.

### 7.7. Sync, export, key usages

Editor only (no runtime): CSV/TSV, `.xlsx`, Google Sheets, export to `.xlsx`/CSV/TSV/JSON, finding key usages. API details — [10.5](#105-localization-tables) and [10.6](#106-sync-export-and-key-usages).

---

## 8. TMPEffects

Enabled by the `TYPECRAFT_TMPEFFECTS` symbol (TMPEffects ≥ 0.3.0). The tags `<wave>…</wave>`, `<+fade>`, `<-fade>`, `<!wait=0.5>`, `<#cmd>` pass through the markup unchanged and are handled by TMPEffects components on the text object.

- Animations (`<x>`, `<+x>`, `<-x>`) need a `TMPAnimator`; commands (`<!…>`, `<#…>`) and the typewriter need a `TMPWriter`. The Editor adds them itself (`EffectComponents.Ensure`).
- `TypecraftText` cuts command tags if the object has no `TMPWriter` (otherwise raw `<!wait>` would end up in the text).
- `TypecraftSettings.AnimationDatabase` is the project animation database (`TypecraftAnimationDatabase`); the window preview and the `TMPAnimator` that Typecraft adds use it. The first custom effect creates the database in `Effects/` next to the `StyleLibrary`: it references all built-in TMPEffects animations plus your own. A `TMPAnimator` added by hand takes the TMPEffects database by default — pick the project database in it for your own effects.
- A style can carry an effect tag (the `effectTag` field, for example `wave`); the closing tag is built from the name up to the first space or `=`.

The typewriter for dialogue lines is `LineTypewriter` (see [9.10](#910-presenters)). The Editor API for effects is `EffectCatalog`, `EffectLibrary`, `EffectComponents` ([10.9](#109-effects-in-the-window)).

---

## 9. Dialogues

The dialogue module depends on neither ECS nor Zenject. Assembly `Velsi.Typecraft.Dialogue`, namespace `Velsi.Typecraft.Dialogue` (`DialogueInputAdvance` lives in `Velsi.Typecraft.Dialogue.InputSystem`). Line texts are Typecraft table keys, so translation, the markup layer, fonts and checks work as for any other text. A working example is the **Branching Dialogue** sample.

### 9.1. `DialogueAsset`

A `ScriptableObject`, menu `Create → Typecraft → Dialogue`. A set of nodes that link to each other.

| Member | Description |
| --- | --- |
| `string Id` | A stable identifier for saves (a GUID without dashes); assigned automatically. For a copy of the asset it is reissued in the Editor (`DialogueIdGuard`) |
| `string DefaultTable { get; set; }` | The default table for nodes without their own |
| `string StartNodeId { get; set; }` | The main start node; empty — the first `StartNode`; `null` if there are no start nodes |
| `IReadOnlyList<DialogueNode> Nodes` | All nodes |
| `string ResolveTable(string tableOverride)` | `tableOverride`, or `DefaultTable` when empty |
| `bool TryGetNode(string id, out DialogueNode node)` | Look up by node `Id` |
| `DialogueNode FindNode(string id)`, `T FindNode<T>(string id)` | The same, `null` if missing |
| `T AddNode<T>(T node)` | Add a node (an empty `Id` is filled in); the first `StartNode` becomes the main one |
| `bool RemoveNode(DialogueNode node)` | Remove a node |
| `void InvalidateLookup()` | Reset the lookup cache (after manual edits of the list) |
| `void RegenerateId()` | Issue a new asset `Id` |

Nodes are stored with `[SerializeReference]`, so the node type is part of the asset data.

### 9.2. Nodes

The base class:

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

`Position` is the location on the graph board. A link is the target node's `Id` in a string field.

| Node | Fields | Ports (names in `DialogueLink.Port`) | Behaviour |
| --- | --- | --- | --- |
| `StartNode` | `Next` | `Next` | Entry point. There can be several; the main one is chosen with the «Сделать основной» (make main) button in the inspector (`StartNodeId`) |
| `LineNode` | `Speaker` (`SpeakerProfile`), `Emotion`, `Table`, `Key`, `Variants` (`List<LineVariant>`), `Advance` (`LineAdvance`), `Seconds`, `Next` | `Next` | A line. `SelectVariant(variables, catalog)` returns the first variant with a non-empty key and a satisfied condition |
| `ChoiceNode` | `Options` (`List<ChoiceOption>`) | `Option 0`, `Option 1`, … | Answer options. Each has a condition and what happens when it fails: `Hide`, or `Disable` (shown but not clickable) |
| `BranchNode` | `Cases` (`List<BranchCase>`), `Else` | `Case 0`, …, `Else` | `Evaluate(variables, catalog)` returns the `Next` of the first case whose condition holds, otherwise `Else` |
| `SetVariablesNode` | `Assignments` (`List<VariableAssignment>`), `Next` | `Next` | `Apply(variables, catalog)` applies the assignments |
| `EventNode` | `EventName`, `Argument`, `Next` | `Next` | An event into the game |
| `WaitNode` | `Seconds` (default 1), `Next` | `Next` | A pause |
| `JumpNode` | `Target` (`DialogueAsset`), `TargetNode` | none | A jump to another dialogue or to a node of this one. Empty `TargetNode` — the target's main start; empty `Target` — this same dialogue |
| `EndNode` | — | none | The end |

Helper types:

- `LineVariant` — `Condition`, `Key`, `Emotion`: another key and emotion under a condition (the first match wins).
- `LineAdvance { Input, AfterSeconds, AfterText }` — advance on input, after `Seconds`, or `Seconds` after the text is fully shown.
- `ChoiceOption` — `Table`, `Key`, `Condition`, `WhenUnavailable` (`ChoiceUnavailable { Hide, Disable }`), `Next`.
- `BranchCase` — `Condition`, `Next`.
- `VariableAssignment` — `Name`, `Operation` (`VariableOperation { Set, Add, Toggle }`), `Value`; constructors `()` and `(string name, VariableOperation operation, DialogueValue value = default)`; `Apply(variables, catalog)`. `Add` adds numbers and concatenates strings; `Toggle` inverts a bool (an unset value comes from the catalog).

Building a dialogue in code:

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

### 9.3. Conditions

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

An empty condition always holds; so does `null` in `Condition.IsMet`. `All` mode requires every comparison, `Any` at least one. An unset flag takes its value from the catalog, and if it is not there either — the type's default value (`DialogueVariableLookup.Get`).

```csharp
var condition = new Condition()
    .Add("has_key", ComparisonOperator.IsTrue)
    .Add("visits", ComparisonOperator.GreaterOrEqual, DialogueValue.Int(2));

bool met = condition.Evaluate(DialogueGlobals.Variables, DialogueGlobals.Catalog);
```

### 9.4. Values and flags

`DialogueValue` is a `[Serializable] struct`, `IEquatable<DialogueValue>`:

| Member | Description |
| --- | --- |
| `DialogueValueType Type`, `bool IsNumber` | The type (`Bool`, `Int`, `Float`, `String`) |
| `AsBool`, `AsInt`, `AsFloat`, `AsString` | Conversions (a string is parsed to a number with the invariant culture, 0 on failure) |
| `static Bool(bool)`, `Int(int)`, `Float(float)`, `String(string)` | Factories |
| `static DefaultOf(DialogueValueType)` | The type's default value |
| `ConvertTo(DialogueValueType)` | Convert |
| `static Parse(string text)`, `Parse(string text, DialogueValueType type)` | Parse a string (bool, then int, then float, otherwise a string) |
| `static Add(left, right)`, `static Compare(left, right)` | Add and compare (numbers numerically, bool by `AsBool`, otherwise ordinal string comparison) |
| `==`, `!=`, `Equals`, `GetHashCode`, `ToString()` | Equality is `Compare == 0`; `ToString` is invariant text |

```csharp
public interface IDialogueVariables
{
    event Action<string> Changed;
    bool TryGet(string name, out DialogueValue value);
    void Set(string name, DialogueValue value);
}
```

`DialogueVariables` is the in-memory implementation:

| Member | Description |
| --- | --- |
| `int Count`, `IEnumerable<string> Names` | Contents |
| `bool TryGet(...)`, `void Set(...)` | `Set` with the same type and value raises no event |
| `bool Remove(string name)`, `void Clear()` | Removal (both raise `Changed` per name) |
| `event Action<string> Changed` | The name of the changed flag |
| `string ToJson(bool prettyPrint = false)` | A snapshot of all flags (including visits) |
| `void FromJson(string json)` | Replace the contents with a snapshot; `Changed` for the names that differ |

`DialogueVariableCatalog` (`ScriptableObject`, menu `Typecraft/Dialogue Variables`) declares flags:

```csharp
IReadOnlyList<DialogueVariableDefinition> Variables { get; }
bool TryGet(string name, out DialogueVariableDefinition definition)
bool Contains(string name)
void Add(DialogueVariableDefinition definition)
bool Remove(string name)
void ApplyDefaults(IDialogueVariables variables, bool overwrite = false)
void InvalidateLookup()
```

`DialogueVariableDefinition`: constructors `()` and `(string name, DialogueValue defaultValue, string description = null)`; `Name`, `Type`, `Default` (converted to `Type`), `Description` — all with setters.

`DialogueVariableLookup.Get(IDialogueVariables variables, DialogueVariableCatalog catalog, string name, DialogueValueType fallback)` returns a flag's value: from `variables`, otherwise from the catalog, otherwise `DefaultOf(fallback)`.

`DialogueGlobals` (`static`) holds the shared flags and the default catalog:

```csharp
static event Action VariablesReplaced
static IDialogueVariables Variables { get; }
static DialogueVariableCatalog Catalog { get; set; }
static void SetVariables(IDialogueVariables variables)
```

`Variables` is created lazily as a `DialogueVariables`. The statics are reset on `SubsystemRegistration`.

`DialogueVariablesSource` is an abstract `MonoBehaviour` for custom flags of a single player: `abstract IDialogueVariables Variables { get; }`, `virtual DialogueVariableCatalog Catalog => null`.

Node visits are counted automatically — flags `visited:<dialogue id>/<node id>` (`DialogueRunner.VisitedVariable(assetId, nodeId)`, prefix `DialogueRunner.VisitedPrefix`). The runner counts visits of `Line` and `Choice` nodes; the visit of the start node is recorded by `DialoguePlayer.Play`.

### 9.5. `DialogueRunner`

Plain C#, no `MonoBehaviour`: walks the graph and reports steps through events.

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

- `Start` begins at `nodeId` or at the main start; nodes without output (`Start`, `Branch`, `SetVariables`, `Event`, `Jump`) are passed through immediately, and it stops at `Line`, `Choice`, `Wait` and `End`. No start, an empty link or a link to a missing node — a console warning and `Ended`. More than `MaxAutoSteps` steps without output (a loop) — a console error and `Ended`.
- `Continue()` works on `Line` and `Wait` steps; on `Choice` and outside a conversation it returns `false`.
- `Choose(index)` takes a `DialogueChoice.Index`; an unavailable option returns `false`.
- A `Choice` with no available option ends the dialogue with a warning.
- `EventRaised(name, argument)` and `NodeEntered` fire synchronously while the node is passed; a handler may stop the dialogue or start another one — the runner survives it (a generation counter).
- Waiting (`Wait`, `AfterSeconds`) is driven by the player's timer; for a bare runner it is up to the calling code to call `Continue()` after the time has passed.
- `State` is the `DialogueRunnerState` of the current place (`null` if no conversation is running).
- `Restore` puts the conversation back on a saved node without counting a repeat visit.

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

`DialogueRunnerState` is a `[Serializable] sealed class` with public fields `string AssetId`, `string NodeId` and a property `bool IsValid`.

### 9.6. Steps, lines, choices

`DialogueStep` (`readonly struct`): `DialogueStepKind Kind`, `DialogueAsset Asset`, `string NodeId`, `DialogueLine Line`, `IReadOnlyList<DialogueChoice> Choices`, `float Seconds`. `enum DialogueStepKind { None, Line, Choice, Wait }`.

`DialogueLine` (`sealed class`): constructor `(DialogueAsset asset, string nodeId, SpeakerProfile speaker, string emotion, Sprite portrait, string table, string key, LineAdvance advance, float seconds)`; properties `Asset`, `NodeId`, `Speaker`, `Emotion`, `Portrait`, `Table`, `Key`, `Advance`, `Seconds`; `string ResolveTable()` (the table taking the dialogue's `DefaultTable` into account).

`DialogueChoice`: constructor `(DialogueAsset asset, string nodeId, int index, string table, string key, bool available)`; properties `Asset`, `NodeId`, `Index` (the option's number in the node, not in the list of shown options), `Table`, `Key`, `Available`; `string ResolveTable()`.

### 9.7. Validating a dialogue

`DialogueValidator` (`static`):

```csharp
static List<DialogueIssue> Validate(DialogueAsset asset, DialogueVariableCatalog catalog = null)
static void Validate(DialogueAsset asset, DialogueVariableCatalog catalog, List<DialogueIssue> issues)
```

It finds: no start, empty and broken links, unreachable nodes, an empty key, empty and undeclared flags, a flag type mismatch, an empty event name, a broken jump, empty and duplicate `Id`s, nodes of an unknown type. Checks that involve the tables (translation, stale markup, font characters, a speaker without a name, an unknown emotion) are done by the Editor-side `DialogueChecks`.

`DialogueIssue` (`readonly struct`): constructor `(DialogueIssueSeverity severity, DialogueIssueKind kind, string nodeId, string message, string subject = null, string detail = null)`; properties `Severity`, `Kind`, `NodeId`, `Message`, `Subject`, `Detail`.

`enum DialogueIssueSeverity { Info, Warning, Error }`.

`enum DialogueIssueKind { NoStartNode, MissingNodeType, EmptyNodeId, DuplicateNodeId, EmptyLink, BrokenLink, Unreachable, EmptyKey, NoOptions, EmptyVariable, UndeclaredVariable, VariableTypeMismatch, EmptyEventName, BrokenJump, MissingKey, MissingTranslation, StaleMarkup, MissingGlyphs, SpeakerWithoutName, UnknownEmotion }`.

### 9.8. `SpeakerProfile`

A `ScriptableObject`, menu `Create → Typecraft → Dialogue Speaker`.

| Member | Description |
| --- | --- |
| `string Id` | The identifier; empty — the asset name |
| `string NameTable`, `NameKey` | The name key (own table; empty — the line's table) |
| `string StyleId` | The name style |
| `List<SpeakerPortrait> Portraits`, `Sprite DefaultPortrait` | Portraits per emotion and the default portrait |
| `AudioClip VoiceBlip` | The "mumble" sound played with the typewriter |
| `SpeakerSide Side` | `None`, `Left`, `Right` — the left or right portrait of the box |
| `string ResolveNameTable(string fallbackTable)` | The name table |
| `Sprite PortraitFor(string emotion)` | The portrait for an emotion (case-insensitive) or the default one |

`SpeakerPortrait`: constructors `()` and `(string emotion, Sprite sprite)`, properties `Emotion`, `Sprite`.

### 9.9. `DialoguePlayer`

A `MonoBehaviour`, menu `Typecraft/Dialogue/Dialogue Player`. A scene facade: dialogue, presenter, flags, events.

Properties (with get/set unless noted):

| Property | Description |
| --- | --- |
| `Asset` | The default dialogue |
| `Presenter` | The main presenter (`DialoguePresenter`) |
| `ChoicePresenter` | A separate presenter for choices, if the main one cannot show them |
| `StartNodeId` | The start node; empty — the main start |
| `PlayOnStart` | Play in `Start()` |
| `Once` | Do not play again (remembered in flags: the `visited` flag of the start node) |
| `TimeMode` | `DialogueTimeMode { Scaled, Unscaled }` for timers |
| `VariablesSource` | Custom flags; empty — `DialogueGlobals` |
| `OnStarted`, `OnEnded` (get) | `UnityEvent` |
| `OnEvent` (get) | `DialogueEventUnityEvent : UnityEvent<string, string>` — a method with two `string`s (dynamic) |
| `Runner`, `IsPlaying`, `Current` (get) | The runner, whether a conversation is running, the current step |
| `Variables`, `Catalog` (get) | The effective flags and catalog |
| `HasPlayed` (get) | Whether the start of `Asset`/`StartNodeId` has been passed |
| `State` (get) | A `DialogueRunnerState` or `null` |

C# events: `Started`, `Ended`, `EventRaised(string name, string argument)`, `LineStarted(DialogueLine)`, `ChoicesPresented(IReadOnlyList<DialogueChoice>)`.

Methods:

| Method | Description |
| --- | --- |
| `bool Play()` | Play `Asset` from `StartNodeId` |
| `bool Play(DialogueAsset asset, string startNodeId = null)` | Play another dialogue or start from a node. `false` if there is no asset, or `Once` is set and the dialogue has already been played; a running conversation is stopped |
| `bool HasPlayedFrom(DialogueAsset asset, string startNodeId = null)` | Whether the start was visited |
| `bool Advance()` | "Next": if the presenter is typing a line it finishes the typing (`true`), otherwise it goes to the next step. `false` outside a line |
| `bool Choose(int index)` | A choice (`DialogueChoice.Index`); for when the presenter does not show buttons itself |
| `void Stop()` | Stop |
| `bool Restore(DialogueRunnerState state, Func<string, DialogueAsset> resolver = null)` | Put the conversation back on a saved node through the player's presenters; without a `resolver` it looks only at its own `Asset` |

Calls from code (`nodeId` is the node id, shown in the node inspector):

```csharp
_player.Play();
_player.Play(_greeting);
_player.Play(_greeting, nodeId);
_player.Advance();
_player.Choose(choice.Index);
_player.Ended += OnDialogueEnded;
```

Behaviour: waiting (`Wait`, `AfterSeconds`, `AfterText`) is driven by the player's timer in `Update`, using `Time.time` or `Time.unscaledTime`. `OnDisable` stops the conversation (raising `Ended`). If no presenter supports choices the player logs a warning and picks the first available option. The runner is recreated when the `Variables` reference changes; the catalog is refreshed on every `Play`.

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

`DialogueTrigger` — a `MonoBehaviour`, menu `Typecraft/Dialogue/Dialogue Trigger`:

| Member | Description |
| --- | --- |
| `Player`, `Mode`, `Tag`, `Once` | The player, the mode `DialogueTriggerMode { Manual, OnStart, OnTriggerEnter }`, the collider tag (empty — any), one-shot |
| `bool HasFired` | Has already fired |
| `bool Fire()` | Call the player's `Play()`. `false` if the player is busy, a one-shot trigger has already fired, or `Play` refused |
| `void ResetFired()` | Reset `HasFired` |

`OnTriggerEnter` and `OnTriggerEnter2D` work in the `OnTriggerEnter` mode.

`DialogueInputAdvance` (`Velsi.Typecraft.Dialogue.InputSystem`, condition `TYPECRAFT_INPUTSYSTEM`, menu `Typecraft/Dialogue/Dialogue Input Advance`): `Player`, `InputActionReference Advance` (re-subscribes automatically if the component is enabled); on `performed` it calls `Player.Advance()` while a conversation is running. It enables the action if it is disabled and the "Enable Action" flag is set (and disables it back only if it enabled it).

### 9.10. Presenters

`DialoguePresenter` is an abstract `MonoBehaviour`, the base of any presenter:

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

Contract: `RaiseLineCompleted()` means "the line is fully shown"; without it the `AfterText` advance never fires. Typewriter: `IsLineComplete` returns `false` while typing, and `TryCompleteLine()` finishes the typing and returns `true` — so the first "next" finishes the line and the second moves on. Choices: `SupportsChoices => true` and `ShowChoices` (unavailable options have `Available == false`, hidden ones are not in the list; on click call `choose(choice.Index)`).

`TextDialoguePresenter` (abstract, extends `DialoguePresenter`) is a ready base with a typewriter, showing through a `CanvasGroup` (or the text alpha) and auto-hiding:

| Member | Description |
| --- | --- |
| `CanvasGroup Root`, `TypecraftText Text` | The root and the text (filled from the components in `Reset`) |
| `float HideAfter { get; set; }` | Hide N seconds after the line is shown; 0 — never hide |
| `bool IsVisible`, `DialogueLine CurrentLine` | State |
| `virtual void ApplyLine(DialogueLine line)` | Write the line into the text (by default `Text.SetKey(line.ResolveTable(), line.Key)`) |
| `virtual void OnLineShowing(DialogueLine)` | Before typing |
| `virtual void OnLineShown()` | The line is fully shown |
| `virtual void OnCharacterShown(char)` | A character was shown |
| `virtual void OnHidden()` | After hiding |
| `virtual void OnTypecraftChanged()` | The locale or device changed while a line is active |
| `virtual void SetVisible(bool)`, `Reset`, `Awake`, `OnEnable`, `OnDisable`, `OnDestroy`, `Update` | Lifecycle; call `base` when overriding |
| `protected LineTypewriter Typewriter`, `protected float Now` | The typewriter and the time (honouring the "Unscaled Time" flag) |

Ready-made presenters (created from `GameObject → Typecraft`):

- `DialogueBoxPresenter` (`Typecraft/Dialogue/Dialogue Box Presenter`) — the speaker name, portraits left and right (`SpeakerSide.Right` → the right one), the typewriter (TMPEffects), a "next" indicator, choice buttons (`IReadOnlyList<Button> ChoiceButtons`), sound (an `AudioSource` and the interval between "mumbles"). `SupportsChoices` is true when a container and a button prefab are set (a button with a `TypecraftText` among its children).
- `SubtitlePresenter` (`Typecraft/Dialogue/Subtitle Presenter`) — the line `{name}: text`; `ShowSpeakerName`, `NamePrefix` (the `{name}` substitution), hides after `HideAfter` (2 s after `Reset`).
- `WorldTextPresenter` (`Typecraft/Dialogue/World Text Presenter`) — world-space text facing the camera: `FaceCamera`, `Camera` (empty — `Camera.main`); `HideAfter` is 3 s after `Reset`.

`DialogueSpeakerText` (`static`) fetches texts for presenters:

```csharp
static void RequestName(SpeakerProfile speaker, string fallbackTable, string fallbackStyle, Action<string> onReady)
static void RequestLine(DialogueLine line, Action<string> onReady)
static string Styled(string markup, string styleId)
```

`RequestName` returns the name wrapped in its style (`[style]name[/style]`), or `null` if the speaker has no name key.

`LineTypewriter` (`sealed class`, `IDisposable`) wraps a `TMPWriter` for a single line:

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

Without a `TMPWriter` on the object (or without the `TYPECRAFT_TMPEFFECTS` symbol, or outside Play Mode) the line is considered shown immediately.

A custom presenter — [11.3](#113-a-custom-dialogue-presenter).

---

## 10. Editor API

Types of the Editor assemblies compile in the Editor only. The embedding points meant for use from your own code: `[TypecraftMarkup]`, `TypecraftEditor`, `MarkupTextField`, `ITextEditTarget`, `ITypecraftTab`, `TypecraftWindow.ShowTab`, `ITableBackend` with `TableBackends`, `KeyUsages.RegisterSource`, `IDialogueBoardView`, and the static table helpers (`TableSync`, `TableExport`, `TableCsv`, `XlsxReader`, `GoogleSheets`, `MarkupLayers`, `Decoration`). The other `public` types are the internals of the window and its tabs; they are listed for completeness, and before version 1.0 their signatures may change.

### 10.1. Editor assembly namespaces

| Namespace | Contents |
| --- | --- |
| `Velsi.Typecraft.Editor` | `TypecraftEditor`, `MarkupTextField`, `TypecraftMarkupDrawer`, `TypecraftTextInspector`, `ProjectScan` |
| `Velsi.Typecraft.Editor.Window` | `TypecraftWindow`, `ITypecraftTab`, `EditorTab`, `LocalizationTab`, `LibraryTab`, `FontsTab` |
| `Velsi.Typecraft.Editor.Window.Targets` | `ITextEditTarget` and the edit targets, `PlayModeEdits` |
| `Velsi.Typecraft.Editor.Window.Markup` | `MarkupField`, `MarkupHighlighter`, `MarkupCandidates`, `MarkupDocument`, `MarkupEdits`, `TagInspector` |
| `Velsi.Typecraft.Editor.Window.Preview` | `TextPreview`, `IconLinePreview`, `ThumbnailRenderer` |
| `Velsi.Typecraft.Editor.Window.Drawer` | `GalleryDrawer` |
| `Velsi.Typecraft.Editor.Window.Effects` | `EffectCatalog`, `EffectLibrary`, `EffectComponents`, `EffectComponentsWarning`, `EffectForm` |
| `Velsi.Typecraft.Editor.Window.Dialogs` | `FormOverlay`/`TypecraftForm`, `ExportDialog`, `KeyUsagesWindow`, `SyncSourceDialog`, `TableSyncDialog`, `TextChangeDialog` |
| `Velsi.Typecraft.Editor.Window.Layout` | `ColumnsLayout`, `SplitPane`, `TypecraftLayout` |
| `Velsi.Typecraft.Editor.Localization` | Tables: `ITableBackend`, `TableBackends`, sync, export, layers, checks, key usages |
| `Velsi.Typecraft.Editor.Library` | Styles, icon sets, `MarkupRewriter` |
| `Velsi.Typecraft.Editor.Fonts` | Fonts and material presets |
| `Velsi.Typecraft.Editor.Art` | Building icon sets and atlases |
| `Velsi.Typecraft.Editor.Setup` | `TypecraftSetup` |
| `Velsi.Typecraft.Dialogue.Editor` | `DialoguesTab`, `DialogueMenu`, `DialogueIdGuard` |
| `Velsi.Typecraft.Dialogue.Editor.Board` | `IDialogueBoardView`, `DialogueBoards`, `DialogueListBoard` |
| `Velsi.Typecraft.Dialogue.Editor.Inspector` | Inspectors of nodes, speakers and flags, and the pick field |
| `Velsi.Typecraft.Dialogue.Editor.Model` | `DialogueEdits`, `DialogueChecks`, `DialogueProject`, `DialogueTexts` and others |
| `Velsi.Typecraft.Dialogue.Editor.Playback` | `DialoguePlaybackPanel`, `DialogueIssuesPanel` |
| `Velsi.Typecraft.Dialogue.Editor.Graph` | The GraphView graph |

Internal types (`TypecraftLog`, `TypecraftContextMenu`, `DialogueAssetInspector`, `DialogueIdPostprocessor`, `DialogueKeyUsages`, `DialogueOpenAsset`, `DialogueValueDrawer`) are not part of the API.

### 10.2. The window and `TypecraftEditor`

`TypecraftEditor` (`namespace Velsi.Typecraft.Editor`, `public static`) is the main embedding entry point:

```csharp
static void Edit(string markup, Action<string> onApply, string title = null, TMP_Text reference = null)
static void Edit(ITextEditTarget target)
static void Edit(TMP_Text text)
static void Edit(SerializedProperty property)
static void RegisterTableBackend(Func<ITextSource, ITableBackend> factory)
```

- `Edit(string, Action<string>, …)` opens the window on arbitrary text. "Apply" calls the callback; `reference` supplies a `TMP_Text` for preview sizing.
- `Edit(ITextEditTarget)` opens the window on your own target ([10.4](#104-edit-targets)).
- `Edit(TMP_Text)` picks the target: a `TypecraftText` in `Localized` mode → `LocalizedTextTarget`, in `Inline` mode → `TypecraftTextTarget`, a bare `TMP_Text` → `TmpTextTarget`.
- `Edit(SerializedProperty)` takes string properties only; `m_text` of a `TMP_Text` and `_markup` of a `TypecraftText` lead to the matching targets, anything else goes to `PropertyTarget`.
- `RegisterTableBackend` registers your own `ITableBackend` for your own `ITextSource` ([10.5](#105-localization-tables)).

```csharp
TypecraftEditor.Edit(node.Text, text =>
{
    Undo.RecordObject(graph, "Edit line");
    node.Text = text;
}, title: $"Line {node.Id}");

TypecraftEditor.Edit(serializedObject.FindProperty("_description"));
TypecraftEditor.Edit(someTmpText);
```

The window pins itself to that text (🔒) and does not switch to the scene selection.

`TypecraftWindow` (`EditorWindow`, `namespace Velsi.Typecraft.Editor.Window`, menu items `Window/Typecraft` Ctrl+Alt+T and `Tools/Typecraft/Open Window`):

```csharp
static TypecraftWindow Open()
static void Edit(ITextEditTarget target)
static void ShowTab<T>(Action<T> then = null) where T : class, ITypecraftTab
override void SaveChanges()
```

`ShowTab<T>` opens the window on the extra tab of type `T` and hands it to `then` (if the window is still being built, it runs after the build). `SaveChanges` offers to write an unsaved edit of the words of a localisation string.

### 10.3. The markup field in the Inspector (`[TypecraftMarkup]`)

```csharp
using UnityEngine;
using Velsi.Typecraft;

public sealed class QuestStep : ScriptableObject
{
    [SerializeField, TypecraftMarkup] private string _description;
    [SerializeField, TypecraftMarkup(preview: false)] private System.Collections.Generic.List<string> _hints;
}
```

The field highlights markup, suggests tags after `{`, `[`, `<`, and shows errors; 👁 turns the preview on, ✎ opens the text in the Typecraft window. The edit is written to the field with Undo. It also works in old IMGUI inspectors (a simplified view, `OnGUI`). `string` fields only — other types show an error message.

Without the attribute: right-click any string field → **Edit in Typecraft**; for a `TMP_Text` there is an `Edit in Typecraft` item in the component menu (`CONTEXT/TMP_Text`).

`TypecraftMarkupDrawer` (a `PropertyDrawer` for `TypecraftMarkupAttribute`): `CreatePropertyGUI(SerializedProperty)` (UI Toolkit, built on `MarkupTextField`), `GetPropertyHeight`, `OnGUI` (IMGUI). `TypecraftTextInspector` (an `Editor` for `TypecraftText`, `CreateInspectorGUI`) is the default inspector plus an `EffectComponentsWarning` bar when the markup needs a `TMPAnimator` or `TMPWriter` that the object lacks.

**`MarkupTextField`** (`namespace Velsi.Typecraft.Editor`, a `VisualElement`, `INotifyValueChanged<string>`) is a UI Toolkit element for your own editors: a markup field with highlighting, a 👁 preview button and a ✎ button.

| Member | Description |
| --- | --- |
| `MarkupTextField()` | Without a label |
| `MarkupTextField(string label, bool showPreview = false)` | With a label; the preview is on right away if `showPreview` |
| `string label` | The label |
| `bool ShowPreview` | Show or hide the preview |
| `string value` | The value; changing it sends a `ChangeEvent<string>` |
| `void SetValueWithoutNotify(string newValue)` | Set the value without an event |
| `event Action EditRequested` | The ✎ button was pressed. Without a subscriber the button opens the window itself (`TypecraftEditor.Edit`) and returns the text to the field |

```csharp
var field = new MarkupTextField("Line", showPreview: true);
field.SetValueWithoutNotify(node.Text);
field.RegisterValueChangedCallback(evt => node.Text = evt.newValue);
field.EditRequested += () => TypecraftEditor.Edit(node.Text, text => field.value = text);
root.Add(field);
```

The field validates the markup through `Typecraft.RenderPreview` with the current locale and device and highlights errors.

### 10.4. Edit targets

`ITextEditTarget` (`namespace Velsi.Typecraft.Editor.Window.Targets`) describes where the window takes the text from and where it writes it back:

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

| Member | Purpose |
| --- | --- |
| `Title` | The tab caption |
| `Markup` | The source markup |
| `IsValid` | The target still exists |
| `CanWrite`, `ReadOnlyReason` | Whether writing is allowed; the reason if not |
| `FontRole` / `SetFontRole` | The preview font role and changing it |
| `Reference` | A `TMP_Text` for preview sizing (may be `null`) |
| `Write(markup)` | Write the result ("Apply") |

Ready implementations: `CallbackTarget(string markup, Action<string> onApply, string title, TMP_Text reference)`, `PropertyTarget(SerializedProperty)`, `TmpTextTarget(TMP_Text)`, `TypecraftTextTarget(TypecraftText)` (+ `Component`), `LocalizedTextTarget(TypecraftText text, Func<string> locale)` (+ `TableText`, `Subject`, `bool ChangesWords(string markup)`, `TransferResult RestoreWords(string markup)`, `IEnumerable<string> AllMarkups()`), `ScratchTarget` (an empty scratch area).

`PlayModeEdits` (`static`): `bool KeepAfterPlay { get; set; }` (EditorPrefs, on by default), `static void Record(TMP_Text text, string markup)` — remember an edit made in Play Mode and apply it after leaving it (by `GlobalObjectId`; objects created at runtime do not survive leaving).

A custom edit object (a graph node, a database record):

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

Open the window on this target:

```csharp
TypecraftEditor.Edit(new QuestNoteTarget(note));
```

The window treats `FontRole == null` as "no role set" and uses `TypecraftSettings.DefaultFontRole`; `SetFontRole` is called when the user picks a role in the gallery.

### 10.5. Localization tables

`namespace Velsi.Typecraft.Editor.Localization`.

`ITableBackend` is everything the "Localization" tab, sync, export and checks need to know about a string store. That is why they work the same for built-in tables, Unity Localization and your own backend.

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

| Member | Contract |
| --- | --- |
| `Name` | The backend name shown in the UI |
| `Locales`, `BaseLocale`, `Tables` | Locales, the base locale, table identifiers |
| `Keys(table)` | The table's keys |
| `Get(table, key, locale)` | The table string (without the markup layer) |
| `Comment`/`SetComment` | A comment for translators |
| `Set` | Write a string (the layer is the caller's concern) |
| `AddKey`, `RemoveKey`, `RenameKey`, `AddLocale` | Table operations; `false` — failed |
| `CreateTable(name)` | Create a table and return its id |
| `Asset(table)` | The table asset (for navigation and for choosing the layer folder) |

`TableBackends` (`static`): `void Register(Func<ITextSource, ITableBackend> factory)`, `ITableBackend Current` — the backend for the source in `TypecraftSettings` (registered factories first, then the built-in one for `TableTextSource` and `UnityLocalizationBackend` for the adapter; otherwise `null`). Implementations: `BuiltInTableBackend(TableTextSource source)` and `UnityLocalizationBackend` (with `TYPECRAFT_LOCALIZATION`).

`MarkupLayers` (`static`) is the markup layer as the Editor sees it:

| Method | Description |
| --- | --- |
| `MarkupLayer Find(string table)` | The table's layer |
| `MarkupLayerEntry Entry(string table, string key, string locale)` | The entry with non-empty markup |
| `LayerState State(ITableBackend backend, string table, string key, string locale)` | `None`, `Applied`, `Stale` (the snapshot does not match the table string) |
| `string Display(ITableBackend backend, string table, string key, string locale)` | The text to display (the layer if applied, otherwise the table) |
| `void Write(ITableBackend, table, key, locale, string markup)` | Write the words to the table (if they changed) and the decoration to the layer |
| `bool Set(ITableBackend, table, key, locale, string markup)` | Layer only; `false` if the words of the markup do not match the table string. Markup without decoration removes the entry |
| `void Remove(table, key, locale)`, `RemoveKey(table, key)`, `RenameKey(table, key, newKey)` | Layer edits (with Undo) |
| `MarkupLayer GetOrCreate(ITableBackend backend, string table)` | Find the layer or create `MarkupLayer_<table>.asset` next to the table and register it in the settings |

`Decoration` (`static`): `Strip(markup)` — text without decoration (layer invariant: `Strip(markup) == source`), `Has(markup)` — whether there is decoration, `Tags(markup)`, `Diff(before, after)` → `List<DiffPart>` (`DiffPart(DiffKind kind, string text)`, `DiffKind { Same, Removed, Added }`), `Transfer(markup, text)` → `TransferResult` (moves decoration onto new text by matching words: `Markup`, `Approximate`, `Dropped`, `Broken`, `Exact`, `Describe()`), `RichDiff(IEnumerable<DiffPart>, bool before)` — highlighted differences for display.

`MarkupLayerMigration` moves decoration out of table cells into layers (menu `Tools/Typecraft/Move Markup To Layers`): `static MarkupLayerMigration Plan(ITableBackend backend)`, `Cells` (`IReadOnlyList<Cell>` — `Cell(table, key, locale, markup, text)`), `string Describe()`, `void Apply(Func<string, MarkupLayer> layerFor)`.

`LocalizationChecks` (`static`): `RowReport Check(ITableBackend backend, string table, string key)`, `string Signature(string markup)` (the tag set used to compare locales), `string MissingCharacters(string locale, string markup)` (characters missing from the locale's font). `RowReport` — `Cells` (`Dictionary<string, CellIssue>`), `Messages`, `All`, an indexer by locale. `[Flags] enum CellIssue { None = 0, Missing = 1, Overflow = 2, MissingGlyph = 4, MarkupMismatch = 8, StaleMarkup = 16 }`.

`SceneTexts` (`static`) — `TypecraftText` in open scenes: `Localized(table, key)`, `Uses(TypecraftText text, string table, string key)` (an empty table counts as a match if the key is found first in this table), `ResolvesTo(table, key)`, `RefreshAll()`, `Batch()` → `IDisposable` (defer the text refresh until the end of the block; nesting is allowed).

`TextPrompt` (`EditorWindow`): `static string Show(string title, string message, string initial)` — a modal string prompt (`ShowModalUtility`).

Registering your own backend:

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

### 10.6. Sync, export and key usages

Everything goes through `ITableBackend`, so it works the same for built-in tables, Unity Localization and a custom backend. Editor only.

```csharp
using Velsi.Typecraft.Editor.Localization;

ITableBackend backend = TableBackends.Current;

SheetImport data = TableCsv.Read("Assets/Strings.csv");
data = SheetImport.FromRows(XlsxReader.Read("Assets/Strings.xlsx", "Strings"));

TableSyncPlan plan = TableSync.Plan(backend, "Main", data, "my source");
TableSync.Apply(backend, plan, new TableSyncOptions { RemoveMissing = false, ClearEmpty = false });
```

`SheetImport` is a parsed table: `const string KeyColumn = "key"`, `CommentColumn = "comment"`; `Locales`, `Rows` (`SheetImport.Row`: `Key`, `Comment`, `Line`, `Values`), `IgnoredColumns`, `DuplicateKeys`, `HasComments`; `static SheetImport FromRows(IReadOnlyList<IReadOnlyList<string>> rows)` accepts rows of cells from anywhere: the first non-empty row holds the headers `key` / `comment` / locale codes.

Reading files: `TableCsv.Read(string path)`, `Read(string path, char delimiter)`, `Parse(string text, char delimiter)`, `char DelimiterFor(string path)` (by extension); `XlsxReader.SheetNames(string path)`, `Read(string path, string sheet = null)` (`null` — the first sheet), `ColumnIndex(string reference)`, `RowIndex(string reference)`. It reads without third-party libraries; the old `.xls` is not supported.

`TableSync` (`static`): `TableSyncPlan Plan(ITableBackend backend, string table, SheetImport data, string origin = null)`, `void Apply(ITableBackend backend, TableSyncPlan plan, TableSyncOptions options)` (one Undo group, scene texts refreshed once at the end), `string Summary(TableSyncPlan, TableSyncOptions)`.

`TableSyncOptions`: the fields `AddLocales = true`, `ClearEmpty` (an empty source cell erases the translation), `RemoveMissing` (delete keys that are absent from the source).

`TableSyncPlan` is a report that writes nothing:

| Member | Description |
| --- | --- |
| `Table`, `Origin` | The table and the source label |
| `NewLocales` | Locales that are not in the project |
| `AddedKeys` | New keys (`List<SheetImport.Row>`) |
| `Changes` | `List<CellChange>`: `Key`, `Locale`, `Before`, `After`, `NewLocale`, `LosesMarkup` (the string has applied layer markup that will become ⚠ after the write), `Clears` |
| `Comments` | `List<CommentChange>`: `Key`, `Before`, `After`, `Clears` |
| `MissingKeys`, `MissingKeysWithMarkup` | Keys absent from the source (and those of them with markup) |
| `DuplicateKeys`, `IgnoredColumns` | Parse warnings |
| `IsEmpty` | Nothing to apply |
| `EffectiveChanges(options)`, `EffectiveComments(options)`, `AddedValues(row, options)`, `int MarkupLost(options)` | What will actually be written given the options |

Sources and their storage: `TableSyncSource(string table, TableSyncKind kind, string path, string sheet, string url)` (`enum TableSyncKind { Csv, Tsv, Xlsx, GoogleSheets }`, properties `Table`, `Kind`, `Path`, `Sheet`, `Url`, `IsFile`, `FullPath`, `Describe()`, `SheetImport ReadFile()`, `static TableSyncKind KindFor(string path)`); `TableSyncSources` (a `ScriptableSingleton`, file `ProjectSettings/TypecraftTableSync.asset`): `static TableSyncSource Find(string table)`, `static void Set(TableSyncSource source)`, `static void Remove(string table)`. Paths inside the project are stored relative.

Google Sheets (by a public link, no keys or OAuth):

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

- `GoogleSheets.TryParse(string link, out GoogleSheetLink parsed)` — a regular link (`…/d/<SHEET_ID>/edit#gid=<GID>`, `?gid=`, `/u/0/d/…`, published `/d/e/…`). `GoogleSheetLink` — `Id`, `Gid`, `Published`, `ExportUrl`, constructor `(string id, string gid, bool published)`.
- `GoogleSheetsDownload.Start(GoogleSheetLink link, Action<string> completed, Action<string> failed)` — downloads the CSV in the background; `IsRunning`, `DownloadedBytes`, `Progress`, `Cancel()`.
- `GoogleSheets.Interpret(long status, string contentType, string body)` — turns a login HTML page and 401/403/404 into a readable error; `const string ClosedMessage`. `TableSyncException : Exception` — the parse and download exception.

Export:

```csharp
TableExport.Export(backend, backend.Tables, TableExportFormat.Xlsx, "Strings.xlsx");

ExportTable table = TableExport.Collect(backend, "Main");
XlsxWriter.Write("Strings.xlsx", new[] { table });
TableJson.Write("Strings.json", new[] { table });
TableCsv.Write(table, "Main.csv", ',');
```

- `enum TableExportFormat { Xlsx, Csv, Tsv, Json }`; `TableExport.Extension(format)`, `WritesFolder(format, tables)` (CSV/TSV write a folder for several tables), `Collect(backend, table)` → `ExportTable`, `Export(backend, IReadOnlyList<string> tables, format, path)` → `List<string>` (the written paths).
- `ExportTable`: `Id`, `Locales`, `Rows`, `Header`, `Cells(row)`. `ExportRow`: `Key`, `Comment`, `StaleMarkup`, `Values`.
- `XlsxWriter.Write(path, tables)`, `SheetNames(ids)`, `Reference(column, row)`; `TableJson.Write(path, tables)`, `Serialize(tables)`, `Quote(text)`; `TableCsv.Export(backend, table, path[, delimiter])`, `Write(ExportTable table, string path, char delimiter)`.
- The JSON format: `{ "tables": [ { "id", "locales", "entries": [ { "key", "comment", "values" } ] } ] }`, UTF-8 without BOM. Markup-layer decoration is not exported.

Finding key usages:

```csharp
List<KeyUsage> usages = KeyUsages.Find("Main", "ui.door.locked");
if (usages != null && usages.Count > 0)
    KeyUsages.Reveal(usages[0]);
```

- `KeyUsages.Find(string table, string key)` → `List<KeyUsage>` (`null` — the user cancelled the progress bar): `TypecraftText` in Localized mode in open and closed scenes and in prefabs under `Assets/`, a `"key"` string in `.cs` files under `Assets/`, plus your own sources.
- `KeyUsages.Reveal(KeyUsage usage)` — navigate: an object, a scene, a prefab or a script line.
- `KeyUsages.RegisterSource(Func<string, string, IEnumerable<KeyUsage>> source)` — add your own search (the arguments are the table and the key). Exceptions in a source are logged and do not stop the search.
- `KeyUsage`: constructors `(KeyUsageKind kind, string assetPath, string objectPath, int[] siblings, int line, GameObject live)` and `(string assetPath, string title, string location, Action reveal)` (a custom kind of usage); `Kind` (`KeyUsageKind { Scene, Prefab, Script, Asset }`), `AssetPath`, `ObjectPath`, `Siblings`, `Line`, `Live`, `KindTitle`, `Location`.

`ProjectScan` (`static`) is the shared project walker; markup rewriting on style and icon renames is built on it: `const string ProjectRoot = "Assets"`, `LoadedScenes()`, `OpenPrefabRoot()`, `OpenPrefabPath()`, `PrefabPaths()`, `ClosedScenePaths()`, `InPrefab<T>(path, Func<GameObject, T>)`, `InClosedScene<T>(path, Func<Scene, T>)`, `MayContain(assetPath, Regex)` (a YAML prefilter), `AssetPaths(filter)`, `IsInProject(path)`, `HierarchyPath(Transform, Transform top = null)`, `SiblingPath(...)`, `Find(IReadOnlyList<GameObject> roots, int[] siblings, string hierarchyPath)`.

Dialog windows: `ExportDialog.Show(ITableBackend backend, string table)`, `KeyUsagesWindow.Show(string table, string key, IReadOnlyList<KeyUsage> usages)`, `SyncSourceDialog.Show(string table)` → `SyncSourceChoice`, `TableSyncDialog.Show(TableSyncPlan plan)` → `TableSyncOptions` (or `null` if declined), `TextChangeDialog.Show(string subject, string before, string after, string restoreRefusal)` → `TextChangeChoice`.

### 10.7. Markup in the Editor

- `MarkupField` (`VisualElement`, `Window.Markup`) — a multi-line markup field with highlighting and autocompletion: constructor `(Func<char, IEnumerable<string>> candidates)`, events `MarkupChanged(string)`, `SelectionChanged(TextRange)`, `ApplyRequested`, properties `Value`, `Selection`, `Caret`, methods `SetValue(string value, bool notify)`, `SetDiagnostics(IReadOnlyList<MarkupDiagnostic>)`, `Select(TextRange)`, `SetReadOnly(bool)`.
- `MarkupHighlighter` — `static string ToRichText(string markup, IReadOnlyList<MarkupDiagnostic> diagnostics)` and the colour constants (`InputColor`, `IconColor`, `StyleColor`, `TmpColor`, `CommandColor`, `ErrorColor`, `EscapeColor`).
- `MarkupCandidates` — autocompletion suggestions: `For(char opener)`, `AllIconSets()`.
- `MarkupEdits`, `TextRange` (`readonly struct`: `TextRange(int start, int end)`, `Start`, `End`, `Length`, `IsEmpty`) — range edits: `Snap(markup, range)`, `Wrap(markup, range, open, close, out result)`, `Insert(markup, index, text, out caret)`, `Replace(markup, range, text, out result)`, `WordAt(markup, range)`.
- `MarkupDocument` (`Parse(markup)`), `MarkupModule` (`Slot`, `Open`, `Close`, `Label`, `IsStyle`, `TryParse`, `IsInlineTag`), `ModuleCoverage { None, Partial, Full }` — a model of the tags around a selection (used by the style buttons and "Style from selection"): `Coverage`, `ModulesIn`, `Toggle`, `RemoveSlot`, `Clear`.
- `TagInspector` (`VisualElement`) — edits the parameters of the tag under the caret: `TryFindTag(markup, caret, out start)`, `Edit(markup, start)`, `Hide()`, event `Changed(int, int, string)`.
- `TextPreview` (`IDisposable`) — a live preview with a typewriter: events `SelectionChanged`, `SelectionCleared`, `CommandClicked`; properties `Element` (`IMGUIContainer`), `Animate`, `CharactersPerSecond`, `IsTypewriting`, `ShowMarkers`, `SelectionBounds`, `VisibleText`, `ExtraEffectTag`; methods `Configure(TMP_Text reference, TMP_FontAsset font, Material material)`, `SetMarkup(string markup, string locale, DeviceFamily device, List<MarkupDiagnostic> diagnostics)`, `RestartTypewriter()`, `ShowAll()`, `HighlightMarkupRange(TextRange)`, `OverrideDatabase(TMPAnimationDatabase)`, `ReplayVisibility(bool)`, `bool Tick(float deltaTime)`.
- `IconLinePreview` — an icon-in-a-line preview: `Element`, `Show(TMP_SpriteAsset, string glyphName, bool tint)`, `Refresh()`. `ThumbnailRenderer` — thumbnails: `Render(string key, string richText, TMP_FontAsset font, Material material, Vector2Int size)`, `Clear()`.
- `GalleryDrawer` — the gallery of styles, icons and effects: events `WrapRequested`, `InsertRequested`, `FontRoleRequested`, `CreateStyleRequested`, `CreateMaterialRequested`, `CreateEffectRequested`; `Refresh(string locale, DeviceFamily device)`, `ShowApplied(Func<string, ModuleCoverage>)`.

The simplest way to embed a preview in your own UI:

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

(`MarkupTextField` with `showPreview: true` does exactly this by itself.)

### 10.8. Fonts, library and art

Fonts (`Editor.Fonts`):

- `FontCharset` — `Collect(string locale, string extra)` (a character set: ASCII, extended punctuation, CJK punctuation for CJK, plus everything used in the locale's table strings), `Used(string locale)` (strings only), `Missing(TMP_FontAsset font, string characters)`.
- `StaticFontBuilder` — a static font for a character set (for CJK): `DefaultPath(Font source, string locale)`, `Create(Font source, string locale, TextAsset extra, string assetPath)` → `Result` (`Font`, `Characters`, `NotInSource`), `IsBuilt(font)`, `LocaleOf(font)`, `Rebuild(font)`. The recipe (source font, locale, extra characters) is saved for `Rebuild`.
- `MaterialPresets` — SDF material presets: `const string FolderName = "Materials"`, `Folder(FontProfile)`, `Variants(FontRole)`, `MaterialName(font, roleId, presetId)`, `Validate(fonts, roleId, presetId)`, `CreatePreviewMaterial(font, settings)`, `Create(fonts, roleId, presetId, settings)` (a material copy for each font of the role), `PresetIds(fonts, roleId)`, `IsOwned(fonts, material)`, `Remove(fonts, roleId, presetId)`, `SyncAtlases(fonts)`. `SdfMaterialSettings` — the fields `FaceDilate`, `Softness`, `OutlineColor`, `OutlineWidth`, `Underlay*`, `Glow*`, `const string FullShader`, `From(Material)`, `SupportsGlow(Material)`, `ApplyTo(Material)`. `MaterialPresetForm` is the preset form ([10.10](#1010-tabs-forms-layout)).

Library (`Editor.Library`):

- `StyleEdits` — `ValidateId(StyleLibrary, string id)`, `Add(StyleLibrary, SerializedProperty draft)`, `Remove(StyleLibrary, string id)`, `IndexOf(SerializedProperty styles, string id)`.
- `StyleFromText` — `Prefill(SerializedProperty style, IEnumerable<MarkupModule> modules, FontProfile fonts)`, `Create(StyleLibrary, FontProfile, TMP_Text)` — a style from a `TMP_Text`.
- `StylePanel` (`StylePanel(StyleLibrary library, int index)`, `static readonly Regex ValidId = ^[A-Za-z][\w.-]*$`, event `Selected`, `Tick`, `Dispose`), `StyleForm`, `StyleFields` — the style panel and form.
- `IconSetPanel(IconSet set)` — the icon set panel; `IconCheckDialog.Show(Texture2D, string fileName, IReadOnlyList<string> problems)` → `IconCheckChoice`.
- `MarkupRewriter` — rewrites tags in tables, layers, open and closed scenes, prefabs, `[TypecraftMarkup]` fields and auto-tag rules when a style or icon is renamed: `Prepare(Regex pattern, string replacement)`, `StylePattern(string id)`, `StyleReplacement(string id)`, `IconPattern(string setId, string key, bool plain)`, `Describe()`, `Apply(string undoName)`, `Cells`, `LayerEntries`, `AutoTags`, `IsEmpty`, `HasIrreversible`, `UndoNote`. Prefabs and closed scenes are saved immediately, without Undo.

Art (`Editor.Art`):

- `IconSetBuilder.Build(IconSet set)` → `bool` — build an atlas and a `TMP_SpriteAsset` from the PNGs in the set's folder.
- `SpriteAtlasBuilder.Build(IReadOnlyList<string> sourceFiles, string atlasAssetPath, int padding, int maxAtlasSize, FilterMode filterMode)` → `Dictionary<string, Sprite>`.
- `TmpSpriteAssetSync` — `CreateSpriteAsset(path)`, `CreateTransient(Texture2D, string glyphName)`, `DestroyTransient`, `SetPlacement(target, glyphName, Vector2 offset, float scale)`, `ApplyPlacement(IconSet)`, `AssignSpriteSheet`, `Sync`.
- `IconImageCheck` — image checks (`StandardSize = 128`, `StandardMargin = 12`, `Load`, `PrevailingSize`, `Inspect`, `Report`); `IconKeyNaming` — `KeysFor(IReadOnlyCollection<string> glyphNames)`, `ToKey(string raw)`; `BasePromptSets` — `IsBaseSet(IconSet)`, `RebuildAll()` (menu `Tools/Typecraft/Rebuild Base Prompt Sets`; context menu `IconSet → Rebuild Atlas`).

Initial setup (`Editor.Setup.TypecraftSetup`): `CreateStringTables()` (menu `Tools/Typecraft/Create String Tables`, creates a `TableTextSource` + `StringTable_Main` and assigns it as the source), `CreateDefaults(string configFolder, string resourcesFolder)` → `TypecraftSettings` (menu `Tools/Typecraft/Create Project Settings`; if settings already exist it returns them), `DefaultFamilies()` (with Input System).

### 10.9. Effects in the window

`namespace Velsi.Typecraft.Editor.Window.Effects`; `TYPECRAFT_TMPEFFECTS` is needed for the TMPEffects parts.

- `EffectCatalog` — the effect catalogue: `Entries` (`IReadOnlyList<EffectEntry>`), `IsEffectTag(string)`, `IsWriterTag(string)`, `DefaultAnimationDatabase`, `DefaultCommandDatabase`, `Invalidate()`. `EffectEntry(EffectKind kind, string name)` — `Kind`, `Name`, `TagName`, `Wraps`, `Open`, `Close`. `enum EffectKind { Basic, Show, Hide, Command }`.
- `EffectLibrary` — custom effects and the project animation database: `const DatabaseName = "TypecraftAnimationDatabase"`, `FolderName = "Effects"`, `Available`, `Folder(TypecraftSettings)`, `ValidateName(kind, name)`, `TagOf(kind, name)`, `Project`, `Current`, `IsCustom`, `AssetOf`, `Bases(kind)` (`EffectBase(string label, Object source, Type type)`), `CreateDraft(EffectBase)`, `EnsureProject()`, `Save(kind, name, draft)`, `Remove(kind, name)`, `CreatePreviewDatabase`, `DestroyPreviewDatabase`, `Configure(TMPAnimator)`, `UsesDefault(TMPAnimator)`.
- `EffectComponents` — `Ensure(TMP_Text text, string markup)` / `Ensure(TMP_Text, IEnumerable<string>)` (adds `TMPAnimator`/`TMPWriter` if the markup requires them), `Missing(text, markups, out bool writer, out bool animator)`, `Markups(TypecraftText)`. `EffectComponentsWarning` (`VisualElement`, event `Added`, `Refresh(TMP_Text[, IEnumerable<string>])`) is the "missing component" bar.
- `EffectForm` (`TypecraftForm`) — the new-effect form.

### 10.10. Tabs, forms, layout

- `ITypecraftTab` is an extra window tab (see [11.4](#114-a-custom-window-tab)). The built-in tabs are `EditorTab`, `LocalizationTab`, `LibraryTab`, `FontsTab` (`VisualElement`s; each has `Reload`/`Tick`/`Dispose` and its own methods; public members of `EditorTab`: `Overlay`, `Target`, `SetTarget(ITextEditTarget)`, `Pin(ITextEditTarget)`, `FollowSelection()`, `HasPendingWordChange`, `ResolvePendingWordChange()`, `OpenStyleForm(bool fromSelection)`, `OpenMaterialForm(string roleId)`, `OpenEffectForm()`, `Tick`, `RefreshAll`, `Dispose`).
- `FormOverlay` (`VisualElement`) is the layer of forms on top of a tab: `IsOpen`, `Form`, `Show(TypecraftForm)`, `Close()`, `Tick`. `TypecraftForm` is an abstract form: `protected TypecraftForm(string title)`, event `Closed`, `Tick`, `Dispose`, `bool Submit()`, protected `Fields`, `Side`, `SaveButton`, `abstract bool Save()`, `SetError(string)`, `Close()`, `static Section(string title)`. Subclasses: `StyleForm`, `MaterialPresetForm`, `EffectForm`.
- `ColumnsLayout`, `SplitPane`, `TypecraftLayout` are helpers for responsive layout (the narrow width is `TypecraftLayout.NarrowWidth = 700f`): `ColumnsLayout(string key, VisualElement list, VisualElement center, VisualElement side, float listWidth, float sideWidth, float sideHeight, float narrowWidth)` (`MenuButton`, `IsNarrow`, `ListShown`, `ShowList()`, `HideList()`); `SplitPane(string key, VisualElement first, VisualElement second, bool fixedFirst, float defaultWidth, float defaultHeight, float minFixed, float minFlex)` (`Vertical`, `NarrowWidth`, `FixedPane`, `ReleaseFixed()`, `RestoreFixed()`, `SetFixedVisible(bool)`, `SetFixedAuto(bool)`); `TypecraftLayout.VerticalScroll(string className = null)`, `FitScroll`, `Responsive`, `Dialog`, `ButtonRow`, `TruncatedTooltips`, the constant `FitClass`.

### 10.11. Dialogue editor

The `Velsi.Typecraft.Dialogue.Editor.*` namespaces ([10.1](#101-editor-assembly-namespaces)).

**The tab.** `DialoguesTab` (`VisualElement`, `ITypecraftTab`, `IDialogueBoardHost`, `IDialogueEditorContext`, `IDialoguePlaybackHost`; `Title` = "☰ Диалоги", `Order` = 100). Open it from code:

```csharp
using Velsi.Typecraft.Dialogue.Editor;

DialoguesTab.Open(asset);
DialoguesTab.Open(asset, nodeId);
DialoguesTab.OpenSpeaker(speaker);
```

Public members: `Asset`, `Locale`, `Root`, `Board`, `SelectedNodeId`, `Issues`, `InspectorElement`, `Playback`, `Problems`, `DockOpen`, `Page` (`DockPage { Playback, Problems }`), `CurrentNodeId`; methods `Select(asset, nodeId)`, `SelectNode(id[, bool frame])`, `SelectSpeaker`, `SelectFlag(catalog, name)`, `Edit(Object target, string undoName, Action change, bool rebuildInspector = false)`, `AddNode(Type, Vector2? position = null)`, `RemoveNode(id)`, `CreateDialogue(path)`, `CreateSpeaker(name)`, `CreateFlag()`, `ShowCurrent`, `Reveal`, `OpenDock(DockPage)`, `PlayFromStart()`, `ShowProblems()`, `RenameFlag(catalog, from, to, bool rewriteDialogues)`, `OnNodeSelected`, `OnBoardEdited`, `OnShow`, `Tick`, `OnTypecraftChanged`, `Dispose`.

`DialogueMenu` (`static`): `CreateDialogueBox(GameObject context)`, `CreateSubtitles(GameObject context)`, `CreateWorldText(GameObject context)` — the `GameObject/Typecraft/…` menu. `DialogueIdGuard.FixDuplicates()` → `int` reissues the `Id` of dialogue copies (called on import); `DialogueIdGuard.Find(table, key)` is a source for `KeyUsages` (key usages inside dialogues).

**Board views.** `IDialogueBoardView : IDisposable`:

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

`IDialogueBoardHost`: `string Locale`, `void OnNodeSelected(string nodeId)`, `void OnBoardEdited(IReadOnlyList<string> selectNodes = null)`. A view with a public parameterless constructor is discovered automatically (`TypeCache`); `[DialogueBoardPriority(int priority, string title)]` sets the caption and the order (higher goes first): the graph is 100 ("Граф"), the list is 0 ("Список"). `DialogueBoards`: `Types`, `Create(Type preferred = null)` (the first one that is created successfully; the fallback is `DialogueListBoard`), `Title(Type)`.

**Inspector context.** `IDialogueEditorContext`: `Asset`, `Locale`, `Root`, `Edit(Object target, string undoName, Action change, bool rebuildInspector = false)`, `SelectNode(string)`, `RemoveNode(string)`.

**Inspectors and fields.** `NodeInspector(IDialogueEditorContext context, DialogueNode node, IReadOnlyList<DialogueIssue> issues)` (+ `static PickField EmotionField(...)`), `SpeakerInspector(context, SpeakerProfile speaker, Action delete)`, `FlagInspector(context, DialogueVariableCatalog catalog, DialogueVariableDefinition definition, Action delete, Func<string, bool> rename)`, `ConditionEditor(context, Condition condition, string title = "Условие")` (+ `VariableItems()`, `TypeChooser(...)`), `DialogueValueField(string label)` (event `Changed`, `Value`, `SetValue`), `KeyField(string label, IDialogueEditorContext context, Func<string> table, Func<string> prefix, bool preview)` (`Changed`, `Value`, `NewText`, `Create()`, `Refresh`, `Tick`), `PickField(string label, string title, Func<IEnumerable<PickItem>> items)` (+ `PickItem(string value, string label, string group = null)`, `PickDropdown.Show(Rect, string title, IEnumerable<PickItem>, Action<string>)`).

**Model and checks.**

- `DialogueEdits` (`static`) — edits with Undo: event `Edited`, `Change(Object target, string name, Action change)`, `Commit(Object)`, `Create(Type)`, `AddNode(asset, type, position, linkFrom, linkPort)`, `RemoveNodes(asset, ids)`, `RemoveWithoutUndo`, `Link(asset, nodeId, port, target)`, `GetLink(node, port)`, `SetLink(node, port, target)`, `Move(asset, moves)`, `SetStart(asset, nodeId)`, `Copy(asset, ids)`, `CanPaste(data)`, `Paste(asset, data, offset)`, `FlowOrder(asset, out depth)`.
- `DialogueChecks` — `Run(DialogueAsset asset)`, `Run(asset, catalog, List<DialogueIssue> issues)`, `Errors(issues)`, `Warnings(issues)`. On top of the runtime checks (`DialogueValidator`) it adds table checks: the key exists, the translation exists, markup is stale, characters exist in the font, the speaker has a name, the emotion is known.
- `DialogueProject` — finding and creating assets: `DefaultFolder = "Assets/Game/Dialogues"`, `Folder { get; set; }` (stored in `EditorUserSettings`), `FindAll<T>()`, `Dialogues()`, `Speakers()`, `Catalogs()`, `Variables()`, `TryVariable(name, out definition)`, `MergedCatalog()`, `FolderOf`, `EnsureFolder`, `CreateDialogue(path, defaultTable)`, `CreateSpeaker(folder, name)`, `CatalogForNewFlag(folder)`, `UniqueVariableName(prefix)`.
- `DialogueTexts` — texts for display in the UI: `Backend`, `Locale(string preferred)`, `HasKey`, `Markup`, `Plain`, `SpeakerName`, `Summary`, `ShortId`, `NodeLabel`, `Shorten`, `Condition`, `Clause`, `Assignment`, `Operator`, `Value`, `SnakeCase`, `NextKey(table, prefix)`.
- `DialogueNodeInfo` — information about node types: the port constants `NextPort`, `ElsePort`, `OptionPort`, `CasePort`, `Types`, `Title`, `Glyph`, `Category` (`NodeCategory { Flow, Speech, Logic }`), `CategoryClass`, `HasInput`, `PortTitle`, `TryIndex`.
- `DialogueIssueText` — `Describe(DialogueIssue)`, `Port(string)`, `Mark(DialogueIssueSeverity)`. `DialogueFlagRename` — renaming a flag: `Prepare(from, to)`, `Count(asset, name)`, `Describe()`, `Apply(catalog, definition, bool rewriteDialogues)`, properties `From`, `To`, `Dialogues`, `Places`.

**Playback and checks in the window.** `DialoguePlaybackPanel(IDialoguePlaybackHost host)` — playback without running the game: `Runner`, `Variables`, `IsPlaying`, `Current`, `Observed`, `Log` (`PlaybackLogEntry(text, asset, nodeId, kind)`), `StatusText`, `WaitRemaining`, `ChoiceButtons`, `HiddenText`; `Play(bool fromSelected)`, `Stop()`, `Next()`, `Choose(int)`, `SkipWait()`, `SetFlag`, `ResetFlags`, `RefreshFlags`, `OnLocaleChanged`, `Observe(DialoguePlayer)`, `Tick`, `Dispose`. `IDialoguePlaybackHost`: `Asset`, `Locale`, `SelectedNodeId`, `ShowCurrent(asset, nodeId)`, `Reveal(asset, nodeId)`. `DialogueIssuesPanel(Action<DialogueAsset, string> reveal)` — `ShowsAll`, `AllResults`, `Summary`, `ShowCurrent`, `Refresh`, `CheckAll()`.

**The graph** (`Dialogue.Editor.Graph`, disabled by `TYPECRAFT_NO_DIALOGUE_GRAPH`): `DialogueGraphBoard : GraphView, IDialogueBoardView` (priority 100), `DialogueGraphNode : Node`, `DialoguePort : Port`, `DialogueEdgeListener : IEdgeConnectorListener`, `DialogueNodeSearch : ScriptableObject, ISearchWindowProvider` (`Create(Action<Type, Vector2> picked)`). `DialogueListBoard` is the fallback list (in the main assembly).

### 10.12. Window and tab behaviour (reference)

A short summary of what the UI does on top of the API above. A step-by-step description is in `Manual.en.md`.

**Window tabs, `Window → Typecraft`.**

- **Editor** (`EditorTab`) — markup with highlighting and autocompletion, a live preview with a typewriter, a gallery of styles, icons and effects, selection with the mouse right in the preview; edits made in Play Mode survive leaving it (`PlayModeEdits`).
- **Localization** (`LocalizationTab`) — a "key × locale" grid with table strings; `CellIssue` flags: "missing translation", "does not fit", "missing glyph", "markup mismatch"; ✦ — the layer has markup, ⚠ — the string changed and its markup is not applied (the «Только изменившиеся» filter, "changed only"); sync with CSV/TSV, Excel and Google Sheets; export to `.xlsx`/CSV/TSV/JSON; finding key usages; the `pseudo` pseudo-locale.
- **Library** (`LibraryTab`) — icon sets (drag in PNGs), the style editor, "style from `TMP_Text`", material presets and custom effects. Renaming a style or an icon rewrites tags in tables and scenes (`MarkupRewriter`).
- **Fonts** (`FontsTab`) — roles × locales, character coverage, a builder of a static font for the characters used by the strings (for CJK).
- **Dialogues** (`DialoguesTab`) — a graph or a list of nodes, a node inspector, playback and checks.

**What lives where: the table and the layer.**

| Where | What it holds |
| --- | --- |
| The table (`StringTable` or Unity Localization) | Words and meaningful tokens: `{key:Interact}`, `{candle}`, `{0}`, `{player:name}` — word order differs between languages, so the translator places them. The escapes `\[`, `\<` are text too |
| The layer (`MarkupLayer`, one per table) | Decoration: `[warn]…[/warn]` styles and TMP/TMPEffects `<…>` tags, including `<!wait=…>`. An entry = key + locale + full text with decoration + a snapshot of the table string the decoration was applied to |

- Layers are listed in `TypecraftSettings → Markup Layers` and are created automatically next to the table when you first add markup. The table is found by id (for Unity Localization — by collection name) and the locale code.
- In the game: if the snapshot matches the current table string, the layer text is shown, otherwise the plain table string. This works for the built-in source and for Unity Localization alike; the `pseudo` locale is built from the decorated text of the base locale.
- A table string was changed (a translator, a CSV import) — the decoration stops applying, and the cell in the "Localization" tab is marked ⚠. «Перенести теги» (transfer tags) re-places the decoration on the new text by matching words (what cannot be attached exactly is shown and asked about; `Decoration.Transfer`), «Сбросить разметку» (reset markup) deletes the layer entry.
- The "Editor" tab shows the layer text. "Apply" without changing the words writes to the layer only; if the words changed, a "before / after" dialog (`TextChangeDialog`) offers «Записать в таблицу» (write to the table) or «Вернуть слова» (restore words). It also appears when you move away from the object and when you close the window with unwritten words.
- Old tables with decoration right in the cells: `Tools/Typecraft/Move Markup To Layers`.
- Renaming a style in the "Library" tab rewrites the layers; `{…}` icons live in the table and are rewritten there (and in the layer snapshots, so the decoration does not go stale).

**Table sync (the "Localization" tab).** Strings can be kept in an external table and pulled in the Editor (there is no sync at runtime). **Источник…** (source) configures the source for the selected table, **Синхронизировать** (sync) loads it in one click, **Импорт…** (import) is a one-off file load without saving the source.

| Source | What to specify |
| --- | --- |
| CSV / TSV | A file path (UTF-8; quotes and line breaks inside cells are supported) |
| Excel `.xlsx` | A file path and a sheet (the first by default). Read without third-party libraries; the old `.xls` is not supported |
| Google Sheets | A regular link `https://docs.google.com/spreadsheets/d/<SHEET_ID>/edit#gid=<GID>` — the sheet is taken by `gid` |

- Format: the first row is the headers. `key` is a required column, `comment` is optional, the others are locale codes (`en`, `ru`, `zh-Hans`…). Columns that do not look like a locale code (`notes`) are skipped. Locales that are not in the project are offered to be added.
- Google Sheets: needs "Anyone with the link can view" (or "Publish to the web"). No keys or OAuth; a closed sheet gives the «Таблица закрыта» (sheet is closed) error — then open access or export `.xlsx`. The download runs in the background and can be cancelled.
- The source is stored in `ProjectSettings/TypecraftTableSync.asset` (Editor only, not in the build). Paths inside the project are written relative. The sheet link goes into the repository — anyone with the link can read the sheet.
- Before applying there is a report (`TableSyncPlan`, `TableSyncDialog`): new keys, changed strings per locale ("was / now"), comments, keys that are not in the source. Missing keys are not deleted by default (the «Удалить ключи…» checkbox); an empty cell in the source does not erase a translation by default (the «Стирать строки…» checkbox). "Apply" is one Undo operation.
- Markup: if a string changed and has decoration in the layer, the decoration stops applying (the snapshot does not match) — the cell gets ⚠. The report says up front how many strings lose their markup; after the sync use the «Только изменившиеся» filter and «Перенести теги». Deleting a key deletes its layer entry too.

**Export and key usages.**

- **Экспорт…** (export) — a format (Excel `.xlsx`, CSV, TSV, JSON) and a scope (the current table or all). Columns `key`, `comment`, locales (the base one first) — the file can be read back by sync. All tables: `.xlsx` is one workbook with a sheet per table (the sheet name is the table id, adjusted for Excel limits); CSV/TSV is a folder with a file per table; JSON is a single file. In `.xlsx` the header is frozen, an empty translation is filled red, a ⚠ row is filled orange. Markup-layer decoration does not go into the file, only the table text.
- **Double-click a key** — where it is used: `TypecraftText` in Localized mode (with this table, or with no table if the key is found in it first) in open and closed scenes and prefabs under `Assets/`, plus a `"key"` string in `.cs` files under `Assets/`. One place — jumps there at once (the object is selected and framed; a closed scene opens after a save prompt, a prefab in Prefab Mode, a script at the right line), several — a list (`KeyUsagesWindow`). Renaming a key is F2 or the cell's context menu.

**Custom styles, materials and effects.** They are created in the window — in the gallery of the "Editor" tab (the «+ стиль», «+ материал», «+ эффект» cards) or with the buttons on the "Library" tab panel. The form (`TypecraftForm` in a `FormOverlay`) opens on top of the window, the preview is live, «Сохранить» (save) writes to assets (with Undo where Unity supports it).

| What | How it works | Where it is stored |
| --- | --- | --- |
| Style | id, colour/gradient, b/i/u, case, spacing, font role, material preset, effect. The preview is on the selected text or on a sample; after saving the selection is wrapped in `[id]…[/id]`. **«Стиль из выделения»** (style from selection; the floating panel, the «Стиль ▾» menu) takes the tags around the selection — `<color>`, `<b>`, `<i>`, `<u>`, `<cspace=…em>`, `<size=…%>`, `<font … material=…>`, `<material>`, an effect — into the form and replaces them with `[id]…[/id]` | `StyleLibrary` |
| Material | An SDF material preset for a font role: Face Dilate, Softness, outline, underlay (`UNDERLAY_ON`), glow (`GLOW_ON`). A copy of the material is created for each font of the role (each locale has its own atlas) with one preset id, so a style works in all locales. Glow exists only in the full shader — a preset with glow is switched to `TextMeshPro/Distance Field` | `Materials/` next to the `FontProfile`, an entry in the `FontProfile` |
| Effect | A kind (permanent `<x>`, show `<+x>`, hide `<-x>`), a base — a copy of a built-in TMPEffects animation, `Generic` (tracks) or `Stack` (several animations), parameters — an asset inspector inside the form. Name: lower-case Latin letters, digits, `_`; it cannot coincide with a built-in effect or a TMP tag | `Effects/` next to the `StyleLibrary` |

- The project animation database: the first custom effect creates `Effects/TypecraftAnimationDatabase.asset`; the reference to it is `TypecraftSettings → Animation Database` (details — [section 8](#8-tmpeffects)). On "Apply", an animator that uses the TMPEffects database is switched to the project database by default; the global TMPEffects settings are not changed.
- Custom effects appear at once in autocompletion, in the gallery (labelled «свой» / custom), in the «Эффект ▾» menu and in the style's effect drop-down.
- Deletion: right-click a material/effect card in the gallery, or «Удалить» (delete) on the "Library" tab. Materials and effect assets go to the OS recycle bin; tags in text stay.
- After «Пересобрать» (rebuild) of a static font the atlas in the preset materials updates itself (`MaterialPresets.SyncAtlases`).

**The "Dialogues" tab.**

- On the left — dialogues, speakers and flags ("+" in a section creates a new one; a new flag creates a catalog if there is none).
- In the centre — the board: a graph (ports, dragging links, space or right-click for a new node, a minimap) or a list of nodes; the view switcher and the text locale are on the toolbar.
- On the right — the inspector of the selected node: speaker and emotion from lists, a key with search and a text preview, «Создать ключ» (create key) — a table string straight from the inspector (text in the base locale, the `[style]` and `<tag>` decoration goes to the markup layer), conditions with flag selection.
- At the bottom — «▶ Проигрывание» (playback) without running the game: from the start or from the selected node, typing, choices, playback flags (editable live, «Сбросить» resets them), a log; the current node is highlighted on the board.
- «Проверить» (check) — dialogue problems (no start, empty and broken links, unreachable nodes, an empty or missing key, no translation, stale markup, no glyphs in the font, an undeclared flag or the wrong type, an unknown emotion…), a click goes to the node; «Проверить все» checks the whole project. Renaming a flag in the tab rewrites it in all dialogues (`DialogueFlagRename`).

---

## 11. Extending

### 11.1. A custom tag `{prefix:argument}`

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

- `TryRender` returned `false` — the tag is not recognised: rendering produces `MarkupIssue.UnresolvedTag`, and in the Editor the tag is highlighted as an error.
- `context` gives access to the locale (`Locale`), the device (`Device`), `Settings` and `TryAppendIcon(reference, output)` for inserting an icon.
- For the tag to work in the window preview as well, register it in the Editor too (`[InitializeOnLoadMethod]`), as in the example and in the Simple Dialogue sample.
- The result of `Typecraft.Render` is cached by the markup text: after the data the tag depends on changes (`PlayerTag.Name`), call `Typecraft.Refresh()`.
- Register on `BeforeSceneLoad` (or later): on `SubsystemRegistration` the package resets its own static handlers itself, and the order of two methods of the same stage is not defined.

A tag with an icon:

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

### 11.2. A custom text source

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

Assign the asset in `TypecraftSettings → Text Source`.

- `Request` returns the **raw** table string: no pseudo-localisation and no argument substitution. In `TextEntry` put the table id and the locale the string was actually taken from (after falling back to the base locale) — Typecraft uses them to find the entry in the markup layer.
- Typecraft applies the layer, the `pseudo` locale and `{0}` from `SetArguments` itself. If the source has its own formatter (like Smart Strings in Unity Localization), pass it as the fifth `TextEntry` argument — `(text, arguments) => …`; it receives the already decorated text.
- For the source's tables to be editable in the "Localization" tab, implement `ITableBackend` and register it (`TypecraftEditor.RegisterTableBackend`, an example is in [10.5](#105-localization-tables)). Until you have a backend the runtime works, but the "Localization" tab, sync, export and the markup layer are unavailable for this source.

The markup layer in your own code:

```csharp
using Velsi.Typecraft;
using Velsi.Typecraft.Text;

LocalizedText.Request(table, key, null, (markup, formatted) => Debug.Log(Typecraft.Render(markup)));

MarkupLayer layer = Typecraft.Settings.FindMarkupLayer(tableId);
string shown = layer != null ? layer.Resolve(key, locale, tableText) : tableText;
```

`Resolve` returns the layer text only if the entry's snapshot matches the table string.

### 11.3. A custom dialogue presenter

Extend `DialoguePresenter`; the player takes it from its `Presenter` field.

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

- `RaiseLineCompleted()` means "the line is fully shown". Without it the `AfterText` advance does not fire.
- Typewriter: return `false` from `IsLineComplete` while typing, and let `TryCompleteLine()` finish the typing and return `true` — then the first "next" finishes the line and the second moves on.
- Choices: `SupportsChoices => true` and `ShowChoices(choices, choose)`; unavailable options have `Available == false` (hidden ones are not in the list); on click call `choose(choice.Index)`.
- A ready base with a typewriter, `CanvasGroup` showing and auto-hide is `TextDialoguePresenter`: override `ApplyLine`, `OnLineShowing`, `OnHidden`. The speaker name is `DialogueSpeakerText.RequestName(speaker, table, style, onReady)`, the portrait is `line.Portrait`.

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

### 11.4. A custom window tab

Any non-abstract class with a public parameterless constructor that implements `ITypecraftTab` is discovered automatically (`TypeCache`) and appears as a window tab; tabs are sorted by `Order` (the built-in "Dialogues" is 100).

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

Open the tab from code:

```csharp
TypecraftWindow.ShowTab<StatsTab>(tab => tab.Focus());
```

| Method | When it is called |
| --- | --- |
| `Title`, `Order`, `Element` | When the window is built; `Element` is added to the body when the tab is selected |
| `OnShow()` | Every time the tab is selected |
| `Tick(deltaTime)` | Once per frame (at most 30 times a second, `deltaTime` capped at 0.1) — only while the tab is active |
| `OnTypecraftChanged()` | On any change of locale, device or settings — for all tabs, active or not |
| `Dispose()` | When the window closes and when tabs are rebuilt |

An exception in a tab constructor is logged; the other tabs are still created.

### 11.5. Custom input prompts

An `IInputGlyphResolver` (a `ScriptableObject`) turns `{key:Action}` into an icon for the device. The built-in `InputGlyphProfile` works with the Input System; for Rewired or your own input system implement the interface yourself:

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

Assign it in `TypecraftSettings → Input Glyphs`. The sets from `IconSets` end up in `Typecraft.Icons`. At runtime you must report the device yourself: `Typecraft.SetDevice(...)` (for `InputGlyphProfile` the `DeviceTracker` does it).

### 11.6. Custom dialogue flags

Flags are an `IDialogueVariables` (`TryGet`, `Set`, the `Changed` event). By default all players share `DialogueGlobals.Variables` (an in-memory `DialogueVariables`). An example — flags backed by your own quest system:

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

Hooking it up:

```csharp
DialogueGlobals.SetVariables(new QuestFlags(questLog));
DialogueGlobals.Catalog = flagsCatalog;
```

- Put everything that is not yours (the `visited:…` visits, `Once` flags, local dialogue flags) into a plain `DialogueVariables` — otherwise `Once` and visit counters stop working.
- Declare `quest.lighthouse` (int) in the catalog — then the "Dialogues" tab will offer it in conditions, check its type, and playback in the Editor will show it and let you change it.
- Call `SetVariables` before the first `Play`: the player recreates the runner on the next `Play`, and a conversation that is already running stays on the old flags. Subscribers can use the `DialogueGlobals.VariablesReplaced` event.
- Custom flags for a single player — a subclass of `DialogueVariablesSource` (properties `Variables` and `Catalog`) in the `Variables Source` field.

### 11.7. Dialogue events into the game

The Event node delivers `(name, argument)` synchronously, at the moment it is passed:

- in the Inspector — `DialoguePlayer → On Event (String, String)`, a method with two strings (dynamic);
- from code — `_player.EventRaised += (name, argument) => …`.

Inside the handler you may stop the dialogue or start another one — the runner survives it. If the event has to wait (a door animation), put a Wait or Line node after the Event.

### 11.8. Dialogues and an ECS game

A dialogue is presentation and script; the simulation does not need it, and the boundary goes through bridges, like for any MonoBehaviour world:

- **Start.** On the interlocutor's object, an authoring component attaches to the entity at birth a bridge component with a reference to the `DialoguePlayer` (or the `DialogueTrigger`). The interaction system, on getting an "interact" request on a target with this bridge, calls `player.Play()`. While a conversation runs, movement input can be muted by a tag on the player, added on `Started` and removed on `Ended`.
- **Events back.** An `EventRaised` handler does not touch the game directly but turns into data: it adds a one-frame request component to the right entity (`OpenDoorRequest`, `GiveItemRequest { Id }`), an ordinary system handles it in its own pipeline stage, and the cleanup system removes it at the end of the tick. The event argument is an id, not an object reference.
- **Flags.** The source of truth is the game state: an `IDialogueVariables` on top of components or the quest service (the example above), not a copy that has to be kept in sync. Visits and small dialogue flags stay in its built-in `DialogueVariables`.
- **Saving.** Dialogue flags are a separate save section (`ToJson`); the place in a conversation is a `DialogueRunnerState` next to the interlocutor's entity, if you need it.

### 11.9. A custom dialogue board view

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

The view is discovered automatically; it is chosen with the view switcher on the "Dialogues" tab toolbar.

### 11.10. A custom key-usage source

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

## 12. Saving and loading

### 12.1. Dialogue flags

`DialogueVariables` serialises to JSON (node visits are part of the snapshot):

```csharp
string json = ((DialogueVariables)DialogueGlobals.Variables).ToJson();
((DialogueVariables)DialogueGlobals.Variables).FromJson(json);
```

Better to apply the catalog after loading: `catalog.ApplyDefaults(variables)` fills in flags that were not in an old save. A custom `IDialogueVariables` is saved by your own system; only `DialogueVariables` has JSON.

### 12.2. The place in a conversation

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

- `State` is the dialogue and the node the conversation stands on (a line, a choice or a wait); `null` if no conversation is running. Save it only if saving in the middle of a conversation is possible; flags are usually enough.
- `Restore` puts the conversation back on the saved node through the player's presenters, does not count a repeat visit, and also works for a player with `Once`. Without a `resolver` the player looks only at its own `Asset`.
- A bare runner without presenters: `runner.Restore(state, id => DialogueAsset)`.
- The dialogue identifier is `DialogueAsset.Id`; it does not change when the asset is renamed or moved.

### 12.3. Locale and device

Typecraft does not save the chosen locale or device. Keep them in your own settings and restore them with `Typecraft.SetLocale(code)` (or `ITextSource.SelectLocale`; for Unity Localization — `LocalizationSettings.SelectedLocale`) and `Typecraft.SetDevice(device)`.

### 12.4. What the package stores in the project

See [14.3](#143-data-storage).

---

## 13. Limitations and pitfalls

**Rendering and markup**

- `Typecraft` is both the namespace `Velsi.Typecraft` and a static class in it. In code inside a namespace that starts with `Velsi.` (other than `Velsi.Typecraft.*`), the C# rules resolve the name `Typecraft` to the namespace, not the class: write `Velsi.Typecraft.Typecraft.Render(...)` or `using Typecraft = Velsi.Typecraft.Typecraft;`. In the global namespace with `using Velsi.Typecraft;`, as well as inside `Velsi.Typecraft.*`, there is no problem.
- `Render` is cached by the markup text. If a tag depends on external data (`{player:name}`), call `Typecraft.Refresh()` after it changes.
- Tag handler registrations are lost when statics are reset on `SubsystemRegistration`; register on `BeforeSceneLoad` or later.
- `TypecraftText` with arguments: if the source did not format the string itself (built-in tables), the rendered result is passed through `string.Format`. A literal `{` or `}` in the final text (after `\{`) throws a `FormatException` — it is caught and logged as a warning, and the text stays without substitution. Named placeholders (`{coin}`) are not substituted by built-in tables: for Typecraft that is the icon `coin`. Named arguments work only through Unity Localization Smart Strings.
- TMPEffects command tags (`<!wait>`, `<#…>`) are cut out when the object has no `TMPWriter`.
- Unclosed and unknown styles do not throw: they produce a `MarkupDiagnostic` (visible in the Editor) and stay in the text as is.
- Icons sit on the line by the lower-left corner of the canvas; the sprite pivot and an offset inside the canvas do not affect the position — fit it in the set or the icon.
- Strings with styles and `<…>` are not stored in the table: decoration lives in the markup layer. When a table string changes the layer stops applying (`LayerState.Stale`) and the game shows the plain string.

**Sources and locales**

- `zh-Hans` and other CJK locales need a font with the required characters; after adding new strings rebuild the static font (the "Fonts" tab), otherwise you get the "missing glyph" flag.
- `Typecraft.SetLocale` does not switch Unity Localization; use `ITextSource.SelectLocale` or `LocalizationSettings.SelectedLocale`. The adapter picks up Unity Localization locale changes into Typecraft by itself.
- The `pseudo` locale is built from the base language and exists only in `Locales` lists (for Unity Localization — when there is at least one locale).
- Sync, export and finding key usages work in the Editor only.
- All `TypecraftText` components subscribe to `Typecraft.Changed` and refresh on every locale or device change, tag registration and `Refresh()`; in scenes with thousands of strings do not call `Refresh()` every frame.
- A key found in several `TableTextSource` tables, with an empty `table`, is taken from the first one in order.

**Input**

- `DeviceTracker` works in Play Mode only. Touch has no bindings — you need overrides in `InputGlyphProfile`.
- An Input System trap: an action from the Unity template may carry a `Hold` interaction — then `performed` arrives only after holding, and a quick press does not trigger `DialogueInputAdvance`.

**Dialogues**

- `DialogueAsset.Id` ends up in saves and in visit flags. A copy of an asset gets a new `Id` (`DialogueIdGuard` fires on import); do not rely on a copy's `Id` until it has been issued.
- `DialoguePlayer.Once` remembers the visit of the start node in `Variables`; reset the flags and the dialogue can be played again.
- Call `SetVariables` before the first `Play`; a conversation that is already running stays on the old flags. The catalog (`DialogueGlobals.Catalog`) is read on every `Play`.
- `EventRaised` and `NodeEntered` are synchronous. A long action in a handler (an animation) blocks the dialogue flow — put a Wait or Line after the Event node.
- `MaxAutoSteps` (1000) guards against loops without output (Branch ↔ SetVariables); when it fires, you get a console error and `Ended`.
- `Choose` with an unavailable option returns `false`. If no presenter can show a choice, the player takes the first available option itself.
- `OnDisable` of `DialoguePlayer` stops the conversation (with `Ended`).
- The player timers follow `Time.time` (`Scaled`) or `Time.unscaledTime`; when paused with `Time.timeScale = 0` choose `Unscaled`.
- By default `TextDialoguePresenter` calls `Hide()` in `Awake` (the `Hide On Awake` field, Play Mode only): the presenter becomes invisible (alpha 0) but the object stays active.
- The graph (`GraphView`) is marked Experimental in Unity; if it breaks in a new Unity version, turn on `TYPECRAFT_NO_DIALOGUE_GRAPH` — the tab works in list view.

**Editor**

- Renaming a style or icon rewrites tags in tables, layers, open and closed scenes, prefabs, `[TypecraftMarkup]` fields and auto-tag rules. Prefabs and closed scenes are saved immediately, without Undo.
- Google Sheets needs "Anyone with the link can view"; a closed sheet gives the «Таблица закрыта» (sheet is closed) error. The sheet link goes into the repository (`ProjectSettings/TypecraftTableSync.asset`).
- Sync sources and settings live in `ProjectSettings`/EditorPrefs/SessionState and do not go into the build.
- A `TMPAnimator` added by hand takes the TMPEffects animation database by default; pick the project database in it for your own effects.
- Before version 1.0, `public` Editor types that are not listed at the start of [section 10](#10-editor-api) as embedding points may change their signatures.

---

## 14. Appendix: menus, assets, storage

### 14.1. Menu items

| Menu | Action |
| --- | --- |
| `Window/Typecraft` (Ctrl+Alt+T), `Tools/Typecraft/Open Window` | The Typecraft window |
| `Tools/Typecraft/Create Project Settings` | `TypecraftSetup.CreateDefaults` |
| `Tools/Typecraft/Create String Tables` | `TypecraftSetup.CreateStringTables` |
| `Tools/Typecraft/Move Markup To Layers` | `MarkupLayerMigration` |
| `Tools/Typecraft/Rebuild Base Prompt Sets` | `BasePromptSets.RebuildAll` |
| `GameObject/Typecraft/Dialogue Box`, `Subtitles`, `World Text` | `DialogueMenu` |
| `CONTEXT/TMP_Text/Edit in Typecraft`, right-click a string field → `Edit in Typecraft` | `TypecraftEditor.Edit` |
| `CONTEXT/IconSet/Rebuild Atlas` | `IconSetBuilder.Build` |

### 14.2. Assets and components

`Create → Typecraft`: `Settings`, `Style Library`, `Font Profile`, `Icon Set`, `Input Glyph Profile`, `Markup Layer`, `String Table`, `Text Sources/String Tables`, `Text Sources/Unity Localization`, `Dialogue`, `Dialogue Speaker`, `Dialogue Variables`.

`Add Component → Typecraft`: `Typecraft Text`, `Dialogue/Dialogue Player`, `Dialogue/Dialogue Trigger`, `Dialogue/Dialogue Input Advance`, `Dialogue/Dialogue Box Presenter`, `Dialogue/Subtitle Presenter`, `Dialogue/World Text Presenter`.

### 14.3. Data storage

| What | Where |
| --- | --- |
| Package settings | `Resources/TypecraftSettings.asset` (`TypecraftSettings.ResourcesPath`) |
| Table sync sources | `ProjectSettings/TypecraftTableSync.asset` (Editor only) |
| The dialogues folder | `EditorUserSettings` (key `Typecraft.Dialogues.Folder`), `Assets/Game/Dialogues` by default |
| Play Mode edits | `SessionState` (`Typecraft.PlayModeEdits`), the keep flag is in `EditorPrefs` (`Typecraft.KeepPlayModeEdits`) |
| The window scratch text | `SessionState` (`Typecraft.Scratch.*`) |
| Typecraft configs (default) | `Assets/Typecraft/`, settings in `Assets/Typecraft/Resources/` |
| Markup layers | Next to the table: `MarkupLayer_<table>.asset` |
| Preset materials, custom effects | `Materials/` next to the `FontProfile`, `Effects/` next to the `StyleLibrary` |
