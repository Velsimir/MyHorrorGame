using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [Serializable]
    public sealed class EventNode : DialogueNode
    {
        [SerializeField] private string _eventName;
        [SerializeField] private string _argument;
        [SerializeField] private string _next;

        public string EventName
        {
            get => _eventName;
            set => _eventName = value;
        }

        public string Argument
        {
            get => _argument;
            set => _argument = value;
        }

        public string Next
        {
            get => _next;
            set => _next = value;
        }

        public override void CollectLinks(List<DialogueLink> links) => links.Add(new DialogueLink("Next", _next));
    }
}
