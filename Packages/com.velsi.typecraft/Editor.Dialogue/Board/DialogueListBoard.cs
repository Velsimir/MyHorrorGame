using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Dialogue.Editor.Model;
using Velsi.Typecraft.Editor.Window.Layout;

namespace Velsi.Typecraft.Dialogue.Editor.Board
{
    [DialogueBoardPriority(0, "Список")]
    public sealed class DialogueListBoard : IDialogueBoardView
    {
        private const float Indent = 18f;

        private readonly ScrollView _scroll = TypecraftLayout.VerticalScroll();
        private readonly Dictionary<string, VisualElement> _rows = new Dictionary<string, VisualElement>(StringComparer.Ordinal);
        private readonly Dictionary<string, DialogueIssueSeverity> _issues = new Dictionary<string, DialogueIssueSeverity>(StringComparer.Ordinal);

        private IDialogueBoardHost _host;
        private DialogueAsset _asset;
        private string _selected;
        private string _current;

        public DialogueListBoard()
        {
            _scroll.AddToClassList("dlg-list");
        }

        public VisualElement Element => _scroll;

        public void Bind(IDialogueBoardHost host) => _host = host;

        public void Show(DialogueAsset asset)
        {
            _asset = asset;
            _selected = null;
            Refresh();
        }

        public void Refresh()
        {
            _scroll.Clear();
            _rows.Clear();
            if (_asset == null)
                return;

            List<DialogueNode> ordered = DialogueEdits.FlowOrder(_asset, out Dictionary<string, int> depth).ToList();
            foreach (DialogueNode node in ordered)
                AddRow(node, depth[node.Id]);

            List<DialogueNode> rest = _asset.Nodes.Where(node => node != null && !depth.ContainsKey(node.Id ?? string.Empty)).ToList();
            if (rest.Count > 0)
            {
                var header = new Label("Не связаны со стартом");
                header.AddToClassList("tc-lib__section");
                _scroll.Add(header);
                foreach (DialogueNode node in rest)
                    AddRow(node, 0);
            }

            int missing = _asset.Nodes.Count(node => node == null);
            if (missing > 0)
            {
                var hint = new Label($"Нод без типа (класс удалён или переименован): {missing}");
                hint.AddToClassList("dlg-issues__warning");
                _scroll.Add(hint);
            }

            Highlight();
        }

        public void RefreshNode(string nodeId) => Refresh();

        public void SelectNode(string nodeId, bool frame)
        {
            _selected = nodeId;
            Highlight();
            if (frame && nodeId != null && _rows.TryGetValue(nodeId, out VisualElement row))
                _scroll.schedule.Execute(() => _scroll.ScrollTo(row));
        }

        public void SetIssues(IReadOnlyList<DialogueIssue> issues)
        {
            _issues.Clear();
            if (issues != null)
            {
                foreach (DialogueIssue issue in issues)
                {
                    if (string.IsNullOrEmpty(issue.NodeId))
                        continue;
                    if (!_issues.TryGetValue(issue.NodeId, out DialogueIssueSeverity current) || issue.Severity > current)
                        _issues[issue.NodeId] = issue.Severity;
                }
            }

            Highlight();
        }

        public void SetCurrent(string nodeId)
        {
            if (_current == nodeId)
                return;

            _current = nodeId;
            Highlight();
            if (nodeId != null && _rows.TryGetValue(nodeId, out VisualElement row))
                _scroll.schedule.Execute(() => _scroll.ScrollTo(row));
        }

        public Vector2 SuggestPosition()
        {
            if (_asset == null)
                return Vector2.zero;
            if (_selected != null && _asset.TryGetNode(_selected, out DialogueNode selected))
                return selected.Position + new Vector2(320f, 0f);

            float bottom = _asset.Nodes.Where(node => node != null).Select(node => node.Position.y).DefaultIfEmpty(-180f).Max();
            return new Vector2(0f, bottom + 180f);
        }

        public void Dispose()
        {
        }

        private void AddRow(DialogueNode node, int level)
        {
            var row = new VisualElement();
            row.AddToClassList("dlg-list__row");
            row.AddToClassList(DialogueNodeInfo.CategoryClass(node));
            row.style.marginLeft = level * Indent;

            var glyph = new Label(DialogueNodeInfo.Glyph(node.GetType()));
            glyph.AddToClassList("dlg-list__glyph");
            row.Add(glyph);

            var title = new Label(DialogueNodeInfo.Title(node));
            title.AddToClassList("dlg-list__title");
            row.Add(title);

            var summary = new Label(DialogueTexts.Summary(_asset, node, _host?.Locale, 90)) { enableRichText = false };
            summary.AddToClassList("dlg-list__summary");
            row.Add(summary);

            string id = node.Id;
            row.RegisterCallback<PointerDownEvent>(_ =>
            {
                _selected = id;
                Highlight();
                _host?.OnNodeSelected(id);
            });

            _scroll.Add(row);
            if (!string.IsNullOrEmpty(id))
                _rows[id] = row;
        }

        private void Highlight()
        {
            foreach (KeyValuePair<string, VisualElement> pair in _rows)
            {
                pair.Value.EnableInClassList("dlg-list__row--selected", pair.Key == _selected);
                pair.Value.EnableInClassList("dlg-node--current", pair.Key == _current);
                bool has = _issues.TryGetValue(pair.Key, out DialogueIssueSeverity severity);
                pair.Value.EnableInClassList("dlg-node--error", has && severity == DialogueIssueSeverity.Error);
                pair.Value.EnableInClassList("dlg-node--warning", has && severity == DialogueIssueSeverity.Warning);
            }
        }
    }
}
