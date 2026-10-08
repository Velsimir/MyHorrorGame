using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Velsi.Typecraft.Dialogue.Editor.Model
{
    public static class DialogueProject
    {
        public const string DefaultFolder = "Assets/Game/Dialogues";
        private const string FolderSetting = "Typecraft.Dialogues.Folder";

        private static DialogueVariableCatalog _merged;

        public static string Folder
        {
            get
            {
                string folder = EditorUserSettings.GetConfigValue(FolderSetting);
                return string.IsNullOrEmpty(folder) ? DefaultFolder : folder;
            }
            set => EditorUserSettings.SetConfigValue(FolderSetting, value);
        }

        public static List<T> FindAll<T>() where T : Object =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name, System.StringComparer.OrdinalIgnoreCase)
                .ToList();

        public static List<DialogueAsset> Dialogues() => FindAll<DialogueAsset>();
        public static List<SpeakerProfile> Speakers() => FindAll<SpeakerProfile>();
        public static List<DialogueVariableCatalog> Catalogs() => FindAll<DialogueVariableCatalog>();

        public static IEnumerable<(DialogueVariableCatalog Catalog, DialogueVariableDefinition Definition)> Variables() =>
            Catalogs().SelectMany(catalog => catalog.Variables.Where(definition => definition != null).Select(definition => (catalog, definition)));

        public static bool TryVariable(string name, out DialogueVariableDefinition definition)
        {
            foreach ((DialogueVariableCatalog _, DialogueVariableDefinition candidate) in Variables())
            {
                if (candidate.Name == name)
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public static DialogueVariableCatalog MergedCatalog()
        {
            if (_merged == null)
            {
                _merged = ScriptableObject.CreateInstance<DialogueVariableCatalog>();
                _merged.hideFlags = HideFlags.HideAndDontSave;
                _merged.name = "Каталоги флагов";
            }

            foreach (DialogueVariableDefinition definition in _merged.Variables.ToList())
                _merged.Remove(definition?.Name);

            var seen = new HashSet<string>();
            foreach ((DialogueVariableCatalog _, DialogueVariableDefinition definition) in Variables())
            {
                if (!string.IsNullOrEmpty(definition.Name) && seen.Add(definition.Name))
                    _merged.Add(definition);
            }

            return _merged;
        }

        public static string FolderOf(Object asset)
        {
            string path = asset != null ? AssetDatabase.GetAssetPath(asset) : null;
            return string.IsNullOrEmpty(path) ? null : Path.GetDirectoryName(path)?.Replace('\\', '/');
        }

        public static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
                return;

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        public static DialogueAsset CreateDialogue(string path, string defaultTable)
        {
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<DialogueAsset>();
            asset.DefaultTable = defaultTable;

            var start = new StartNode { Position = new Vector2(0f, 0f) };
            var end = new EndNode { Position = new Vector2(320f, 0f) };
            start.Next = end.Id;
            asset.AddNode(start);
            asset.AddNode(end);

            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static SpeakerProfile CreateSpeaker(string folder, string name)
        {
            EnsureFolder(folder);
            var speaker = ScriptableObject.CreateInstance<SpeakerProfile>();
            AssetDatabase.CreateAsset(speaker, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.asset"));
            AssetDatabase.SaveAssets();
            return speaker;
        }

        public static DialogueVariableCatalog CatalogForNewFlag(string folder)
        {
            DialogueVariableCatalog catalog = Catalogs().FirstOrDefault();
            if (catalog != null)
                return catalog;

            EnsureFolder(folder);
            catalog = ScriptableObject.CreateInstance<DialogueVariableCatalog>();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/DialogueVariables.asset");
            AssetDatabase.CreateAsset(catalog, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Typecraft] Создан каталог флагов диалогов: {path}");
            return catalog;
        }

        public static string UniqueVariableName(string prefix)
        {
            var names = new HashSet<string>(Variables().Select(entry => entry.Definition.Name));
            for (int i = 1; ; i++)
            {
                string candidate = prefix + "_" + i;
                if (!names.Contains(candidate))
                    return candidate;
            }
        }
    }
}
