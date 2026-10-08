using TMPro;
using UnityEditor;
using UnityEngine;
using Velsi.Typecraft.Editor.Window.Effects;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Editor.Window.Targets
{
    public sealed class TypecraftTextTarget : ITextEditTarget
    {
        private readonly TypecraftText _text;

        public TypecraftTextTarget(TypecraftText text)
        {
            _text = text;
        }

        public string Title => _text != null ? $"{_text.name} · Typecraft Text" : "—";
        public string Markup => _text == null ? string.Empty : _text.Mode == TextMode.Inline ? _text.Markup ?? string.Empty : $"{{{_text.Table}/{_text.Key}}}";
        public bool IsValid => _text != null;
        public bool CanWrite => _text != null && _text.Mode == TextMode.Inline;
        public string ReadOnlyReason => CanWrite ? null : "Текст берётся из таблицы локализации — правка строк появится во вкладке «Локализация».";
        public string FontRole => _text != null ? _text.FontRole : null;
        public TMP_Text Reference => _text != null ? _text.Target : null;
        public TypecraftText Component => _text;

        public void Write(string markup)
        {
            if (!CanWrite)
                return;

            Undo.RecordObject(_text, "Typecraft Edit");
            var serialized = new SerializedObject(_text);
            serialized.FindProperty("_markup").stringValue = markup;
            serialized.ApplyModifiedProperties();
            EffectComponents.Ensure(_text.Target, markup);
            _text.Refresh();

            if (Application.isPlaying)
                PlayModeEdits.Record(_text.Target, markup);
        }

        public void SetFontRole(string role)
        {
            if (_text == null)
                return;

            Undo.RecordObject(_text, "Typecraft Font Role");
            var serialized = new SerializedObject(_text);
            serialized.FindProperty("_fontRole").stringValue = role;
            serialized.ApplyModifiedProperties();
            _text.Refresh();
        }
    }
}
