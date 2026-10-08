using System;
using UnityEngine.UIElements;

namespace Velsi.Typecraft.Dialogue.Editor.Inspector
{
    public sealed class DialogueValueField : VisualElement
    {
        private readonly string _label;
        private DialogueValue _value;

        public DialogueValueField(string label)
        {
            _label = label;
            AddToClassList("dlg-value");
        }

        public event Action<DialogueValue> Changed;

        public DialogueValue Value => _value;

        public void SetValue(DialogueValue value)
        {
            _value = value;
            Clear();
            switch (value.Type)
            {
                case DialogueValueType.Bool:
                    var toggle = new Toggle(_label) { value = value.AsBool };
                    toggle.RegisterValueChangedCallback(evt => Commit(DialogueValue.Bool(evt.newValue)));
                    Add(toggle);
                    break;
                case DialogueValueType.Int:
                    var integer = new IntegerField(_label) { value = value.AsInt, isDelayed = true };
                    integer.RegisterValueChangedCallback(evt => Commit(DialogueValue.Int(evt.newValue)));
                    Add(integer);
                    break;
                case DialogueValueType.Float:
                    var number = new FloatField(_label) { value = value.AsFloat, isDelayed = true };
                    number.RegisterValueChangedCallback(evt => Commit(DialogueValue.Float(evt.newValue)));
                    Add(number);
                    break;
                default:
                    var text = new TextField(_label) { value = value.AsString, isDelayed = true };
                    text.RegisterValueChangedCallback(evt => Commit(DialogueValue.String(evt.newValue)));
                    Add(text);
                    break;
            }
        }

        private void Commit(DialogueValue value)
        {
            _value = value;
            Changed?.Invoke(value);
        }
    }
}
