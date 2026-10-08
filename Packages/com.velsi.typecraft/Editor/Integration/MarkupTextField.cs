using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Window;
using Velsi.Typecraft.Editor.Window.Markup;
using Velsi.Typecraft.Editor.Window.Preview;
using Velsi.Typecraft.Markup;

namespace Velsi.Typecraft.Editor
{
    public sealed class MarkupTextField : VisualElement, INotifyValueChanged<string>
    {
        private readonly Label _label;
        private readonly MarkupField _field;
        private readonly Button _previewToggle;
        private readonly VisualElement _previewHost;
        private readonly List<MarkupDiagnostic> _diagnostics = new List<MarkupDiagnostic>();
        private TextPreview _preview;
        private bool _showPreview;
        private string _value = string.Empty;

        public MarkupTextField() : this(null)
        {
        }

        public MarkupTextField(string label, bool showPreview = false)
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(TypecraftWindow.StylePath);
            if (styleSheet != null)
                styleSheets.Add(styleSheet);
            AddToClassList("tc-markup-field");

            var header = new VisualElement();
            header.AddToClassList("tc-markup-field__header");
            Add(header);

            _label = new Label(label);
            _label.AddToClassList("tc-markup-field__label");
            header.Add(_label);

            _previewToggle = new Button(() => ShowPreview = !ShowPreview) { text = "👁", tooltip = "Показать превью" };
            header.Add(_previewToggle);
            header.Add(new Button(OpenWindow) { text = "✎", tooltip = "Открыть в окне Typecraft" });

            _field = new MarkupField(MarkupCandidates.For);
            _field.MarkupChanged += OnEdited;
            Add(_field);

            _previewHost = new VisualElement();
            _previewHost.AddToClassList("tc-markup-field__preview");
            Add(_previewHost);

            RegisterCallback<AttachToPanelEvent>(_ => UpdatePreview());
            RegisterCallback<DetachFromPanelEvent>(_ => DisposePreview());
            ShowPreview = showPreview;
        }

        public event Action EditRequested;

        public string label
        {
            get => _label.text;
            set => _label.text = value;
        }

        public bool ShowPreview
        {
            get => _showPreview;
            set
            {
                _showPreview = value;
                _previewHost.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                _previewToggle.EnableInClassList("tc-markup-field__toggle--on", value);
                if (value)
                    UpdatePreview();
                else
                    DisposePreview();
            }
        }

        public string value
        {
            get => _value;
            set
            {
                string previous = _value;
                SetValueWithoutNotify(value);
                if (previous != _value)
                    Notify(previous);
            }
        }

        public void SetValueWithoutNotify(string newValue)
        {
            _value = newValue ?? string.Empty;
            if (_field.Value != _value)
                _field.SetValue(_value, notify: false);
            Validate();
        }

        private void OnEdited(string text)
        {
            string previous = _value;
            _value = text ?? string.Empty;
            Validate();
            if (previous != _value)
                Notify(previous);
        }

        private void Notify(string previous)
        {
            using ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(previous, _value);
            evt.target = this;
            SendEvent(evt);
        }

        private void Validate()
        {
            _diagnostics.Clear();
            Typecraft.RenderPreview(_value, Typecraft.Locale, Typecraft.Device, _diagnostics, null);
            _field.SetDiagnostics(_diagnostics);
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (!_showPreview || panel == null)
                return;

            if (_preview == null)
            {
                _preview = new TextPreview { Animate = false };
                _preview.Configure(null, null, null);
                _previewHost.Add(_preview.Element);
            }

            _preview.SetMarkup(_value, Typecraft.Locale, Typecraft.Device, null);
        }

        private void DisposePreview()
        {
            if (_preview == null)
                return;

            _previewHost.Clear();
            _preview.Dispose();
            _preview = null;
        }

        private void OpenWindow()
        {
            if (EditRequested != null)
                EditRequested.Invoke();
            else
                TypecraftEditor.Edit(_value, text => value = text, label);
        }
    }
}
