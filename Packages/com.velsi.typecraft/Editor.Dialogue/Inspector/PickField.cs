using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace Velsi.Typecraft.Dialogue.Editor.Inspector
{
    public readonly struct PickItem
    {
        public PickItem(string value, string label, string group = null)
        {
            Value = value;
            Label = label;
            Group = group;
        }

        public string Value { get; }
        public string Label { get; }
        public string Group { get; }
    }

    public sealed class PickField : VisualElement
    {
        private readonly TextField _text;
        private readonly Func<IEnumerable<PickItem>> _items;
        private readonly string _title;

        public PickField(string label, string title, Func<IEnumerable<PickItem>> items)
        {
            _title = title;
            _items = items;
            AddToClassList("dlg-pick");

            _text = new TextField(label) { isDelayed = true };
            _text.AddToClassList("dlg-pick__text");
            _text.RegisterValueChangedCallback(evt => Changed?.Invoke(evt.newValue ?? string.Empty));
            Add(_text);

            var button = new Button(Open) { text = "▾", tooltip = "Выбрать из списка (с поиском)" };
            button.AddToClassList("dlg-pick__button");
            Add(button);
        }

        public event Action<string> Changed;

        public string Value
        {
            get => _text.value;
            set => _text.SetValueWithoutNotify(value ?? string.Empty);
        }

        public TextField Text => _text;

        public void Pick(string value) => _text.value = value ?? string.Empty;

        private void Open() => PickDropdown.Show(worldBound, _title, _items(), Pick);
    }

    public sealed class PickDropdown : AdvancedDropdown
    {
        private readonly string _title;
        private readonly List<PickItem> _items;
        private readonly Action<string> _picked;

        private PickDropdown(string title, IEnumerable<PickItem> items, Action<string> picked) : base(new AdvancedDropdownState())
        {
            _title = title;
            _items = items?.ToList() ?? new List<PickItem>();
            _picked = picked;
            minimumSize = new Vector2(340f, 320f);
        }

        public static void Show(Rect rect, string title, IEnumerable<PickItem> items, Action<string> picked) =>
            new PickDropdown(title, items, picked).Show(rect);

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(_title);
            var groups = new Dictionary<string, AdvancedDropdownItem>();
            foreach (PickItem item in _items)
            {
                AdvancedDropdownItem parent = root;
                if (!string.IsNullOrEmpty(item.Group) && !groups.TryGetValue(item.Group, out parent))
                {
                    parent = new AdvancedDropdownItem(item.Group);
                    groups[item.Group] = parent;
                    root.AddChild(parent);
                }

                parent.AddChild(new Entry(item.Label, item.Value));
            }

            if (_items.Count == 0)
                root.AddChild(new AdvancedDropdownItem("(пусто)") { enabled = false });

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is Entry entry)
                _picked?.Invoke(entry.Value);
        }

        private sealed class Entry : AdvancedDropdownItem
        {
            public Entry(string label, string value) : base(label)
            {
                Value = value;
            }

            public string Value { get; }
        }
    }
}
