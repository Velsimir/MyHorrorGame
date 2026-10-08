using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    public sealed class DialogueLine
    {
        public DialogueLine(DialogueAsset asset, string nodeId, SpeakerProfile speaker, string emotion, Sprite portrait, string table, string key, LineAdvance advance, float seconds)
        {
            Asset = asset;
            NodeId = nodeId;
            Speaker = speaker;
            Emotion = emotion;
            Portrait = portrait;
            Table = table;
            Key = key;
            Advance = advance;
            Seconds = seconds;
        }

        public DialogueAsset Asset { get; }
        public string NodeId { get; }
        public SpeakerProfile Speaker { get; }
        public string Emotion { get; }
        public Sprite Portrait { get; }
        public string Table { get; }
        public string Key { get; }
        public LineAdvance Advance { get; }
        public float Seconds { get; }

        public string ResolveTable() => Asset != null ? Asset.ResolveTable(Table) : Table;

        public override string ToString() => $"{(Speaker != null ? Speaker.Id : "-")}: {ResolveTable()}/{Key}";
    }
}
