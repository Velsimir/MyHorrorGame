using System.Collections.Generic;
using System.Text;
using Velsi.Typecraft.Markup;

namespace Velsi.Typecraft.Editor.Window.Markup
{
    public static class MarkupHighlighter
    {
        public const string InputColor = "#4EC9B0";
        public const string IconColor = "#7FD8C4";
        public const string StyleColor = "#DCB862";
        public const string TmpColor = "#9CDCFE";
        public const string CommandColor = "#C586C0";
        public const string ErrorColor = "#FF6B6B";
        public const string EscapeColor = "#808080";

        public static string ToRichText(string markup, IReadOnlyList<MarkupDiagnostic> diagnostics)
        {
            if (string.IsNullOrEmpty(markup))
                return string.Empty;

            var errors = new HashSet<int>();
            if (diagnostics != null)
            {
                foreach (MarkupDiagnostic diagnostic in diagnostics)
                {
                    if (diagnostic.Issue != MarkupIssue.UnclosedStyle)
                        errors.Add(diagnostic.Index);
                }
            }

            var output = new StringBuilder(markup.Length * 2);
            var plain = new StringBuilder();
            int i = 0;

            while (i < markup.Length)
            {
                char c = markup[i];

                if (c == '\\' && i + 1 < markup.Length)
                {
                    FlushPlain(plain, output);
                    AppendColored(output, markup.Substring(i, 2), EscapeColor);
                    i += 2;
                    continue;
                }

                char close = c == '{' ? '}' : c == '[' ? ']' : c == '<' ? '>' : '\0';
                int end = close == '\0' ? -1 : FindClose(markup, i, close);

                if (end > i)
                {
                    FlushPlain(plain, output);
                    string token = markup.Substring(i, end - i + 1);
                    AppendColored(output, token, errors.Contains(i) ? ErrorColor : ColorOf(token));
                    i = end + 1;
                    continue;
                }

                plain.Append(c);
                i++;
            }

            FlushPlain(plain, output);
            return output.ToString();
        }

        private static string ColorOf(string token)
        {
            switch (token[0])
            {
                case '{':
                    return token.StartsWith("{key:") ? InputColor : IconColor;
                case '[':
                    return StyleColor;
                default:
                    return token.Length > 1 && (token[1] == '!' || (token.Length > 2 && token[1] == '/' && token[2] == '!')) ? CommandColor : TmpColor;
            }
        }

        private static int FindClose(string markup, int start, char close)
        {
            for (int j = start + 1; j < markup.Length; j++)
            {
                if (markup[j] == close)
                    return j;
                if (markup[j] == '\n' || markup[j] == markup[start])
                    return -1;
            }

            return -1;
        }

        private static void AppendColored(StringBuilder output, string text, string color) =>
            output.Append("<color=").Append(color).Append("><noparse>").Append(Sanitize(text)).Append("</noparse></color>");

        private static void FlushPlain(StringBuilder plain, StringBuilder output)
        {
            if (plain.Length == 0)
                return;

            output.Append("<noparse>").Append(Sanitize(plain.ToString())).Append("</noparse>");
            plain.Clear();
        }

        private static string Sanitize(string text) => text.Replace("</noparse>", "<​/noparse>");
    }
}
