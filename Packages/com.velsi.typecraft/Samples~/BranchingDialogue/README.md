# Branching Dialogue — сэмпл диалогов Typecraft

Одна сцена: маяк, смотритель, записка и дверь. Показывает модуль диалогов целиком.

| Что | Где в сцене |
| --- | --- |
| Окно диалога с портретами двух говорящих (слева Эли, справа Смотритель), эмоции меняют портрет | `Dialogue Box` + `Keeper Dialogue` |
| Выбор реплик: вариант недоступен без флага (**Disable**) и вариант, скрытый после флага (**Hide**) | нода Choice в `Dialogue_Keeper` |
| Разговор меняется от флага `note_found`: нода **Branch** выбирает приветствие, **вариант реплики** (Line → Variants) меняет ответ Эли | `Dialogue_Keeper` |
| Записка в мире (**World Text**), при чтении ставит `note_found = true` нодой **Set Variables** | `Note Text` + `Note Dialogue` |
| Субтитры: мысль Эли при входе в зону, один раз (`Once` у DialoguePlayer) | `Subtitles` + `Thought Dialogue` + `Cold Zone` |
| Событие из диалога: нода **Event** `door/open` → `DialoguePlayer.OnEvent` → дверь открывается и желтеет | `Keeper Dialogue` → `Door` |
| «Далее» кнопкой или пробелом, сброс флагов, смена языка | верхняя панель |

## Запуск

1. В проекте должен быть `TypecraftSettings` (`Tools/Typecraft/Create Project Settings`)
   со встроенными таблицами строк (`TableTextSource`).
2. `Tools/Typecraft/Samples/Connect Branching Dialogue Table` — подключает таблицу сэмпла
   к источнику строк, её слой разметки к `TypecraftSettings` и добавляет языки en, ru, zh-Hans.
   Отключить обратно — `Tools/Typecraft/Samples/Disconnect Branching Dialogue Table`.
3. Открой `BranchingDialogue.unity` и нажми Play.
4. Сценарий: «Talk to the keeper» → пройди разговор (кнопка «Next» или пробел, первое
   нажатие допечатывает реплику) → «Read the note» → снова «Talk to the keeper»: другое
   приветствие, вариант «Ask about the note» стал доступен, «I found nothing upstairs» исчез.
   Выбери записку — дверь справа откроется. «Step into the cold» — мысль в субтитрах,
   второй раз не повторяется. «Reset flags» возвращает всё в начало.

Если в проекте стоит Input System, пробел и East на геймпаде читаются через неё; иначе —
через старый Input Manager.

## Как устроено

```
DialoguePlayer (ассет диалога, вывод, события)
   └─ DialogueRunner — идёт по нодам, читает и пишет флаги
        ├─ Line / Choice → вывод: DialogueBoxPresenter, SubtitlePresenter, WorldTextPresenter
        ├─ Set Variables / Branch / условия → флаги: DialogueGlobals.Variables
        └─ Event → DialoguePlayer.OnEvent (UnityEvent) и событие EventRaised
```

- **Три диалога — три `DialoguePlayer`**, у каждого свой вывод: окно, текст в мире, субтитры.
  Флаги у всех общие — `DialogueGlobals.Variables`, поэтому записка, прочитанная одним
  плеером, меняет разговор в другом.
- **Каталог флагов** `DialogueVariables_BranchingDialogue` описывает `note_found` (тип,
  значение по умолчанию, описание). Скрипт сэмпла кладёт его в `DialogueGlobals.Catalog`;
  во вкладке «Диалоги» каталог подхватывается сам — флаг выбирается из списка в условиях.
- **Тексты** — только ключи из `StringTable_BranchingDialogue`; оформление (`[whisper]`,
  `[warn]`, `<shake>`) лежит в `MarkupLayer_BranchingDialogue`. Имена говорящих — ключи
  `speaker.*` в той же таблице, у Смотрителя стиль имени `hint`.
- **Говорящие** `Speaker_Eli` / `Speaker_Keeper`: ключ имени, портреты по эмоциям
  (`neutral`, `worried`, `angry`, `smile`), сторона портрета.
- **Зона**: `Cold Zone` — `DialogueTrigger` в режиме `OnTriggerEnter` с тегом `Player`.
  В сэмпле нет персонажа с физикой, поэтому кнопка вызывает `DialogueTrigger.Fire()` —
  то же, что сделал бы вход в триггер. Брось в зону объект с тегом `Player`, коллайдером
  и Rigidbody — сработает сам.
- `BranchingDialogueSample.cs` — только связка UI: кнопки, «Далее», сброс флагов, язык.
  `SampleDoor.cs` — приёмник события `door`.

Открой `Window → Typecraft` → вкладка «Диалоги», выбери `Dialogue_Keeper`: граф, инспектор
ноды, «▶ Проиграть» (проигрывание без запуска игры, флаги — в нижней панели) и «Проверить».

## Как сделать свой диалог

1. Вкладка «Диалоги» → «+» у раздела «Диалоги» — новый диалог; таблицу строк выбери
   в выпадающем списке на панели.
2. «+ Нода» (на графе — пробел или правый клик): Line, Choice, Branch, Set Variables, Event…
   Соедини порты. В инспекторе Line — говорящий, эмоция, ключ; «Создать ключ» заводит строку
   в таблице прямо оттуда.
3. Флаги — «+» у раздела «Флаги» (каталог создастся, если его нет) или свой каталог
   `Create → Typecraft → Dialogue Variables`.
4. «Проверить» → «▶ Проиграть».
5. В сцене: `GameObject → Typecraft → Dialogue Box` (или Subtitles, World Text),
   рядом `DialoguePlayer` с диалогом и выводом. Запуск — `Play On Start`, `DialogueTrigger`
   или `player.Play()` из кода.

## Если что-то не так

- **Вместо текста ключи `keeper.greet`** — таблица не подключена, см. шаг 2.
- **Нет курсива, цвета и дрожи** — не подключён слой разметки (шаг 2), или строку в таблице
  поменяли: во вкладке «Локализация» ячейка с ⚠, нажми «Перенести теги».
- **Квадраты вместо иероглифов (zh-Hans)** — статический шрифт проекта собран под другие
  строки. Вкладка «Шрифты» → «Пересобрать» у шрифта zh-Hans.
- **Текст появляется сразу, без машинки; `<shake>` виден как тег** — не установлен TMPEffects.
- **Флаги «залипли» между запусками** — `DialogueGlobals` сбрасывается при входе в Play Mode;
  внутри сессии жми «Reset flags».

Портреты — простые картинки, нарисованные кодом для сэмпла; используй как угодно.

---

# Branching Dialogue — Typecraft dialogue sample

One scene: a lighthouse, its keeper, a note and a door. It shows the whole dialogue module.

| What | Where in the scene |
| --- | --- |
| Dialogue box with portraits of two speakers (Eli on the left, the Keeper on the right); emotions switch portraits | `Dialogue Box` + `Keeper Dialogue` |
| Choices: one unavailable without a flag (**Disable**), one hidden once the flag is set (**Hide**) | the Choice node in `Dialogue_Keeper` |
| The conversation follows the `note_found` flag: a **Branch** node picks the greeting, a **line variant** (Line → Variants) changes Eli's reply | `Dialogue_Keeper` |
| A note in the world (**World Text**); reading it sets `note_found = true` with a **Set Variables** node | `Note Text` + `Note Dialogue` |
| Subtitles: Eli's thought on entering a zone, once (`Once` on the DialoguePlayer) | `Subtitles` + `Thought Dialogue` + `Cold Zone` |
| An event from the dialogue: **Event** node `door/open` → `DialoguePlayer.OnEvent` → the door opens and turns yellow | `Keeper Dialogue` → `Door` |
| "Next" button or Space, flag reset, language switch | top bar |

## Running

1. The project needs a `TypecraftSettings` (`Tools/Typecraft/Create Project Settings`) with
   the built-in string tables (`TableTextSource`).
2. `Tools/Typecraft/Samples/Connect Branching Dialogue Table` adds the sample table to the
   text source, its markup layer to `TypecraftSettings`, and the en, ru, zh-Hans locales.
   To undo: `Tools/Typecraft/Samples/Disconnect Branching Dialogue Table`.
3. Open `BranchingDialogue.unity` and press Play.
4. Walkthrough: "Talk to the keeper" → go through the talk ("Next" or Space; the first press
   finishes the line) → "Read the note" → "Talk to the keeper" again: a different greeting,
   "Ask about the note" is now available and "I found nothing upstairs" is gone. Pick the
   note — the door on the right opens. "Step into the cold" shows a thought in the subtitles,
   only once. "Reset flags" starts over.

With the Input System installed, Space and the gamepad East button are read through it;
otherwise through the legacy Input Manager.

## How it works

```
DialoguePlayer (dialogue asset, output, events)
   └─ DialogueRunner — walks the nodes, reads and writes flags
        ├─ Line / Choice → output: DialogueBoxPresenter, SubtitlePresenter, WorldTextPresenter
        ├─ Set Variables / Branch / conditions → flags: DialogueGlobals.Variables
        └─ Event → DialoguePlayer.OnEvent (UnityEvent) and the EventRaised event
```

- **Three dialogues, three `DialoguePlayer`s**, each with its own output: the box, world
  text, subtitles. They share flags through `DialogueGlobals.Variables`, so a note read by
  one player changes the conversation in another.
- **The flag catalog** `DialogueVariables_BranchingDialogue` declares `note_found` (type,
  default, description). The sample script puts it into `DialogueGlobals.Catalog`; the
  Dialogues tab finds catalogs on its own and offers the flag in condition dropdowns.
- **Text** is keys in `StringTable_BranchingDialogue`; decoration (`[whisper]`, `[warn]`,
  `<shake>`) lives in `MarkupLayer_BranchingDialogue`. Speaker names are `speaker.*` keys
  in the same table; the Keeper's name uses the `hint` style.
- **Speakers** `Speaker_Eli` / `Speaker_Keeper`: name key, portraits per emotion
  (`neutral`, `worried`, `angry`, `smile`), portrait side.
- **The zone**: `Cold Zone` is a `DialogueTrigger` in `OnTriggerEnter` mode with the
  `Player` tag. The sample has no physics character, so the button calls
  `DialogueTrigger.Fire()` — exactly what entering the trigger does. Drop an object with the
  `Player` tag, a collider and a Rigidbody into the zone and it fires by itself.
- `BranchingDialogueSample.cs` only wires the UI: buttons, Next, flag reset, language.
  `SampleDoor.cs` receives the `door` event.

Open `Window → Typecraft` → Dialogues tab and pick `Dialogue_Keeper`: the graph, the node
inspector, «▶ Проиграть» (play without entering Play Mode, flags in the bottom panel) and
«Проверить» (check).

## Making your own dialogue

1. Dialogues tab → "+" next to «Диалоги» (dialogues) — a new dialogue; pick its string
   table in the toolbar dropdown.
2. «+ Нода» (on the graph: Space or right-click): Line, Choice, Branch, Set Variables,
   Event… Connect the ports. The Line inspector has speaker, emotion and key; «Создать ключ»
   (create key) adds the string to the table from there.
3. Flags: "+" next to «Флаги» (flags; a catalog is created if there is none) or your own
   catalog via `Create → Typecraft → Dialogue Variables`.
4. «Проверить» (check) → «▶ Проиграть» (play).
5. In the scene: `GameObject → Typecraft → Dialogue Box` (or Subtitles, World Text) and a
   `DialoguePlayer` with the dialogue and the output. Start it with `Play On Start`, a
   `DialogueTrigger` or `player.Play()` from code.

## Troubleshooting

- **Keys like `keeper.greet` instead of text** — the table is not connected, see step 2.
- **No italics, colors or shaking** — the markup layer is not connected (step 2), or the
  table string changed: the cell shows ⚠ in the Localization tab, press «Перенести теги»
  (transfer tags).
- **Boxes instead of Chinese characters** — the project's static zh-Hans font was built for
  other strings. Fonts tab → «Пересобрать» (rebuild) on the zh-Hans font.
- **No typewriter, `<shake>` shows as a tag** — TMPEffects is not installed.
- **Flags stick between runs** — `DialogueGlobals` resets on entering Play Mode; inside a
  session press "Reset flags".

The portraits are simple images drawn by code for this sample; use them freely.
