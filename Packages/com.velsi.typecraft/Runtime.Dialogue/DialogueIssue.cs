namespace Velsi.Typecraft.Dialogue
{
    public readonly struct DialogueIssue
    {
        public DialogueIssue(DialogueIssueSeverity severity, DialogueIssueKind kind, string nodeId, string message, string subject = null, string detail = null)
        {
            Severity = severity;
            Kind = kind;
            NodeId = nodeId;
            Message = message;
            Subject = subject;
            Detail = detail;
        }

        public DialogueIssueSeverity Severity { get; }
        public DialogueIssueKind Kind { get; }
        public string NodeId { get; }
        public string Message { get; }
        public string Subject { get; }
        public string Detail { get; }

        public override string ToString() => $"{Severity} {Kind} [{NodeId}] {Message}";
    }

    public enum DialogueIssueSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum DialogueIssueKind
    {
        NoStartNode,
        MissingNodeType,
        EmptyNodeId,
        DuplicateNodeId,
        EmptyLink,
        BrokenLink,
        Unreachable,
        EmptyKey,
        NoOptions,
        EmptyVariable,
        UndeclaredVariable,
        VariableTypeMismatch,
        EmptyEventName,
        BrokenJump,
        MissingKey,
        MissingTranslation,
        StaleMarkup,
        MissingGlyphs,
        SpeakerWithoutName,
        UnknownEmotion
    }
}
