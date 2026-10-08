using System;

namespace Velsi.Typecraft.Dialogue
{
    [Serializable]
    public sealed class DialogueRunnerState
    {
        public string AssetId;
        public string NodeId;

        public bool IsValid => !string.IsNullOrEmpty(AssetId) && !string.IsNullOrEmpty(NodeId);
    }
}
