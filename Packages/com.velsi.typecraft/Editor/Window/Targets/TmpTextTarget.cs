using TMPro;
using UnityEditor;
using UnityEngine;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Editor.Window.Targets
{
    public sealed class TmpTextTarget : ITextEditTarget
    {
        private readonly TMP_Text _text;

        public TmpTextTarget(TMP_Text text)
        {
            _text = text;
        }

        public string Title => _text != null ? $"{_text.name} · TMP_Text (добавит Typecraft Text при применении)" : "—";
        public string Markup => _text != null ? _text.text ?? string.Empty : string.Empty;
        public bool IsValid => _text != null && _text.GetComponent<TypecraftText>() == null;
        public bool CanWrite => _text != null;
        public string ReadOnlyReason => null;
        public string FontRole => null;
        public TMP_Text Reference => _text;

        public void Write(string markup)
        {
            if (_text == null)
                return;

            var component = Undo.AddComponent<TypecraftText>(_text.gameObject);
            new TypecraftTextTarget(component).Write(markup);
            Selection.activeGameObject = _text.gameObject;
        }

        public void SetFontRole(string role)
        {
        }
    }
}
