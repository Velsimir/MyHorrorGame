using TMPro;
using UnityEditor;
using UnityEngine;

namespace Velsi.Typecraft.Editor.Window.Targets
{
    public sealed class PropertyTarget : ITextEditTarget
    {
        private readonly Object _owner;
        private readonly string _path;
        private readonly string _label;

        public PropertyTarget(SerializedProperty property)
        {
            _owner = property.serializedObject.targetObject;
            _path = property.propertyPath;
            _label = property.displayName;
        }

        public string Title => _owner != null ? $"{_owner.name} · {_label}" : "—";
        public string Markup => Property()?.stringValue ?? string.Empty;
        public bool IsValid => Property() != null;
        public bool CanWrite => IsValid;
        public string ReadOnlyReason => null;
        public string FontRole => null;
        public TMP_Text Reference => _owner is Component component ? component.GetComponentInChildren<TMP_Text>(true) : null;

        public void Write(string markup)
        {
            SerializedProperty property = Property();
            if (property == null)
                return;

            property.stringValue = markup;
            property.serializedObject.ApplyModifiedProperties();
        }

        public void SetFontRole(string role)
        {
        }

        private SerializedProperty Property()
        {
            if (_owner == null)
                return null;

            SerializedProperty property = new SerializedObject(_owner).FindProperty(_path);
            return property != null && property.propertyType == SerializedPropertyType.String ? property : null;
        }
    }
}
