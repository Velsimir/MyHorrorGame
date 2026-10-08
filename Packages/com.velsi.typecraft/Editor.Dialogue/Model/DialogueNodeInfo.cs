using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Velsi.Typecraft.Dialogue.Editor.Model
{
    public enum NodeCategory
    {
        Flow,
        Speech,
        Logic
    }

    public static class DialogueNodeInfo
    {
        public const string NextPort = "Next";
        public const string ElsePort = "Else";
        public const string OptionPort = "Option ";
        public const string CasePort = "Case ";

        private static readonly Type[] Known =
        {
            typeof(StartNode), typeof(LineNode), typeof(ChoiceNode), typeof(BranchNode), typeof(SetVariablesNode),
            typeof(EventNode), typeof(WaitNode), typeof(JumpNode), typeof(EndNode)
        };

        private static IReadOnlyList<Type> _types;

        public static IReadOnlyList<Type> Types => _types ??= Known
            .Concat(TypeCache.GetTypesDerivedFrom<DialogueNode>()
                .Where(type => !type.IsAbstract && !Known.Contains(type) && type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.Name))
            .ToList();

        public static string Title(Type type)
        {
            if (type == typeof(StartNode)) return "Старт";
            if (type == typeof(LineNode)) return "Реплика";
            if (type == typeof(ChoiceNode)) return "Выбор";
            if (type == typeof(BranchNode)) return "Условие";
            if (type == typeof(SetVariablesNode)) return "Флаги";
            if (type == typeof(EventNode)) return "Событие";
            if (type == typeof(WaitNode)) return "Пауза";
            if (type == typeof(JumpNode)) return "Переход";
            if (type == typeof(EndNode)) return "Конец";
            string name = type.Name.EndsWith("Node", StringComparison.Ordinal) ? type.Name.Substring(0, type.Name.Length - 4) : type.Name;
            return ObjectNames.NicifyVariableName(name);
        }

        public static string Title(DialogueNode node) => node == null ? "(тип не найден)" : Title(node.GetType());

        public static string Glyph(Type type)
        {
            if (type == typeof(StartNode)) return "▶";
            if (type == typeof(LineNode)) return "❝";
            if (type == typeof(ChoiceNode)) return "☰";
            if (type == typeof(BranchNode)) return "◇";
            if (type == typeof(SetVariablesNode)) return "⚑";
            if (type == typeof(EventNode)) return "✦";
            if (type == typeof(WaitNode)) return "◷";
            if (type == typeof(JumpNode)) return "↪";
            if (type == typeof(EndNode)) return "■";
            return "•";
        }

        public static NodeCategory Category(Type type)
        {
            if (type == typeof(LineNode))
                return NodeCategory.Speech;
            if (type == typeof(StartNode) || type == typeof(EndNode))
                return NodeCategory.Flow;
            return NodeCategory.Logic;
        }

        public static string CategoryClass(DialogueNode node) => node == null
            ? "dlg-node--missing"
            : Category(node.GetType()) switch
            {
                NodeCategory.Speech => "dlg-node--speech",
                NodeCategory.Flow => "dlg-node--flow",
                _ => "dlg-node--logic"
            };

        public static bool HasInput(DialogueNode node) => !(node is StartNode);

        public static string PortTitle(DialogueNode node, string port, string locale, string table)
        {
            if (port == NextPort)
                return "Далее";
            if (port == ElsePort)
                return "Иначе";

            if (node is ChoiceNode choice && TryIndex(port, OptionPort, out int option) && option < choice.Options.Count)
            {
                ChoiceOption entry = choice.Options[option];
                if (entry == null || string.IsNullOrEmpty(entry.Key))
                    return $"{option + 1}. —";

                string text = DialogueTexts.Plain(string.IsNullOrEmpty(entry.Table) ? table : entry.Table, entry.Key, locale, 40);
                return $"{option + 1}. {(string.IsNullOrEmpty(text) ? entry.Key : text)}";
            }

            if (node is BranchNode branch && TryIndex(port, CasePort, out int index) && index < branch.Cases.Count)
                return "Если " + DialogueTexts.Shorten(DialogueTexts.Condition(branch.Cases[index]?.Condition), 40);

            return port;
        }

        public static bool TryIndex(string port, string prefix, out int index)
        {
            index = -1;
            return port != null && port.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(port.Substring(prefix.Length), out index) && index >= 0;
        }
    }
}
