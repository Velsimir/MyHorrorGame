using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    public sealed class DialogueRunner
    {
        public const int DefaultMaxAutoSteps = 1000;
        public const string VisitedPrefix = "visited:";

        private readonly IDialogueVariables _variables;
        private readonly List<DialogueChoice> _choices = new List<DialogueChoice>();

        private DialogueAsset _asset;
        private ChoiceNode _choiceNode;
        private int _generation;

        public DialogueRunner(IDialogueVariables variables, DialogueVariableCatalog catalog = null)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            Catalog = catalog;
        }

        public event Action<DialogueLine> LineStarted;
        public event Action<IReadOnlyList<DialogueChoice>> ChoicesPresented;
        public event Action<float> WaitRequested;
        public event Action<string, string> EventRaised;
        public event Action Ended;
        public event Action<DialogueAsset, DialogueNode> NodeEntered;

        public IDialogueVariables Variables => _variables;
        public DialogueVariableCatalog Catalog { get; set; }
        public bool TrackVisits { get; set; } = true;
        public int MaxAutoSteps { get; set; } = DefaultMaxAutoSteps;

        public bool IsRunning => _asset != null;
        public DialogueAsset Asset => _asset;
        public DialogueStep Current { get; private set; }

        public DialogueRunnerState State => IsRunning && Current.Kind != DialogueStepKind.None
            ? new DialogueRunnerState { AssetId = _asset.Id, NodeId = Current.NodeId }
            : null;

        public static string VisitedVariable(string assetId, string nodeId) => VisitedPrefix + assetId + "/" + nodeId;

        public int VisitCount(DialogueAsset asset, string nodeId) =>
            asset != null && _variables.TryGet(VisitedVariable(asset.Id, nodeId), out DialogueValue value) ? value.AsInt : 0;

        public void Start(DialogueAsset asset, string nodeId = null)
        {
            if (asset == null)
            {
                Debug.LogWarning("[Typecraft] Dialogue: Start called without an asset");
                return;
            }

            Reset();
            _asset = asset;
            string startId = string.IsNullOrEmpty(nodeId) ? asset.StartNodeId : nodeId;
            if (string.IsNullOrEmpty(startId))
            {
                Fail($"dialogue '{asset.name}' ({asset.Id}) has no start node");
                return;
            }

            Advance(startId, countVisit: true);
        }

        public bool Continue()
        {
            if (!IsRunning)
                return false;

            DialogueNode node = _asset.FindNode(Current.NodeId);
            string next;
            switch (node)
            {
                case LineNode line when Current.Kind == DialogueStepKind.Line:
                    next = line.Next;
                    break;
                case WaitNode wait when Current.Kind == DialogueStepKind.Wait:
                    next = wait.Next;
                    break;
                default:
                    Debug.LogWarning($"[Typecraft] Dialogue '{_asset.name}': Continue ignored at {Current.Kind} node {Current.NodeId}");
                    return false;
            }

            Advance(next, countVisit: true);
            return true;
        }

        public bool Choose(int index)
        {
            if (!IsRunning || Current.Kind != DialogueStepKind.Choice || _choiceNode == null)
                return false;

            DialogueChoice choice = null;
            foreach (DialogueChoice candidate in _choices)
            {
                if (candidate.Index == index)
                    choice = candidate;
            }

            if (choice == null || !choice.Available || index >= _choiceNode.Options.Count)
            {
                Debug.LogWarning($"[Typecraft] Dialogue '{_asset.name}': choice {index} is not available at node {Current.NodeId}");
                return false;
            }

            Advance(_choiceNode.Options[index].Next, countVisit: true);
            return true;
        }

        public void Stop()
        {
            if (IsRunning)
                Finish();
        }

        public bool Restore(DialogueRunnerState state, Func<string, DialogueAsset> resolver)
        {
            if (state == null || !state.IsValid || resolver == null)
                return false;

            DialogueAsset asset = resolver(state.AssetId);
            if (asset == null)
            {
                Debug.LogWarning($"[Typecraft] Dialogue: cannot restore, asset {state.AssetId} not found");
                return false;
            }

            if (!asset.TryGetNode(state.NodeId, out _))
            {
                Debug.LogWarning($"[Typecraft] Dialogue '{asset.name}': cannot restore, node {state.NodeId} not found");
                return false;
            }

            Reset();
            _asset = asset;
            Advance(state.NodeId, countVisit: false);
            return true;
        }

        private void Advance(string nodeId, bool countVisit)
        {
            int generation = _generation;
            int steps = 0;

            while (true)
            {
                if (++steps > MaxAutoSteps)
                {
                    Fail($"dialogue '{_asset.name}' made {MaxAutoSteps} steps without output, last node {nodeId}", error: true);
                    return;
                }

                if (string.IsNullOrEmpty(nodeId))
                {
                    Fail($"dialogue '{_asset.name}' reached an empty link, add an End node");
                    return;
                }

                if (!_asset.TryGetNode(nodeId, out DialogueNode node) || node == null)
                {
                    Fail($"dialogue '{_asset.name}' links to missing node {nodeId}");
                    return;
                }

                if (NodeEntered != null)
                {
                    NodeEntered(_asset, node);
                    if (generation != _generation)
                        return;
                }

                switch (node)
                {
                    case StartNode start:
                        nodeId = start.Next;
                        break;

                    case LineNode line:
                        PresentLine(line, countVisit, generation);
                        return;

                    case ChoiceNode choice:
                        PresentChoices(choice, countVisit, generation);
                        return;

                    case WaitNode wait:
                        Current = DialogueStep.ForWait(_asset, wait.Id, wait.Seconds);
                        WaitRequested?.Invoke(wait.Seconds);
                        return;

                    case BranchNode branch:
                        nodeId = branch.Evaluate(_variables, Catalog);
                        break;

                    case SetVariablesNode set:
                        set.Apply(_variables, Catalog);
                        if (generation != _generation)
                            return;
                        nodeId = set.Next;
                        break;

                    case EventNode raised:
                        EventRaised?.Invoke(raised.EventName, raised.Argument);
                        if (generation != _generation)
                            return;
                        nodeId = raised.Next;
                        break;

                    case JumpNode jump:
                        DialogueAsset target = jump.Target != null ? jump.Target : _asset;
                        nodeId = string.IsNullOrEmpty(jump.TargetNode) ? target.StartNodeId : jump.TargetNode;
                        if (string.IsNullOrEmpty(nodeId))
                        {
                            Fail($"jump {jump.Id} in '{_asset.name}' targets '{target.name}' which has no start node");
                            return;
                        }

                        _asset = target;
                        break;

                    case EndNode _:
                        Finish();
                        return;

                    default:
                        Fail($"dialogue '{_asset.name}' has unsupported node {node}");
                        return;
                }
            }
        }

        private void PresentLine(LineNode node, bool countVisit, int generation)
        {
            LineVariant variant = node.SelectVariant(_variables, Catalog);
            string key = variant != null ? variant.Key : node.Key;
            string emotion = variant != null && !string.IsNullOrEmpty(variant.Emotion) ? variant.Emotion : node.Emotion;
            Sprite portrait = node.Speaker != null ? node.Speaker.PortraitFor(emotion) : null;

            var line = new DialogueLine(_asset, node.Id, node.Speaker, emotion, portrait, node.Table, key, node.Advance, node.Seconds);
            if (countVisit && !MarkVisited(node, generation))
                return;

            Current = DialogueStep.ForLine(line);
            LineStarted?.Invoke(line);
        }

        private void PresentChoices(ChoiceNode node, bool countVisit, int generation)
        {
            _choices.Clear();
            bool anyAvailable = false;
            List<ChoiceOption> options = node.Options;
            for (int i = 0; i < options.Count; i++)
            {
                ChoiceOption option = options[i];
                if (option == null)
                    continue;

                bool available = Condition.IsMet(option.Condition, _variables, Catalog);
                if (!available && option.WhenUnavailable == ChoiceUnavailable.Hide)
                    continue;

                anyAvailable |= available;
                _choices.Add(new DialogueChoice(_asset, node.Id, i, option.Table, option.Key, available));
            }

            if (!anyAvailable)
            {
                Fail($"choice {node.Id} in '{_asset.name}' has no available options");
                return;
            }

            if (countVisit && !MarkVisited(node, generation))
                return;

            _choiceNode = node;
            var presented = _choices.ToArray();
            Current = DialogueStep.ForChoices(_asset, node.Id, presented);
            ChoicesPresented?.Invoke(presented);
        }

        private bool MarkVisited(DialogueNode node, int generation)
        {
            if (!TrackVisits)
                return true;

            string name = VisitedVariable(_asset.Id, node.Id);
            int count = _variables.TryGet(name, out DialogueValue value) ? value.AsInt : 0;
            _variables.Set(name, DialogueValue.Int(count + 1));
            return generation == _generation;
        }

        private void Fail(string message, bool error = false)
        {
            if (error)
                Debug.LogError("[Typecraft] Dialogue: " + message);
            else
                Debug.LogWarning("[Typecraft] Dialogue: " + message);
            Finish();
        }

        private void Finish()
        {
            Reset();
            Ended?.Invoke();
        }

        private void Reset()
        {
            _generation++;
            _asset = null;
            _choiceNode = null;
            _choices.Clear();
            Current = default;
        }
    }
}
