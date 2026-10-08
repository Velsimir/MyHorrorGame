using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Velsi.Typecraft.Dialogue.Editor.Board
{
    public interface IDialogueBoardHost
    {
        string Locale { get; }
        void OnNodeSelected(string nodeId);
        void OnBoardEdited(IReadOnlyList<string> selectNodes = null);
    }

    public interface IDialogueBoardView : IDisposable
    {
        VisualElement Element { get; }
        void Bind(IDialogueBoardHost host);
        void Show(DialogueAsset asset);
        void Refresh();
        void RefreshNode(string nodeId);
        void SelectNode(string nodeId, bool frame);
        void SetIssues(IReadOnlyList<DialogueIssue> issues);
        void SetCurrent(string nodeId);
        Vector2 SuggestPosition();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DialogueBoardPriorityAttribute : Attribute
    {
        public DialogueBoardPriorityAttribute(int priority, string title)
        {
            Priority = priority;
            Title = title;
        }

        public int Priority { get; }
        public string Title { get; }
    }
}
