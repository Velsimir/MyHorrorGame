using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Window.Effects;

namespace Velsi.Typecraft.Editor.Window.Markup
{
    public sealed class TagInspector : VisualElement
    {
        private static readonly Regex Token = new Regex(@"([^\s=]+)(?:=(""[^""]*""|\S+))?");

        private readonly List<string[]> _parameters = new List<string[]>();
        private int _start = -1;
        private int _length;
        private string _name;
        private string _value;

        public TagInspector()
        {
            AddToClassList("tc-tag");
            style.display = DisplayStyle.None;
        }

        public event Action<int, int, string> Changed;

        public static bool TryFindTag(string markup, int caret, out int start)
        {
            start = -1;
            if (string.IsNullOrEmpty(markup) || caret < 1 || caret > markup.Length)
                return false;

            int open = markup.LastIndexOf('<', caret - 1);
            if (open < 0)
                return false;

            int close = markup.IndexOf('>', open);
            if (close < caret - 1 || close < 0 || markup.IndexOf('<', open + 1, close - open) >= 0)
                return false;

            if (!IsEditable(TagName(markup.Substring(open + 1, close - open - 1))))
                return false;

            start = open;
            return true;
        }

        public void Edit(string markup, int start)
        {
            int close = start >= 0 && start < markup.Length && markup[start] == '<' ? markup.IndexOf('>', start) : -1;
            if (close < 0)
            {
                Hide();
                return;
            }

            string inner = markup.Substring(start + 1, close - start - 1);
            if (!IsEditable(TagName(inner)))
            {
                Hide();
                return;
            }

            _start = start;
            _length = close - start + 1;
            _parameters.Clear();
            _name = null;
            _value = null;

            foreach (Match match in Token.Matches(inner))
            {
                string value = match.Groups[2].Success ? match.Groups[2].Value : null;
                if (_name == null)
                {
                    _name = match.Groups[1].Value;
                    _value = value;
                }
                else
                {
                    _parameters.Add(new[] { match.Groups[1].Value, value });
                }
            }

            Build();
            style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _start = -1;
            style.display = DisplayStyle.None;
        }

        private static bool IsEditable(string name) =>
            !string.IsNullOrEmpty(name) && name[0] != '/' && name[0] != '#' &&
            (name[0] == '!' || name[0] == '+' || name[0] == '-' || EffectCatalog.IsEffectTag(name));

        private static string TagName(string inner)
        {
            int end = 0;
            while (end < inner.Length && inner[end] != '=' && inner[end] != ' ')
                end++;
            return inner.Substring(0, end);
        }

        private bool IsCommand => _name.StartsWith("!");

        private void Build()
        {
            Clear();

            var header = new VisualElement();
            header.AddToClassList("tc-tag__header");
            header.Add(new Label(Describe(_name)) { name = "tc-tag__title" });
            if (IsCommand)
                header.Add(new Button(Remove) { text = "Удалить", tooltip = "Убрать команду из текста" });
            header.Add(new Button(Hide) { text = "✕" });
            Add(header);

            if (_value != null)
                AddValueRow(IsCommand ? "сек" : "значение", _value, ValueRange(_name), changed =>
                {
                    _value = changed;
                    Emit();
                }, null);

            foreach (string[] parameter in _parameters)
            {
                string[] captured = parameter;
                AddValueRow(parameter[0], parameter[1] ?? string.Empty, new Vector2(0f, 5f), changed =>
                {
                    captured[1] = changed;
                    Emit();
                }, () =>
                {
                    _parameters.Remove(captured);
                    Emit();
                    Build();
                });
            }

            if (IsCommand)
                return;

            var add = new TextField { tooltip = "Новый параметр: имя=значение, Enter — добавить" };
            add.AddToClassList("tc-tag__add");
            add.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                    return;

                Match match = Token.Match(add.value ?? string.Empty);
                if (!match.Success)
                    return;

                _parameters.Add(new[] { match.Groups[1].Value, match.Groups[2].Success ? match.Groups[2].Value : null });
                Emit();
                Build();
            }, TrickleDown.TrickleDown);
            Add(add);
        }

        private void AddValueRow(string label, string value, Vector2 range, Action<string> changed, Action remove)
        {
            var row = new VisualElement();
            row.AddToClassList("tc-tag__row");
            row.Add(new Label(label) { name = "tc-tag__label" });

            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
            {
                var slider = new Slider(range.x, Mathf.Max(range.y, number * 2f)) { value = number };
                slider.AddToClassList("tc-tag__slider");
                var field = new FloatField { value = number };
                field.AddToClassList("tc-tag__number");

                slider.RegisterValueChangedCallback(evt =>
                {
                    float rounded = (float)Math.Round(evt.newValue, 3);
                    field.SetValueWithoutNotify(rounded);
                    changed(Format(rounded));
                });
                field.RegisterValueChangedCallback(evt =>
                {
                    slider.SetValueWithoutNotify(evt.newValue);
                    changed(Format(evt.newValue));
                });

                row.Add(slider);
                row.Add(field);
            }
            else
            {
                var text = new TextField { value = value, isDelayed = true };
                text.AddToClassList("tc-tag__text");
                text.RegisterValueChangedCallback(evt => changed(evt.newValue));
                row.Add(text);
            }

            if (remove != null)
                row.Add(new Button(remove) { text = "✕", tooltip = "Убрать параметр" });

            Add(row);
        }

        private void Emit()
        {
            if (_start < 0)
                return;

            var text = new StringBuilder("<").Append(_name);
            if (_value != null)
                text.Append('=').Append(_value);
            foreach (string[] parameter in _parameters)
            {
                text.Append(' ').Append(parameter[0]);
                if (parameter[1] != null)
                    text.Append('=').Append(parameter[1]);
            }
            text.Append('>');

            string result = text.ToString();
            Changed?.Invoke(_start, _length, result);
            _length = result.Length;
        }

        private void Remove()
        {
            if (_start >= 0)
                Changed?.Invoke(_start, _length, string.Empty);
            Hide();
        }

        private static string Format(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static Vector2 ValueRange(string name) => name switch
        {
            "!wait" => new Vector2(0f, 3f),
            "!delay" => new Vector2(0f, 0.3f),
            _ => new Vector2(0f, 5f)
        };

        private static string Describe(string name) => name switch
        {
            "!wait" => "Пауза",
            "!delay" => "Задержка между символами",
            _ when name.StartsWith("!") => "Команда " + name.Substring(1),
            _ when name.StartsWith("+") => "Появление " + name.Substring(1),
            _ when name.StartsWith("-") => "Исчезание " + name.Substring(1),
            _ => "Эффект " + name
        };
    }
}
