# Simple Dialogue — учебный сэмпл Typecraft

Диалоговое окно на пять реплик: стили, иконки, подсказка ввода, эффекты TMPEffects,
свой тег и переключение языка и устройства на лету.

## Запуск

1. В проекте должен быть `TypecraftSettings` (`Tools/Typecraft/Create Project Settings`)
   со встроенными таблицами строк в качестве источника (`TableTextSource`).
2. `Tools/Typecraft/Samples/Connect Simple Dialogue Table` — подключает таблицу сэмпла
   к источнику строк, её слой разметки к `TypecraftSettings` и добавляет языки en, ru, zh-Hans.
3. Открой `SimpleDialogue.unity` и нажми Play.
4. «Далее», Space, Enter или South на геймпаде — следующая реплика (первое нажатие
   досрочно допечатывает текущую). Кнопки сверху переключают язык и устройство.

## Что посмотреть

| Где | Что показывает |
| --- | --- |
| `StringTable_SimpleDialogue` | Реплики на трёх языках — только слова и токены `{…}`, то, что видит переводчик; открой во вкладке «Локализация» окна Typecraft |
| `MarkupLayer_SimpleDialogue` | Слой разметки: те же реплики со стилями и тегами TMPEffects. Ячейки с оформлением помечены ✦ |
| `SimpleDialogue.cs` | `TypecraftText.SetKey` для строк из таблицы и `SetMarkup` для строки без ключа |
| `SimpleDialogue` → `Lines` в инспекторе | Атрибут `[TypecraftMarkup]`: поле с подсветкой, превью и кнопкой ✎ |
| `PlayerTag.cs` | Свой тег `{player:name}` через `IMarkupTagHandler` |
| `SampleSwitchers.cs` | `ITextSource.SelectLocale` и `Typecraft.SetDevice` |
| `sample.hint` | `{key:Interact}` — глиф по текущему устройству и биндингу |

## Если что-то не так

- **Вместо текста виден ключ `sample.greeting`** — таблица не подключена, см. шаг 2.
- **Текст без стилей и эффектов** — не подключён слой разметки (шаг 2) или строку в таблице
  поменяли: во вкладке «Локализация» ячейка помечена ⚠, нажми «Перенести теги».
- **Вместо кнопки красный `[Interact]`** — в проекте нет действия `Interact` в
  `InputGlyphProfile`. Замени ключ в таблице на своё действие.
- **Квадраты вместо иероглифов** — во вкладке «Шрифты» собери статический шрифт для
  zh-Hans (подойдёт Noto Sans SC) или нажми «Пересобрать» у существующего.
- **Текст не печатается машинкой, `<wave>` виден как тег** — не установлен TMPEffects.

---

# Simple Dialogue — Typecraft tutorial sample

A five-line dialogue box: styles, icons, an input prompt, TMPEffects animations, a custom
tag, and live language and device switching.

## Running

1. The project needs a `TypecraftSettings` (`Tools/Typecraft/Create Project Settings`) with
   the built-in string tables (`TableTextSource`) as the text source.
2. `Tools/Typecraft/Samples/Connect Simple Dialogue Table` adds the sample table to the text
   source, its markup layer to `TypecraftSettings`, and adds the en, ru and zh-Hans locales.
3. Open `SimpleDialogue.unity` and press Play.
4. "Next", Space, Enter or the gamepad South button shows the next line (the first press
   finishes the current one). The top buttons switch language and device.

## What to look at

| Where | What it shows |
| --- | --- |
| `StringTable_SimpleDialogue` | Lines in three languages — words and `{…}` tokens only, what a translator sees; open it in the Localization tab of the Typecraft window |
| `MarkupLayer_SimpleDialogue` | The markup layer: the same lines with styles and TMPEffects tags. Decorated cells are marked ✦ |
| `SimpleDialogue.cs` | `TypecraftText.SetKey` for table lines and `SetMarkup` for a line without a key |
| `SimpleDialogue` → `Lines` in the Inspector | The `[TypecraftMarkup]` attribute: highlighted field, preview and ✎ button |
| `PlayerTag.cs` | A custom `{player:name}` tag via `IMarkupTagHandler` |
| `SampleSwitchers.cs` | `ITextSource.SelectLocale` and `Typecraft.SetDevice` |
| `sample.hint` | `{key:Interact}` — glyph for the current device and binding |

## Troubleshooting

- **The key `sample.greeting` is shown instead of text** — the table is not connected, see step 2.
- **Text shows without styles and effects** — the markup layer is not connected (step 2), or the
  table string was changed: the cell is marked ⚠ in the Localization tab, press «Перенести теги» (transfer tags).
- **A red `[Interact]` instead of a button** — the project has no `Interact` action in its
  `InputGlyphProfile`. Change the key in the table to one of your actions.
- **Boxes instead of Chinese characters** — build a static font for zh-Hans in the Fonts tab
  (Noto Sans SC works) or press "Rebuild" on the existing one.
- **No typewriter, `<wave>` shows as a tag** — TMPEffects is not installed.
