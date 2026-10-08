#if TYPECRAFT_LOCALIZATION
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using LocalizationStringTable = UnityEngine.Localization.Tables.StringTable;

namespace Velsi.Typecraft.Editor.Localization
{
    public sealed class UnityLocalizationBackend : ITableBackend
    {
        private const string DefaultFolder = "Assets/Localization";

        public string Name => "Unity Localization";

        public IReadOnlyList<string> Locales => LocalizationEditorSettings.GetLocales().Select(locale => locale.Identifier.Code).ToList();

        public string BaseLocale
        {
            get
            {
                Locale project = LocalizationSettings.HasSettings ? LocalizationSettings.ProjectLocale : null;
                return project != null ? project.Identifier.Code : Locales.FirstOrDefault() ?? "en";
            }
        }

        public IReadOnlyList<string> Tables => LocalizationEditorSettings.GetStringTableCollections().Select(collection => collection.TableCollectionName).ToList();

        public IReadOnlyList<string> Keys(string table) =>
            Find(table)?.SharedData.Entries.Select(entry => entry.Key).ToList() ?? new List<string>();

        public string Get(string table, string key, string locale)
        {
            StringTableCollection collection = Find(table);
            var stringTable = collection?.GetTable(locale) as LocalizationStringTable;
            return stringTable?.GetEntry(key)?.Value ?? string.Empty;
        }

        public string Comment(string table, string key) =>
            Find(table)?.SharedData.GetEntry(key)?.Metadata.GetMetadata<Comment>()?.CommentText ?? string.Empty;

        public void Set(string table, string key, string locale, string text)
        {
            StringTableCollection collection = Find(table);
            if (collection == null)
                return;

            var stringTable = collection.GetTable(locale) as LocalizationStringTable;
            if (stringTable == null)
            {
                Locale target = LocalizationEditorSettings.GetLocale(locale);
                if (target == null)
                    return;
                stringTable = collection.AddNewTable(target.Identifier) as LocalizationStringTable;
            }

            Undo.RecordObjects(new Object[] { stringTable, collection.SharedData }, "Typecraft: правка строки");
            if (string.IsNullOrEmpty(text))
            {
                StringTableEntry entry = stringTable.GetEntry(key);
                if (entry != null)
                    entry.Value = string.Empty;
            }
            else
            {
                stringTable.AddEntry(key, text);
            }

            EditorUtility.SetDirty(stringTable);
            EditorUtility.SetDirty(collection.SharedData);
            SceneTexts.RefreshAll();
        }

        public void SetComment(string table, string key, string comment)
        {
            StringTableCollection collection = Find(table);
            SharedTableData.SharedTableEntry entry = collection?.SharedData.GetEntry(key) ?? collection?.SharedData.AddKey(key);
            if (entry == null)
                return;

            Undo.RecordObject(collection.SharedData, "Typecraft: комментарий");
            Comment existing = entry.Metadata.GetMetadata<Comment>();
            if (existing == null)
            {
                existing = new Comment();
                entry.Metadata.AddMetadata(existing);
            }

            existing.CommentText = comment;
            EditorUtility.SetDirty(collection.SharedData);
        }

        public bool AddKey(string table, string key)
        {
            StringTableCollection collection = Find(table);
            if (collection == null || string.IsNullOrWhiteSpace(key) || collection.SharedData.Contains(key))
                return false;

            Undo.RecordObject(collection.SharedData, "Typecraft: новый ключ");
            collection.SharedData.AddKey(key);
            EditorUtility.SetDirty(collection.SharedData);
            return true;
        }

        public bool RemoveKey(string table, string key)
        {
            StringTableCollection collection = Find(table);
            if (collection == null || !collection.SharedData.Contains(key))
                return false;

            Undo.RecordObject(collection.SharedData, "Typecraft: удаление ключа");
            collection.RemoveEntry(key);
            EditorUtility.SetDirty(collection.SharedData);
            SceneTexts.RefreshAll();
            return true;
        }

        public bool RenameKey(string table, string key, string newKey)
        {
            StringTableCollection collection = Find(table);
            if (collection == null || string.IsNullOrWhiteSpace(newKey) || collection.SharedData.Contains(newKey))
                return false;

            Undo.RecordObject(collection.SharedData, "Typecraft: переименование ключа");
            collection.SharedData.RenameKey(key, newKey);
            EditorUtility.SetDirty(collection.SharedData);
            return true;
        }

        public bool AddLocale(string locale)
        {
            if (string.IsNullOrWhiteSpace(locale) || LocalizationEditorSettings.GetLocale(locale) != null)
                return false;

            EnsureFolder(DefaultFolder);
            Locale created = Locale.CreateLocale(locale);
            AssetDatabase.CreateAsset(created, AssetDatabase.GenerateUniqueAssetPath($"{DefaultFolder}/{created.name}.asset"));
            LocalizationEditorSettings.AddLocale(created);

            foreach (StringTableCollection collection in LocalizationEditorSettings.GetStringTableCollections())
            {
                if (collection.GetTable(created.Identifier) == null)
                    collection.AddNewTable(created.Identifier);
            }

            AssetDatabase.SaveAssets();
            Typecraft.Refresh();
            return true;
        }

        public string CreateTable(string name)
        {
            EnsureFolder(DefaultFolder);
            StringTableCollection collection = LocalizationEditorSettings.CreateStringTableCollection(name, DefaultFolder);
            AssetDatabase.SaveAssets();
            return collection != null ? collection.TableCollectionName : null;
        }

        public Object Asset(string table) => Find(table);

        private static StringTableCollection Find(string table) =>
            string.IsNullOrEmpty(table) ? null : LocalizationEditorSettings.GetStringTableCollection(table);

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            int slash = folder.LastIndexOf('/');
            AssetDatabase.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
        }
    }
}
#endif
