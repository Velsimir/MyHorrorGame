using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [Serializable]
    public sealed class SetVariablesNode : DialogueNode
    {
        [SerializeField] private List<VariableAssignment> _assignments = new List<VariableAssignment>();
        [SerializeField] private string _next;

        public List<VariableAssignment> Assignments => _assignments ??= new List<VariableAssignment>();

        public string Next
        {
            get => _next;
            set => _next = value;
        }

        public void Apply(IDialogueVariables variables, DialogueVariableCatalog catalog = null)
        {
            if (_assignments == null || variables == null)
                return;

            foreach (VariableAssignment assignment in _assignments)
                assignment?.Apply(variables, catalog);
        }

        public override void CollectLinks(List<DialogueLink> links) => links.Add(new DialogueLink("Next", _next));
    }

    [Serializable]
    public sealed class VariableAssignment
    {
        [SerializeField] private string _name;
        [SerializeField] private VariableOperation _operation;
        [SerializeField] private DialogueValue _value;

        public VariableAssignment()
        {
        }

        public VariableAssignment(string name, VariableOperation operation, DialogueValue value = default)
        {
            _name = name;
            _operation = operation;
            _value = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public VariableOperation Operation
        {
            get => _operation;
            set => _operation = value;
        }

        public DialogueValue Value
        {
            get => _value;
            set => _value = value;
        }

        public void Apply(IDialogueVariables variables, DialogueVariableCatalog catalog = null)
        {
            if (variables == null || string.IsNullOrEmpty(_name))
                return;

            switch (_operation)
            {
                case VariableOperation.Set:
                    variables.Set(_name, _value);
                    break;
                case VariableOperation.Add:
                    variables.Set(_name, DialogueValue.Add(DialogueVariableLookup.Get(variables, catalog, _name, _value.Type), _value));
                    break;
                case VariableOperation.Toggle:
                    variables.Set(_name, DialogueValue.Bool(!DialogueVariableLookup.Get(variables, catalog, _name, DialogueValueType.Bool).AsBool));
                    break;
            }
        }
    }

    public enum VariableOperation
    {
        Set,
        Add,
        Toggle
    }
}
