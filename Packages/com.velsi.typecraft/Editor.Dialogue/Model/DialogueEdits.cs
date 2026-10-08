using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Velsi.Typecraft.Dialogue.Editor.Model
{
    public static class DialogueEdits
    {
        private const string ClipboardHeader = "typecraft.dialogue.nodes";

        public static event Action<Object> Edited;

        public static void Change(Object target, string name, Action change)
        {
            if (target == null)
                return;

            Undo.RecordObject(target, "Typecraft: " + name);
            change();
            Commit(target);
        }

        public static void Commit(Object target)
        {
            if (target is DialogueAsset asset)
                asset.InvalidateLookup();
            if (target is DialogueVariableCatalog catalog)
                catalog.InvalidateLookup();
            EditorUtility.SetDirty(target);
            Edited?.Invoke(target);
        }

        public static DialogueNode Create(Type type)
        {
            var node = (DialogueNode)Activator.CreateInstance(type);
            switch (node)
            {
                case ChoiceNode choice:
                    choice.Options.Add(new ChoiceOption());
                    break;
                case BranchNode branch:
                    branch.Cases.Add(new BranchCase());
                    break;
            }

            return node;
        }

        public static DialogueNode AddNode(DialogueAsset asset, Type type, Vector2 position, string linkFrom = null, string linkPort = null)
        {
            DialogueNode node = Create(type);
            node.Position = position;
            Change(asset, "новая нода", () =>
            {
                asset.AddNode(node);
                if (!string.IsNullOrEmpty(linkFrom) && asset.TryGetNode(linkFrom, out DialogueNode source))
                    SetLink(source, linkPort, node.Id);
            });
            return node;
        }

        public static void RemoveNodes(DialogueAsset asset, ICollection<string> ids)
        {
            if (asset == null || ids == null || ids.Count == 0)
                return;

            Change(asset, "удаление нод", () => RemoveWithoutUndo(asset, ids));
        }

        public static void RemoveWithoutUndo(DialogueAsset asset, ICollection<string> ids)
        {
            foreach (DialogueNode node in asset.Nodes.Where(node => node != null && ids.Contains(node.Id)).ToList())
                asset.RemoveNode(node);

            foreach (DialogueNode node in asset.Nodes)
            {
                if (node == null)
                    continue;

                foreach (DialogueLink link in node.GetLinks())
                {
                    if (!link.IsEmpty && ids.Contains(link.Target))
                        SetLink(node, link.Port, null);
                }

                if (node is JumpNode jump && (jump.Target == null || jump.Target == asset) && ids.Contains(jump.TargetNode))
                    jump.TargetNode = null;
            }

            asset.InvalidateLookup();
        }

        public static void Link(DialogueAsset asset, string nodeId, string port, string target)
        {
            if (asset == null || !asset.TryGetNode(nodeId, out DialogueNode node) || GetLink(node, port) == target)
                return;

            Change(asset, "связь", () => SetLink(node, port, target));
        }

        public static string GetLink(DialogueNode node, string port)
        {
            if (node == null)
                return null;

            foreach (DialogueLink link in node.GetLinks())
            {
                if (link.Port == port)
                    return string.IsNullOrEmpty(link.Target) ? null : link.Target;
            }

            return null;
        }

        public static bool SetLink(DialogueNode node, string port, string target)
        {
            target = string.IsNullOrEmpty(target) ? null : target;
            switch (node)
            {
                case ChoiceNode choice when DialogueNodeInfo.TryIndex(port, DialogueNodeInfo.OptionPort, out int option):
                    if (option >= choice.Options.Count || choice.Options[option] == null)
                        return false;
                    choice.Options[option].Next = target;
                    return true;
                case BranchNode branch when port == DialogueNodeInfo.ElsePort:
                    branch.Else = target;
                    return true;
                case BranchNode branch when DialogueNodeInfo.TryIndex(port, DialogueNodeInfo.CasePort, out int index):
                    if (index >= branch.Cases.Count || branch.Cases[index] == null)
                        return false;
                    branch.Cases[index].Next = target;
                    return true;
            }

            if (node == null || port != DialogueNodeInfo.NextPort)
                return false;

            PropertyInfo next = node.GetType().GetProperty("Next", BindingFlags.Public | BindingFlags.Instance);
            if (next == null || next.PropertyType != typeof(string) || !next.CanWrite)
                return false;

            next.SetValue(node, target);
            return true;
        }

        public static void Move(DialogueAsset asset, IReadOnlyCollection<(string Id, Vector2 Position)> moves)
        {
            if (asset == null || moves == null || moves.Count == 0)
                return;

            Change(asset, "перемещение нод", () =>
            {
                foreach ((string id, Vector2 position) in moves)
                {
                    if (asset.TryGetNode(id, out DialogueNode node))
                        node.Position = position;
                }
            });
        }

        public static void SetStart(DialogueAsset asset, string nodeId) => Change(asset, "стартовая нода", () => asset.StartNodeId = nodeId);

        public static string Copy(DialogueAsset asset, IEnumerable<string> ids)
        {
            var builder = new StringBuilder(ClipboardHeader);
            foreach (string id in ids)
            {
                if (asset == null || !asset.TryGetNode(id, out DialogueNode node))
                    continue;

                builder.Append('\n').Append(node.GetType().AssemblyQualifiedName).Append('\t').Append(EditorJsonUtility.ToJson(node));
            }

            return builder.ToString();
        }

        public static bool CanPaste(string data) => !string.IsNullOrEmpty(data) && data.StartsWith(ClipboardHeader + "\n", StringComparison.Ordinal);

        public static List<string> Paste(DialogueAsset asset, string data, Vector2 offset)
        {
            var created = new List<DialogueNode>();
            if (asset == null || !CanPaste(data))
                return new List<string>();

            var ids = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string line in data.Split('\n').Skip(1))
            {
                int tab = line.IndexOf('\t');
                Type type = tab > 0 ? Type.GetType(line.Substring(0, tab)) : null;
                if (type == null || !typeof(DialogueNode).IsAssignableFrom(type) || type.IsAbstract)
                    continue;

                var node = (DialogueNode)Activator.CreateInstance(type);
                EditorJsonUtility.FromJsonOverwrite(line.Substring(tab + 1), node);
                string newId = DialogueNode.NewId();
                if (!string.IsNullOrEmpty(node.Id))
                    ids[node.Id] = newId;
                node.Id = newId;
                node.Position += offset;
                created.Add(node);
            }

            if (created.Count == 0)
                return new List<string>();

            Change(asset, "вставка нод", () =>
            {
                foreach (DialogueNode node in created)
                {
                    foreach (DialogueLink link in node.GetLinks())
                        SetLink(node, link.Port, !link.IsEmpty && ids.TryGetValue(link.Target, out string mapped) ? mapped : null);

                    if (node is JumpNode jump && jump.Target == null && !string.IsNullOrEmpty(jump.TargetNode))
                        jump.TargetNode = ids.TryGetValue(jump.TargetNode, out string mapped) ? mapped : jump.TargetNode;

                    asset.AddNode(node);
                }
            });

            return created.Select(node => node.Id).ToList();
        }

        public static IEnumerable<DialogueNode> FlowOrder(DialogueAsset asset, out Dictionary<string, int> depth)
        {
            depth = new Dictionary<string, int>(StringComparer.Ordinal);
            var order = new List<DialogueNode>();
            if (asset == null)
                return order;

            var pending = new Stack<(string Id, int Depth)>();
            string start = asset.StartNodeId;
            if (!string.IsNullOrEmpty(start))
                pending.Push((start, 0));

            while (pending.Count > 0)
            {
                (string id, int level) = pending.Pop();
                if (depth.ContainsKey(id) || !asset.TryGetNode(id, out DialogueNode node))
                    continue;

                depth[id] = level;
                order.Add(node);
                List<DialogueLink> links = node.GetLinks();
                bool branching = links.Count > 1;
                for (int i = links.Count - 1; i >= 0; i--)
                {
                    if (!links[i].IsEmpty)
                        pending.Push((links[i].Target, branching ? level + 1 : level));
                }
            }

            return order;
        }
    }
}
