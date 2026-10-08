using System;
using TMPro;

namespace Velsi.Typecraft.Editor.Window.Targets
{
    public sealed class CallbackTarget : ITextEditTarget
    {
        private readonly Action<string> _onApply;
        private string _markup;

        public CallbackTarget(string markup, Action<string> onApply, string title, TMP_Text reference)
        {
            _markup = markup ?? string.Empty;
            _onApply = onApply;
            Title = string.IsNullOrEmpty(title) ? "Внешний текст" : title;
            Reference = reference;
        }

        public string Title { get; }
        public string Markup => _markup;
        public bool IsValid => true;
        public bool CanWrite => _onApply != null;
        public string ReadOnlyReason => _onApply == null ? "Текст открыт только для просмотра." : null;
        public string FontRole { get; private set; }
        public TMP_Text Reference { get; }

        public void Write(string markup)
        {
            _markup = markup ?? string.Empty;
            _onApply?.Invoke(_markup);
        }

        public void SetFontRole(string role) => FontRole = role;
    }
}
