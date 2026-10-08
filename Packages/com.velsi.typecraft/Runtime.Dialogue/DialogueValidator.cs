using System;
using System.Collections.Generic;

namespace Velsi.Typecraft.Dialogue
{
    public static class DialogueValidator
    {
        public static List<DialogueIssue> Validate(DialogueAsset asset, DialogueVariableCatalog catalog = null)
        {
            var issues = new List<DialogueIssue>();
            Validate(asset, catalog, issues);
            return issues;
        }

        public static void Validate(DialogueAsset asset, DialogueVariableCatalog catalog, List<DialogueIssue> issues)
        {
            if (asset == null || issues == null)
                return;

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var links = new List<DialogueLink>();

            for (int i = 0; i < asset.Nodes.Count; i++)
            {
                DialogueNode node = asset.Nodes[i];
                if (node == null)
                {
                    issues.Add(new DialogueIssue(DialogueIssueSeverity.Warning, DialogueIssueKind.MissingNodeType, null, $"Node #{i} is null (missing type?)"));
                    continue;
                }

                if (string.IsNullOrEmpty(node.Id))
                    issues.Add(new DialogueIssue(DialogueIssueSeverity.Error, DialogueIssueKind.EmptyNodeId, null, $"{node.GetType().Name} #{i} has no id"));
                else if (!ids.Add(node.Id))
                    issues.Add(new DialogueIssue(DialogueIssueSeverity.Error, DialogueIssueKind.DuplicateNodeId, node.Id, $"Duplicate node id {node.Id}", node.Id));
            }

            string startId = asset.StartNodeId;
            if (string.IsNullOrEmpty(startId))
                issues.Add(new DialogueIssue(DialogueIssueSeverity.Error, DialogueIssueKind.NoStartNode, null, "Dialogue has no start node"));
            else if (!asset.TryGetNode(startId, out _))
                issues.Add(new DialogueIssue(DialogueIssueSeverity.Error, DialogueIssueKind.NoStartNode, startId, $"Start node {startId} does not exist", startId));

            foreach (DialogueNode node in asset.Nodes)
            {
                if (node == null)
                    continue;

                links.Clear();
                node.CollectLinks(links);
                foreach (DialogueLink link in links)
                {
                    if (link.IsEmpty)
                    {
                        if (link.Required)
                            issues.Add(new DialogueIssue(DialogueIssueSeverity.Warning, DialogueIssueKind.EmptyLink, node.Id, $"{node.GetType().Name}: '{link.Port}' is not connected", link.Port));
                    }
                    else if (!asset.TryGetNode(link.Target, out _))
                    {
                        issues.Add(new DialogueIssue(DialogueIssueSeverity.Error, DialogueIssueKind.BrokenLink, node.Id, $"{node.GetType().Name}: '{link.Port}' links to missing node {link.Target}", link.Port, link.Target));
                    }
                }

                ValidateNode(asset, node, catalog, issues);
            }

            if (!string.IsNullOrEmpty(startId) && asset.TryGetNode(startId, out _))
                ValidateReachability(asset, startId, issues);
        }

        private static void ValidateNode(DialogueAsset asset, DialogueNode node, DialogueVariableCatalog catalog, List<DialogueIssue> issues)
        {
            switch (node)
            {
                case LineNode line:
                    if (string.IsNullOrEmpty(line.Key))
                        issues.Add(new DialogueIssue(DialogueIssueSeverity.Error, DialogueIssueKind.EmptyKey, node.Id, "Line has no key", "Line"));
                    for (int i = 0; i < line.Variants.Count; i++)
                    {
                        LineVariant variant = line.Variants[i];
                        if (variant == null)
                            continue;
                        if (string.IsNullOrEmpty(variant.Key))
                            issues.Add(new DialogueIssue(DialogueIssueSeverity.Warning, DialogueIssueKind.EmptyKey, node.Id, $"Line variant {i} has no key and is skipped", "Variant", (i + 1).ToString()));
                        ValidateCondition(variant.Condition, node.Id, catalog, issues);
                    }
                    break;

                case ChoiceNode choice:
                    if (choice.Options.Count == 0)
                        issues.Add(new DialogueIssue(DialogueIssueSeverity.Error, DialogueIssueKind.NoOptions, node.Id, "Choice has no options"));
                    for (int i = 0; i < choice.Options.Count; i++)
                    {
                        ChoiceOption option = choice.Options[i];
                        if (option == null)
                            continue;
                        if (string.IsNullOrEmpty(option.Key))
                            issues.Add(new DialogueIssue(DialogueIssueSeverity.Error, DialogueIssueKind.EmptyKey, node.Id, $"Option {i} has no key", "Option", (i + 1).ToString()));
                        ValidateCondition(option.Condition, node.Id, catalog, issues);
                    }
                    break;

                case BranchNode branch:
                    foreach (BranchCase branchCase in branch.Cases)
                    {
                        if (branchCase != null)
                            ValidateCondition(branchCase.Condition, node.Id, catalog, issues);
                    }
                    break;

                case SetVariablesNode set:
                    foreach (VariableAssignment assignment in set.Assignments)
                    {
                        if (assignment == null)
                            continue;
                        DialogueValueType? type = assignment.Operation == VariableOperation.Toggle ? DialogueValueType.Bool : assignment.Value.Type;
                        ValidateVariable(assignment.Name, type, node.Id, catalog, issues);
                    }
                    break;

                case EventNode raised:
                    if (string.IsNullOrEmpty(raised.EventName))
                        issues.Add(new DialogueIssue(DialogueIssueSeverity.Warning, DialogueIssueKind.EmptyEventName, node.Id, "Event has no name"));
                    break;

                case JumpNode jump:
                    DialogueAsset target = jump.Target != null ? jump.Target : asset;
                    if (string.IsNullOrEmpty(jump.TargetNode))
                    {
                        if (string.IsNullOrEmpty(target.StartNodeId))
                            issues.Add(new DialogueIssue(DialogueIssueSeverity.Error, DialogueIssueKind.BrokenJump, node.Id, $"Jump target '{target.name}' has no start node", target.name));
                    }
                    else if (!target.TryGetNode(jump.TargetNode, out _))
                    {
                        issues.Add(new DialogueIssue(DialogueIssueSeverity.Error, DialogueIssueKind.BrokenJump, node.Id, $"Jump target '{target.name}' has no node {jump.TargetNode}", target.name, jump.TargetNode));
                    }
                    break;
            }
        }

        private static void ValidateCondition(Condition condition, string nodeId, DialogueVariableCatalog catalog, List<DialogueIssue> issues)
        {
            if (condition == null)
                return;

            foreach (ConditionClause clause in condition.Clauses)
            {
                if (clause != null)
                    ValidateVariable(clause.Variable, clause.UsesValue ? clause.Value.Type : (DialogueValueType?)null, nodeId, catalog, issues);
            }
        }

        private static void ValidateVariable(string name, DialogueValueType? type, string nodeId, DialogueVariableCatalog catalog, List<DialogueIssue> issues)
        {
            if (string.IsNullOrEmpty(name))
            {
                issues.Add(new DialogueIssue(DialogueIssueSeverity.Error, DialogueIssueKind.EmptyVariable, nodeId, "Variable name is empty"));
                return;
            }

            if (catalog == null || name.StartsWith(DialogueRunner.VisitedPrefix, StringComparison.Ordinal))
                return;

            if (!catalog.TryGet(name, out DialogueVariableDefinition definition))
            {
                issues.Add(new DialogueIssue(DialogueIssueSeverity.Warning, DialogueIssueKind.UndeclaredVariable, nodeId, $"Variable '{name}' is not declared in '{catalog.name}'", name, catalog.name));
                return;
            }

            if (type.HasValue && !Compatible(definition.Type, type.Value))
                issues.Add(new DialogueIssue(DialogueIssueSeverity.Warning, DialogueIssueKind.VariableTypeMismatch, nodeId, $"Variable '{name}' is {definition.Type}, used as {type.Value}", name, $"{definition.Type} / {type.Value}"));
        }

        private static bool Compatible(DialogueValueType declared, DialogueValueType used) =>
            declared == used || (IsNumber(declared) && IsNumber(used));

        private static bool IsNumber(DialogueValueType type) => type == DialogueValueType.Int || type == DialogueValueType.Float;

        private static void ValidateReachability(DialogueAsset asset, string startId, List<DialogueIssue> issues)
        {
            var reached = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Stack<string>();
            var links = new List<DialogueLink>();

            if (!string.IsNullOrEmpty(startId))
                pending.Push(startId);

            while (pending.Count > 0)
            {
                string id = pending.Pop();
                if (!reached.Add(id) || !asset.TryGetNode(id, out DialogueNode node))
                    continue;

                links.Clear();
                node.CollectLinks(links);
                if (node is JumpNode jump && (jump.Target == null || jump.Target == asset) && !string.IsNullOrEmpty(jump.TargetNode))
                    links.Add(new DialogueLink("Jump", jump.TargetNode));

                foreach (DialogueLink link in links)
                {
                    if (!link.IsEmpty)
                        pending.Push(link.Target);
                }
            }

            foreach (DialogueNode node in asset.Nodes)
            {
                if (node != null && !string.IsNullOrEmpty(node.Id) && !reached.Contains(node.Id))
                    issues.Add(new DialogueIssue(DialogueIssueSeverity.Warning, DialogueIssueKind.Unreachable, node.Id, $"{node.GetType().Name} is unreachable from the start node"));
            }
        }
    }
}
