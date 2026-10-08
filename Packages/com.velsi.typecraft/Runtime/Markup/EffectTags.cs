using System;
using System.Text;
using UnityEngine;

namespace Velsi.Typecraft.Markup
{
    public static class EffectTags
    {
        private const string NoParse = "noparse";
        private const string NoParseClose = "</noparse>";

        public static bool IsWriterTag(string name) =>
            !string.IsNullOrEmpty(name) && (name[0] == '!' || (name[0] == '#' && !ColorUtility.TryParseHtmlString(name, out _)));

        public static bool IsShowHideTag(string name) =>
            !string.IsNullOrEmpty(name) && (name[0] == '+' || name[0] == '-');

        public static void Scan(string rendered, Func<string, bool> isAnimation, out bool animations, out bool commands)
        {
            animations = false;
            commands = false;
            if (string.IsNullOrEmpty(rendered))
                return;

            int index = 0;
            while (TryNextTag(rendered, ref index, out _, out string name))
            {
                if (IsShowHideTag(name))
                {
                    animations = true;
                    commands = true;
                }
                else if (IsWriterTag(name))
                {
                    commands = true;
                }
                else if (isAnimation != null && isAnimation(name))
                {
                    animations = true;
                }
            }
        }

        public static string StripWriterTags(string rendered)
        {
            if (string.IsNullOrEmpty(rendered) || rendered.IndexOf('<') < 0)
                return rendered;

            StringBuilder output = null;
            int copied = 0;
            int index = 0;

            while (TryNextTag(rendered, ref index, out int open, out string name))
            {
                if (!IsWriterTag(name))
                    continue;

                output ??= new StringBuilder(rendered.Length);
                output.Append(rendered, copied, open - copied);
                copied = index;
            }

            if (output == null)
                return rendered;

            output.Append(rendered, copied, rendered.Length - copied);
            return output.ToString();
        }

        private static bool TryNextTag(string text, ref int index, out int open, out string name)
        {
            while (index < text.Length)
            {
                open = text.IndexOf('<', index);
                int close = open >= 0 ? text.IndexOf('>', open + 1) : -1;
                if (close < 0)
                    break;

                bool closing = text[open + 1] == '/';
                int start = closing ? open + 2 : open + 1;
                int end = start;
                while (end < close && text[end] != '=' && text[end] != ' ' && text[end] != '<')
                    end++;

                if (end == start || text.IndexOf('<', end, close - end) >= 0)
                {
                    index = open + 1;
                    continue;
                }

                name = text.Substring(start, end - start);
                index = close + 1;

                if (!closing && string.Equals(name, NoParse, StringComparison.OrdinalIgnoreCase))
                {
                    int skip = text.IndexOf(NoParseClose, index, StringComparison.OrdinalIgnoreCase);
                    index = skip < 0 ? text.Length : skip + NoParseClose.Length;
                    continue;
                }

                return true;
            }

            open = -1;
            name = null;
            index = text.Length;
            return false;
        }
    }
}
