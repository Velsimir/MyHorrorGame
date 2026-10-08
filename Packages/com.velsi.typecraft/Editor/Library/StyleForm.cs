using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Window.Dialogs;
using Velsi.Typecraft.Editor.Window.Markup;
using Velsi.Typecraft.Editor.Window.Preview;
using Velsi.Typecraft.Fonts;
using Velsi.Typecraft.Styles;

namespace Velsi.Typecraft.Editor.Library
{
    public sealed class StyleForm : TypecraftForm
    {
        private const string Sample = "Дверь заперта — The door is locked 123";

        private readonly StyleLibrary _library;
        private readonly StyleLibrary _draft;
        private readonly SerializedObject _serialized;
        private readonly SerializedProperty _style;
        private readonly TextPreview _preview = new TextPreview();
        private readonly StyleFields _fields;
        private readonly TextField _id;
        private readonly string _locale;
        private readonly DeviceFamily _device;
        private readonly string _base;
        private readonly TextRange _range;

        public StyleForm(StyleLibrary library, string markup, TextRange selection, bool fromSelection, string locale, DeviceFamily device)
            : base(fromSelection ? "Стиль из выделения" : "Новый стиль")
        {
            _library = library;
            _locale = locale ?? Typecraft.Locale;
            _device = device;
            _base = markup ?? string.Empty;
            _range = selection.IsEmpty || _base.Length == 0 ? default : MarkupEdits.Snap(_base, selection);

            _draft = ScriptableObject.CreateInstance<StyleLibrary>();
            _draft.hideFlags = HideFlags.HideAndDontSave;
            _draft.Add(new TextStyle(SuggestId(library), string.Empty));
            _serialized = new SerializedObject(_draft);
            _style = _serialized.FindProperty("_styles").GetArrayElementAtIndex(0);

            if (fromSelection && !_range.IsEmpty)
                (_base, _range) = TakeTags(_base, _range);

            _id = new TextField("Id") { value = _style.FindPropertyRelative("_id").stringValue, tooltip = "Имя тега: [id]текст[/id]" };
            _id.RegisterValueChangedCallback(evt => ApplyId(evt.newValue));
            Fields.Add(_id);
            Fields.Add(new PropertyField(_style.FindPropertyRelative("_displayName"), "Название"));

            _fields = new StyleFields(_serialized, _style);
            Fields.Add(_fields);

            var hint = new Label(_range.IsEmpty
                ? "Превью на образце. «Сохранить» добавит стиль в библиотеку."
                : fromSelection
                    ? "Теги вокруг выделения перенесены в стиль. «Сохранить» заменит их на [id]…[/id]."
                    : "«Сохранить» добавит стиль и обернёт выделение в [id]…[/id].");
            hint.AddToClassList("tc-lib__hint");
            Side.Add(hint);
            _preview.Element.AddToClassList("tc-form__preview");
            Side.Add(_preview.Element);

            this.Bind(_serialized);
            this.TrackSerializedObjectValue(_serialized, _ => OnChanged());
            _fields.RefreshChoices();
            UpdatePreview();
            Validate();
        }

        public event Action<StyleForm> Saved;

        public string SavedId { get; private set; }
        public string ResultMarkup { get; private set; }
        public TextRange ResultRange { get; private set; }
        public string PreviewMarkup { get; private set; }
        public TextPreview Preview => _preview;
        public SerializedProperty Draft => _style;

        public void SetId(string id)
        {
            _id.SetValueWithoutNotify(id);
            ApplyId(id);
        }

        public override void Tick(float deltaTime) => _preview.Tick(deltaTime);

        public override void Dispose()
        {
            _preview.Dispose();
            if (_draft != null)
                UnityEngine.Object.DestroyImmediate(_draft);
        }

        public void Refresh()
        {
            _serialized.Update();
            OnChanged();
        }

        protected override bool Save()
        {
            _serialized.ApplyModifiedPropertiesWithoutUndo();
            string id = _style.FindPropertyRelative("_id").stringValue;
            try
            {
                StyleEdits.Add(_library, _style);
            }
            catch (ArgumentException error)
            {
                SetError(error.Message);
                return false;
            }

            SavedId = id;
            if (!_range.IsEmpty)
            {
                ResultMarkup = MarkupEdits.Wrap(_base, _range, $"[{id}]", $"[/{id}]", out TextRange result);
                ResultRange = result;
            }

            Saved?.Invoke(this);
            return true;
        }

        private void ApplyId(string id)
        {
            _serialized.Update();
            _style.FindPropertyRelative("_id").stringValue = id?.Trim() ?? string.Empty;
            _serialized.ApplyModifiedPropertiesWithoutUndo();
            Validate();
        }

        private void OnChanged()
        {
            _fields.RefreshChoices();
            UpdatePreview();
            Validate();
        }

        private void Validate() => SetError(StyleEdits.ValidateId(_library, _style.FindPropertyRelative("_id").stringValue));

        private void UpdatePreview()
        {
            TextStyle style = _draft.Styles[0];
            FontProfile fonts = Typecraft.Settings != null ? Typecraft.Settings.Fonts : null;
            var open = new StringBuilder();
            var close = new StringBuilder();
            style.AppendOpen(open, fonts, _locale);
            style.AppendClose(close, fonts, _locale);

            PreviewMarkup = _range.IsEmpty
                ? open + Sample + close
                : MarkupEdits.Wrap(_base, _range, open.ToString(), close.ToString(), out _);

            TypecraftSettings settings = Typecraft.Settings;
            FontVariant body = settings != null && settings.Fonts != null ? settings.Fonts.Resolve(settings.DefaultFontRole, _locale) : null;
            _preview.Configure(null, body?.Font, null);
            _preview.SetMarkup(PreviewMarkup, _locale, _device, null);
        }

        private (string Markup, TextRange Range) TakeTags(string markup, TextRange range)
        {
            FontProfile fonts = Typecraft.Settings != null ? Typecraft.Settings.Fonts : null;
            List<MarkupModule> modules = MarkupDocument.Parse(markup).ModulesIn(range)
                .Where(entry => entry.Coverage == ModuleCoverage.Full && !entry.Module.IsStyle)
                .Select(entry => entry.Module)
                .ToList();

            IReadOnlyList<string> consumed = StyleFromText.Prefill(_style, modules, fonts);
            _serialized.ApplyModifiedPropertiesWithoutUndo();

            foreach (string slot in consumed.Distinct())
            {
                string updated = MarkupDocument.Parse(markup).RemoveSlot(range, slot, out TextRange result);
                if (result.IsEmpty)
                    continue;

                markup = updated;
                range = result;
            }

            return (markup, range);
        }

        private static string SuggestId(StyleLibrary library)
        {
            string id = "style";
            for (int n = 2; library != null && library.TryGet(id, out _); n++)
                id = $"style{n}";
            return id;
        }
    }
}
