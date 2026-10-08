using System;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Dialogue
{
    public static class DialogueSpeakerText
    {
        public static void RequestName(SpeakerProfile speaker, string fallbackTable, string fallbackStyle, Action<string> onReady)
        {
            if (onReady == null)
                return;

            if (speaker == null || string.IsNullOrEmpty(speaker.NameKey))
            {
                onReady(null);
                return;
            }

            string style = string.IsNullOrEmpty(speaker.StyleId) ? fallbackStyle : speaker.StyleId;
            LocalizedText.Request(speaker.ResolveNameTable(fallbackTable), speaker.NameKey, null, (text, _) => onReady(Styled(text, style)));
        }

        public static void RequestLine(DialogueLine line, Action<string> onReady)
        {
            if (line == null || onReady == null)
                return;

            LocalizedText.Request(line.ResolveTable(), line.Key, null, (text, _) => onReady(text));
        }

        public static string Styled(string markup, string styleId) =>
            string.IsNullOrEmpty(styleId) || string.IsNullOrEmpty(markup) ? markup : $"[{styleId}]{markup}[/{styleId}]";
    }
}
