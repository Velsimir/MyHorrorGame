using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Window.Layout;
using Velsi.Typecraft.Editor.Window.Preview;
using Velsi.Typecraft.Fonts;
using Velsi.Typecraft.Styles;

namespace Velsi.Typecraft.Editor.Library
{
    public sealed class StylePanel : VisualElement, IDisposable
    {
        public static readonly Regex ValidId = new Regex(@"^[A-Za-z][\w.-]*$");

        private readonly StyleLibrary _library;
        private readonly SerializedObject _serialized;
        private readonly SerializedProperty _style;
        private readonly TextPreview _preview = new TextPreview();
        private readonly TextField _sample;
        private readonly StyleFields _fields;
        private readonly Label _title;

        public StylePanel(StyleLibrary library, int index)
        {
            _library = library;
            _serialized = new SerializedObject(library);
            _style = _serialized.FindProperty("_styles").GetArrayElementAtIndex(index);
            AddToClassList("tc-lib__panel");

            var header = new VisualElement();
            header.AddToClassList("tc-lib__header");
            _title = new Label();
            _title.AddToClassList("tc-lib__title");
            header.Add(_title);
            header.Add(new Button(Duplicate) { text = "Дублировать" });
            header.Add(new Button(Remove) { text = "Удалить" });
            Add(header);

            ScrollView form = TypecraftLayout.VerticalScroll("tc-lib__form");
            Add(form);

            var id = new TextField("Id") { isDelayed = true, value = Id, tooltip = "Имя тега: [id]текст[/id]. При переименовании теги в таблицах и сценах переписываются" };
            id.RegisterValueChangedCallback(evt => Rename(evt.newValue, id));
            form.Add(id);
            form.Add(Field("_displayName", "Название"));

            _fields = new StyleFields(_serialized, _style);
            form.Add(_fields);

            form.Add(Field("_autoTagWords", "Слова автотега"));

            var previewBox = new VisualElement();
            previewBox.AddToClassList("tc-lib__preview");
            _sample = new TextField("Образец") { value = "Дверь заперта — The door is locked 123" };
            _sample.RegisterValueChangedCallback(_ => UpdatePreview());
            previewBox.Add(_sample);
            previewBox.Add(_preview.Element);
            Add(previewBox);

            this.Bind(_serialized);
            this.TrackSerializedObjectValue(_serialized, _ => OnChanged());
            RefreshChoices();
            UpdatePreview();
        }

        public event Action<string> Selected;

        private string Id => _style.FindPropertyRelative("_id").stringValue;

        public void Tick(float deltaTime) => _preview.Tick(deltaTime);

        public void Dispose() => _preview.Dispose();

        private PropertyField Field(string name, string label) => new PropertyField(_style.FindPropertyRelative(name), label);

        private void OnChanged()
        {
            Typecraft.Refresh();
            RefreshChoices();
            UpdatePreview();
        }

        private void RefreshChoices()
        {
            _title.text = $"[{Id}]";
            _fields.RefreshChoices();
        }

        private void UpdatePreview()
        {
            TypecraftSettings settings = Typecraft.Settings;
            FontVariant body = settings != null && settings.Fonts != null ? settings.Fonts.Resolve(settings.DefaultFontRole, Typecraft.Locale) : null;
            _preview.Configure(null, body?.Font, null);
            _preview.SetMarkup($"[{Id}]{_sample.value}[/{Id}]", Typecraft.Locale, Typecraft.Device, null);
        }

        private void SetString(string field, string value)
        {
            _serialized.Update();
            _style.FindPropertyRelative(field).stringValue = value;
            _serialized.ApplyModifiedProperties();
        }

        private void Rename(string value, TextField field)
        {
            string oldId = Id;
            string newId = value?.Trim() ?? string.Empty;
            if (newId == oldId)
                return;

            if (!ValidId.IsMatch(newId) || _library.TryGet(newId, out _))
            {
                EditorUtility.DisplayDialog("Typecraft", $"Id «{newId}» должен начинаться с латинской буквы, состоять из букв, цифр, «_», «.», «-» и не повторяться.", "OK");
                field.SetValueWithoutNotify(oldId);
                return;
            }

            MarkupRewriter rewriter = MarkupRewriter.Prepare(MarkupRewriter.StylePattern(oldId), MarkupRewriter.StyleReplacement(newId));
            if (!rewriter.IsEmpty && !EditorUtility.DisplayDialog("Typecraft", $"Теги [{oldId}] будут переписаны на [{newId}]:\n\n{rewriter.Describe()}{rewriter.UndoNote}", "Переименовать", "Отмена"))
            {
                field.SetValueWithoutNotify(oldId);
                return;
            }

            Undo.SetCurrentGroupName("Typecraft: переименование стиля");
            int group = Undo.GetCurrentGroup();
            SetString("_id", newId);
            rewriter.Apply("Typecraft: переименование стиля");
            Undo.CollapseUndoOperations(group);
            Typecraft.Refresh();
            Selected?.Invoke(newId);
        }

        private void Duplicate()
        {
            string id = Id;
            string copy = id + "_copy";
            for (int n = 2; _library.TryGet(copy, out _); n++)
                copy = $"{id}_copy{n}";

            _serialized.Update();
            SerializedProperty styles = _serialized.FindProperty("_styles");
            int index = IndexOf(styles, id);
            styles.InsertArrayElementAtIndex(index);
            styles.GetArrayElementAtIndex(index + 1).FindPropertyRelative("_id").stringValue = copy;
            _serialized.ApplyModifiedProperties();
            Typecraft.Refresh();
            Selected?.Invoke(copy);
        }

        private void Remove()
        {
            string id = Id;
            MarkupRewriter uses = MarkupRewriter.Prepare(MarkupRewriter.StylePattern(id), "$0");
            string message = uses.IsEmpty
                ? $"Удалить стиль [{id}]?"
                : $"Стиль [{id}] используется:\n\n{uses.Describe()}\n\nТеги останутся в тексте и будут подсвечены как ошибка.";
            if (!EditorUtility.DisplayDialog("Typecraft", message, "Удалить", "Отмена"))
                return;

            StyleEdits.Remove(_library, id);
            Selected?.Invoke(null);
        }

        private static int IndexOf(SerializedProperty styles, string id) => StyleEdits.IndexOf(styles, id);
    }
}
