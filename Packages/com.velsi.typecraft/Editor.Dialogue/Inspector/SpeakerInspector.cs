using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Dialogue.Editor.Model;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Styles;

namespace Velsi.Typecraft.Dialogue.Editor.Inspector
{
    public sealed class SpeakerInspector : VisualElement, IDisposable
    {
        private static readonly List<string> Sides = new List<string> { "Не важно", "Слева", "Справа" };

        private readonly KeyField _name;

        public SpeakerInspector(IDialogueEditorContext context, SpeakerProfile speaker, Action delete)
        {
            AddToClassList("dlg-inspector");
            void Edit(string undoName, Action change, bool rebuild = false) => context.Edit(speaker, undoName, change, rebuild);

            var header = new VisualElement();
            header.AddToClassList("tc-lib__header");
            var title = new Label("☺ " + speaker.name);
            title.AddToClassList("tc-lib__title");
            header.Add(title);
            header.Add(new Button(() => EditorGUIUtility.PingObject(speaker)) { text = "Показать" });
            header.Add(new Button(delete) { text = "Удалить" });
            Add(header);

            var id = new TextField("Id") { value = speaker.Id, isDelayed = true, tooltip = "Пусто — имя ассета" };
            id.RegisterValueChangedCallback(evt => Edit("id говорящего", () => speaker.Id = evt.newValue == speaker.name ? null : evt.newValue));
            Add(id);

            ITableBackend backend = DialogueTexts.Backend;
            List<string> tables = backend != null ? backend.Tables.Distinct().ToList() : new List<string>();
            if (!string.IsNullOrEmpty(speaker.NameTable) && !tables.Contains(speaker.NameTable))
                tables.Add(speaker.NameTable);
            var tableChoices = new List<string> { "(таблица диалога)" };
            tableChoices.AddRange(tables);
            var table = new DropdownField("Таблица имени", tableChoices, string.IsNullOrEmpty(speaker.NameTable) ? 0 : tables.IndexOf(speaker.NameTable) + 1);
            table.RegisterValueChangedCallback(_ => Edit("таблица имени", () => speaker.NameTable = table.index <= 0 ? null : tables[table.index - 1], true));
            Add(table);

            string NameTable() => speaker.ResolveNameTable(context.Asset != null && !string.IsNullOrEmpty(context.Asset.DefaultTable) ? context.Asset.DefaultTable : tables.FirstOrDefault());
            _name = new KeyField("Имя (ключ)", context, NameTable, () => "speaker." + DialogueTexts.SnakeCase(speaker.name), false) { Value = speaker.NameKey };
            _name.Changed += value => Edit("имя говорящего", () => speaker.NameKey = value);
            Add(_name);

            StyleLibrary library = Typecraft.Settings != null ? Typecraft.Settings.Styles : null;
            List<string> styles = library != null ? library.Styles.Where(style => style != null && !string.IsNullOrEmpty(style.Id)).Select(style => style.Id).ToList() : new List<string>();
            if (!string.IsNullOrEmpty(speaker.StyleId) && !styles.Contains(speaker.StyleId))
                styles.Add(speaker.StyleId);
            var styleChoices = new List<string> { "(без стиля)" };
            styleChoices.AddRange(styles);
            var style = new DropdownField("Стиль имени", styleChoices, string.IsNullOrEmpty(speaker.StyleId) ? 0 : styles.IndexOf(speaker.StyleId) + 1) { tooltip = "Стиль из библиотеки Typecraft для имени говорящего" };
            style.RegisterValueChangedCallback(_ => Edit("стиль говорящего", () => speaker.StyleId = style.index <= 0 ? null : styles[style.index - 1]));
            Add(style);

            var side = new DropdownField("Сторона", Sides, (int)speaker.Side);
            side.RegisterValueChangedCallback(_ => Edit("сторона говорящего", () => speaker.Side = (SpeakerSide)side.index));
            Add(side);

            var voice = new ObjectField("Звук голоса") { objectType = typeof(AudioClip), allowSceneObjects = false, value = speaker.VoiceBlip };
            voice.RegisterValueChangedCallback(evt => Edit("звук говорящего", () => speaker.VoiceBlip = evt.newValue as AudioClip));
            Add(voice);

            var portrait = new ObjectField("Портрет по умолчанию") { objectType = typeof(Sprite), allowSceneObjects = false, value = speaker.DefaultPortrait };
            portrait.RegisterValueChangedCallback(evt => Edit("портрет", () => speaker.DefaultPortrait = evt.newValue as Sprite));
            Add(portrait);

            var section = new VisualElement();
            section.AddToClassList("dlg-row");
            section.AddToClassList("dlg-section");
            var sectionTitle = new Label("Портреты по эмоциям");
            sectionTitle.AddToClassList("tc-lib__section");
            section.Add(sectionTitle);
            section.Add(ConditionEditor.Grow());
            section.Add(new Button(() => Edit("портрет эмоции", () => speaker.Portraits.Add(new SpeakerPortrait("emotion", null)), true)) { text = "+ портрет" });
            Add(section);

            foreach (SpeakerPortrait entry in speaker.Portraits.ToList())
            {
                if (entry == null)
                    continue;

                var row = new VisualElement();
                row.AddToClassList("dlg-row");
                var emotion = new TextField { value = entry.Emotion, isDelayed = true };
                emotion.AddToClassList("dlg-portrait__emotion");
                emotion.RegisterValueChangedCallback(evt => Edit("эмоция портрета", () => entry.Emotion = evt.newValue));
                row.Add(emotion);
                var sprite = new ObjectField { objectType = typeof(Sprite), allowSceneObjects = false, value = entry.Sprite };
                sprite.style.flexGrow = 1;
                sprite.RegisterValueChangedCallback(evt => Edit("спрайт портрета", () => entry.Sprite = evt.newValue as Sprite));
                row.Add(sprite);
                row.Add(ConditionEditor.SmallButton("✕", "Удалить портрет", () => Edit("удаление портрета", () => speaker.Portraits.Remove(entry), true)));
                Add(row);
            }
        }

        public void Tick(float deltaTime) => _name.Tick(deltaTime);

        public void Dispose() => _name.Dispose();
    }
}
