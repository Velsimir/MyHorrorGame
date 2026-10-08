using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Velsi.Typecraft.Dialogue.Editor.Inspector
{
    [CustomPropertyDrawer(typeof(DialogueValue))]
    internal sealed class DialogueValueDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;

            SerializedProperty type = property.FindPropertyRelative("_type");
            var typeField = new PropertyField(type, property.displayName);
            typeField.style.minWidth = 220;
            root.Add(typeField);

            var valueHost = new VisualElement();
            valueHost.style.flexGrow = 1;
            root.Add(valueHost);

            void Rebuild()
            {
                valueHost.Clear();
                string name = (DialogueValueType)type.enumValueIndex switch
                {
                    DialogueValueType.Bool => "_bool",
                    DialogueValueType.Int => "_int",
                    DialogueValueType.Float => "_float",
                    _ => "_string"
                };

                var field = new PropertyField(property.FindPropertyRelative(name), string.Empty);
                field.Bind(property.serializedObject);
                valueHost.Add(field);
            }

            Rebuild();
            typeField.RegisterValueChangeCallback(_ => Rebuild());
            return root;
        }
    }
}
