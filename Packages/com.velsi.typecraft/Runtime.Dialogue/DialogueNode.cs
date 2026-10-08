using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [Serializable]
    public abstract class DialogueNode
    {
        [SerializeField] private string _id = NewId();
        [SerializeField] private Vector2 _position;

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public Vector2 Position
        {
            get => _position;
            set => _position = value;
        }

        public virtual void CollectLinks(List<DialogueLink> links)
        {
        }

        public List<DialogueLink> GetLinks()
        {
            var links = new List<DialogueLink>();
            CollectLinks(links);
            return links;
        }

        public static string NewId() => Guid.NewGuid().ToString("N");

        public override string ToString() => $"{GetType().Name} {_id}";
    }

    public readonly struct DialogueLink
    {
        public DialogueLink(string port, string target, bool required = true)
        {
            Port = port;
            Target = target;
            Required = required;
        }

        public string Port { get; }
        public string Target { get; }
        public bool Required { get; }
        public bool IsEmpty => string.IsNullOrEmpty(Target);

        public override string ToString() => $"{Port} -> {Target}";
    }
}
