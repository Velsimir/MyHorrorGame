using System;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Velsi.Typecraft.Dialogue.Editor.Inspector
{
    public interface IDialogueEditorContext
    {
        DialogueAsset Asset { get; }
        string Locale { get; }
        VisualElement Root { get; }
        void Edit(Object target, string undoName, Action change, bool rebuildInspector = false);
        void SelectNode(string nodeId);
        void RemoveNode(string nodeId);
    }
}
