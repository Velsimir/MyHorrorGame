using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [Serializable]
    public sealed class LineNode : DialogueNode
    {
        [SerializeField] private SpeakerProfile _speaker;
        [SerializeField] private string _emotion;
        [SerializeField] private string _table;
        [SerializeField] private string _key;
        [SerializeField] private List<LineVariant> _variants = new List<LineVariant>();
        [SerializeField] private LineAdvance _advance = LineAdvance.Input;
        [SerializeField, Min(0f)] private float _seconds;
        [SerializeField] private string _next;

        public SpeakerProfile Speaker
        {
            get => _speaker;
            set => _speaker = value;
        }

        public string Emotion
        {
            get => _emotion;
            set => _emotion = value;
        }

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

        public List<LineVariant> Variants => _variants ??= new List<LineVariant>();

        public LineAdvance Advance
        {
            get => _advance;
            set => _advance = value;
        }

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

        public LineVariant SelectVariant(IDialogueVariables variables, DialogueVariableCatalog catalog = null)
        {
            if (_variants == null)
                return null;

            foreach (LineVariant variant in _variants)
            {
                if (variant != null && !string.IsNullOrEmpty(variant.Key) && Condition.IsMet(variant.Condition, variables, catalog))
                    return variant;
            }

            return null;
        }

        public override void CollectLinks(List<DialogueLink> links) => links.Add(new DialogueLink("Next", _next));
    }

    [Serializable]
    public sealed class LineVariant
    {
        [SerializeField] private Condition _condition = new Condition();
        [SerializeField] private string _key;
        [SerializeField] private string _emotion;

        public Condition Condition
        {
            get => _condition ??= new Condition();
            set => _condition = value;
        }

        public string Key
        {
            get => _key;
            set => _key = value;
        }

        public string Emotion
        {
            get => _emotion;
            set => _emotion = value;
        }
    }

    public enum LineAdvance
    {
        Input,
        AfterSeconds,
        AfterText
    }
}
