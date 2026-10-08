using System.Text.RegularExpressions;

namespace Velsi.Typecraft.Editor.Window.Markup
{
    public readonly struct TextRange
    {
        public TextRange(int start, int end)
        {
            Start = start < end ? start : end;
            End = start < end ? end : start;
        }

        public int Start { get; }
        public int End { get; }
        public int Length => End - Start;
        public bool IsEmpty => Start == End;
    }

    public static class MarkupEdits
    {
        public static TextRange Snap(string markup, TextRange range)
        {
            int start = SnapOutOfToken(markup, range.Start, towardsStart: true);
            int end = SnapOutOfToken(markup, range.End, towardsStart: false);
            return new TextRange(start, end);
        }

        public static string Wrap(string markup, TextRange range, string open, string close, out TextRange result)
        {
            range = Snap(markup, range);
            string wrapped = markup.Substring(0, range.Start) + open + markup.Substring(range.Start, range.Length) + close + markup.Substring(range.End);
            result = new TextRange(range.Start, range.End + open.Length + close.Length);
            return wrapped;
        }

        public static string Insert(string markup, int index, string text, out int caret)
        {
            index = SnapOutOfToken(markup, index, towardsStart: false);
            caret = index + text.Length;
            return markup.Substring(0, index) + text + markup.Substring(index);
        }

        public static string Replace(string markup, TextRange range, string text, out TextRange result)
        {
            range = Snap(markup, range);
            result = new TextRange(range.Start, range.Start + text.Length);
            return markup.Substring(0, range.Start) + text + markup.Substring(range.End);
        }

        public static string WordAt(string markup, TextRange range)
        {
            range = Snap(markup, range);
            string text = markup.Substring(range.Start, range.Length);
            return Regex.Replace(text, @"\[[^\]]*\]|<[^>]*>|\{[^}]*\}", string.Empty).Trim();
        }

        private static int SnapOutOfToken(string markup, int index, bool towardsStart)
        {
            if (string.IsNullOrEmpty(markup))
                return 0;

            index = index < 0 ? 0 : index > markup.Length ? markup.Length : index;

            for (int i = index - 1; i >= 0; i--)
            {
                char c = markup[i];
                if (c == '}' || c == ']' || c == '>' || c == '\n')
                    return index;

                if (c == '{' || c == '[' || c == '<')
                {
                    char close = c == '{' ? '}' : c == '[' ? ']' : '>';
                    int closeIndex = markup.IndexOf(close, i + 1);
                    if (closeIndex < 0)
                        return index;

                    return towardsStart ? i : closeIndex + 1;
                }
            }

            return index;
        }
    }
}
