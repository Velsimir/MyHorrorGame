using System;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [Serializable]
    public sealed class JumpNode : DialogueNode
    {
        [SerializeField] private DialogueAsset _target;
        [SerializeField] private string _targetNode;

        public DialogueAsset Target
        {
            get => _target;
            set => _target = value;
        }

        public string TargetNode
        {
            get => _targetNode;
            set => _targetNode = value;
        }
    }
}
