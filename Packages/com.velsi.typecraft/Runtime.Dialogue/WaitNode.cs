using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [Serializable]
    public sealed class WaitNode : DialogueNode
    {
        [SerializeField, Min(0f)] private float _seconds = 1f;
        [SerializeField] private string _next;

        public float Seconds
        {
            get => _seconds;
            set => _seconds = value;
        }

        public string Next
        {
            get => _next;
            set => _next = value;
        }

        public override void CollectLinks(List<DialogueLink> links) => links.Add(new DialogueLink("Next", _next));
    }
}
