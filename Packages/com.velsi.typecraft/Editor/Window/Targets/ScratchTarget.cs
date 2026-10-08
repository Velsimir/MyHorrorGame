using TMPro;
using UnityEditor;

namespace Velsi.Typecraft.Editor.Window.Targets
{
    public sealed class ScratchTarget : ITextEditTarget
    {
        private const string MarkupKey = "Typecraft.Scratch.Markup";
        private const string RoleKey = "Typecraft.Scratch.Role";
        private const string DefaultMarkup = "Дверь заперта. Нажми {key:Interact}, чтобы осмотреть [warn]замок[/warn].\n[whisper]Он всё ещё <wave>здесь</wave>…[/whisper]";

        public string Title => "Черновик";
        public string Markup => SessionState.GetString(MarkupKey, DefaultMarkup);
        public bool IsValid => true;
        public bool CanWrite => true;
        public string ReadOnlyReason => null;
        public string FontRole => SessionState.GetString(RoleKey, null);
        public TMP_Text Reference => null;

        public void Write(string markup) => SessionState.SetString(MarkupKey, markup);
        public void SetFontRole(string role) => SessionState.SetString(RoleKey, role);
    }
}
