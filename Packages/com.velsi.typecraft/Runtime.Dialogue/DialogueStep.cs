using System.Collections.Generic;

namespace Velsi.Typecraft.Dialogue
{
    public readonly struct DialogueStep
    {
        private DialogueStep(DialogueStepKind kind, DialogueAsset asset, string nodeId, DialogueLine line, IReadOnlyList<DialogueChoice> choices, float seconds)
        {
            Kind = kind;
            Asset = asset;
            NodeId = nodeId;
            Line = line;
            Choices = choices;
            Seconds = seconds;
        }

        public DialogueStepKind Kind { get; }
        public DialogueAsset Asset { get; }
        public string NodeId { get; }
        public DialogueLine Line { get; }
        public IReadOnlyList<DialogueChoice> Choices { get; }
        public float Seconds { get; }

        internal static DialogueStep ForLine(DialogueLine line) => new DialogueStep(DialogueStepKind.Line, line.Asset, line.NodeId, line, null, line.Seconds);

        internal static DialogueStep ForChoices(DialogueAsset asset, string nodeId, IReadOnlyList<DialogueChoice> choices) =>
            new DialogueStep(DialogueStepKind.Choice, asset, nodeId, null, choices, 0f);

        internal static DialogueStep ForWait(DialogueAsset asset, string nodeId, float seconds) =>
            new DialogueStep(DialogueStepKind.Wait, asset, nodeId, null, null, seconds);
    }

    public enum DialogueStepKind
    {
        None,
        Line,
        Choice,
        Wait
    }
}
