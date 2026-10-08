using System.Linq;
using UnityEditor;
using UnityEngine;
using Velsi.Typecraft.Text;
using Velsi.Typecraft.Text.Tables;

namespace Velsi.Typecraft.Samples.BranchingDialogue.Editor
{
    public static class BranchingDialogueSetup
    {
        private const string TableName = "StringTable_BranchingDialogue";
        private const string LayerName = "MarkupLayer_BranchingDialogue";

        [MenuItem("Tools/Typecraft/Samples/Connect Branching Dialogue Table")]
        public static void Connect()
        {
            if (!TryLoad(out TypecraftSettings settings, out TableTextSource source, out StringTable table, out MarkupLayer layer))
                return;

            var serialized = new SerializedObject(source);
            AddReference(serialized.FindProperty("_tables"), table);

            SerializedProperty locales = serialized.FindProperty("_locales");
            foreach (string locale in new[] { "en", "ru", "zh-Hans" })
            {
                if (Enumerable.Range(0, locales.arraySize).Any(i => locales.GetArrayElementAtIndex(i).stringValue == locale))
                    continue;

                locales.arraySize++;
                locales.GetArrayElementAtIndex(locales.arraySize - 1).stringValue = locale;
            }

            serialized.ApplyModifiedProperties();

            var serializedSettings = new SerializedObject(settings);
            AddReference(serializedSettings.FindProperty("_markupLayers"), layer);
            serializedSettings.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            Typecraft.Refresh();
            Debug.Log($"[Typecraft] {TableName} и {LayerName} подключены к {source.name} / connected to {source.name}");
        }

        [MenuItem("Tools/Typecraft/Samples/Disconnect Branching Dialogue Table")]
        public static void Disconnect()
        {
            if (!TryLoad(out TypecraftSettings settings, out TableTextSource source, out StringTable table, out MarkupLayer layer))
                return;

            var serialized = new SerializedObject(source);
            RemoveReference(serialized.FindProperty("_tables"), table);
            serialized.ApplyModifiedProperties();

            var serializedSettings = new SerializedObject(settings);
            RemoveReference(serializedSettings.FindProperty("_markupLayers"), layer);
            serializedSettings.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            Typecraft.Refresh();
            Debug.Log($"[Typecraft] {TableName} и {LayerName} отключены от {source.name} / disconnected from {source.name}");
        }

        private static bool TryLoad(out TypecraftSettings settings, out TableTextSource source, out StringTable table, out MarkupLayer layer)
        {
            settings = Typecraft.Settings;
            source = settings != null ? settings.TextSource as TableTextSource : null;
            table = Find<StringTable>(TableName);
            layer = Find<MarkupLayer>(LayerName);

            if (source == null)
            {
                EditorUtility.DisplayDialog("Typecraft", "Сэмпл работает со встроенными таблицами Typecraft: в TypecraftSettings источником строк должен быть TableTextSource.\n\nThe sample needs TableTextSource as the text source in TypecraftSettings.", "OK");
                return false;
            }

            if (table == null || layer == null)
            {
                string missing = table == null ? TableName : LayerName;
                EditorUtility.DisplayDialog("Typecraft", $"Не найден {missing}. Импортируй сэмпл заново.\n\n{missing} not found. Re-import the sample.", "OK");
                return false;
            }

            return true;
        }

        private static T Find<T>(string assetName) where T : Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name} {assetName}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(asset => asset != null && asset.name == assetName);

        private static void AddReference(SerializedProperty list, Object asset)
        {
            if (Enumerable.Range(0, list.arraySize).Any(i => list.GetArrayElementAtIndex(i).objectReferenceValue == asset))
                return;

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = asset;
        }

        private static void RemoveReference(SerializedProperty list, Object asset)
        {
            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue != asset)
                    continue;

                list.GetArrayElementAtIndex(i).objectReferenceValue = null;
                list.DeleteArrayElementAtIndex(i);
            }
        }
    }
}
