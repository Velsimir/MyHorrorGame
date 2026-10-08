using TMPro;
using UnityEditor;
using UnityEngine;

namespace Velsi.Typecraft.Editor
{
    [InitializeOnLoad]
    internal static class TypecraftContextMenu
    {
        static TypecraftContextMenu()
        {
            EditorApplication.contextualPropertyMenu -= OnPropertyMenu;
            EditorApplication.contextualPropertyMenu += OnPropertyMenu;
        }

        private static void OnPropertyMenu(GenericMenu menu, SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.String || property.serializedObject.isEditingMultipleObjects)
                return;

            SerializedProperty copy = property.Copy();
            menu.AddItem(new GUIContent("Edit in Typecraft"), false, () => TypecraftEditor.Edit(copy));
        }

        [MenuItem("CONTEXT/TMP_Text/Edit in Typecraft")]
        private static void EditText(MenuCommand command)
        {
            if (command.context is TMP_Text text)
                TypecraftEditor.Edit(text);
        }
    }
}
