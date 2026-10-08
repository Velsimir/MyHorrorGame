using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Velsi.Typecraft.Dialogue.Editor.Model;

namespace Velsi.Typecraft.Dialogue.Editor.Graph
{
    public sealed class DialogueNodeSearch : ScriptableObject, ISearchWindowProvider
    {
        private Action<Type, Vector2> _picked;

        public static DialogueNodeSearch Create(Action<Type, Vector2> picked)
        {
            var search = CreateInstance<DialogueNodeSearch>();
            search.hideFlags = HideFlags.HideAndDontSave;
            search._picked = picked;
            return search;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry> { new SearchTreeGroupEntry(new GUIContent("Новая нода"), 0) };
            foreach (Type type in DialogueNodeInfo.Types)
                tree.Add(new SearchTreeEntry(new GUIContent($"{DialogueNodeInfo.Glyph(type)}  {DialogueNodeInfo.Title(type)}")) { level = 1, userData = type });
            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (!(entry.userData is Type type))
                return false;

            _picked?.Invoke(type, context.screenMousePosition);
            return true;
        }
    }
}
