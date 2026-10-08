using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Editor.Fonts
{
    public static class FontCharset
    {
        private const string Extended = "…—–«»“”‘’„·•№°×±€";
        private const string CjkPunctuation = "，。、！？：；（）《》〈〉「」『』【】～·“”‘’…—";
        private static readonly Regex AnyTag = new Regex(@"<[^>]*>");
        private static readonly HashSet<string> CjkLanguages = new HashSet<string> { "zh", "ja", "ko" };

        public static string Collect(string locale, string extra) => Collect(locale, extra, true);

        public static string Used(string locale) => Collect(locale, null, false);

        private static string Collect(string locale, string extra, bool defaults)
        {
            var codes = new SortedSet<int>();
            if (defaults)
            {
                for (int c = 0x20; c <= 0x7E; c++)
                    codes.Add(c);

                AddText(codes, Extended);
                if (CjkLanguages.Contains(LocaleCodes.Language(locale)))
                    AddText(codes, CjkPunctuation);
            }

            AddText(codes, extra);

            ITableBackend backend = TableBackends.Current;
            if (backend != null && !Pseudolocalizer.IsPseudo(locale))
            {
                foreach (string table in backend.Tables)
                foreach (string key in backend.Keys(table))
                {
                    string value = MarkupLayers.Display(backend, table, key, locale);
                    if (string.IsNullOrEmpty(value))
                        continue;

                    string rendered = Typecraft.RenderPreview(value, locale, Typecraft.Device, null, null);
                    AddText(codes, AnyTag.Replace(rendered, string.Empty));
                }
            }

            var result = new StringBuilder(codes.Count);
            foreach (int code in codes)
                result.Append(char.ConvertFromUtf32(code));
            return result.ToString();
        }

        public static string Missing(TMP_FontAsset font, string characters)
        {
            if (font == null || string.IsNullOrEmpty(characters))
                return string.Empty;

            if (font.HasCharacters(characters, out uint[] missing, true, false) || missing == null)
                return string.Empty;

            return string.Concat(missing.Distinct().Select(code => char.ConvertFromUtf32((int)code)));
        }

        private static void AddText(ISet<int> codes, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            for (int i = 0; i < text.Length; i++)
            {
                int code = char.IsHighSurrogate(text[i]) && i + 1 < text.Length ? char.ConvertToUtf32(text[i], text[++i]) : text[i];
                if (code >= 0x20 && !char.IsControl((char)System.Math.Min(code, 0xFFFF)) && code != 0x200B && code != 0xFEFF)
                    codes.Add(code);
            }
        }
    }
}
