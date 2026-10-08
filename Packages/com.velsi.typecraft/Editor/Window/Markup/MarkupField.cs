using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Markup;

namespace Velsi.Typecraft.Editor.Window.Markup
{
    public sealed class MarkupField : VisualElement
    {
        private const int MaxSuggestions = 9;
        private static readonly Regex OpenToken = new Regex(@"(\{[\w./:-]*|\[/?[\w.-]*|<[\w!+\-#/]*)$");

        private readonly TextField _field;
        private readonly Label _highlight;
        private readonly VisualElement _suggestionsBox;
        private readonly List<string> _suggestions = new List<string>();
        private readonly Func<char, IEnumerable<string>> _candidates;
        private readonly VisualElement _highlightClip;
        private TextElement _textElement;
        private int _suggestionIndex;
        private int _tokenStart = -1;
        private bool _suppressSuggestions;

        public MarkupField(Func<char, IEnumerable<string>> candidates)
        {
            _candidates = candidates;
            AddToClassList("tc-markup");

            _field = new TextField { multiline = true, isDelayed = false };
            _field.AddToClassList("tc-markup__field");
            _field.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _field.selectAllOnFocus = false;
            _field.selectAllOnMouseUp = false;
            Add(_field);

            _highlightClip = new VisualElement { pickingMode = PickingMode.Ignore };
            _highlightClip.AddToClassList("tc-markup__clip");
            Add(_highlightClip);

            _highlight = new Label { enableRichText = true, pickingMode = PickingMode.Ignore };
            _highlight.AddToClassList("tc-markup__highlight");
            _highlightClip.Add(_highlight);

            _suggestionsBox = new VisualElement();
            _suggestionsBox.AddToClassList("tc-suggest");
            _suggestionsBox.style.display = DisplayStyle.None;
            Add(_suggestionsBox);

            ApplyMonospace(_highlight);
            _field.RegisterCallback<GeometryChangedEvent>(_ => AttachHighlight());
            schedule.Execute(AttachHighlight).Every(50);
            _field.RegisterValueChangedCallback(OnValueChanged);
            _field.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _field.RegisterCallback<FocusOutEvent>(_ => schedule.Execute(HideSuggestions).StartingIn(150));
            _field.RegisterCallback<PointerUpEvent>(_ => SelectionChanged?.Invoke(Selection));
            _field.RegisterCallback<KeyUpEvent>(_ => SelectionChanged?.Invoke(Selection));
        }

        public event Action<string> MarkupChanged;
        public event Action<TextRange> SelectionChanged;
        public event Action ApplyRequested;

        public string Value => _field.value ?? string.Empty;
        public TextRange Selection => new TextRange(_field.cursorIndex, _field.selectIndex);
        public int Caret => _field.cursorIndex;

        public void SetValue(string value, bool notify)
        {
            if (notify)
                _field.value = value;
            else
                _field.SetValueWithoutNotify(value);
        }

        public void SetDiagnostics(IReadOnlyList<MarkupDiagnostic> diagnostics) =>
            _highlight.text = MarkupHighlighter.ToRichText(Value, diagnostics);

        public void Select(TextRange range) => _field.SelectRange(range.End, range.Start);

        public void SetReadOnly(bool readOnly) => _field.isReadOnly = readOnly;

        private void AttachHighlight()
        {
            if (_textElement == null)
            {
                _textElement = _field.Q<TextElement>(className: "unity-text-element__selectable");
                if (_textElement == null)
                    return;

                ApplyMonospace(_textElement);
                _textElement.AddToClassList("tc-markup__text");
            }

            VisualElement input = _textElement.parent;
            while (input != null && input.parent != _field)
                input = input.parent;

            Rect clip = input != null ? this.WorldToLocal(input.worldBound) : this.WorldToLocal(_field.worldBound);
            _highlightClip.style.left = clip.x;
            _highlightClip.style.top = clip.y;
            _highlightClip.style.width = clip.width;
            _highlightClip.style.height = clip.height;

            Rect text = _highlightClip.WorldToLocal(_textElement.worldBound);
            _highlight.style.left = text.x;
            _highlight.style.top = text.y;
            _highlight.style.width = text.width;
            _highlight.style.height = text.height;
        }

        private static void ApplyMonospace(VisualElement element)
        {
            if (EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") is Font font)
                element.style.unityFontDefinition = FontDefinition.FromFont(font);
            element.style.fontSize = 12;
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            MarkupChanged?.Invoke(evt.newValue);
            if (!_suppressSuggestions)
                UpdateSuggestions();
            _suppressSuggestions = false;
            schedule.Execute(AttachHighlight);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if ((evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) && (evt.ctrlKey || evt.commandKey))
            {
                ApplyRequested?.Invoke();
                evt.StopImmediatePropagation();
                return;
            }

            if (_suggestions.Count == 0)
                return;

            switch (evt.keyCode)
            {
                case KeyCode.DownArrow:
                    MoveSuggestion(1);
                    break;
                case KeyCode.UpArrow:
                    MoveSuggestion(-1);
                    break;
                case KeyCode.Tab:
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    AcceptSuggestion(_suggestions[_suggestionIndex]);
                    break;
                case KeyCode.Escape:
                    HideSuggestions();
                    break;
                default:
                    return;
            }

            evt.StopImmediatePropagation();
            _field.focusController?.IgnoreEvent(evt);
        }

        private void UpdateSuggestions()
        {
            int caret = _field.cursorIndex;
            string before = Value.Substring(0, Mathf.Clamp(caret, 0, Value.Length));
            Match match = OpenToken.Match(before);

            if (!match.Success)
            {
                HideSuggestions();
                return;
            }

            _tokenStart = match.Index;
            string typed = match.Value.Substring(1);
            char opener = match.Value[0];

            _suggestions.Clear();
            _suggestions.AddRange(_candidates(opener)
                .Where(candidate => candidate.IndexOf(typed, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(candidate => candidate.StartsWith(typed, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(candidate => candidate.Length)
                .Take(MaxSuggestions));

            if (_suggestions.Count == 0 || (_suggestions.Count == 1 && _suggestions[0] == typed))
            {
                HideSuggestions();
                return;
            }

            _suggestionIndex = 0;
            RebuildSuggestionList();

            Vector2 cursor = _field.cursorPosition;
            _suggestionsBox.style.left = Mathf.Max(0, cursor.x);
            _suggestionsBox.style.top = cursor.y + 18;
            _suggestionsBox.style.display = DisplayStyle.Flex;
        }

        private void RebuildSuggestionList()
        {
            _suggestionsBox.Clear();
            for (int i = 0; i < _suggestions.Count; i++)
            {
                string suggestion = _suggestions[i];
                var row = new Label(suggestion);
                row.AddToClassList("tc-suggest__row");
                if (i == _suggestionIndex)
                    row.AddToClassList("tc-suggest__row--selected");
                row.RegisterCallback<PointerDownEvent>(evt =>
                {
                    AcceptSuggestion(suggestion);
                    evt.StopPropagation();
                });
                _suggestionsBox.Add(row);
            }
        }

        private void MoveSuggestion(int delta)
        {
            _suggestionIndex = (_suggestionIndex + delta + _suggestions.Count) % _suggestions.Count;
            RebuildSuggestionList();
        }

        private void AcceptSuggestion(string suggestion)
        {
            if (_tokenStart < 0)
                return;

            string value = Value;
            int caret = Mathf.Clamp(_field.cursorIndex, 0, value.Length);
            char opener = value[_tokenStart];
            char closer = opener == '{' ? '}' : opener == '[' ? ']' : '>';

            int tokenEnd = caret;
            if (tokenEnd < value.Length && value[tokenEnd] == closer)
                tokenEnd++;

            string insertion = opener + suggestion + closer;
            string updated = value.Substring(0, _tokenStart) + insertion + value.Substring(tokenEnd);
            int newCaret = _tokenStart + insertion.Length;

            HideSuggestions();
            _suppressSuggestions = true;
            _field.value = updated;
            _field.SelectRange(newCaret, newCaret);
        }

        private void HideSuggestions()
        {
            _suggestions.Clear();
            _tokenStart = -1;
            _suggestionsBox.style.display = DisplayStyle.None;
        }
    }
}
