using System;
using UnityEditor;
using Velsi.Typecraft.Styles;

namespace Velsi.Typecraft.Editor.Library
{
    public static class StyleEdits
    {
        public static string ValidateId(StyleLibrary library, string id)
        {
            if (string.IsNullOrEmpty(id))
                return "Введи id стиля";
            if (!StylePanel.ValidId.IsMatch(id))
                return "Id: латинская буква первой, дальше буквы, цифры, «_», «.», «-»";
            if (library != null && library.TryGet(id, out _))
                return $"Стиль [{id}] уже есть";
            return null;
        }

        public static void Add(StyleLibrary library, SerializedProperty draft)
        {
            string id = draft.FindPropertyRelative("_id").stringValue;
            string error = ValidateId(library, id);
            if (error != null)
                throw new ArgumentException(error);

            var serialized = new SerializedObject(library);
            SerializedProperty styles = serialized.FindProperty("_styles");
            styles.arraySize++;
            styles.GetArrayElementAtIndex(styles.arraySize - 1).boxedValue = draft.boxedValue;
            Undo.SetCurrentGroupName("Typecraft: новый стиль");
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Typecraft.Refresh();
        }

        public static bool Remove(StyleLibrary library, string id)
        {
            var serialized = new SerializedObject(library);
            SerializedProperty styles = serialized.FindProperty("_styles");
            int index = IndexOf(styles, id);
            if (index < 0)
                return false;

            styles.DeleteArrayElementAtIndex(index);
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Typecraft.Refresh();
            return true;
        }

        public static int IndexOf(SerializedProperty styles, string id)
        {
            for (int i = 0; i < styles.arraySize; i++)
            {
                if (styles.GetArrayElementAtIndex(i).FindPropertyRelative("_id").stringValue == id)
                    return i;
            }

            return -1;
        }
    }
}
