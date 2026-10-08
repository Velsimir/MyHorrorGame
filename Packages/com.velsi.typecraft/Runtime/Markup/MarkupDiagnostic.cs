namespace Velsi.Typecraft.Markup
{
    public readonly struct MarkupDiagnostic
    {
        public MarkupDiagnostic(MarkupIssue issue, string value, int index)
        {
            Issue = issue;
            Value = value;
            Index = index;
        }

        public MarkupIssue Issue { get; }
        public string Value { get; }
        public int Index { get; }

        public override string ToString() => $"{Issue} '{Value}' at {Index}";
    }

    public enum MarkupIssue
    {
        UnknownIcon,
        PlaceholderIcon,
        UnknownStyle,
        UnknownTag,
        UnresolvedTag,
        StrayClosingStyle,
        UnclosedStyle
    }
}
