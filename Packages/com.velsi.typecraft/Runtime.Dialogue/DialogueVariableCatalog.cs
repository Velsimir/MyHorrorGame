using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [CreateAssetMenu(fileName = "DialogueVariables", menuName = "Typecraft/Dialogue Variables")]
    public sealed class DialogueVariableCatalog : ScriptableObject
    {
        [SerializeField] private List<DialogueVariableDefinition> _variables = new List<DialogueVariableDefinition>();

        private Dictionary<string, DialogueVariableDefinition> _lookup;

        public IReadOnlyList<DialogueVariableDefinition> Variables => _variables;

        public bool TryGet(string name, out DialogueVariableDefinition definition)
        {
            if (string.IsNullOrEmpty(name))
            {
                definition = null;
                return false;
            }

            _lookup ??= BuildLookup();
            return _lookup.TryGetValue(name, out definition);
        }

        public bool Contains(string name) => TryGet(name, out _);

        public void Add(DialogueVariableDefinition definition)
        {
            if (definition == null)
                return;

            _variables.Add(definition);
            _lookup = null;
        }

        public bool Remove(string name)
        {
            int removed = _variables.RemoveAll(definition => definition != null && definition.Name == name);
            _lookup = null;
            return removed > 0;
        }

        public void ApplyDefaults(IDialogueVariables variables, bool overwrite = false)
        {
            if (variables == null)
                return;

            foreach (DialogueVariableDefinition definition in _variables)
            {
                if (definition == null || string.IsNullOrEmpty(definition.Name))
                    continue;

                if (overwrite || !variables.TryGet(definition.Name, out _))
                    variables.Set(definition.Name, definition.Default);
            }
        }

        public void InvalidateLookup() => _lookup = null;

        private void OnValidate() => _lookup = null;

        private Dictionary<string, DialogueVariableDefinition> BuildLookup()
        {
            var lookup = new Dictionary<string, DialogueVariableDefinition>(_variables.Count, StringComparer.Ordinal);
            foreach (DialogueVariableDefinition definition in _variables)
            {
                if (definition != null && !string.IsNullOrEmpty(definition.Name) && !lookup.ContainsKey(definition.Name))
                    lookup[definition.Name] = definition;
            }

            return lookup;
        }
    }

    [Serializable]
    public sealed class DialogueVariableDefinition
    {
        [SerializeField] private string _name;
        [SerializeField] private DialogueValueType _type;
        [SerializeField] private DialogueValue _default;
        [SerializeField, TextArea(1, 4)] private string _description;

        public DialogueVariableDefinition()
        {
        }

        public DialogueVariableDefinition(string name, DialogueValue defaultValue, string description = null)
        {
            _name = name;
            _type = defaultValue.Type;
            _default = defaultValue;
            _description = description;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public DialogueValueType Type
        {
            get => _type;
            set => _type = value;
        }

        public DialogueValue Default
        {
            get => _default.Type == _type ? _default : _default.ConvertTo(_type);
            set => _default = value;
        }

        public string Description
        {
            get => _description;
            set => _description = value;
        }
    }
}
