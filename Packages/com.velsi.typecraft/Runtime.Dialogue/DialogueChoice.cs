namespace Velsi.Typecraft.Dialogue
{
    public sealed class DialogueChoice
    {
        public DialogueChoice(DialogueAsset asset, string nodeId, int index, string table, string key, bool available)
        {
            Asset = asset;
            NodeId = nodeId;
            Index = index;
            Table = table;
            Key = key;
            Available = available;
        }

        public DialogueAsset Asset { get; }
        public string NodeId { get; }
        public int Index { get; }
        public string Table { get; }
        public string Key { get; }
        public bool Available { get; }

        public string ResolveTable() => Asset != null ? Asset.ResolveTable(Table) : Table;

        public override string ToString() => $"[{Index}{(Available ? string.Empty : " disabled")}] {ResolveTable()}/{Key}";
    }
}
