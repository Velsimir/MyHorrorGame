# Showcase — обзорный сэмпл Typecraft

Одна сцена и набор данных, на которых построено руководство `Documentation~/Manual.ru.md`: строки со
стилями, иконками, подсказкой ввода и эффектом, диалог «Смотритель маяка» с ветвлением, выбором и
условиями, два говорящих с портретами, каталог флагов, своя таблица (en, ru, часть zh-Hans) со слоем
разметки и свой набор иконок.

## Запуск

1. В проекте должен быть `TypecraftSettings` (`Tools/Typecraft/Create Project Settings`) со встроенными
   таблицами строк (`TableTextSource`). Нужны TMPEffects (эффекты) и Input System (глиф кнопки).
2. `Tools/Typecraft/Samples/Connect Showcase Table` — подключает таблицу `Showcase` к источнику строк,
   слой разметки и набор иконок — к `TypecraftSettings`, добавляет языки en, ru, zh-Hans.
   Отключить: `Tools/Typecraft/Samples/Disconnect Showcase Table`.
3. Открой `Showcase.unity` и нажми Play: «Talk to the keeper» → «Next» или пробел (первое нажатие
   допечатывает реплику) → выбор. «Light the lamp» недоступен, пока не взяты спички; после выбора
   лампа в сцене загорается (событие `lamp / on`). «Reset flags» возвращает всё в начало.

Если проект стартует с собственной загрузочной сцены (DI-контейнер, загрузчик сцен), Play сначала
загрузит её — запусти `Showcase.unity` как стартовую или загрузи её из кода.

## Что где

| Что | Где |
| --- | --- |
| Строки, комментарии, проблемы (нет перевода, ⚠, нет символа) | `StringTable_Showcase`, вкладка «Локализация» |
| Оформление `title`, `hint`, `warn`, `whisper`, эффекты `wave`, `shake` | `MarkupLayer_Showcase` |
| Иконки `candle`, `key`, `lamp` | `Icons/Showcase_Icons` (вкладка «Библиотека») |
| Диалог | `Dialogue_Showcase` (вкладка «Диалоги») |
| Говорящие, флаги | `Speaker_Eli`, `Speaker_Keeper`, `DialogueVariables_Showcase` |
| Связка сцены | `Scripts/ShowcaseSample.cs`, `Scripts/Editor/ShowcaseSetup.cs` |

Стили `warn`, `whisper`, `title`, `hint` берутся из `StyleLibrary` проекта (создаётся в
`Create Project Settings`). Набор иконок собран заранее: поле «Папка исходников» у него пустое, поэтому
«Пересобрать» работает после того, как ты укажешь папку с PNG (`Icons/Source`) — либо создай свой набор.
Портреты — простые картинки, нарисованные кодом; используй как угодно.

Руководство со скриншотами: `Documentation~/Manual.ru.md`.

---

# Showcase — Typecraft tour sample

One scene and a set of data behind the `Documentation~/Manual.en.md` manual: strings with styles, icons, an
input prompt and an effect, the "Lighthouse Keeper" dialogue with a branch, a choice and conditions, two
speakers with portraits, a flag catalog, its own table (en, ru, part of zh-Hans) with a markup layer and its own
icon set.

## Running

1. The project needs a `TypecraftSettings` (`Tools/Typecraft/Create Project Settings`) with the built-in
   string tables (`TableTextSource`). TMPEffects (effects) and the Input System (button glyph) are needed.
2. `Tools/Typecraft/Samples/Connect Showcase Table` connects the `Showcase` table to the text source and
   the markup layer and the icon set to `TypecraftSettings`, adds the en, ru, zh-Hans locales.
   To undo: `Tools/Typecraft/Samples/Disconnect Showcase Table`.
3. Open `Showcase.unity` and press Play: "Talk to the keeper" → "Next" or Space (the first press finishes the
   line) → the choice. "Light the lamp" is unavailable until the matches are taken; after it the lamp in the
   scene lights up (the `lamp / on` event). "Reset flags" starts over.

If the project starts from its own bootstrap scene (a DI container, a scene loader), Play loads it first —
run `Showcase.unity` as the start scene or load it from code.

## What is where

| What | Where |
| --- | --- |
| Strings, comments, issues (missing translation, ⚠, missing glyph) | `StringTable_Showcase`, Localization tab |
| Decoration `title`, `hint`, `warn`, `whisper`, effects `wave`, `shake` | `MarkupLayer_Showcase` |
| Icons `candle`, `key`, `lamp` | `Icons/Showcase_Icons` (Library tab) |
| The dialogue | `Dialogue_Showcase` (Dialogues tab) |
| Speakers, flags | `Speaker_Eli`, `Speaker_Keeper`, `DialogueVariables_Showcase` |
| Scene glue | `Scripts/ShowcaseSample.cs`, `Scripts/Editor/ShowcaseSetup.cs` |

The styles `warn`, `whisper`, `title`, `hint` come from the project's `StyleLibrary` (created by
`Create Project Settings`). The icon set is prebuilt: its source folder field is empty, so "Rebuild" works once
you point it at a folder with PNGs (`Icons/Source`) — or create your own set. The portraits are simple images
drawn by code; use them freely.

Manual with screenshots: `Documentation~/Manual.en.md`.
