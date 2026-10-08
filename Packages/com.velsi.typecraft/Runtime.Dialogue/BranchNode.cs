using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [Serializable]
    public sealed class BranchNode : DialogueNode
    {
        [SerializeField] private List<BranchCase> _cases = new List<BranchCase>();
        [SerializeField] private string _else;

        public List<BranchCase> Cases => _cases ??= new List<BranchCase>();

        public string Else
        {
            get => _else;
            set => _else = value;
        }

        public string Evaluate(IDialogueVariables variables, DialogueVariableCatalog catalog = null)
        {
            if (_cases != null)
            {
                foreach (BranchCase branchCase in _cases)
                {
                    if (branchCase != null && Condition.IsMet(branchCase.Condition, variables, catalog))
                        return branchCase.Next;
                }
            }

            return _else;
        }

        public override void CollectLinks(List<DialogueLink> links)
        {
            if (_cases != null)
            {
                for (int i = 0; i < _cases.Count; i++)
                {
                    if (_cases[i] != null)
                        links.Add(new DialogueLink("Case " + i, _cases[i].Next));
                }
            }

            links.Add(new DialogueLink("Else", _else));
        }
    }

    [Serializable]
    public sealed class BranchCase
    {
        [SerializeField] private Condition _condition = new Condition();
        [SerializeField] private string _next;

        public Condition Condition
        {
            get => _condition ??= new Condition();
            set => _condition = value;
        }

        public string Next
        {
            get => _next;
            set => _next = value;
        }
    }
}
