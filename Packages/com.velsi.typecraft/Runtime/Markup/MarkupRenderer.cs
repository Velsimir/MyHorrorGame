using System.Collections.Generic;
using System.Text;
using Velsi.Typecraft.Styles;

namespace Velsi.Typecraft.Markup
{
    public static class MarkupRenderer
    {
        private const string Escapable = "{}[]<>\\";

        public static string Render(string markup, MarkupContext context, IReadOnlyDictionary<string, IMarkupTagHandler> handlers) =>
            Render(markup, context, handlers, null);

        public static string Render(string markup, MarkupContext context, IReadOnlyDictionary<string, IMarkupTagHandler> handlers, List<int> sourceMap)
        {
            sourceMap?.Clear();
            if (string.IsNullOrEmpty(markup))
                return markup ?? string.Empty;

            var output = new StringBuilder(markup.Length + 64);
            var openStyles = new List<TextStyle>();
            StyleLibrary styles = context.Settings != null ? context.Settings.Styles : null;
            int i = 0;

            while (i < markup.Length)
            {
                context.Position = i;
                char c = markup[i];

                if (c == '\\' && i + 1 < markup.Length && Escapable.IndexOf(markup[i + 1]) >= 0)
                {
                    AppendLiteral(markup[i + 1], output);
                    Track(sourceMap, output, i + 1);
                    i += 2;
                    continue;
                }

                if (c == '{' && TryReadSpan(markup, i, '}', out string inner, out int next))
                {
                    RenderBrace(inner, context, handlers, output);
                    Track(sourceMap, output, i);
                    i = next;
                    continue;
                }

                if (c == '[' && TryReadSpan(markup, i, ']', out inner, out next)
                    && TryRenderStyle(inner, styles, openStyles, context, output))
                {
                    Track(sourceMap, output, i);
                    i = next;
                    continue;
                }

                if (c == '<' && TryReadSpan(markup, i, '>', out _, out next))
                {
                    for (int k = i; k < next; k++)
                    {
                        output.Append(markup[k]);
                        Track(sourceMap, output, k);
                    }

                    i = next;
                    continue;
                }

                output.Append(c);
                Track(sourceMap, output, i);
                i++;
            }

            context.Position = markup.Length;
            for (int s = openStyles.Count - 1; s >= 0; s--)
            {
                context.Report(MarkupIssue.UnclosedStyle, openStyles[s].Id);
                openStyles[s].AppendClose(output, context.Settings.Fonts, context.Locale);
            }

            Track(sourceMap, output, markup.Length);
            return output.ToString();
        }

        private static void Track(List<int> sourceMap, StringBuilder output, int sourceIndex)
        {
            if (sourceMap == null)
                return;

            while (sourceMap.Count < output.Length)
                sourceMap.Add(sourceIndex);
        }

        private static void RenderBrace(string inner, MarkupContext context, IReadOnlyDictionary<string, IMarkupTagHandler> handlers, StringBuilder output)
        {
            if (inner.Length == 0 || char.IsDigit(inner[0]))
            {
                output.Append('{').Append(inner).Append('}');
                return;
            }

            int colon = inner.IndexOf(':');
            if (colon > 0)
            {
                string prefix = inner.Substring(0, colon);
                string argument = inner.Substring(colon + 1);

                if (!handlers.TryGetValue(prefix, out IMarkupTagHandler handler))
                {
                    context.Report(MarkupIssue.UnknownTag, inner);
                    MarkupContext.AppendMissing(inner, output);
                    return;
                }

                if (!handler.TryRender(argument, context, output))
                {
                    context.Report(MarkupIssue.UnresolvedTag, inner);
                    MarkupContext.AppendMissing(argument, output);
                }

                return;
            }

            if (!context.TryAppendIcon(inner, output))
            {
                context.Report(MarkupIssue.UnknownIcon, inner);
                MarkupContext.AppendMissing(inner, output);
            }
        }

        private static bool TryRenderStyle(string inner, StyleLibrary styles, List<TextStyle> openStyles, MarkupContext context, StringBuilder output)
        {
            if (styles == null || inner.Length == 0)
                return false;

            bool closing = inner[0] == '/';
            string id = closing ? inner.Substring(1) : inner;

            if (!IsIdentifier(id))
                return false;

            if (!styles.TryGet(id, out TextStyle style))
            {
                context.Report(MarkupIssue.UnknownStyle, id);
                return false;
            }

            if (!closing)
            {
                style.AppendOpen(output, context.Settings.Fonts, context.Locale);
                openStyles.Add(style);
                return true;
            }

            int index = openStyles.LastIndexOf(style);
            if (index < 0)
            {
                context.Report(MarkupIssue.StrayClosingStyle, id);
                return true;
            }

            for (int s = openStyles.Count - 1; s >= index; s--)
                openStyles[s].AppendClose(output, context.Settings.Fonts, context.Locale);

            for (int s = openStyles.Count - 1; s > index; s--)
                openStyles[s].AppendOpen(output, context.Settings.Fonts, context.Locale);

            openStyles.RemoveAt(index);
            return true;
        }

        private static bool TryReadSpan(string markup, int start, char close, out string inner, out int next)
        {
            for (int j = start + 1; j < markup.Length; j++)
            {
                char c = markup[j];
                if (c == close)
                {
                    inner = markup.Substring(start + 1, j - start - 1);
                    next = j + 1;
                    return true;
                }

                if (c == '\n' || (c == markup[start] && close != '>'))
                    break;
            }

            inner = null;
            next = start;
            return false;
        }

        private static bool IsIdentifier(string value)
        {
            if (value.Length == 0 || !char.IsLetter(value[0]))
                return false;

            foreach (char c in value)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.')
                    return false;
            }

            return true;
        }

        private static void AppendLiteral(char c, StringBuilder output)
        {
            if (c == '<')
                output.Append("<noparse><</noparse>");
            else
                output.Append(c);
        }
    }
}
