using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using Velsi.Typecraft.Editor.Window.Effects;
using Velsi.Typecraft.Fonts;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Editor.Localization
{
    [Flags]
    public enum CellIssue
    {
        None = 0,
        Missing = 1,
        Overflow = 2,
        MissingGlyph = 4,
        MarkupMismatch = 8,
        StaleMarkup = 16
    }

    public sealed class RowReport
    {
        public readonly Dictionary<string, CellIssue> Cells = new Dictionary<string, CellIssue>();
        public readonly List<string> Messages = new List<string>();

        public CellIssue All => Cells.Values.Aggregate(CellIssue.None, (sum, issue) => sum | issue);
        public CellIssue this[string locale] => Cells.TryGetValue(locale, out CellIssue issue) ? issue : CellIssue.None;
    }

    public static class LocalizationChecks
    {
        private static readonly Regex Token = new Regex(@"\{[^}]*\}|\[/?[A-Za-z][\w.-]*\]|<(/?[!#+\-]?[A-Za-z][\w-]*)[^>]*>");
        private static readonly Regex AnyTag = new Regex(@"<[^>]*>");

        public static RowReport Check(ITableBackend backend, string table, string key)
        {
            var report = new RowReport();
            string baseLocale = backend.BaseLocale;
            string baseText = MarkupLayers.Display(backend, table, key, baseLocale);
            string baseSignature = Signature(baseText);
            List<TypecraftText> objects = SceneTexts.Localized(table, key).Where(text => text != null && text.Target != null).ToList();

            foreach (string locale in backend.Locales)
            {
                string value = MarkupLayers.Display(backend, table, key, locale);
                CellIssue issue = CellIssue.None;

                if (string.IsNullOrEmpty(value))
                {
                    report.Cells[locale] = CellIssue.Missing;
                    report.Messages.Add($"{locale}: нет перевода");
                    continue;
                }

                if (MarkupLayers.State(backend, table, key, locale) == LayerState.Stale)
                {
                    issue |= CellIssue.StaleMarkup;
                    report.Messages.Add($"{locale}: строка в таблице изменилась, разметка не применяется");
                }

                if (locale != baseLocale && !string.IsNullOrEmpty(baseText) && Signature(value) != baseSignature)
                {
                    issue |= CellIssue.MarkupMismatch;
                    report.Messages.Add($"{locale}: разметка расходится с {baseLocale} ({Describe(value)} вместо {Describe(baseText)})");
                }

                string rendered = Typecraft.RenderPreview(value, locale, Typecraft.Device, null, null);
                string missing = MissingGlyphs(locale, rendered);
                if (missing.Length > 0)
                {
                    issue |= CellIssue.MissingGlyph;
                    report.Messages.Add($"{locale}: в шрифте нет символов «{missing}»");
                }

                foreach (TypecraftText text in missing.Length > 0 ? Enumerable.Empty<TypecraftText>() : objects)
                {
                    if (!Overflows(text.Target, rendered))
                        continue;

                    issue |= CellIssue.Overflow;
                    report.Messages.Add($"{locale}: не помещается в «{text.name}»");
                    break;
                }

                report.Cells[locale] = issue;
            }

            return report;
        }

        public static string Signature(string markup)
        {
            if (string.IsNullOrEmpty(markup))
                return string.Empty;

            return string.Join("|", Token.Matches(markup).Cast<Match>().Select(match => match.Groups[1].Success ? "<" + match.Groups[1].Value.ToLowerInvariant() : match.Value).OrderBy(token => token, StringComparer.Ordinal));
        }

        private static string Describe(string markup)
        {
            string signature = Signature(markup).Replace("|", " ");
            return string.IsNullOrEmpty(signature) ? "без тегов" : signature;
        }

        public static string MissingCharacters(string locale, string markup) =>
            string.IsNullOrEmpty(markup) ? string.Empty : MissingGlyphs(locale, Typecraft.RenderPreview(markup, locale, Typecraft.Device, null, null) ?? string.Empty);

        private static string MissingGlyphs(string locale, string rendered)
        {
            TMP_FontAsset font = FontFor(locale);
            if (font == null)
                return string.Empty;

            string visible = AnyTag.Replace(rendered, string.Empty);
            var probe = new StringBuilder();
            foreach (char c in visible)
            {
                if (!char.IsWhiteSpace(c) && !char.IsControl(c) && probe.ToString().IndexOf(c) < 0)
                    probe.Append(c);
            }

            if (probe.Length == 0 || font.HasCharacters(probe.ToString(), out uint[] missing, true, true) || missing == null)
                return string.Empty;

            return string.Concat(missing.Distinct().Take(12).Select(code => char.ConvertFromUtf32((int)code)));
        }

        private static TMP_FontAsset FontFor(string locale)
        {
            TypecraftSettings settings = Typecraft.Settings;
            FontVariant variant = settings != null && settings.Fonts != null ? settings.Fonts.Resolve(settings.DefaultFontRole, locale) : null;
            return variant?.Font != null ? variant.Font : TMP_Settings.defaultFontAsset;
        }

        private static bool Overflows(TMP_Text target, string rendered)
        {
            if (target.enableAutoSizing)
                return false;

            Rect rect = target.rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                return false;

            string measured = AnyTag.Replace(rendered, match => EffectCatalog.IsEffectTag(TagName(match.Value)) ? string.Empty : match.Value);
            bool wraps = target.textWrappingMode != TextWrappingModes.NoWrap && target.textWrappingMode != TextWrappingModes.PreserveWhitespaceNoWrap;
            Vector2 preferred = target.GetPreferredValues(measured, wraps ? rect.width : Mathf.Infinity, Mathf.Infinity);

            return wraps ? preferred.y > rect.height + 0.5f : preferred.x > rect.width + 0.5f;
        }

        private static string TagName(string tag)
        {
            int start = tag.Length > 1 && tag[1] == '/' ? 2 : 1;
            int end = start;
            while (end < tag.Length - 1 && tag[end] != '=' && tag[end] != ' ')
                end++;
            return tag.Substring(start, end - start);
        }
    }
}
