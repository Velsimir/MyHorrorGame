using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Editor.Localization
{
    public enum LayerState
    {
        None,
        Applied,
        Stale
    }

    [InitializeOnLoad]
    public static class MarkupLayers
    {
        static MarkupLayers()
        {
            Undo.undoRedoPerformed += InvalidateAll;
        }

        public static MarkupLayer Find(string table) => Typecraft.Settings != null ? Typecraft.Settings.FindMarkupLayer(table) : null;

        public static MarkupLayerEntry Entry(string table, string key, string locale)
        {
            MarkupLayerEntry entry = Find(table)?.Find(key, locale);
            return entry != null && !string.IsNullOrEmpty(entry.Markup) ? entry : null;
        }

        public static LayerState State(ITableBackend backend, string table, string key, string locale)
        {
            MarkupLayerEntry entry = Entry(table, key, locale);
            if (entry == null)
                return LayerState.None;
            return entry.Source == backend.Get(table, key, locale) ? LayerState.Applied : LayerState.Stale;
        }

        public static string Display(ITableBackend backend, string table, string key, string locale)
        {
            string text = backend.Get(table, key, locale);
            MarkupLayer layer = Find(table);
            return layer != null ? layer.Resolve(key, locale, text) : text;
        }

        public static void Write(ITableBackend backend, string table, string key, string locale, string markup)
        {
            string text = Decoration.Strip(markup);
            if (backend.Get(table, key, locale) != text)
                backend.Set(table, key, locale, text);
            Set(backend, table, key, locale, markup);
        }

        public static bool Set(ITableBackend backend, string table, string key, string locale, string markup)
        {
            string source = Decoration.Strip(markup);
            if (!Decoration.Has(markup))
            {
                Remove(table, key, locale);
                return true;
            }

            if (source != backend.Get(table, key, locale))
                return false;

            MarkupLayer layer = GetOrCreate(backend, table);
            if (layer == null)
                return false;

            Undo.RecordObject(layer, "Typecraft: разметка строки");
            layer.Set(key, locale, markup, source);
            Changed(layer);
            return true;
        }

        public static void Remove(string table, string key, string locale)
        {
            MarkupLayer layer = Find(table);
            if (layer == null || layer.Find(key, locale) == null)
                return;

            Undo.RecordObject(layer, "Typecraft: сброс разметки");
            layer.Remove(key, locale);
            Changed(layer);
        }

        public static void RemoveKey(string table, string key)
        {
            MarkupLayer layer = Find(table);
            if (layer == null || layer.Entries.All(entry => entry == null || entry.Key != key))
                return;

            Undo.RecordObject(layer, "Typecraft: удаление ключа");
            layer.RemoveKey(key);
            Changed(layer);
        }

        public static void RenameKey(string table, string key, string newKey)
        {
            MarkupLayer layer = Find(table);
            if (layer == null || layer.Entries.All(entry => entry == null || entry.Key != key))
                return;

            Undo.RecordObject(layer, "Typecraft: переименование ключа");
            layer.RenameKey(key, newKey);
            Changed(layer);
        }

        public static MarkupLayer GetOrCreate(ITableBackend backend, string table)
        {
            MarkupLayer existing = Find(table);
            TypecraftSettings settings = Typecraft.Settings;
            if (existing != null || settings == null || string.IsNullOrEmpty(table))
                return existing;

            string folder = FolderOf(backend.Asset(table)) ?? FolderOf(settings.TextSource as Object) ?? "Assets";
            string name = new string(table.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/MarkupLayer_{name}.asset");

            var layer = ScriptableObject.CreateInstance<MarkupLayer>();
            layer.SetTableId(table);
            AssetDatabase.CreateAsset(layer, path);

            Undo.RecordObject(settings, "Typecraft: слой разметки");
            settings.AddMarkupLayer(layer);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            TypecraftLog.Info($"Создан слой разметки для таблицы «{table}»: {path}");
            return layer;
        }

        private static void InvalidateAll()
        {
            TypecraftSettings settings = Typecraft.Settings;
            if (settings == null)
                return;

            foreach (MarkupLayer layer in settings.MarkupLayers)
            {
                if (layer != null)
                    layer.Invalidate();
            }

            SceneTexts.RefreshAll();
        }

        private static string FolderOf(Object asset)
        {
            string path = asset != null ? AssetDatabase.GetAssetPath(asset) : null;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/"))
                return null;
            return Path.GetDirectoryName(path)?.Replace('\\', '/');
        }

        private static void Changed(MarkupLayer layer)
        {
            EditorUtility.SetDirty(layer);
            SceneTexts.RefreshAll();
        }
    }
}
