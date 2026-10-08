using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Velsi.Typecraft.Editor.Localization;

namespace Velsi.Typecraft.Dialogue.Editor.Model
{
    public static class DialogueTexts
    {
        private static readonly Regex Sprites = new Regex("<sprite[^<>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex Tags = new Regex("<[^<>]*>", RegexOptions.Compiled);
        private static readonly Regex Spaces = new Regex(@"\s+", RegexOptions.Compiled);

        public static ITableBackend Backend => TableBackends.Current;

        public static string Locale(string preferred)
        {
            ITableBackend backend = Backend;
            if (backend == null)
                return preferred ?? Typecraft.Locale;
            if (!string.IsNullOrEmpty(preferred) && backend.Locales.Contains(preferred))
                return preferred;
            return backend.Locales.Contains(Typecraft.Locale) ? Typecraft.Locale : backend.BaseLocale;
        }

        public static bool HasKey(string table, string key)
        {
            ITableBackend backend = Backend;
            return backend != null && !string.IsNullOrEmpty(table) && !string.IsNullOrEmpty(key) && backend.Tables.Contains(table) && backend.Keys(table).Contains(key);
        }

        public static string Markup(string table, string key, string locale)
        {
            ITableBackend backend = Backend;
            if (backend == null || string.IsNullOrEmpty(table) || string.IsNullOrEmpty(key) || !backend.Tables.Contains(table))
                return string.Empty;
            return MarkupLayers.Display(backend, table, key, locale) ?? string.Empty;
        }

        public static string Plain(string table, string key, string locale, int max = 80)
        {
            string markup = Markup(table, key, locale);
            if (string.IsNullOrEmpty(markup))
                return string.Empty;

            string rendered = Typecraft.RenderPreview(markup, locale, Typecraft.Device, null, null);
            return Shorten(Spaces.Replace(Tags.Replace(Sprites.Replace(rendered ?? string.Empty, "▣"), string.Empty), " ").Trim(), max);
        }

        public static string SpeakerName(SpeakerProfile speaker, string fallbackTable, string locale)
        {
            if (speaker == null)
                return string.Empty;

            string name = Plain(speaker.ResolveNameTable(fallbackTable), speaker.NameKey, locale, 40);
            return string.IsNullOrEmpty(name) ? speaker.Id : name;
        }

        public static string Summary(DialogueAsset asset, DialogueNode node, string locale, int max = 60)
        {
            switch (node)
            {
                case LineNode line:
                    string speaker = SpeakerName(line.Speaker, asset != null ? asset.ResolveTable(line.Table) : line.Table, locale);
                    string text = Plain(asset != null ? asset.ResolveTable(line.Table) : line.Table, line.Key, locale, max);
                    string who = string.IsNullOrEmpty(speaker) ? string.Empty : speaker + (string.IsNullOrEmpty(line.Emotion) ? string.Empty : " · " + line.Emotion) + ": ";
                    return who + (string.IsNullOrEmpty(text) ? line.Key ?? "—" : text);
                case ChoiceNode choice:
                    return $"вариантов: {choice.Options.Count}";
                case BranchNode branch:
                    return branch.Cases.Count == 1 ? "если " + Shorten(Condition(branch.Cases[0]?.Condition), max) : $"условий: {branch.Cases.Count}";
                case SetVariablesNode set:
                    return Shorten(string.Join(", ", set.Assignments.Where(entry => entry != null).Select(Assignment)), max);
                case EventNode raised:
                    return string.IsNullOrEmpty(raised.Argument) ? raised.EventName ?? string.Empty : $"{raised.EventName} ({raised.Argument})";
                case WaitNode wait:
                    return $"{wait.Seconds:0.##} с";
                case JumpNode jump:
                    DialogueAsset target = jump.Target != null ? jump.Target : asset;
                    string targetNode = string.IsNullOrEmpty(jump.TargetNode) ? "старт" : target != null && target.TryGetNode(jump.TargetNode, out DialogueNode found) ? DialogueNodeInfo.Title(found) + " #" + ShortId(found.Id) : "нет ноды";
                    return jump.Target != null && jump.Target != asset ? $"{jump.Target.name} → {targetNode}" : "→ " + targetNode;
            }

            return string.Empty;
        }

        public static string ShortId(string id) => string.IsNullOrEmpty(id) ? "?" : id.Substring(0, System.Math.Min(4, id.Length));

        public static string NodeLabel(DialogueAsset asset, DialogueNode node, string locale)
        {
            string summary = Summary(asset, node, locale, 40);
            return $"{DialogueNodeInfo.Glyph(node.GetType())} {DialogueNodeInfo.Title(node)} #{ShortId(node.Id)}{(string.IsNullOrEmpty(summary) ? string.Empty : " — " + summary.Replace('/', '∕'))}";
        }

        public static string Shorten(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || max <= 0 || text.Length <= max)
                return text ?? string.Empty;
            return text.Substring(0, max - 1).TrimEnd() + "…";
        }

        public static string Condition(Condition condition)
        {
            if (condition == null || condition.IsEmpty)
                return "всегда";

            string joint = condition.Mode == ConditionMode.All ? " и " : " или ";
            var parts = new List<string>();
            foreach (ConditionClause clause in condition.Clauses)
            {
                if (clause != null)
                    parts.Add(Clause(clause));
            }

            return string.Join(joint, parts);
        }

        public static string Clause(ConditionClause clause)
        {
            string name = string.IsNullOrEmpty(clause.Variable) ? "?" : clause.Variable;
            return clause.Comparison switch
            {
                ComparisonOperator.IsTrue => name,
                ComparisonOperator.IsFalse => "не " + name,
                _ => $"{name} {Operator(clause.Comparison)} {Value(clause.Value)}"
            };
        }

        public static string Assignment(VariableAssignment assignment)
        {
            string name = string.IsNullOrEmpty(assignment.Name) ? "?" : assignment.Name;
            return assignment.Operation switch
            {
                VariableOperation.Toggle => $"{name} = !{name}",
                VariableOperation.Add => $"{name} += {Value(assignment.Value)}",
                _ => $"{name} = {Value(assignment.Value)}"
            };
        }

        public static string Operator(ComparisonOperator comparison) => comparison switch
        {
            ComparisonOperator.IsTrue => "истина",
            ComparisonOperator.IsFalse => "ложь",
            ComparisonOperator.Equal => "=",
            ComparisonOperator.NotEqual => "≠",
            ComparisonOperator.Greater => ">",
            ComparisonOperator.GreaterOrEqual => "≥",
            ComparisonOperator.Less => "<",
            _ => "≤"
        };

        public static string Value(DialogueValue value) => value.Type == DialogueValueType.String ? "\"" + value.AsString + "\"" : value.AsString;

        public static string SnakeCase(string name)
        {
            var builder = new StringBuilder();
            bool separator = false;
            for (int i = 0; i < (name ?? string.Empty).Length; i++)
            {
                char c = name[i];
                if (char.IsLetterOrDigit(c) && c < 128)
                {
                    if (char.IsUpper(c) && i > 0 && builder.Length > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1])))
                        separator = true;
                    if (separator && builder.Length > 0)
                        builder.Append('_');
                    builder.Append(char.ToLowerInvariant(c));
                    separator = false;
                }
                else
                {
                    separator = true;
                }
            }

            return builder.Length > 0 ? builder.ToString() : "dialogue";
        }

        public static string NextKey(string table, string prefix)
        {
            ITableBackend backend = Backend;
            int max = 0;
            if (backend != null && backend.Tables.Contains(table))
            {
                foreach (string key in backend.Keys(table))
                {
                    if (key != null && key.StartsWith(prefix + ".", System.StringComparison.Ordinal) && int.TryParse(key.Substring(prefix.Length + 1), out int number))
                        max = System.Math.Max(max, number);
                }
            }

            string candidate;
            do
            {
                max++;
                candidate = $"{prefix}.{max:000}";
            } while (backend != null && backend.Tables.Contains(table) && backend.Keys(table).Contains(candidate));

            return candidate;
        }
    }
}
