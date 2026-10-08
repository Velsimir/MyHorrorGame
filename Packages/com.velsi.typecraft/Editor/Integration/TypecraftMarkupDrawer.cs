using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Velsi.Typecraft.Editor
{
    [CustomPropertyDrawer(typeof(TypecraftMarkupAttribute))]
    public sealed class TypecraftMarkupDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 24f;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.String)
                return new Label($"[TypecraftMarkup] работает только со string: {property.displayName}");

            SerializedObject serialized = property.serializedObject;
            string path = property.propertyPath;
            var field = new MarkupTextField(property.displayName, ((TypecraftMarkupAttribute)attribute).Preview);
            field.SetValueWithoutNotify(property.stringValue);

            field.RegisterValueChangedCallback(evt =>
            {
                serialized.Update();
                serialized.FindProperty(path).stringValue = evt.newValue;
                serialized.ApplyModifiedProperties();
            });
            field.TrackPropertyValue(property, changed =>
            {
                if (changed.stringValue != field.value)
                    field.SetValueWithoutNotify(changed.stringValue);
            });
            field.EditRequested += () => TypecraftEditor.Edit(serialized.FindProperty(path));
            return field;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight * 3f + EditorGUIUtility.standardVerticalSpacing;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "[TypecraftMarkup] работает только со string");
                return;
            }

            Rect fieldRect = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width - ButtonWidth - 2f, position.height), label);
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            string value = EditorGUI.TextArea(fieldRect, property.stringValue);
            if (EditorGUI.EndChangeCheck())
                property.stringValue = value;
            EditorGUI.EndProperty();

            if (GUI.Button(new Rect(position.xMax - ButtonWidth, position.y, ButtonWidth, EditorGUIUtility.singleLineHeight), new GUIContent("✎", "Открыть в окне Typecraft")))
                TypecraftEditor.Edit(property.Copy());
        }
    }
}
