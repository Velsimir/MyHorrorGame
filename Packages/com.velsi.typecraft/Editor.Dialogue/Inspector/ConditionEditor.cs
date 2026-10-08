using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Velsi.Typecraft.Dialogue.Editor.Model;

namespace Velsi.Typecraft.Dialogue.Editor.Inspector
{
    public sealed class ConditionEditor : VisualElement
    {
        private static readonly List<string> Modes = new List<string> { "все (И)", "любое (ИЛИ)" };

        private static readonly (ComparisonOperator Operator, string Title)[] Operators =
        {
            (ComparisonOperator.IsTrue, "истина"),
            (ComparisonOperator.IsFalse, "ложь"),
            (ComparisonOperator.Equal, "="),
            (ComparisonOperator.NotEqual, "≠"),
            (ComparisonOperator.Greater, ">"),
            (ComparisonOperator.GreaterOrEqual, "≥"),
            (ComparisonOperator.Less, "<"),
            (ComparisonOperator.LessOrEqual, "≤")
        };

        private static readonly List<string> Types = new List<string> { "bool", "int", "float", "string" };

        public ConditionEditor(IDialogueEditorContext context, Condition condition, string title = "Условие")
        {
            AddToClassList("dlg-condition");

            var header = new VisualElement();
            header.AddToClassList("dlg-row");
            var label = new Label(title);
            label.AddToClassList("dlg-condition__title");
            header.Add(label);

            var mode = new DropdownField(Modes, condition.Mode == ConditionMode.All ? 0 : 1);
            mode.AddToClassList("dlg-condition__mode");
            mode.SetEnabled(condition.Clauses.Count > 1);
            mode.RegisterValueChangedCallback(_ => context.Edit(context.Asset, "режим условия", () => condition.Mode = mode.index == 0 ? ConditionMode.All : ConditionMode.Any));
            header.Add(mode);

            header.Add(Grow());
            header.Add(SmallButton("+", "Добавить проверку", () => context.Edit(context.Asset, "проверка условия", () => condition.Clauses.Add(new ConditionClause(string.Empty, ComparisonOperator.IsTrue, DialogueValue.Bool(true))), true)));
            Add(header);

            if (condition.Clauses.Count == 0)
            {
                var empty = new Label("Нет проверок — выполняется всегда.");
                empty.AddToClassList("dlg-hint");
                Add(empty);
            }

            for (int i = 0; i < condition.Clauses.Count; i++)
            {
                ConditionClause clause = condition.Clauses[i];
                if (clause != null)
                    Add(ClauseRow(context, condition, clause));
            }
        }

        public static IEnumerable<PickItem> VariableItems()
        {
            List<DialogueVariableCatalog> catalogs = DialogueProject.Catalogs();
            bool grouped = catalogs.Count > 1;
            foreach (DialogueVariableCatalog catalog in catalogs)
            foreach (DialogueVariableDefinition definition in catalog.Variables.Where(definition => definition != null && !string.IsNullOrEmpty(definition.Name)))
                yield return new PickItem(definition.Name, $"{definition.Name}   · {definition.Type}", grouped ? catalog.name : null);
        }

        public static VisualElement TypeChooser(DialogueValueType type, System.Action<DialogueValueType> changed)
        {
            var field = new DropdownField(Types, (int)type) { tooltip = "Тип значения: флаг не объявлен в каталоге" };
            field.AddToClassList("dlg-type");
            field.RegisterValueChangedCallback(_ => changed((DialogueValueType)field.index));
            return field;
        }

        private static VisualElement ClauseRow(IDialogueEditorContext context, Condition condition, ConditionClause clause)
        {
            var row = new VisualElement();
            row.AddToClassList("dlg-row");
            row.AddToClassList("dlg-clause");

            var variable = new PickField(null, "Флаги", VariableItems) { Value = clause.Variable };
            variable.AddToClassList("dlg-clause__variable");
            variable.Changed += name => context.Edit(context.Asset, "флаг условия", () =>
            {
                clause.Variable = name;
                if (DialogueProject.TryVariable(name, out DialogueVariableDefinition definition) && clause.UsesValue)
                    clause.Value = clause.Value.ConvertTo(definition.Type);
            }, true);
            row.Add(variable);

            int selected = System.Array.FindIndex(Operators, entry => entry.Operator == clause.Comparison);
            var comparison = new DropdownField(Operators.Select(entry => entry.Title).ToList(), System.Math.Max(0, selected));
            comparison.AddToClassList("dlg-clause__operator");
            comparison.RegisterValueChangedCallback(_ => context.Edit(context.Asset, "сравнение", () =>
            {
                clause.Comparison = Operators[comparison.index].Operator;
                if (clause.UsesValue && DialogueProject.TryVariable(clause.Variable, out DialogueVariableDefinition definition))
                    clause.Value = clause.Value.ConvertTo(definition.Type);
            }, true));
            row.Add(comparison);

            if (clause.UsesValue)
            {
                bool declared = DialogueProject.TryVariable(clause.Variable, out DialogueVariableDefinition definition);
                DialogueValue value = declared && clause.Value.Type != definition.Type ? clause.Value.ConvertTo(definition.Type) : clause.Value;
                var field = new DialogueValueField(null);
                field.AddToClassList("dlg-clause__value");
                field.SetValue(value);
                field.Changed += changed => context.Edit(context.Asset, "значение условия", () => clause.Value = changed);
                row.Add(field);

                if (!declared)
                    row.Add(TypeChooser(clause.Value.Type, type => context.Edit(context.Asset, "тип значения", () => clause.Value = clause.Value.ConvertTo(type), true)));
            }

            row.Add(SmallButton("✕", "Убрать проверку", () => context.Edit(context.Asset, "удаление проверки", () => condition.Clauses.Remove(clause), true)));
            return row;
        }

        internal static Button SmallButton(string text, string tooltip, System.Action click)
        {
            var button = new Button(click) { text = text, tooltip = tooltip };
            button.AddToClassList("dlg-small");
            return button;
        }

        internal static VisualElement Grow()
        {
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            return spacer;
        }
    }
}
