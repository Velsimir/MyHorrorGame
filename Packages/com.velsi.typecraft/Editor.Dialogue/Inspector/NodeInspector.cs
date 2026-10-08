using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Velsi.Typecraft.Dialogue.Editor.Model;
using Velsi.Typecraft.Editor.Localization;

namespace Velsi.Typecraft.Dialogue.Editor.Inspector
{
    public sealed class NodeInspector : VisualElement, IDisposable
    {
        private const string None = "(нет)";

        private static readonly List<string> AdvanceTitles = new List<string> { "По нажатию", "Через N секунд", "После печати текста" };
        private static readonly List<string> UnavailableTitles = new List<string> { "Скрыть", "Показать неактивным" };
        private static readonly List<string> OperationTitles = new List<string> { "=", "+=", "переключить" };

        private readonly IDialogueEditorContext _context;
        private readonly DialogueAsset _asset;
        private readonly List<KeyField> _keys = new List<KeyField>();

        public NodeInspector(IDialogueEditorContext context, DialogueNode node, IReadOnlyList<DialogueIssue> issues)
        {
            _context = context;
            _asset = context.Asset;
            AddToClassList("dlg-inspector");

            AddHeader(node);
            AddIssues(issues);

            switch (node)
            {
                case StartNode _:
                    Add(Hint("Отсюда начинается диалог. Можно завести несколько точек входа и запускать их по id."));
                    break;
                case LineNode line:
                    BuildLine(line);
                    break;
                case ChoiceNode choice:
                    BuildChoice(choice);
                    break;
                case BranchNode branch:
                    BuildBranch(branch);
                    break;
                case SetVariablesNode set:
                    BuildSet(set);
                    break;
                case EventNode raised:
                    BuildEvent(raised);
                    break;
                case WaitNode wait:
                    BuildWait(wait);
                    break;
                case JumpNode jump:
                    BuildJump(jump);
                    break;
                case EndNode _:
                    Add(Hint("Диалог заканчивается."));
                    break;
            }

            AddLinks(node);
        }

        public void Tick(float deltaTime)
        {
            foreach (KeyField key in _keys)
                key.Tick(deltaTime);
        }

        public void Dispose()
        {
            foreach (KeyField key in _keys)
                key.Dispose();
            _keys.Clear();
        }

        private string KeyPrefix() => "dlg." + DialogueTexts.SnakeCase(_asset != null ? _asset.name : null);

        private void Edit(string undoName, Action change, bool rebuild = false) => _context.Edit(_asset, undoName, change, rebuild);

        private void AddHeader(DialogueNode node)
        {
            var header = new VisualElement();
            header.AddToClassList("tc-lib__header");
            var title = new Label($"{DialogueNodeInfo.Glyph(node.GetType())} {DialogueNodeInfo.Title(node)}");
            title.AddToClassList("tc-lib__title");
            header.Add(title);

            if (node is StartNode && _asset.StartNodeId != node.Id)
                header.Add(new Button(() => _context.Edit(_asset, "стартовая нода", () => _asset.StartNodeId = node.Id, true)) { text = "Сделать основной", tooltip = "С этой ноды диалог начинается по умолчанию" });

            header.Add(new Button(() => _context.RemoveNode(node.Id)) { text = "Удалить" });
            Add(header);

            var id = new Label("id " + node.Id + (_asset.StartNodeId == node.Id ? " · старт по умолчанию" : string.Empty));
            id.AddToClassList("dlg-id");
            Add(id);
        }

        private void AddIssues(IReadOnlyList<DialogueIssue> issues)
        {
            if (issues == null || issues.Count == 0)
                return;

            var box = new VisualElement();
            box.AddToClassList("dlg-issues");
            foreach (DialogueIssue issue in issues)
            {
                var label = new Label((issue.Severity == DialogueIssueSeverity.Error ? "✖ " : "⚠ ") + DialogueIssueText.Describe(issue)) { tooltip = issue.Message };
                label.AddToClassList(issue.Severity == DialogueIssueSeverity.Error ? "dlg-issues__error" : "dlg-issues__warning");
                box.Add(label);
            }

            Add(box);
        }

        private void BuildLine(LineNode line)
        {
            var speaker = new ObjectField("Говорящий") { objectType = typeof(SpeakerProfile), allowSceneObjects = false, value = line.Speaker };
            speaker.RegisterValueChangedCallback(evt => Edit("говорящий", () => line.Speaker = evt.newValue as SpeakerProfile, true));
            Add(speaker);

            if (line.Speaker != null)
                Add(EmotionField("Эмоция", line.Speaker, line.Emotion, value => Edit("эмоция", () => line.Emotion = value)));

            Add(TableField(line.Table, value => Edit("таблица реплики", () => line.Table = value, true)));

            KeyField key = Key("Ключ", () => _asset.ResolveTable(line.Table), true);
            key.Value = line.Key;
            key.Changed += value => Edit("ключ реплики", () => line.Key = value);
            Add(key);

            var advance = new DropdownField("Переход дальше", AdvanceTitles, (int)line.Advance);
            var seconds = new FloatField("Секунды") { value = line.Seconds, isDelayed = true };
            seconds.style.display = line.Advance == LineAdvance.Input ? DisplayStyle.None : DisplayStyle.Flex;
            advance.RegisterValueChangedCallback(_ =>
            {
                Edit("переход реплики", () => line.Advance = (LineAdvance)advance.index);
                seconds.style.display = line.Advance == LineAdvance.Input ? DisplayStyle.None : DisplayStyle.Flex;
            });
            seconds.RegisterValueChangedCallback(evt => Edit("секунды реплики", () => line.Seconds = Math.Max(0f, evt.newValue)));
            Add(advance);
            Add(seconds);

            Add(Section("Варианты по условию", "+ вариант", () => Edit("вариант реплики", () => line.Variants.Add(new LineVariant()), true)));
            if (line.Variants.Count == 0)
                Add(Hint("Первый вариант, чьё условие выполнено, заменяет ключ и эмоцию реплики."));

            for (int i = 0; i < line.Variants.Count; i++)
            {
                LineVariant variant = line.Variants[i];
                if (variant == null)
                    continue;

                VisualElement box = Box($"Вариант {i + 1}", line.Variants, i, "вариант реплики");
                box.Add(new ConditionEditor(_context, variant.Condition));
                KeyField variantKey = Key("Ключ", () => _asset.ResolveTable(line.Table), false);
                variantKey.Value = variant.Key;
                variantKey.Changed += value => Edit("ключ варианта", () => variant.Key = value);
                box.Add(variantKey);
                if (line.Speaker != null)
                    box.Add(EmotionField("Эмоция", line.Speaker, variant.Emotion, value => Edit("эмоция варианта", () => variant.Emotion = value)));
                Add(box);
            }
        }

        private void BuildChoice(ChoiceNode choice)
        {
            Add(Section("Варианты ответа", "+ вариант", () => Edit("вариант выбора", () => choice.Options.Add(new ChoiceOption()), true)));
            for (int i = 0; i < choice.Options.Count; i++)
            {
                ChoiceOption option = choice.Options[i];
                if (option == null)
                    continue;

                VisualElement box = Box($"Вариант {i + 1}", choice.Options, i, "вариант выбора");
                box.Add(TableField(option.Table, value => Edit("таблица варианта", () => option.Table = value, true)));
                KeyField key = Key("Ключ", () => _asset.ResolveTable(option.Table), false);
                key.Value = option.Key;
                key.Changed += value => Edit("ключ варианта", () => option.Key = value);
                box.Add(key);
                box.Add(new ConditionEditor(_context, option.Condition, "Доступен, если"));
                var unavailable = new DropdownField("Если недоступен", UnavailableTitles, (int)option.WhenUnavailable);
                unavailable.RegisterValueChangedCallback(_ => Edit("недоступный вариант", () => option.WhenUnavailable = (ChoiceUnavailable)unavailable.index));
                box.Add(unavailable);
                Add(box);
            }
        }

        private void BuildBranch(BranchNode branch)
        {
            Add(Section("Случаи", "+ случай", () => Edit("случай условия", () => branch.Cases.Add(new BranchCase()), true)));
            Add(Hint("Проверяются сверху вниз, срабатывает первый выполненный. Ни один — выход «Иначе»."));
            for (int i = 0; i < branch.Cases.Count; i++)
            {
                BranchCase branchCase = branch.Cases[i];
                if (branchCase == null)
                    continue;

                VisualElement box = Box($"Случай {i + 1}", branch.Cases, i, "случай условия");
                box.Add(new ConditionEditor(_context, branchCase.Condition));
                Add(box);
            }
        }

        private void BuildSet(SetVariablesNode set)
        {
            Add(Section("Присваивания", "+ присваивание", () => Edit("присваивание", () => set.Assignments.Add(new VariableAssignment(string.Empty, VariableOperation.Set, DialogueValue.Bool(true))), true)));
            foreach (VariableAssignment assignment in set.Assignments.ToList())
            {
                if (assignment == null)
                    continue;

                var row = new VisualElement();
                row.AddToClassList("dlg-row");
                row.AddToClassList("dlg-clause");

                var name = new PickField(null, "Флаги", ConditionEditor.VariableItems) { Value = assignment.Name };
                name.AddToClassList("dlg-clause__variable");
                name.Changed += value => Edit("флаг присваивания", () =>
                {
                    assignment.Name = value;
                    if (DialogueProject.TryVariable(value, out DialogueVariableDefinition definition))
                        assignment.Value = assignment.Value.ConvertTo(definition.Type);
                }, true);
                row.Add(name);

                var operation = new DropdownField(OperationTitles, (int)assignment.Operation);
                operation.AddToClassList("dlg-clause__operator");
                operation.RegisterValueChangedCallback(_ => Edit("операция", () => assignment.Operation = (VariableOperation)operation.index, true));
                row.Add(operation);

                if (assignment.Operation != VariableOperation.Toggle)
                {
                    bool declared = DialogueProject.TryVariable(assignment.Name, out DialogueVariableDefinition definition);
                    var value = new DialogueValueField(null);
                    value.AddToClassList("dlg-clause__value");
                    value.SetValue(declared && assignment.Value.Type != definition.Type ? assignment.Value.ConvertTo(definition.Type) : assignment.Value);
                    value.Changed += changed => Edit("значение флага", () => assignment.Value = changed);
                    row.Add(value);
                    if (!declared)
                        row.Add(ConditionEditor.TypeChooser(assignment.Value.Type, type => Edit("тип значения", () => assignment.Value = assignment.Value.ConvertTo(type), true)));
                }

                row.Add(ConditionEditor.SmallButton("✕", "Убрать", () => Edit("удаление присваивания", () => set.Assignments.Remove(assignment), true)));
                Add(row);
            }
        }

        private void BuildEvent(EventNode raised)
        {
            var name = new TextField("Имя события") { value = raised.EventName, isDelayed = true };
            name.RegisterValueChangedCallback(evt => Edit("имя события", () => raised.EventName = evt.newValue));
            Add(name);
            var argument = new TextField("Аргумент") { value = raised.Argument, isDelayed = true };
            argument.RegisterValueChangedCallback(evt => Edit("аргумент события", () => raised.Argument = evt.newValue));
            Add(argument);
            Add(Hint("Код игры подписывается на DialogueRunner.EventRaised(имя, аргумент)."));
        }

        private void BuildWait(WaitNode wait)
        {
            var seconds = new FloatField("Секунды") { value = wait.Seconds, isDelayed = true };
            seconds.RegisterValueChangedCallback(evt => Edit("пауза", () => wait.Seconds = Math.Max(0f, evt.newValue)));
            Add(seconds);
        }

        private void BuildJump(JumpNode jump)
        {
            var target = new ObjectField("Диалог") { objectType = typeof(DialogueAsset), allowSceneObjects = false, value = jump.Target, tooltip = "Пусто — этот же диалог" };
            target.RegisterValueChangedCallback(evt => Edit("цель перехода", () =>
            {
                jump.Target = evt.newValue as DialogueAsset == _asset ? null : evt.newValue as DialogueAsset;
                jump.TargetNode = null;
            }, true));
            Add(target);

            DialogueAsset asset = jump.Target != null ? jump.Target : _asset;
            List<DialogueNode> nodes = asset.Nodes.Where(node => node != null && node != jump).ToList();
            var labels = new List<string> { "(старт диалога)" };
            labels.AddRange(nodes.Select(node => DialogueTexts.NodeLabel(asset, node, _context.Locale)));
            int index = nodes.FindIndex(node => node.Id == jump.TargetNode);
            if (index < 0 && !string.IsNullOrEmpty(jump.TargetNode))
                labels.Add("⚠ нет ноды " + jump.TargetNode);

            var node = new DropdownField("Нода", labels, index >= 0 ? index + 1 : string.IsNullOrEmpty(jump.TargetNode) ? 0 : labels.Count - 1);
            node.RegisterValueChangedCallback(_ =>
            {
                int picked = node.index - 1;
                if (picked >= nodes.Count)
                    return;
                Edit("нода перехода", () => jump.TargetNode = picked < 0 ? null : nodes[picked].Id, true);
            });
            Add(node);
        }

        private void AddLinks(DialogueNode node)
        {
            List<DialogueLink> links = node.GetLinks();
            if (links.Count == 0)
                return;

            Add(Section("Переходы", null, null));
            List<DialogueNode> nodes = _asset.Nodes.Where(candidate => candidate != null && !(candidate is StartNode)).ToList();
            var labels = new List<string> { None };
            labels.AddRange(nodes.Select(candidate => DialogueTexts.NodeLabel(_asset, candidate, _context.Locale)));

            foreach (DialogueLink link in links)
            {
                string port = link.Port;
                int index = nodes.FindIndex(candidate => candidate.Id == link.Target);
                var choices = new List<string>(labels);
                if (index < 0 && !link.IsEmpty)
                    choices.Add("⚠ нет ноды " + link.Target);

                var field = new DropdownField(DialogueNodeInfo.PortTitle(node, port, _context.Locale, _asset.DefaultTable), choices, index >= 0 ? index + 1 : link.IsEmpty ? 0 : choices.Count - 1);
                field.AddToClassList("dlg-link");
                field.RegisterValueChangedCallback(_ =>
                {
                    int picked = field.index - 1;
                    if (picked >= nodes.Count)
                        return;
                    Edit("связь", () => DialogueEdits.SetLink(node, port, picked < 0 ? null : nodes[picked].Id), true);
                });

                var row = new VisualElement();
                row.AddToClassList("dlg-row");
                field.style.flexGrow = 1;
                row.Add(field);
                if (index >= 0)
                    row.Add(ConditionEditor.SmallButton("→", "Перейти к ноде", () => _context.SelectNode(link.Target)));
                Add(row);
            }
        }

        private KeyField Key(string label, Func<string> table, bool preview)
        {
            var key = new KeyField(label, _context, table, KeyPrefix, preview);
            _keys.Add(key);
            return key;
        }

        private VisualElement TableField(string current, Action<string> changed)
        {
            ITableBackend backend = DialogueTexts.Backend;
            var tables = backend != null ? backend.Tables.Distinct().ToList() : new List<string>();
            if (!string.IsNullOrEmpty(current) && !tables.Contains(current))
                tables.Add(current);

            var choices = new List<string> { $"(таблица диалога: {(_asset != null && !string.IsNullOrEmpty(_asset.DefaultTable) ? _asset.DefaultTable : "не задана")})" };
            choices.AddRange(tables);
            var field = new DropdownField("Таблица", choices, string.IsNullOrEmpty(current) ? 0 : tables.IndexOf(current) + 1);
            field.RegisterValueChangedCallback(_ => changed(field.index <= 0 ? null : tables[field.index - 1]));
            return field;
        }

        public static PickField EmotionField(string label, SpeakerProfile speaker, string value, Action<string> changed)
        {
            var field = new PickField(label, "Эмоции говорящего", () => speaker.Portraits
                .Where(portrait => portrait != null && !string.IsNullOrEmpty(portrait.Emotion))
                .Select(portrait => portrait.Emotion)
                .Distinct()
                .Select(emotion => new PickItem(emotion, emotion))) { Value = value };
            field.Changed += changed;
            return field;
        }

        private VisualElement Box<T>(string title, List<T> list, int index, string undoName)
        {
            var box = new VisualElement();
            box.AddToClassList("dlg-box");
            var header = new VisualElement();
            header.AddToClassList("dlg-row");
            var label = new Label(title);
            label.AddToClassList("dlg-box__title");
            header.Add(label);
            header.Add(ConditionEditor.Grow());

            var up = ConditionEditor.SmallButton("↑", "Выше", () => Edit(undoName, () =>
            {
                T item = list[index];
                list[index] = list[index - 1];
                list[index - 1] = item;
            }, true));
            up.SetEnabled(index > 0);
            header.Add(up);
            header.Add(ConditionEditor.SmallButton("✕", "Удалить", () => Edit(undoName, () => list.RemoveAt(index), true)));
            box.Add(header);
            return box;
        }

        private static VisualElement Section(string title, string action, Action click)
        {
            var row = new VisualElement();
            row.AddToClassList("dlg-row");
            row.AddToClassList("dlg-section");
            var label = new Label(title);
            label.AddToClassList("tc-lib__section");
            row.Add(label);
            row.Add(ConditionEditor.Grow());
            if (action != null)
                row.Add(new Button(click) { text = action });
            return row;
        }

        internal static Label Hint(string text)
        {
            var label = new Label(text);
            label.AddToClassList("tc-lib__hint");
            return label;
        }
    }
}
