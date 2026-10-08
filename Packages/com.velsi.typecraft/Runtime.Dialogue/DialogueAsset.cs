using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [CreateAssetMenu(fileName = "Dialogue", menuName = "Typecraft/Dialogue")]
    public sealed class DialogueAsset : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _defaultTable;
        [SerializeField] private string _startNode;
        [SerializeReference] private List<DialogueNode> _nodes = new List<DialogueNode>();

        private Dictionary<string, DialogueNode> _lookup;

        public string Id
        {
            get
            {
                EnsureId();
                return _id;
            }
        }

        public string DefaultTable
        {
            get => _defaultTable;
            set => _defaultTable = value;
        }

        public string StartNodeId
        {
            get
            {
                if (!string.IsNullOrEmpty(_startNode))
                    return _startNode;

                foreach (DialogueNode node in Nodes)
                {
                    if (node is StartNode)
                        return node.Id;
                }

                return null;
            }
            set => _startNode = value;
        }

        public IReadOnlyList<DialogueNode> Nodes => _nodes ??= new List<DialogueNode>();

        public string ResolveTable(string tableOverride) => string.IsNullOrEmpty(tableOverride) ? _defaultTable : tableOverride;

        public bool TryGetNode(string id, out DialogueNode node)
        {
            if (string.IsNullOrEmpty(id))
            {
                node = null;
                return false;
            }

            _lookup ??= BuildLookup();
            if (_lookup.TryGetValue(id, out node) && node != null && node.Id == id)
                return true;

            _lookup = BuildLookup();
            return _lookup.TryGetValue(id, out node);
        }

        public DialogueNode FindNode(string id) => TryGetNode(id, out DialogueNode node) ? node : null;

        public T FindNode<T>(string id) where T : DialogueNode => FindNode(id) as T;

        public T AddNode<T>(T node) where T : DialogueNode
        {
            if (node == null)
                return null;

            if (string.IsNullOrEmpty(node.Id))
                node.Id = DialogueNode.NewId();

            _nodes ??= new List<DialogueNode>();
            _nodes.Add(node);
            _lookup = null;

            if (node is StartNode && string.IsNullOrEmpty(_startNode))
                _startNode = node.Id;

            return node;
        }

        public bool RemoveNode(DialogueNode node)
        {
            if (node == null || _nodes == null || !_nodes.Remove(node))
                return false;

            _lookup = null;
            if (_startNode == node.Id)
                _startNode = null;
            return true;
        }

        public void InvalidateLookup() => _lookup = null;

        public void RegenerateId()
        {
            _id = Guid.NewGuid().ToString("N");
        }

        private void OnEnable() => EnsureId();

        private void OnValidate()
        {
            EnsureId();
            _lookup = null;
        }

        private void EnsureId()
        {
            if (string.IsNullOrEmpty(_id))
                RegenerateId();
        }

        private Dictionary<string, DialogueNode> BuildLookup()
        {
            var lookup = new Dictionary<string, DialogueNode>(StringComparer.Ordinal);
            if (_nodes == null)
                return lookup;

            foreach (DialogueNode node in _nodes)
            {
                if (node != null && !string.IsNullOrEmpty(node.Id) && !lookup.ContainsKey(node.Id))
                    lookup[node.Id] = node;
            }

            return lookup;
        }
    }
}
