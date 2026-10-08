using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Dialogue.Editor.Board;
using Velsi.Typecraft.Dialogue.Editor.Model;
using Object = UnityEngine.Object;

namespace Velsi.Typecraft.Dialogue.Editor.Graph
{
    [DialogueBoardPriority(100, "Граф")]
    public sealed class DialogueGraphBoard : GraphView, IDialogueBoardView
    {
        private const string StylePath = "Packages/com.velsi.typecraft/Editor.Dialogue.Graph/DialogueGraph.uss";
        private const string MiniMapSetting = "Typecraft.Dialogues.MiniMap";
        private static readonly Vector2 ColumnStep = new Vector2(340f, 0f);
        private const float RowStep = 190f;

        private readonly Dictionary<string, DialogueGraphNode> _nodes = new Dictionary<string, DialogueGraphNode>(StringComparer.Ordinal);
        private readonly Dictionary<string, (DialogueIssueSeverity Severity, string Text)> _issues = new Dictionary<string, (DialogueIssueSeverity, string)>(StringComparer.Ordinal);
        private readonly MiniMap _miniMap;
        private readonly DialogueNodeSearch _search;
        private readonly DialogueEdgeListener _edgeListener;

        private IDialogueBoardHost _host;
        private DialogueAsset _asset;
        private bool _rebuilding;
        private bool _autoLayout;
        private bool _frameOnLayout;
        private Vector2 _pasteOffset;
        private Vector2 _mouse;
        private EditorWindow _window;
        private string _current;
        private (string From, string Port, string To) _pendingLink;

        public DialogueGraphBoard()
        {
            AddToClassList("dlg-graph");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (styleSheet != null)
                styleSheets.Add(styleSheet);

            SetupZoom(0.15f, 2f);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            _miniMap = new MiniMap { anchored = true };
            _miniMap.SetPosition(new Rect(8f, 8f, 180f, 120f));
            _miniMap.style.display = EditorPrefs.GetBool(MiniMapSetting, true) ? DisplayStyle.Flex : DisplayStyle.None;
            Add(_miniMap);

            _search = DialogueNodeSearch.Create(CreateAtScreen);
            _edgeListener = new DialogueEdgeListener(OnEdgeDroppedOutside);
            nodeCreationRequest = context => OpenSearch(context.screenMousePosition);
            graphViewChanged = OnGraphViewChanged;
            serializeGraphElements = Serialize;
            canPasteSerializedData = DialogueEdits.CanPaste;
            unserializeAndPaste = Paste;

            RegisterCallback<PointerMoveEvent>(evt => _mouse = evt.position);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (!_frameOnLayout || _nodes.Count == 0)
                    return;
                _frameOnLayout = false;
                schedule.Execute(() => FrameAll());
            });
        }

        public VisualElement Element => this;

        public void Bind(IDialogueBoardHost host) => _host = host;

        public void Show(DialogueAsset asset)
        {
            _asset = asset;
            _pasteOffset = Vector2.zero;
            Rebuild(null);
            _frameOnLayout = true;
            if (layout.width > 0f)
                schedule.Execute(() =>
                {
                    _frameOnLayout = false;
                    FrameAll();
                });
        }

        public void Refresh() => Rebuild(selection.OfType<DialogueGraphNode>().Select(node => node.Id).ToList());

        public void RefreshNode(string nodeId) => Refresh();

        public void SelectNode(string nodeId, bool frame)
        {
            ClearSelection();
            if (nodeId == null || !_nodes.TryGetValue(nodeId, out DialogueGraphNode node))
                return;

            AddToSelection(node);
            if (frame)
                schedule.Execute(() => FrameSelection());
        }

        public void SetIssues(IReadOnlyList<DialogueIssue> issues)
        {
            _issues.Clear();
            if (issues != null)
            {
                foreach (IGrouping<string, DialogueIssue> group in issues.Where(issue => !string.IsNullOrEmpty(issue.NodeId)).GroupBy(issue => issue.NodeId))
                    _issues[group.Key] = (group.Max(issue => issue.Severity), string.Join("\n", group.Select(DialogueIssueText.Describe).Distinct()));
            }

            ApplyIssues();
        }

        public void SetCurrent(string nodeId)
        {
            if (_current != null && _nodes.TryGetValue(_current, out DialogueGraphNode previous))
                previous.RemoveFromClassList("dlg-node--current");

            _current = nodeId;
            if (nodeId == null || !_nodes.TryGetValue(nodeId, out DialogueGraphNode node))
                return;

            node.AddToClassList("dlg-node--current");
            schedule.Execute(() => Reveal(node));
        }

        public Vector2 SuggestPosition()
        {
            Vector2 center = contentViewContainer.WorldToLocal(worldBound.center);
            return center - new Vector2(120f, 60f);
        }

        public void Dispose()
        {
            if (_search != null)
                Object.DestroyImmediate(_search);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) =>
            ports.Where(port => port.direction != startPort.direction && port.node != startPort.node).ToList();

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is GraphView && _asset != null)
            {
                Vector2 position = contentViewContainer.WorldToLocal(evt.mousePosition);
                foreach (Type type in DialogueNodeInfo.Types)
                {
                    Type captured = type;
                    evt.menu.AppendAction($"Новая нода/{DialogueNodeInfo.Glyph(type)} {DialogueNodeInfo.Title(type)}", _ => CreateAt(captured, position));
                }

                evt.menu.AppendAction("Упорядочить", _ => Arrange());
                evt.menu.AppendAction("Показать всё", _ => FrameAll());
                evt.menu.AppendAction("Мини-карта", _ => ToggleMiniMap(), _ => _miniMap.style.display == DisplayStyle.None ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Checked);
                evt.menu.AppendSeparator();
            }

            base.BuildContextualMenu(evt);
        }

        private void Rebuild(IReadOnlyCollection<string> keepSelection)
        {
            _rebuilding = true;
            try
            {
                DeleteElements(graphElements.ToList());
                _nodes.Clear();

                if (_asset == null)
                    return;

                string locale = _host?.Locale;
                List<DialogueNode> models = _asset.Nodes.Where(node => node != null && !string.IsNullOrEmpty(node.Id)).ToList();
                Dictionary<string, Vector2> automatic = AutoLayout(models);
                _autoLayout = automatic != null;

                foreach (DialogueNode model in models)
                {
                    if (_nodes.ContainsKey(model.Id))
                        continue;

                    var view = new DialogueGraphNode(_asset, model, locale, id => _host?.OnNodeSelected(id), _edgeListener);
                    if (automatic != null)
                        view.SetPosition(new Rect(automatic[model.Id], Vector2.zero));
                    _nodes[model.Id] = view;
                    AddElement(view);
                }

                foreach (DialogueNode model in models)
                {
                    if (!_nodes.TryGetValue(model.Id, out DialogueGraphNode source))
                        continue;

                    foreach (DialogueLink link in model.GetLinks())
                    {
                        if (link.IsEmpty || !source.Outputs.TryGetValue(link.Port, out Port output) || !_nodes.TryGetValue(link.Target, out DialogueGraphNode target) || target.Input == null)
                            continue;

                        AddElement(output.ConnectTo(target.Input));
                    }
                }

                ApplyIssues();
                if (_current != null && _nodes.TryGetValue(_current, out DialogueGraphNode current))
                    current.AddToClassList("dlg-node--current");
                if (keepSelection != null)
                {
                    foreach (string id in keepSelection)
                    {
                        if (_nodes.TryGetValue(id, out DialogueGraphNode node))
                            AddToSelection(node);
                    }
                }
            }
            finally
            {
                _rebuilding = false;
            }
        }

        private void ApplyIssues()
        {
            foreach (KeyValuePair<string, DialogueGraphNode> pair in _nodes)
            {
                if (_issues.TryGetValue(pair.Key, out (DialogueIssueSeverity Severity, string Text) issue) && issue.Severity != DialogueIssueSeverity.Info)
                    pair.Value.SetIssue(issue.Severity, issue.Text);
                else
                    pair.Value.SetIssue(null, null);
            }
        }

        private Dictionary<string, Vector2> AutoLayout(List<DialogueNode> models)
        {
            if (models.Count < 2 || models.Any(node => node.Position != models[0].Position))
                return null;

            return Layout(models);
        }

        private Dictionary<string, Vector2> Layout(List<DialogueNode> models)
        {
            var level = new Dictionary<string, int>(StringComparer.Ordinal);
            var queue = new Queue<string>();
            foreach (DialogueNode start in models.Where(node => node is StartNode || node.Id == _asset.StartNodeId))
            {
                if (level.ContainsKey(start.Id))
                    continue;
                level[start.Id] = 0;
                queue.Enqueue(start.Id);
            }

            while (queue.Count > 0)
            {
                string id = queue.Dequeue();
                if (!_asset.TryGetNode(id, out DialogueNode node))
                    continue;

                foreach (DialogueLink link in node.GetLinks())
                {
                    if (link.IsEmpty || level.ContainsKey(link.Target) || !_asset.TryGetNode(link.Target, out _))
                        continue;
                    level[link.Target] = level[id] + 1;
                    queue.Enqueue(link.Target);
                }
            }

            int last = level.Count > 0 ? level.Values.Max() + 1 : 0;
            var rows = new Dictionary<int, int>();
            var positions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
            foreach (DialogueNode node in models)
            {
                int column = level.TryGetValue(node.Id, out int found) ? found : last;
                rows.TryGetValue(column, out int row);
                rows[column] = row + 1;
                positions[node.Id] = ColumnStep * column + new Vector2(0f, row * RowStep);
            }

            return positions;
        }

        private void Arrange()
        {
            if (_asset == null)
                return;

            Dictionary<string, Vector2> positions = Layout(_asset.Nodes.Where(node => node != null && !string.IsNullOrEmpty(node.Id)).ToList());
            DialogueEdits.Move(_asset, positions.Select(pair => (pair.Key, pair.Value)).ToList());
            Refresh();
            _host?.OnBoardEdited();
            schedule.Execute(() => FrameAll());
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_rebuilding || _asset == null)
                return change;

            List<string> removedNodes = change.elementsToRemove?.OfType<DialogueGraphNode>().Select(node => node.Id).ToList() ?? new List<string>();
            List<Edge> removedEdges = change.elementsToRemove?.OfType<Edge>().ToList() ?? new List<Edge>();
            List<Edge> createdEdges = change.edgesToCreate ?? new List<Edge>();
            List<DialogueGraphNode> moved = change.movedElements?.OfType<DialogueGraphNode>().ToList() ?? new List<DialogueGraphNode>();

            if (removedNodes.Count + removedEdges.Count + createdEdges.Count + moved.Count == 0)
                return change;

            string undoName = removedNodes.Count > 0 ? "удаление нод" : createdEdges.Count + removedEdges.Count > 0 ? "связь" : "перемещение нод";
            DialogueEdits.Change(_asset, undoName, () =>
            {
                foreach (Edge edge in removedEdges)
                {
                    if (Endpoints(edge, out DialogueNode source, out string port, out string target) && !removedNodes.Contains(source.Id) && DialogueEdits.GetLink(source, port) == target)
                        DialogueEdits.SetLink(source, port, null);
                }

                foreach (Edge edge in createdEdges)
                {
                    if (Endpoints(edge, out DialogueNode source, out string port, out string target))
                        DialogueEdits.SetLink(source, port, target);
                }

                if (removedNodes.Count > 0)
                    DialogueEdits.RemoveWithoutUndo(_asset, removedNodes);

                IEnumerable<DialogueGraphNode> positioned = _autoLayout && moved.Count > 0 ? _nodes.Values : moved;
                foreach (DialogueGraphNode view in positioned)
                {
                    if (_asset.TryGetNode(view.Id, out DialogueNode node))
                        node.Position = view.Placed;
                }
            });

            if (moved.Count > 0)
                _autoLayout = false;
            foreach (string id in removedNodes)
                _nodes.Remove(id);

            _host?.OnBoardEdited();
            return change;
        }

        private bool Endpoints(Edge edge, out DialogueNode source, out string port, out string target)
        {
            source = null;
            port = edge?.output?.userData as string;
            target = (edge?.input?.node as DialogueGraphNode)?.Id;
            string sourceId = (edge?.output?.node as DialogueGraphNode)?.Id;
            return port != null && target != null && sourceId != null && _asset.TryGetNode(sourceId, out source);
        }

        private string Serialize(IEnumerable<GraphElement> elements)
        {
            _pasteOffset = Vector2.zero;
            return DialogueEdits.Copy(_asset, elements.OfType<DialogueGraphNode>().Select(node => node.Id));
        }

        private void Paste(string operation, string data)
        {
            if (_asset == null)
                return;

            _pasteOffset += new Vector2(40f, 40f);
            List<string> created = DialogueEdits.Paste(_asset, data, _pasteOffset);
            if (created.Count == 0)
                return;

            schedule.Execute(() =>
            {
                Rebuild(null);
                ClearSelection();
                foreach (string id in created)
                {
                    if (_nodes.TryGetValue(id, out DialogueGraphNode node))
                        AddToSelection(node);
                }

                _host?.OnBoardEdited(created);
            });
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Space || _asset == null || evt.actionKey || evt.altKey || panel?.focusController?.focusedElement is TextField)
                return;

            evt.StopPropagation();
            _window = EditorWindow.focusedWindow;
            OpenSearch(_window != null ? _window.position.position + _mouse : _mouse);
        }

        private void Reveal(DialogueGraphNode node)
        {
            if (node.panel == null || layout.width <= 0f)
                return;

            Rect view = worldBound;
            Rect bounds = node.worldBound;
            if (view.Contains(bounds.min) && view.Contains(bounds.max))
                return;

            float zoom = scale;
            Vector2 center = node.layout.center;
            UpdateViewTransform(layout.size / 2f - center * zoom, new Vector3(zoom, zoom, 1f));
        }

        private void OnEdgeDroppedOutside(Edge edge, Vector2 position)
        {
            if (_asset == null || edge == null)
                return;

            string from = (edge.output?.node as DialogueGraphNode)?.Id;
            string to = (edge.input?.node as DialogueGraphNode)?.Id;
            if (edge.output != null && edge.input == null && from != null)
                _pendingLink = (from, edge.output.userData as string, null);
            else if (edge.input != null && edge.output == null && to != null)
                _pendingLink = (null, null, to);
            else
                return;

            _window = EditorWindow.focusedWindow;
            SearchWindow.Open(new SearchWindowContext(_window != null ? _window.position.position + position : position), _search);
        }

        private void OpenSearch(Vector2 screenPosition)
        {
            if (_asset == null)
                return;

            _pendingLink = default;

            _window = EditorWindow.focusedWindow;
            SearchWindow.Open(new SearchWindowContext(screenPosition), _search);
        }

        private void CreateAtScreen(Type type, Vector2 screenPosition)
        {
            Vector2 world = screenPosition;
            if (_window != null)
            {
                VisualElement root = _window.rootVisualElement;
                world = root.ChangeCoordinatesTo(root.parent, screenPosition - _window.position.position);
            }

            CreateAt(type, contentViewContainer.WorldToLocal(world));
        }

        private void CreateAt(Type type, Vector2 position)
        {
            (string from, string port, string to) = _pendingLink;
            _pendingLink = default;
            CreateLinked(type, position, from, port, to);
        }

        private void CreateLinked(Type type, Vector2 position, string from, string port, string to)
        {
            if (_asset == null)
                return;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            bool hasInput = type != typeof(StartNode);
            DialogueNode node = DialogueEdits.AddNode(_asset, type, position, hasInput ? from : null, hasInput ? port : null);
            string output = to != null ? node.GetLinks().Select(link => link.Port).FirstOrDefault() : null;
            if (output != null)
                DialogueEdits.Link(_asset, node.Id, output, to);
            Undo.CollapseUndoOperations(group);

            Rebuild(null);
            SelectNode(node.Id, false);
            _host?.OnBoardEdited(new[] { node.Id });
        }

        private void ToggleMiniMap()
        {
            bool show = _miniMap.style.display == DisplayStyle.None;
            _miniMap.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            EditorPrefs.SetBool(MiniMapSetting, show);
        }
    }
}
