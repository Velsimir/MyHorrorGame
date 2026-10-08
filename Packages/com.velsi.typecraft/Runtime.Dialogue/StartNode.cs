using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [Serializable]
    public sealed class StartNode : DialogueNode
    {
        [SerializeField] private string _next;

        public string Next
        {
            get => _next;
            set => _next = value;
        }

        public override void CollectLinks(List<DialogueLink> links) => links.Add(new DialogueLink("Next", _next));
    }
}
