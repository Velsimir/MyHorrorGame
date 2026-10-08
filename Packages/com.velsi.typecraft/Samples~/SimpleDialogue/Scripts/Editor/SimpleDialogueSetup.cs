using System.Linq;
using UnityEditor;
using UnityEngine;
using Velsi.Typecraft.Text;
using Velsi.Typecraft.Text.Tables;

namespace Velsi.Typecraft.Samples.SimpleDialogue.Editor
{
    public static class SimpleDialogueSetup
    {
        private const string TableName = "StringTable_SimpleDialogue";
        private const string LayerName = "MarkupLayer_SimpleDialogue";

        [MenuItem("Tools/Typecraft/Samples/Connect Simple Dialogue Table")]
        public static void Connect()
        {
            TypecraftSettings settings = Typecraft.Settings;
            if (settings == null || settings.TextSource is not TableTextSource source)
            {
                EditorUtility.DisplayDialog("Typecraft", "Сэмпл работает со встроенными таблицами Typecraft: в TypecraftSettings источником строк должен быть TableTextSource.\n\nThe sample needs TableTextSource as the text source in TypecraftSettings.", "OK");
                return;
            }

            StringTable table = Find<StringTable>(TableName);
            MarkupLayer layer = Find<MarkupLayer>(LayerName);
            if (table == null || layer == null)
            {
                string missing = table == null ? TableName : LayerName;
                EditorUtility.DisplayDialog("Typecraft", $"Не найден {missing}. Импортируй сэмпл заново.\n\n{missing} not found. Re-import the sample.", "OK");
                return;
            }

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
    }
}
