using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Velsi.Typecraft.Styles;

namespace Velsi.Typecraft.AutoTags
{
    public sealed class AutoTagger
    {
        private const string ProtectedSpans = @"(\\.|\[([\w.-]+)\][\s\S]*?\[/\2\]|\{[^}]*\}|\[[^\]]*\]|<[^>]*>)";

        private readonly Regex _regex;
        private readonly Dictionary<string, string> _templates;

        private AutoTagger(Regex regex, Dictionary<string, string> templates)
        {
            _regex = regex;
            _templates = templates;
        }

        public static AutoTagger Build(IEnumerable<AutoTagRule> rules, StyleLibrary styles)
        {
            var templates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (styles != null)
            {
                foreach (TextStyle style in styles.Styles)
                {
                    if (style == null || string.IsNullOrEmpty(style.Id))
                        continue;

                    foreach (string word in style.AutoTagWords)
                        Add(templates, word, $"[{style.Id}]$0[/{style.Id}]");
                }
            }

            if (rules != null)
            {
                foreach (AutoTagRule rule in rules)
                {
                    if (rule == null || string.IsNullOrEmpty(rule.Template))
                        continue;

                    foreach (string word in rule.Words)
                        Add(templates, word, rule.Template);
                }
            }

            if (templates.Count == 0)
                return null;

            string words = string.Join("|", templates.Keys.OrderByDescending(w => w.Length).Select(Regex.Escape));
            var regex = new Regex(
                $@"{ProtectedSpans}|(?<![\p{{L}}\p{{N}}_])(?:{words})(?![\p{{L}}\p{{N}}_])",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

            return new AutoTagger(regex, templates);
        }

        public string Apply(string markup)
        {
            if (string.IsNullOrEmpty(markup))
                return markup;

            return _regex.Replace(markup, match =>
            {
                if (match.Groups[1].Success)
                    return match.Value;

                return _templates.TryGetValue(match.Value, out string template)
                    ? template.Replace("$0", match.Value)
                    : match.Value;
            });
        }

        public string Apply(string markup, List<int> sourceMap)
        {
            sourceMap.Clear();
            if (string.IsNullOrEmpty(markup))
                return markup;

            var output = new System.Text.StringBuilder(markup.Length + 32);
            int cursor = 0;

            foreach (Match match in _regex.Matches(markup))
            {
                CopyRange(markup, cursor, match.Index, output, sourceMap);
                cursor = match.Index + match.Length;

                if (match.Groups[1].Success || !_templates.TryGetValue(match.Value, out string template))
                {
                    CopyRange(markup, match.Index, cursor, output, sourceMap);
                    continue;
                }

                int placeholder = template.IndexOf("$0", StringComparison.Ordinal);
                if (placeholder < 0)
                {
                    AppendMapped(template, match.Index, output, sourceMap);
                    continue;
                }

                AppendMapped(template.Substring(0, placeholder), match.Index, output, sourceMap);
                CopyRange(markup, match.Index, cursor, output, sourceMap);
                AppendMapped(template.Substring(placeholder + 2).Replace("$0", match.Value), cursor, output, sourceMap);
            }

            CopyRange(markup, cursor, markup.Length, output, sourceMap);
            return output.ToString();
        }

        private static void CopyRange(string source, int from, int to, System.Text.StringBuilder output, List<int> sourceMap)
        {
            for (int i = from; i < to; i++)
            {
                output.Append(source[i]);
                sourceMap.Add(i);
            }
        }

        private static void AppendMapped(string text, int sourceIndex, System.Text.StringBuilder output, List<int> sourceMap)
        {
            output.Append(text);
            for (int i = 0; i < text.Length; i++)
                sourceMap.Add(sourceIndex);
        }

        private static void Add(Dictionary<string, string> templates, string word, string template)
        {
            if (!string.IsNullOrWhiteSpace(word))
                templates[word.Trim()] = template;
        }
    }
}
