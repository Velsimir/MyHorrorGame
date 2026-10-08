using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Velsi.Typecraft.Editor.Localization
{
    public enum DiffKind
    {
        Same,
        Removed,
        Added
    }

    public readonly struct DiffPart
    {
        public DiffPart(DiffKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }

        public DiffKind Kind { get; }
        public string Text { get; }
    }

    public sealed class TransferResult
    {
        public string Markup { get; internal set; }
        public List<string> Approximate { get; } = new List<string>();
        public List<string> Dropped { get; } = new List<string>();
        public bool Broken { get; internal set; }

        public bool Exact => !Broken && Approximate.Count == 0 && Dropped.Count == 0;

        public string Describe()
        {
            var lines = new List<string>();
            if (Approximate.Count > 0)
                lines.Add("Поставлены по соседству — слово под ними изменилось: " + string.Join(" ", Approximate));
            if (Dropped.Count > 0)
                lines.Add("Сняты — слово под ними удалено: " + string.Join(" ", Dropped));
            if (Broken)
                lines.Add("Теги склеились с соседним текстом — перенос невозможен.");
            return string.Join("\n", lines);
        }
    }

    public static class Decoration
    {
        private const string Escapable = "{}[]<>\\";
        private static readonly Regex TagStart = new Regex(@"^</?(?:[!#+\-]?[A-Za-z]|#[0-9A-Fa-f])");

        private readonly struct Piece
        {
            public Piece(string text, bool decoration)
            {
                Text = text;
                IsDecoration = decoration;
            }

            public string Text { get; }
            public bool IsDecoration { get; }
        }

        private readonly struct Token
        {
            public Token(string text, int start)
            {
                Text = text;
                Start = start;
            }

            public string Text { get; }
            public int Start { get; }
            public int End => Start + Text.Length;
        }

        public static string Strip(string markup)
        {
            if (string.IsNullOrEmpty(markup))
                return markup ?? string.Empty;

            var output = new StringBuilder(markup.Length);
            foreach (Piece piece in Split(markup))
            {
                if (!piece.IsDecoration)
                    output.Append(piece.Text);
            }

            return output.ToString();
        }

        public static bool Has(string markup) => !string.IsNullOrEmpty(markup) && Split(markup).Any(piece => piece.IsDecoration);

        public static IReadOnlyList<string> Tags(string markup) =>
            string.IsNullOrEmpty(markup) ? Array.Empty<string>() : Split(markup).Where(piece => piece.IsDecoration).Select(piece => piece.Text).ToList();

        public static List<DiffPart> Diff(string before, string after)
        {
            List<Token> left = Words(before ?? string.Empty);
            List<Token> right = Words(after ?? string.Empty);
            int[] match = Lcs(left, right);
            var parts = new List<DiffPart>();
            int j = 0;

            for (int i = 0; i < left.Count; i++)
            {
                if (match[i] < 0)
                {
                    Append(parts, DiffKind.Removed, left[i].Text);
                    continue;
                }

                for (; j < match[i]; j++)
                    Append(parts, DiffKind.Added, right[j].Text);

                Append(parts, DiffKind.Same, left[i].Text);
                j = match[i] + 1;
            }

            for (; j < right.Count; j++)
                Append(parts, DiffKind.Added, right[j].Text);

            return parts;
        }

        public static TransferResult Transfer(string markup, string text)
        {
            markup ??= string.Empty;
            text ??= string.Empty;
            var result = new TransferResult();
            var tags = new List<(string Tag, int Position)>();
            int position = 0;

            foreach (Piece piece in Split(markup))
            {
                if (piece.IsDecoration)
                    tags.Add((piece.Text, position));
                else
                    position += piece.Text.Length;
            }

            string source = Strip(markup);
            List<Token> left = Words(source);
            List<Token> right = Words(text);
            int[] match = Lcs(left, right);

            var placed = new List<(string Tag, int Target, int Order, bool Exact)>();
            for (int t = 0; t < tags.Count; t++)
            {
                (string tag, int at) = tags[t];
                bool after = IsClosing(tag) ? at > 0 : at == source.Length && at > 0;
                int word = after ? WordEndingAt(left, at) : WordStartingAt(left, at);

                if (word >= 0 && match[word] >= 0)
                {
                    Token from = left[word];
                    Token to = right[match[word]];
                    int target = after ? to.End - Math.Min(to.Text.Length, from.End - at) : to.Start + Math.Min(to.Text.Length, at - from.Start);
                    placed.Add((tag, target, t, true));
                    continue;
                }

                if (left.Count == 0)
                {
                    placed.Add((tag, after ? text.Length : 0, t, true));
                    continue;
                }

                int anchor = word >= 0 ? word : after ? left.Count - 1 : 0;
                (int gapStart, int gapEnd) = Gap(left, right, match, anchor);
                placed.Add((tag, after ? gapEnd : gapStart, t, false));
            }

            placed = placed.OrderBy(p => p.Target).ThenBy(p => p.Order).ToList();
            var collapsed = new HashSet<int>();
            for (int i = 0; i + 1 < placed.Count; i++)
            {
                if (placed[i].Target != placed[i + 1].Target || IsClosing(placed[i].Tag) || !IsClosing(placed[i + 1].Tag) || Name(placed[i].Tag) != Name(placed[i + 1].Tag))
                    continue;

                collapsed.Add(i);
                collapsed.Add(i + 1);
                i++;
            }

            var output = new StringBuilder(text.Length + markup.Length);
            int cursor = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                (string tag, int target, int _, bool exact) = placed[i];
                if (collapsed.Contains(i))
                {
                    result.Dropped.Add(tag);
                    continue;
                }

                if (!exact)
                    result.Approximate.Add(tag);

                output.Append(text, cursor, target - cursor);
                output.Append(tag);
                cursor = target;
            }

            output.Append(text, cursor, text.Length - cursor);
            result.Markup = output.ToString();
            result.Broken = Strip(result.Markup) != text;
            return result;
        }

        public static string RichDiff(IEnumerable<DiffPart> parts, bool before)
        {
            var output = new StringBuilder();
            foreach (DiffPart part in parts)
            {
                if ((part.Kind == DiffKind.Added && before) || (part.Kind == DiffKind.Removed && !before))
                    continue;

                string text = "<noparse>" + part.Text.Replace("\r", string.Empty).Replace("\n", "⏎\n") + "</noparse>";
                if (part.Kind == DiffKind.Removed)
                    output.Append("<mark=#E0505050><color=#FF8A80><s>").Append(text).Append("</s></color></mark>");
                else if (part.Kind == DiffKind.Added)
                    output.Append("<mark=#40C05050><color=#8AF0A0>").Append(text).Append("</color></mark>");
                else
                    output.Append(text);
            }

            return output.ToString();
        }

        private static bool IsClosing(string tag) => tag.StartsWith("[/", StringComparison.Ordinal) || tag.StartsWith("</", StringComparison.Ordinal);

        private static string Name(string tag)
        {
            int start = IsClosing(tag) ? 2 : 1;
            int end = start;
            while (end < tag.Length - 1 && tag[end] != '=' && tag[end] != ' ')
                end++;
            return tag.Substring(start, end - start);
        }

        private static int WordStartingAt(List<Token> words, int position)
        {
            for (int i = 0; i < words.Count; i++)
            {
                if (words[i].Start <= position && position < words[i].End)
                    return i;
            }

            return -1;
        }

        private static int WordEndingAt(List<Token> words, int position)
        {
            for (int i = 0; i < words.Count; i++)
            {
                if (words[i].Start < position && position <= words[i].End)
                    return i;
            }

            return -1;
        }

        private static (int Start, int End) Gap(List<Token> left, List<Token> right, int[] match, int anchor)
        {
            int start = 0;
            for (int i = anchor - 1; i >= 0; i--)
            {
                if (match[i] >= 0)
                {
                    start = right[match[i]].End;
                    break;
                }
            }

            int end = right.Count > 0 ? right[right.Count - 1].End : 0;
            for (int i = anchor + 1; i < left.Count; i++)
            {
                if (match[i] >= 0)
                {
                    end = right[match[i]].Start;
                    break;
                }
            }

            return (start, Math.Max(start, end));
        }

        private static int[] Lcs(List<Token> left, List<Token> right)
        {
            int n = left.Count;
            int m = right.Count;
            var table = new int[n + 1, m + 1];
            for (int i = n - 1; i >= 0; i--)
            for (int j = m - 1; j >= 0; j--)
                table[i, j] = left[i].Text == right[j].Text ? table[i + 1, j + 1] + 1 : Math.Max(table[i + 1, j], table[i, j + 1]);

            var match = new int[n];
            for (int i = 0; i < n; i++)
                match[i] = -1;

            int a = 0;
            int b = 0;
            while (a < n && b < m)
            {
                if (left[a].Text == right[b].Text)
                {
                    match[a] = b;
                    a++;
                    b++;
                }
                else if (table[a + 1, b] >= table[a, b + 1])
                {
                    a++;
                }
                else
                {
                    b++;
                }
            }

            return match;
        }

        private static void Append(List<DiffPart> parts, DiffKind kind, string text)
        {
            if (parts.Count > 0 && parts[parts.Count - 1].Kind == kind)
                parts[parts.Count - 1] = new DiffPart(kind, parts[parts.Count - 1].Text + text);
            else
                parts.Add(new DiffPart(kind, text));
        }

        private static List<Token> Words(string text)
        {
            var words = new List<Token>();
            int i = 0;
            while (i < text.Length)
            {
                int start = i;
                char c = text[i];

                if (c == '\\' && i + 1 < text.Length && Escapable.IndexOf(text[i + 1]) >= 0)
                {
                    i += 2;
                }
                else if (c == '{' && TryReadSpan(text, i, '}', out int next))
                {
                    i = next;
                }
                else if (char.IsWhiteSpace(c))
                {
                    while (i < text.Length && char.IsWhiteSpace(text[i]))
                        i++;
                }
                else if (IsWordChar(c) && !IsCjk(c))
                {
                    while (i < text.Length && IsWordChar(text[i]) && !IsCjk(text[i]))
                        i++;
                }
                else
                {
                    i += char.IsHighSurrogate(c) && i + 1 < text.Length ? 2 : 1;
                }

                words.Add(new Token(text.Substring(start, i - start), start));
            }

            return words;
        }

        private static bool IsWordChar(char c) =>
            char.IsLetterOrDigit(c) || c == '_' || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark;

        private static bool IsCjk(char c) =>
            (c >= '぀' && c <= 'ヿ') || (c >= '㐀' && c <= '䶿') || (c >= '一' && c <= '鿿') || (c >= '豈' && c <= '﫿');

        private static List<Piece> Split(string markup)
        {
            var pieces = new List<Piece>();
            var content = new StringBuilder();
            int i = 0;

            while (i < markup.Length)
            {
                char c = markup[i];

                if (c == '\\' && i + 1 < markup.Length && Escapable.IndexOf(markup[i + 1]) >= 0)
                {
                    content.Append(markup, i, 2);
                    i += 2;
                    continue;
                }

                if (c == '{' && TryReadSpan(markup, i, '}', out int next))
                {
                    content.Append(markup, i, next - i);
                    i = next;
                    continue;
                }

                if ((c == '[' || c == '<') && TryReadSpan(markup, i, c == '[' ? ']' : '>', out next) && IsDecoration(markup.Substring(i, next - i)))
                {
                    if (content.Length > 0)
                    {
                        pieces.Add(new Piece(content.ToString(), false));
                        content.Clear();
                    }

                    pieces.Add(new Piece(markup.Substring(i, next - i), true));
                    i = next;
                    continue;
                }

                content.Append(c);
                i++;
            }

            if (content.Length > 0)
                pieces.Add(new Piece(content.ToString(), false));
            return pieces;
        }

        private static bool IsDecoration(string span)
        {
            if (span[0] == '<')
                return span.IndexOf('<', 1) < 0 && TagStart.IsMatch(span);

            string id = span.Substring(1, span.Length - 2);
            if (id.StartsWith("/", StringComparison.Ordinal))
                id = id.Substring(1);
            if (id.Length == 0 || !char.IsLetter(id[0]))
                return false;

            foreach (char c in id)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.')
                    return false;
            }

            return true;
        }

        private static bool TryReadSpan(string markup, int start, char close, out int next)
        {
            for (int j = start + 1; j < markup.Length; j++)
            {
                char c = markup[j];
                if (c == close)
                {
                    next = j + 1;
                    return true;
                }

                if (c == '\n' || (c == markup[start] && close != '>'))
                    break;
            }

            next = start;
            return false;
        }
    }
}
