using TMPro;

namespace Velsi.Typecraft.Editor.Window.Targets
{
    public interface ITextEditTarget
    {
        string Title { get; }
        string Markup { get; }
        bool IsValid { get; }
        bool CanWrite { get; }
        string ReadOnlyReason { get; }
        string FontRole { get; }
        TMP_Text Reference { get; }
        void Write(string markup);
        void SetFontRole(string role);
    }
}
