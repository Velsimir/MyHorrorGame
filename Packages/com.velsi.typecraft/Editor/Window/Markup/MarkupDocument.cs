using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Velsi.Typecraft.Editor.Window.Markup
{
    public enum ModuleCoverage
    {
        None,
        Partial,
        Full
    }

    public readonly struct MarkupModule : IEquatable<MarkupModule>
    {
        private const string StyleSlot = "[style]";

        private static readonly Regex StyleOpen = new Regex(@"^\[([A-Za-z][\w.-]*)\]$");
        private static readonly Regex TagOpen = new Regex(@"^<([A-Za-z+\-][^<>=\s/]*)([^<>]*)>$");

        private static readonly HashSet<string> InlineTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "br", "sprite", "page", "space", "pos", "zwsp", "nbsp", "shy", "cr", "zwj"
        };

        private MarkupModule(string slot, string open, string close, string label)
        {
            Slot = slot;
            Open = open;
            Close = close;
            Label = label;
        }

        public string Slot { get; }
        public string Open { get; }
        public string Close { get; }
        public string Label { get; }
        public bool IsStyle => Slot == StyleSlot;

        public static bool TryParse(string open, out MarkupModule module)
        {
            module = default;
            if (string.IsNullOrEmpty(open))
                return false;

            Match style = StyleOpen.Match(open);
            if (style.Success)
            {
                string id = style.Groups[1].Value;
                module = new MarkupModule(StyleSlot, open, $"[/{id}]", id);
                return true;
            }

            Match tag = TagOpen.Match(open);
            if (!tag.Success)
                return false;

            string name = tag.Groups[1].Value.ToLowerInvariant();
            if (InlineTags.Contains(name) || name == "noparse")
                return false;

            module = new MarkupModule(name, open, $"</{name}>", open.Substring(1, open.Length - 2));
            return true;
        }

        public static bool IsInlineTag(string name) =>
            InlineTags.Contains(name) || name.StartsWith("!") || name.StartsWith("#");

        public bool Equals(MarkupModule other) => Slot == other.Slot && Open == other.Open;
        public override bool Equals(object obj) => obj is MarkupModule other && Equals(other);
        public override int GetHashCode() => (Slot?.GetHashCode() ?? 0) * 31 + (Open?.GetHashCode() ?? 0);
    }

    public sealed class MarkupDocument
    {
        private static readonly Regex StyleTag = new Regex(@"\G\[(/?)([A-Za-z][\w.-]*)\]");
        private static readonly Regex AngleTag = new Regex(@"\G<(/?)([A-Za-z!#+\-][^<>]*)>");

        private readonly List<Unit> _units = new List<Unit>();

        private MarkupDocument()
        {
        }

        public static MarkupDocument Parse(string markup)
        {
            var document = new MarkupDocument();
            document.Read(markup ?? string.Empty);
            return document;
        }

        public ModuleCoverage Coverage(TextRange range, Func<MarkupModule, bool> match)
        {
            List<Unit> units = Considered(range);
            if (units.Count == 0)
                return ModuleCoverage.None;

            int covered = units.Count(unit => unit.Modules.Any(match));
            return covered == 0 ? ModuleCoverage.None : covered == units.Count ? ModuleCoverage.Full : ModuleCoverage.Partial;
        }

        public IReadOnlyList<(MarkupModule Module, ModuleCoverage Coverage)> ModulesIn(TextRange range)
        {
            List<Unit> units = Considered(range);
            var result = new List<(MarkupModule, ModuleCoverage)>();
            if (units.Count == 0)
                return result;

            var seen = new List<MarkupModule>();
            foreach (Unit unit in units)
            {
                foreach (MarkupModule module in unit.Modules)
                {
                    if (!seen.Contains(module))
                        seen.Add(module);
                }
            }

            foreach (MarkupModule module in seen)
            {
                int covered = units.Count(unit => unit.Modules.Contains(module));
                result.Add((module, covered == units.Count ? ModuleCoverage.Full : ModuleCoverage.Partial));
            }

            return result;
        }

        public string Toggle(TextRange range, MarkupModule module, out TextRange result)
        {
            bool remove = Coverage(range, m => m.Equals(module)) == ModuleCoverage.Full;
            return Rewrite(range, modules =>
            {
                modules.RemoveAll(m => m.Slot == module.Slot);
                if (!remove)
                    modules.Add(module);
            }, out result);
        }

        public string RemoveSlot(TextRange range, string slot, out TextRange result) =>
            Rewrite(range, modules => modules.RemoveAll(m => m.Slot == slot), out result);

        public string Clear(TextRange range, out TextRange result) =>
            Rewrite(range, modules => modules.Clear(), out result);

        private string Rewrite(TextRange range, Action<List<MarkupModule>> change, out TextRange result)
        {
            int first = -1;
            int last = -1;
            for (int i = 0; i < _units.Count; i++)
            {
                if (!Inside(_units[i], range))
                    continue;

                change(_units[i].Modules);
                if (first < 0)
                    first = i;
                last = i;
            }

            return Serialize(first, last, out result);
        }

        private string Serialize(int first, int last, out TextRange result)
        {
            var output = new StringBuilder();
            var open = new List<MarkupModule>();
            int start = 0;
            int end = 0;

            for (int i = 0; i < _units.Count; i++)
            {
                List<MarkupModule> target = _units[i].Modules;

                int keep = 0;
                while (keep < open.Count && target.Contains(open[keep]))
                    keep++;

                for (int j = open.Count - 1; j >= keep; j--)
                    output.Append(open[j].Close);
                open.RemoveRange(keep, open.Count - keep);

                int index = i;
                foreach (MarkupModule module in target.Where(m => !open.Contains(m)).OrderByDescending(m => RunLength(index, m)).ToList())
                {
                    output.Append(module.Open);
                    open.Add(module);
                }

                if (i == first)
                    start = output.Length;
                output.Append(_units[i].Text);
                if (i == last)
                    end = output.Length;
            }

            for (int j = open.Count - 1; j >= 0; j--)
                output.Append(open[j].Close);

            result = first < 0 ? default : new TextRange(start, end);
            return output.ToString();
        }

        private int RunLength(int from, MarkupModule module)
        {
            int length = 0;
            while (from + length < _units.Count && _units[from + length].Modules.Contains(module))
                length++;
            return length;
        }

        private List<Unit> Considered(TextRange range)
        {
            List<Unit> inside = _units.Where(unit => Inside(unit, range)).ToList();
            List<Unit> visible = inside.Where(unit => !string.IsNullOrWhiteSpace(unit.Text)).ToList();
            return visible.Count > 0 ? visible : inside;
        }

        private static bool Inside(Unit unit, TextRange range) =>
            !range.IsEmpty && unit.Start >= range.Start && unit.End <= range.End;

        private void Read(string markup)
        {
            var active = new List<MarkupModule>();
            int i = 0;

            while (i < markup.Length)
            {
                char c = markup[i];

                if (c == '\\' && i + 1 < markup.Length)
                {
                    Add(markup, i, 2, active);
                    i += 2;
                    continue;
                }

                if (c == '{')
                {
                    int close = markup.IndexOf('}', i + 1);
                    if (close > i)
                    {
                        Add(markup, i, close - i + 1, active);
                        i = close + 1;
                        continue;
                    }
                }

                if (c == '[')
                {
                    Match style = StyleTag.Match(markup, i);
                    if (style.Success)
                    {
                        if (style.Groups[1].Length == 0)
                        {
                            if (MarkupModule.TryParse(style.Value, out MarkupModule module))
                                active.Add(module);
                        }
                        else if (!CloseActive(active, $"[/{style.Groups[2].Value}]"))
                            Add(markup, i, style.Length, active);

                        i += style.Length;
                        continue;
                    }
                }

                if (c == '<')
                {
                    Match tag = AngleTag.Match(markup, i);
                    if (tag.Success)
                    {
                        string name = TagName(tag.Groups[2].Value);
                        if (tag.Groups[1].Length > 0)
                        {
                            if (!CloseActive(active, $"</{name.ToLowerInvariant()}>"))
                                Add(markup, i, tag.Length, active);
                            i += tag.Length;
                            continue;
                        }

                        if (string.Equals(name, "noparse", StringComparison.OrdinalIgnoreCase))
                        {
                            int close = markup.IndexOf("</noparse>", i, StringComparison.OrdinalIgnoreCase);
                            int length = close < 0 ? markup.Length - i : close + "</noparse>".Length - i;
                            Add(markup, i, length, active);
                            i += length;
                            continue;
                        }

                        if (!MarkupModule.IsInlineTag(name) && MarkupModule.TryParse(tag.Value, out MarkupModule wrapper))
                            active.Add(wrapper);
                        else
                            Add(markup, i, tag.Length, active);

                        i += tag.Length;
                        continue;
                    }
                }

                int size = char.IsHighSurrogate(c) && i + 1 < markup.Length ? 2 : 1;
                Add(markup, i, size, active);
                i += size;
            }
        }

        private static bool CloseActive(List<MarkupModule> active, string close)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (string.Equals(active[i].Close, close, StringComparison.OrdinalIgnoreCase))
                {
                    active.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private void Add(string markup, int start, int length, List<MarkupModule> active)
        {
            var effective = new List<MarkupModule>();
            foreach (MarkupModule module in active)
            {
                int existing = effective.FindIndex(m => m.Slot == module.Slot);
                if (existing >= 0)
                    effective.RemoveAt(existing);
                effective.Add(module);
            }

            _units.Add(new Unit(markup.Substring(start, length), start, start + length, effective));
        }

        private static string TagName(string inner)
        {
            int end = 0;
            while (end < inner.Length && inner[end] != '=' && inner[end] != ' ')
                end++;
            return inner.Substring(0, end);
        }

        private sealed class Unit
        {
            public Unit(string text, int start, int end, List<MarkupModule> modules)
            {
                Text = text;
                Start = start;
                End = end;
                Modules = modules;
            }

            public string Text { get; }
            public int Start { get; }
            public int End { get; }
            public List<MarkupModule> Modules { get; }
        }
    }
}
