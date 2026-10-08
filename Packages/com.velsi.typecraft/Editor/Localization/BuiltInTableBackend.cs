using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Velsi.Typecraft.Text.Tables;

namespace Velsi.Typecraft.Editor.Localization
{
    public sealed class BuiltInTableBackend : ITableBackend
    {
        private readonly TableTextSource _source;

        public BuiltInTableBackend(TableTextSource source)
        {
            _source = source;
        }

        public string Name => "Таблицы Typecraft";
        public IReadOnlyList<string> Locales => _source.ProjectLocales;
        public string BaseLocale => _source.BaseLocale;
        public IReadOnlyList<string> Tables => _source.Tables.Where(table => table != null).Select(table => table.TableId).ToList();

        public IReadOnlyList<string> Keys(string table) =>
            Find(table)?.Entries.Where(entry => entry != null).Select(entry => entry.Key).ToList() ?? new List<string>();

        public string Get(string table, string key, string locale)
        {
            StringEntry entry = Find(table)?.Find(key);
            return entry != null && entry.TryGet(locale, out string value) ? value : string.Empty;
        }

        public string Comment(string table, string key) => Find(table)?.Find(key)?.Comment ?? string.Empty;

        public void Set(string table, string key, string locale, string text) =>
            Modify(table, "Typecraft: правка строки", asset => asset.Add(key).Set(locale, text));

        public void SetComment(string table, string key, string comment) =>
            Modify(table, "Typecraft: комментарий", asset => asset.Add(key).Comment = comment);

        public bool AddKey(string table, string key)
        {
            StringTable asset = Find(table);
            if (asset == null || string.IsNullOrWhiteSpace(key) || asset.Contains(key))
                return false;

            Modify(table, "Typecraft: новый ключ", target => target.Add(key));
            return true;
        }

        public bool RemoveKey(string table, string key)
        {
            bool removed = false;
            Modify(table, "Typecraft: удаление ключа", asset => removed = asset.Remove(key));
            return removed;
        }

        public bool RenameKey(string table, string key, string newKey)
        {
            bool renamed = false;
            Modify(table, "Typecraft: переименование ключа", asset => renamed = asset.Rename(key, newKey));
            return renamed;
        }

        public bool AddLocale(string locale)
        {
            if (string.IsNullOrWhiteSpace(locale) || _source.ProjectLocales.Contains(locale))
                return false;

            Undo.RecordObject(_source, "Typecraft: новый язык");
            _source.AddLocale(locale);
            EditorUtility.SetDirty(_source);
            Typecraft.Refresh();
            return true;
        }

        public string CreateTable(string name)
        {
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(_source))?.Replace('\\', '/') ?? "Assets";
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.asset");

            var table = ScriptableObject.CreateInstance<StringTable>();
            table.SetTableId(name);
            AssetDatabase.CreateAsset(table, path);

            Undo.RecordObject(_source, "Typecraft: новая таблица");
            _source.AddTable(table);
            EditorUtility.SetDirty(_source);
            AssetDatabase.SaveAssets();
            Typecraft.Refresh();
            return table.TableId;
        }

        public Object Asset(string table) => Find(table);

        private StringTable Find(string table) => _source.Tables.FirstOrDefault(asset => asset != null && asset.TableId == table);

        private void Modify(string table, string undoName, System.Action<StringTable> change)
        {
            StringTable asset = Find(table);
            if (asset == null)
                return;

            Undo.RecordObject(asset, undoName);
            change(asset);
            EditorUtility.SetDirty(asset);
            SceneTexts.RefreshAll();
        }
    }
}
