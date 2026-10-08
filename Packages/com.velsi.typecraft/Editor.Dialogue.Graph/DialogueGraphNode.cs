using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Dialogue.Editor.Model;

namespace Velsi.Typecraft.Dialogue.Editor.Graph
{
    public sealed class DialogueGraphNode : Node
    {
        private static readonly Color SpeechPort = new Color(0.36f, 0.62f, 0.86f);
        private static readonly Color LogicPort = new Color(0.66f, 0.52f, 0.86f);
        private static readonly Color FlowPort = new Color(0.7f, 0.7f, 0.7f);

        private readonly Action<string> _selected;

        public DialogueGraphNode(DialogueAsset asset, DialogueNode model, string locale, Action<string> selected, IEdgeConnectorListener listener)
        {
            _selected = selected;
            Id = model.Id;
            AddToClassList("dlg-gnode");
            AddToClassList(DialogueNodeInfo.CategoryClass(model));
            title = $"{DialogueNodeInfo.Glyph(model.GetType())}  {DialogueNodeInfo.Title(model)}";
            if (asset.StartNodeId == model.Id && model is StartNode)
                AddToClassList("dlg-gnode--start");

            Color portColor = DialogueNodeInfo.Category(model.GetType()) switch
            {
                NodeCategory.Speech => SpeechPort,
                NodeCategory.Logic => LogicPort,
                _ => FlowPort
            };

            if (DialogueNodeInfo.HasInput(model))
            {
                Input = DialoguePort.Create(Direction.Input, Port.Capacity.Multi, listener);
                Input.portName = string.Empty;
                Input.portColor = portColor;
                inputContainer.Add(Input);
            }

            foreach (DialogueLink link in model.GetLinks())
            {
                Port port = DialoguePort.Create(Direction.Output, Port.Capacity.Single, listener);
                port.portName = DialogueNodeInfo.PortTitle(model, link.Port, locale, asset.DefaultTable);
                port.userData = link.Port;
                port.portColor = portColor;
                port.tooltip = port.portName;
                Outputs[link.Port] = port;
                outputContainer.Add(port);
            }

            BuildBody(asset, model, locale);
            RefreshExpandedState();
            RefreshPorts();
            SetPosition(new Rect(model.Position, Vector2.zero));
        }

        public string Id { get; }
        public Port Input { get; }
        public Dictionary<string, Port> Outputs { get; } = new Dictionary<string, Port>(StringComparer.Ordinal);

        public Vector2 Placed => style.left.keyword == StyleKeyword.Undefined && style.top.keyword == StyleKeyword.Undefined
            ? new Vector2(style.left.value.value, style.top.value.value)
            : GetPosition().position;

        public void SetIssue(DialogueIssueSeverity? severity, string tooltipText)
        {
            EnableInClassList("dlg-node--error", severity == DialogueIssueSeverity.Error);
            EnableInClassList("dlg-node--warning", severity == DialogueIssueSeverity.Warning);
            tooltip = tooltipText ?? string.Empty;
        }

        public override void OnSelected()
        {
            base.OnSelected();
            _selected?.Invoke(Id);
        }

        private void BuildBody(DialogueAsset asset, DialogueNode model, string locale)
        {
            var body = new VisualElement();
            body.AddToClassList("dlg-gnode__body");

            switch (model)
            {
                case LineNode line:
                    string table = asset.ResolveTable(line.Table);
                    string speaker = DialogueTexts.SpeakerName(line.Speaker, table, locale);
                    if (!string.IsNullOrEmpty(speaker) || !string.IsNullOrEmpty(line.Emotion))
                        body.Add(Text(string.IsNullOrEmpty(line.Emotion) ? speaker : $"{speaker} · {line.Emotion}", "dlg-gnode__speaker"));
                    string text = DialogueTexts.Plain(table, line.Key, locale, 140);
                    body.Add(Text(string.IsNullOrEmpty(text) ? (string.IsNullOrEmpty(line.Key) ? "Нет ключа" : "⚠ нет текста") : text, "dlg-gnode__text"));
                    body.Add(Text(string.IsNullOrEmpty(line.Key) ? "—" : line.Key, "dlg-gnode__key"));
                    if (line.Variants.Count > 0)
                        body.Add(Text($"вариантов по условию: {line.Variants.Count}", "dlg-gnode__note"));
                    break;
                case SetVariablesNode set:
                    foreach (VariableAssignment assignment in set.Assignments.Where(entry => entry != null))
                        body.Add(Text(DialogueTexts.Assignment(assignment), "dlg-gnode__text"));
                    break;
                case ChoiceNode _:
                case BranchNode _:
                case StartNode _:
                case EndNode _:
                    break;
                default:
                    string summary = DialogueTexts.Summary(asset, model, locale, 80);
                    if (!string.IsNullOrEmpty(summary))
                        body.Add(Text(summary, "dlg-gnode__text"));
                    break;
            }

            if (body.childCount > 0)
                extensionContainer.Add(body);
        }

        private static Label Text(string text, string className)
        {
            var label = new Label(text) { enableRichText = false };
            label.AddToClassList(className);
            return label;
        }
    }
}
