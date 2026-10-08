using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Velsi.Typecraft.Dialogue.Editor.Model;

namespace Velsi.Typecraft.Dialogue.Editor.Playback
{
    public sealed class DialogueIssuesPanel : VisualElement
    {
        private readonly Action<DialogueAsset, string> _reveal;
        private readonly Label _summary;
        private readonly Button _back;
        private readonly ScrollView _list;
        private readonly List<(DialogueAsset Asset, List<DialogueIssue> Issues)> _all = new List<(DialogueAsset, List<DialogueIssue>)>();

        private DialogueAsset _asset;
        private IReadOnlyList<DialogueIssue> _issues = Array.Empty<DialogueIssue>();
        private bool _showAll;

        public DialogueIssuesPanel(Action<DialogueAsset, string> reveal)
        {
            _reveal = reveal;
            AddToClassList("dlg-problems");

            var header = new VisualElement();
            header.AddToClassList("dlg-row");
            header.AddToClassList("dlg-play__header");
            _summary = new Label();
            _summary.AddToClassList("dlg-problems__summary");
            header.Add(_summary);
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            header.Add(spacer);
            _back = new Button(() => ShowCurrent(_asset, _issues)) { text = "Только текущий", tooltip = "Проблемы выбранного диалога" };
            header.Add(_back);
            header.Add(new Button(CheckAll) { text = "Проверить все", tooltip = "Проверить все диалоги проекта" });
            Add(header);

            _list = new ScrollView();
            _list.AddToClassList("dlg-problems__list");
            Add(_list);
        }

        public bool ShowsAll => _showAll;
        public IReadOnlyList<(DialogueAsset Asset, List<DialogueIssue> Issues)> AllResults => _all;
        public string Summary => _summary.text;

        public void ShowCurrent(DialogueAsset asset, IReadOnlyList<DialogueIssue> issues)
        {
            _showAll = false;
            _asset = asset;
            _issues = issues ?? Array.Empty<DialogueIssue>();
            _back.style.display = DisplayStyle.None;
            _list.Clear();

            if (asset == null)
            {
                _summary.text = "Диалог не выбран";
                return;
            }

            int errors = DialogueChecks.Errors(_issues);
            int warnings = DialogueChecks.Warnings(_issues);
            _summary.text = errors + warnings == 0 ? $"«{asset.name}»: проблем нет ✓" : $"«{asset.name}»: ошибок {errors}, предупреждений {warnings}";
            foreach (DialogueIssue issue in Sorted(_issues))
                AddRow(asset, issue, false);
        }

        public void Refresh(DialogueAsset asset, IReadOnlyList<DialogueIssue> issues)
        {
            if (_showAll)
            {
                _asset = asset;
                _issues = issues ?? Array.Empty<DialogueIssue>();
                return;
            }

            ShowCurrent(asset, issues);
        }

        public void CheckAll()
        {
            _showAll = true;
            _back.style.display = DisplayStyle.Flex;
            _all.Clear();
            _list.Clear();

            DialogueVariableCatalog catalog = DialogueProject.MergedCatalog();
            foreach (DialogueAsset asset in DialogueProject.Dialogues())
            {
                var issues = new List<DialogueIssue>();
                DialogueChecks.Run(asset, catalog, issues);
                _all.Add((asset, issues));
            }

            int broken = _all.Count(entry => DialogueChecks.Errors(entry.Issues) > 0);
            int warned = _all.Count(entry => DialogueChecks.Errors(entry.Issues) == 0 && DialogueChecks.Warnings(entry.Issues) > 0);
            int errors = _all.Sum(entry => DialogueChecks.Errors(entry.Issues));
            int warnings = _all.Sum(entry => DialogueChecks.Warnings(entry.Issues));
            _summary.text = $"Диалогов: {_all.Count} · с ошибками: {broken} · только с предупреждениями: {warned} · ошибок {errors}, предупреждений {warnings}";

            foreach ((DialogueAsset asset, List<DialogueIssue> issues) in _all.OrderByDescending(entry => DialogueChecks.Errors(entry.Issues)).ThenByDescending(entry => entry.Issues.Count).ThenBy(entry => entry.Asset.name))
            {
                var title = new Label(issues.Count == 0 ? $"{asset.name} — ✓" : $"{asset.name} — ошибок {DialogueChecks.Errors(issues)}, предупреждений {DialogueChecks.Warnings(issues)}");
                title.AddToClassList("dlg-problems__dialogue");
                DialogueAsset captured = asset;
                title.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0)
                        _reveal(captured, null);
                });
                _list.Add(title);
                foreach (DialogueIssue issue in Sorted(issues))
                    AddRow(asset, issue, true);
            }
        }

        private static IEnumerable<DialogueIssue> Sorted(IEnumerable<DialogueIssue> issues) =>
            issues.OrderByDescending(issue => issue.Severity).ThenBy(issue => issue.Kind);

        private void AddRow(DialogueAsset asset, DialogueIssue issue, bool indent)
        {
            string where = asset != null && issue.NodeId != null && asset.TryGetNode(issue.NodeId, out DialogueNode node)
                ? $"{DialogueNodeInfo.Glyph(node.GetType())} {DialogueNodeInfo.Title(node)} #{DialogueTexts.ShortId(node.Id)}"
                : "Диалог";

            var row = new VisualElement();
            row.AddToClassList("dlg-row");
            row.AddToClassList("dlg-problems__row");
            row.EnableInClassList("dlg-problems__row--indent", indent);
            var mark = new Label(DialogueIssueText.Mark(issue.Severity));
            mark.AddToClassList(issue.Severity == DialogueIssueSeverity.Error ? "dlg-issues__error" : "dlg-issues__warning");
            mark.AddToClassList("dlg-problems__mark");
            row.Add(mark);
            var place = new Label(where);
            place.AddToClassList("dlg-problems__where");
            row.Add(place);
            var text = new Label(DialogueIssueText.Describe(issue)) { enableRichText = false, tooltip = issue.Message };
            text.AddToClassList("dlg-problems__text");
            row.Add(text);

            string nodeId = issue.NodeId;
            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0)
                    _reveal(asset, nodeId);
            });
            _list.Add(row);
        }
    }
}
