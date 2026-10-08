using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [Serializable]
    public sealed class Condition
    {
        [SerializeField] private ConditionMode _mode = ConditionMode.All;
        [SerializeField] private List<ConditionClause> _clauses = new List<ConditionClause>();

        public ConditionMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        public List<ConditionClause> Clauses => _clauses ??= new List<ConditionClause>();

        public bool IsEmpty
        {
            get
            {
                if (_clauses == null)
                    return true;

                foreach (ConditionClause clause in _clauses)
                {
                    if (clause != null)
                        return false;
                }

                return true;
            }
        }

        public Condition Add(string variable, ComparisonOperator comparison, DialogueValue value = default)
        {
            Clauses.Add(new ConditionClause(variable, comparison, value));
            return this;
        }

        public bool Evaluate(IDialogueVariables variables, DialogueVariableCatalog catalog = null)
        {
            if (IsEmpty)
                return true;

            bool any = _mode == ConditionMode.Any;
            foreach (ConditionClause clause in _clauses)
            {
                if (clause == null)
                    continue;

                if (clause.Evaluate(variables, catalog) == any)
                    return any;
            }

            return !any;
        }

        public static bool IsMet(Condition condition, IDialogueVariables variables, DialogueVariableCatalog catalog = null) =>
            condition == null || condition.Evaluate(variables, catalog);
    }

    [Serializable]
    public sealed class ConditionClause
    {
        [SerializeField] private string _variable;
        [SerializeField] private ComparisonOperator _comparison;
        [SerializeField] private DialogueValue _value;

        public ConditionClause()
        {
        }

        public ConditionClause(string variable, ComparisonOperator comparison, DialogueValue value)
        {
            _variable = variable;
            _comparison = comparison;
            _value = value;
        }

        public string Variable
        {
            get => _variable;
            set => _variable = value;
        }

        public ComparisonOperator Comparison
        {
            get => _comparison;
            set => _comparison = value;
        }

        public DialogueValue Value
        {
            get => _value;
            set => _value = value;
        }

        public bool UsesValue => _comparison != ComparisonOperator.IsTrue && _comparison != ComparisonOperator.IsFalse;

        public bool Evaluate(IDialogueVariables variables, DialogueVariableCatalog catalog = null)
        {
            DialogueValueType fallback = UsesValue ? _value.Type : DialogueValueType.Bool;
            DialogueValue current = DialogueVariableLookup.Get(variables, catalog, _variable, fallback);

            return _comparison switch
            {
                ComparisonOperator.IsTrue => current.AsBool,
                ComparisonOperator.IsFalse => !current.AsBool,
                ComparisonOperator.Equal => DialogueValue.Compare(current, _value) == 0,
                ComparisonOperator.NotEqual => DialogueValue.Compare(current, _value) != 0,
                ComparisonOperator.Greater => DialogueValue.Compare(current, _value) > 0,
                ComparisonOperator.GreaterOrEqual => DialogueValue.Compare(current, _value) >= 0,
                ComparisonOperator.Less => DialogueValue.Compare(current, _value) < 0,
                ComparisonOperator.LessOrEqual => DialogueValue.Compare(current, _value) <= 0,
                _ => false
            };
        }
    }

    public enum ConditionMode
    {
        All,
        Any
    }

    public enum ComparisonOperator
    {
        IsTrue,
        IsFalse,
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual
    }
}
