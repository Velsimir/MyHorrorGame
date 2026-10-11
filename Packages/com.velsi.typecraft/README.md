# Typecraft

[Русский](#русский) · [English](#english)

---

## Русский

Инструмент для текста поверх TextMeshPro: короткая разметка (`{key:Interact}`, `{candle}`, `[warn]…[/warn]`), библиотеки стилей и иконок, подсказки ввода по текущему устройству, роли шрифтов по языкам, встроенные таблицы строк, слой разметки, интеграция с TMPEffects и Unity Localization, модуль диалогов с графовым редактором.

Unity 6000.0+, пространство имён `Velsi.Typecraft.*`, версия `0.2.0`.

### Установка

1. Положи `com.velsi.typecraft-0.2.0.tgz` в папку `Packages/` проекта (так путь в `manifest.json` останется рабочим у всех, кто клонирует проект).
2. `Window → Package Manager → + → Install package from tarball…` → выбери файл.
3. После импорта Typecraft сам доставит обязательное и откроет окно **Welcome**:
   - **TMP Essential Resources** — штатным импортёром TMP;
   - **TMPEffects 0.3.2** (MIT) — из копии внутри пакета (`Vendor~/`), без интернета: архив копируется в `Packages/` проекта и подключается как `file:`;
   - **TMPEffects Required Resources** — базы встроенных анимаций.
4. Необязательные **Input System** и **Localization** ставятся кнопкой в окне Welcome.

Окно Welcome открывается при первой установке и после обновления версии; вручную — `Help → Typecraft Welcome` или `Tools → Typecraft → Welcome`.

Другие способы: embedded (папка пакета в `Packages/`) или git-URL. Тогда TMPEffects, если его ещё нет, тоже поставится автоматически из `Vendor~/`.

| Пакет | Что даёт | Без него |
| --- | --- | --- |
| TMPEffects | Анимации `<wave>`, машинка, команды `<!wait>` | Теги эффектов не работают |
| Input System | Глифы `{key:Action}` по биндингам | Глифы ввода не подставляются |
| Unity Localization | Адаптер `UnityLocalizationTextSource` | Работают встроенные таблицы |

### Быстрый старт

1. `Tools/Typecraft/Create Project Settings` — настройки, библиотека стилей, профиль шрифтов, профиль глифов и таблица строк.
2. На объект с `TMP_Text` добавь `Typecraft Text` (режим Inline или Localized).
3. `Window → Typecraft` (Ctrl+Alt+T) — окно редактора.

### Документация

| Документ | Для чего |
| --- | --- |
| [`Documentation~/Manual.ru.md`](Documentation~/Manual.ru.md) | Руководство пользователя (русский) |
| [`Documentation~/Manual.en.md`](Documentation~/Manual.en.md) | User manual (English) |
| [`Documentation~/API.ru.md`](Documentation~/API.ru.md) | Справочник по программному API, расширение, сохранение (русский) |
| [`Documentation~/API.en.md`](Documentation~/API.en.md) | Programmatic API reference, extending, saving (English) |
| [`Documentation~/ART_RULES.ru.md`](Documentation~/ART_RULES.ru.md) | Правила для своих картинок-иконок |

### Сэмплы

Package Manager → Typecraft → Samples:

- **Simple Dialogue** — учебный диалог: таблица на трёх языках, стили, иконки, подсказка ввода, TMPEffects, свой тег, переключение языка и устройства.
- **Branching Dialogue** — модуль диалогов: окно с портретами, выбор с Disable/Hide, ветка от флага записки, текст в мире, субтитры, событие из диалога, сброс флагов.
- **Extra Input Prompts** — редкие и устаревшие контроллеры (Xbox 360, PS3, Vita, Move, Wii, Wii U, Ouya, Stadia, Luna, VR) и схемы геймпадов.

### Арт

- `Art~/Prompts/` — исходники базовых подсказок ввода (CC0, Nicolae "Xelu" Berbece).
- `Art/Prompts/` — собранные атласы, `TMP_SpriteAsset` и `IconSet`.

---

## English

A text toolkit on top of TextMeshPro: short markup (`{key:Interact}`, `{candle}`, `[warn]…[/warn]`), style and icon libraries, input prompts that follow the current device, font roles per language, built-in string tables, a markup layer, TMPEffects and Unity Localization integration, and a dialogue module with a graph editor.

Unity 6000.0+, namespace `Velsi.Typecraft.*`, version `0.2.0`.

### Installation

1. Put `com.velsi.typecraft-0.2.0.tgz` into the project's `Packages/` folder (so the path in `manifest.json` keeps working for everyone who clones the project).
2. `Window → Package Manager → + → Install package from tarball…` → pick the file.
3. After import Typecraft installs the required parts itself and opens the **Welcome** window:
   - **TMP Essential Resources** — through the standard TMP importer;
   - **TMPEffects 0.3.2** (MIT) — from the copy bundled in the package (`Vendor~/`), no internet needed: the archive is copied into the project's `Packages/` and referenced as `file:`;
   - **TMPEffects Required Resources** — the built-in animation databases.
4. Optional **Input System** and **Localization** install with a button in the Welcome window.

The Welcome window opens on first install and after a version upgrade; manually — `Help → Typecraft Welcome` or `Tools → Typecraft → Welcome`.

Other ways: embedded (the package folder in `Packages/`) or a git URL. TMPEffects, if missing, is still installed automatically from `Vendor~/`.

| Package | What it gives | Without it |
| --- | --- | --- |
| TMPEffects | `<wave>` animations, typewriter, `<!wait>` commands | Effect tags do not work |
| Input System | `{key:Action}` glyphs from bindings | Input glyphs are not substituted |
| Unity Localization | The `UnityLocalizationTextSource` adapter | Built-in tables work |

### Quick start

1. `Tools/Typecraft/Create Project Settings` — settings, a style library, a font profile, a glyph profile and a string table.
2. Add `Typecraft Text` to an object with a `TMP_Text` (Inline or Localized mode).
3. `Window → Typecraft` (Ctrl+Alt+T) — the editor window.

### Documentation

| Document | Purpose |
| --- | --- |
| [`Documentation~/Manual.ru.md`](Documentation~/Manual.ru.md) | User manual (Russian) |
| [`Documentation~/Manual.en.md`](Documentation~/Manual.en.md) | User manual (English) |
| [`Documentation~/API.ru.md`](Documentation~/API.ru.md) | Programmatic API reference, extending, saving (Russian) |
| [`Documentation~/API.en.md`](Documentation~/API.en.md) | Programmatic API reference, extending, saving (English) |
| [`Documentation~/ART_RULES.en.md`](Documentation~/ART_RULES.en.md) | Rules for your own icon images |

### Samples

Package Manager → Typecraft → Samples:

- **Simple Dialogue** — a tutorial dialogue: a three-language table, styles, icons, an input prompt, TMPEffects, a custom tag, live language and device switching.
- **Branching Dialogue** — the dialogue module: a box with portraits, choices with Disable/Hide, a branch on a note flag, world text, subtitles, a dialogue event, flag reset.
- **Extra Input Prompts** — legacy and niche controllers (Xbox 360, PS3, Vita, Move, Wii, Wii U, Ouya, Stadia, Luna, VR) and gamepad diagrams.

### Art

- `Art~/Prompts/` — sources of the base input prompts (CC0, Nicolae "Xelu" Berbece).
- `Art/Prompts/` — built atlases, `TMP_SpriteAsset` and `IconSet`.
