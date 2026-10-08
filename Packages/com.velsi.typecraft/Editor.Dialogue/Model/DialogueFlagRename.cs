using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Velsi.Typecraft.Dialogue.Editor.Model
{
    public sealed class DialogueFlagRename
    {
        private const string UndoName = "Typecraft: переименование флага";

        private readonly List<(DialogueAsset Asset, int Count)> _dialogues;

        private DialogueFlagRename(string from, string to, List<(DialogueAsset Asset, int Count)> dialogues)
        {
            From = from;
            To = to;
            _dialogues = dialogues;
        }

        public string From { get; }
        public string To { get; }
        public IReadOnlyList<(DialogueAsset Asset, int Count)> Dialogues => _dialogues;
        public int Places => _dialogues.Sum(entry => entry.Count);

        public static DialogueFlagRename Prepare(string from, string to)
        {
            var dialogues = new List<(DialogueAsset, int)>();
            foreach (DialogueAsset asset in DialogueProject.Dialogues())
            {
                int count = Rewrite(asset, from, null);
                if (count > 0)
                    dialogues.Add((asset, count));
            }

            return new DialogueFlagRename(from, to, dialogues);
        }

        public static int Count(DialogueAsset asset, string name) => Rewrite(asset, name, null);

        public string Describe()
        {
            if (_dialogues.Count == 0)
                return "В диалогах флаг не используется.";

            IEnumerable<string> lines = _dialogues.Take(12).Select(entry => $"  {entry.Asset.name}: {entry.Count}");
            return $"Мест в диалогах: {Places} (диалогов: {_dialogues.Count})\n" + string.Join("\n", lines) + (_dialogues.Count > 12 ? "\n  …" : string.Empty);
        }

        public void Apply(DialogueVariableCatalog catalog, DialogueVariableDefinition definition, bool rewriteDialogues)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            var targets = new List<Object>();
            if (catalog != null)
                targets.Add(catalog);
            if (rewriteDialogues)
                targets.AddRange(_dialogues.Select(entry => (Object)entry.Asset));
            if (targets.Count > 0)
                Undo.RecordObjects(targets.ToArray(), UndoName);

            if (definition != null)
                definition.Name = To;

            if (rewriteDialogues)
            {
                foreach ((DialogueAsset asset, int _) in _dialogues)
                    Rewrite(asset, From, To);
            }

            foreach (Object target in targets)
                DialogueEdits.Commit(target);
            Undo.CollapseUndoOperations(group);
        }

        private static int Rewrite(DialogueAsset asset, string from, string to)
        {
            if (asset == null || string.IsNullOrEmpty(from))
                return 0;

            int count = 0;
            foreach (DialogueNode node in asset.Nodes)
            {
                switch (node)
                {
                    case LineNode line:
                        foreach (LineVariant variant in line.Variants.Where(variant => variant != null))
                            count += Rewrite(variant.Condition, from, to);
                        break;
                    case ChoiceNode choice:
                        foreach (ChoiceOption option in choice.Options.Where(option => option != null))
                            count += Rewrite(option.Condition, from, to);
                        break;
                    case BranchNode branch:
                        foreach (BranchCase branchCase in branch.Cases.Where(branchCase => branchCase != null))
                            count += Rewrite(branchCase.Condition, from, to);
                        break;
                    case SetVariablesNode set:
                        foreach (VariableAssignment assignment in set.Assignments.Where(assignment => assignment != null && assignment.Name == from))
                        {
                            count++;
                            if (to != null)
                                assignment.Name = to;
                        }

                        break;
                }
            }

            return count;
        }

        private static int Rewrite(Condition condition, string from, string to)
        {
            if (condition == null)
                return 0;

            int count = 0;
            foreach (ConditionClause clause in condition.Clauses.Where(clause => clause != null && clause.Variable == from))
            {
                count++;
                if (to != null)
                    clause.Variable = to;
            }

            return count;
        }
    }
}
