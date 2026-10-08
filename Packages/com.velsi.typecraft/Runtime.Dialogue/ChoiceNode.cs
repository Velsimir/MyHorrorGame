using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [Serializable]
    public sealed class ChoiceNode : DialogueNode
    {
        [SerializeField] private List<ChoiceOption> _options = new List<ChoiceOption>();

        public List<ChoiceOption> Options => _options ??= new List<ChoiceOption>();

        public override void CollectLinks(List<DialogueLink> links)
        {
            if (_options == null)
                return;

            for (int i = 0; i < _options.Count; i++)
            {
                if (_options[i] != null)
                    links.Add(new DialogueLink("Option " + i, _options[i].Next));
            }
        }
    }

    [Serializable]
    public sealed class ChoiceOption
    {
        [SerializeField] private string _table;
        [SerializeField] private string _key;
        [SerializeField] private Condition _condition = new Condition();
        [SerializeField] private ChoiceUnavailable _whenUnavailable = ChoiceUnavailable.Hide;
        [SerializeField] private string _next;

        public string Table
        {
            get => _table;
            set => _table = value;
        }

        public string Key
        {
            get => _key;
            set => _key = value;
        }

        public Condition Condition
        {
            get => _condition ??= new Condition();
            set => _condition = value;
        }

        public ChoiceUnavailable WhenUnavailable
        {
            get => _whenUnavailable;
            set => _whenUnavailable = value;
        }

        public string Next
        {
            get => _next;
            set => _next = value;
        }
    }

    public enum ChoiceUnavailable
    {
        Hide,
        Disable
    }
}
