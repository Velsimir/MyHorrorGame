using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Text;
using Object = UnityEngine.Object;

namespace Velsi.Typecraft.Editor.Library
{
    public sealed class MarkupRewriter
    {
        private const string MarkupField = "_markup";
        private const string UnescapedGuard = @"(?<!\\)";

        private enum Place
        {
            Scene,
            Prefab,
            Asset
        }

        private readonly struct Spot
        {
            public Spot(Object target, string path, string value)
            {
                Target = target;
                Path = path;
                Value = value;
            }

            public Object Target { get; }
            public string Path { get; }
            public string Value { get; }
        }

        private static readonly Dictionary<(Type, string), bool> MarkedPaths = new Dictionary<(Type, string), bool>();

        private readonly Regex _pattern;
        private readonly string _replacement;
        private readonly HashSet<string> _markedNames;
        private readonly List<(string Table, string Key, string Locale, string Text)> _cells = new List<(string, string, string, string)>();
        private readonly List<(MarkupLayer Layer, string Key, string Locale, string Markup, string Source)> _layerEntries = new List<(MarkupLayer, string, string, string, string)>();
        private readonly List<(Spot Spot, Place Place)> _loaded = new List<(Spot, Place)>();
        private readonly List<(string Path, int Count)> _prefabs = new List<(string, int)>();
        private readonly List<(string Path, int Count)> _closedScenes = new List<(string, int)>();
        private readonly List<(int Index, string Template)> _autoTags = new List<(int, string)>();

        private MarkupRewriter(Regex pattern, string replacement)
        {
            _pattern = pattern;
            _replacement = replacement;
            _markedNames = new HashSet<string>(TypeCache.GetFieldsWithAttribute<TypecraftMarkupAttribute>().Select(field => field.Name));
        }

        public int Cells => _cells.Count;
        public int LayerEntries => _layerEntries.Count;
        public int AutoTags => _autoTags.Count;
        public bool IsEmpty => _cells.Count + _layerEntries.Count + _loaded.Count + _prefabs.Count + _closedScenes.Count + _autoTags.Count == 0;
        public bool HasIrreversible => _prefabs.Count + _closedScenes.Count > 0;

        public string UndoNote => HasIrreversible
            ? "\n\nПрефабы и закрытые сцены будут сразу сохранены на диск — Undo их не откатит."
            : string.Empty;

        public string Describe()
        {
            var lines = new List<string>();
            if (_cells.Count > 0)
                lines.Add($"Таблицы — строк: {_cells.Count}");
            if (_layerEntries.Count > 0)
                lines.Add($"Слои разметки — записей: {_layerEntries.Count}");

            int openScenes = LoadedCount(Place.Scene);
            if (openScenes + _closedScenes.Count > 0)
                lines.Add($"Сцены — в открытых мест: {openScenes}; закрытых сцен: {_closedScenes.Count}, мест в них: {_closedScenes.Sum(s => s.Count)}");

            int openPrefab = LoadedCount(Place.Prefab);
            if (openPrefab + _prefabs.Count > 0)
                lines.Add($"Префабы — файлов: {_prefabs.Count}, мест: {_prefabs.Sum(p => p.Count) + openPrefab}" + (openPrefab > 0 ? $" (в открытом префабе: {openPrefab})" : string.Empty));

            int assets = LoadedCount(Place.Asset);
            if (assets + _autoTags.Count > 0)
                lines.Add($"Ассеты — полей: {assets}; правил автотегов: {_autoTags.Count}");

            foreach ((string path, int _) in _closedScenes.Concat(_prefabs).Take(10))
                lines.Add("  " + path);

            return string.Join("\n", lines);
        }

        public static Regex StylePattern(string id) => new Regex(UnescapedGuard + @"\[(/?)" + Regex.Escape(id) + @"\]");

        public static string StyleReplacement(string id) => "[${1}" + id + "]";

        public static Regex IconPattern(string setId, string key, bool plain) =>
            new Regex(UnescapedGuard + @"\{" + "(?:" + Regex.Escape(setId) + "/)" + (plain ? "?" : string.Empty) + Regex.Escape(key) + @"\}");

        public static MarkupRewriter Prepare(Regex pattern, string replacement)
        {
            var plan = new MarkupRewriter(pattern, replacement);
            plan.CollectCells();
            plan.CollectLayers();
            plan.CollectOpenScenes();
            plan.CollectOpenPrefab();
            plan.CollectAssets();
            plan.CollectFiles();
            plan.CollectAutoTags();
            return plan;
        }

        public void Apply(string undoName)
        {
            ITableBackend backend = TableBackends.Current;
            foreach ((string table, string key, string locale, string text) in _cells)
                backend.Set(table, key, locale, text);

            foreach (IGrouping<MarkupLayer, (MarkupLayer Layer, string Key, string Locale, string Markup, string Source)> group in _layerEntries.GroupBy(entry => entry.Layer))
            {
                Undo.RecordObject(group.Key, undoName);
                foreach ((MarkupLayer layer, string key, string locale, string markup, string source) in group)
                    layer.Set(key, locale, markup, source);
                EditorUtility.SetDirty(group.Key);
            }

            if (_layerEntries.Count > 0)
                SceneTexts.RefreshAll();

            foreach (string path in _prefabs.Select(p => p.Path))
                RewritePrefab(path);

            foreach (string path in _closedScenes.Select(s => s.Path))
                RewriteClosedScene(path);

            foreach ((Spot spot, Place _) in _loaded)
                Write(spot, undoName, true);

            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && LoadedCount(Place.Prefab) > 0)
                EditorSceneManager.MarkSceneDirty(stage.scene);

            TypecraftSettings settings = Typecraft.Settings;
            if (settings != null && _autoTags.Count > 0)
            {
                var serialized = new SerializedObject(settings);
                SerializedProperty rules = serialized.FindProperty("_autoTags");
                foreach ((int index, string template) in _autoTags)
                    rules.GetArrayElementAtIndex(index).FindPropertyRelative("_template").stringValue = template;
                serialized.ApplyModifiedProperties();
            }
        }

        private int LoadedCount(Place place) => _loaded.Count(entry => entry.Place == place);

        private void CollectCells()
        {
            ITableBackend backend = TableBackends.Current;
            if (backend == null)
                return;

            foreach (string table in backend.Tables)
            foreach (string key in backend.Keys(table))
            foreach (string locale in backend.Locales)
            {
                string value = backend.Get(table, key, locale);
                if (!string.IsNullOrEmpty(value) && _pattern.IsMatch(value))
                    _cells.Add((table, key, locale, _pattern.Replace(value, _replacement)));
            }
        }

        private void CollectLayers()
        {
            TypecraftSettings settings = Typecraft.Settings;
            if (settings == null)
                return;

            foreach (MarkupLayer layer in settings.MarkupLayers.Where(layer => layer != null).Distinct())
            foreach (MarkupLayerEntry entry in layer.Entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Markup))
                    continue;

                string markup = _pattern.Replace(entry.Markup, _replacement);
                string source = string.IsNullOrEmpty(entry.Source) ? entry.Source : _pattern.Replace(entry.Source, _replacement);
                if (markup != entry.Markup || source != entry.Source)
                    _layerEntries.Add((layer, entry.Key, entry.Locale, markup, source));
            }
        }

        private void CollectOpenScenes()
        {
            foreach (Scene scene in ProjectScan.LoadedScenes())
            foreach (Spot spot in FindInScene(scene))
                _loaded.Add((spot, Place.Scene));
        }

        private void CollectOpenPrefab()
        {
            GameObject root = ProjectScan.OpenPrefabRoot();
            if (root == null)
                return;

            foreach (Spot spot in FindInHierarchy(root))
                _loaded.Add((spot, Place.Prefab));
        }

        private void CollectAssets()
        {
            if (_markedNames.Count == 0)
                return;

            Regex filter = LooseFilter();
            foreach (string path in ProjectScan.AssetPaths("t:ScriptableObject").Where(p => p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)))
            {
                if (!ProjectScan.MayContain(path, filter))
                    continue;

                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is ScriptableObject && asset != Typecraft.Settings)
                    {
                        foreach (Spot spot in FindInObject(asset))
                            _loaded.Add((spot, Place.Asset));
                    }
                }
            }
        }

        private void CollectFiles()
        {
            Regex filter = LooseFilter();

            foreach (string path in ProjectScan.PrefabPaths().Where(p => ProjectScan.MayContain(p, filter)))
            {
                int count = ProjectScan.InPrefab(path, root => FindInHierarchy(root).Count());
                if (count > 0)
                    _prefabs.Add((path, count));
            }

            foreach (string path in ProjectScan.ClosedScenePaths().Where(p => ProjectScan.MayContain(p, filter)))
            {
                int count = ProjectScan.InClosedScene(path, scene => FindInScene(scene).Count());
                if (count > 0)
                    _closedScenes.Add((path, count));
            }
        }

        private void CollectAutoTags()
        {
            TypecraftSettings settings = Typecraft.Settings;
            if (settings == null)
                return;

            for (int i = 0; i < settings.AutoTags.Count; i++)
            {
                string template = settings.AutoTags[i]?.Template;
                if (!string.IsNullOrEmpty(template) && _pattern.IsMatch(template))
                    _autoTags.Add((i, _pattern.Replace(template, _replacement)));
            }
        }

        private void RewritePrefab(string path) =>
            ProjectScan.InPrefab(path, root =>
            {
                List<Spot> spots = FindInHierarchy(root).ToList();
                foreach (Spot spot in spots)
                    Write(spot, null, false);

                if (spots.Count > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
                    if (!saved)
                        TypecraftLog.Error($"Не удалось сохранить префаб {path}");
                }

                return spots.Count;
            });

        private void RewriteClosedScene(string path) =>
            ProjectScan.InClosedScene(path, scene =>
            {
                List<Spot> spots = FindInScene(scene).ToList();
                foreach (Spot spot in spots)
                    Write(spot, null, false);

                if (spots.Count > 0 && !EditorSceneManager.SaveScene(scene))
                    TypecraftLog.Error($"Не удалось сохранить сцену {path}");

                return spots.Count;
            });

        private static void Write(Spot spot, string undoName, bool undoable)
        {
            if (spot.Target == null)
                return;

            if (undoable && spot.Target is TypecraftText text && spot.Path == MarkupField)
            {
                Undo.RecordObject(text, undoName);
                text.SetMarkup(spot.Value);
            }
            else
            {
                var serialized = new SerializedObject(spot.Target);
                SerializedProperty property = serialized.FindProperty(spot.Path);
                if (property == null)
                    return;

                property.stringValue = spot.Value;
                if (undoable)
                    serialized.ApplyModifiedProperties();
                else
                    serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(spot.Target);
            if (undoable && spot.Target is Component component && component.gameObject.scene.IsValid() && !EditorSceneManager.IsPreviewScene(component.gameObject.scene))
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
        }

        private IEnumerable<Spot> FindInScene(Scene scene) =>
            scene.GetRootGameObjects().SelectMany(FindInHierarchy);

        private IEnumerable<Spot> FindInHierarchy(GameObject root) =>
            root.GetComponentsInChildren<MonoBehaviour>(true).Where(behaviour => behaviour != null).SelectMany(FindInObject);

        private IEnumerable<Spot> FindInObject(Object target)
        {
            bool isText = target is TypecraftText text && text.Mode == TextMode.Inline;
            if (!isText && _markedNames.Count == 0)
                yield break;

            var serialized = new SerializedObject(target);
            if (isText)
            {
                Spot? spot = Match(serialized.FindProperty(MarkupField));
                if (spot.HasValue)
                    yield return spot.Value;
            }

            if (_markedNames.Count == 0)
                yield break;

            SerializedProperty iterator = serialized.GetIterator();
            bool enter = true;
            while (iterator.Next(enter))
            {
                enter = iterator.propertyType != SerializedPropertyType.String;
                if (enter || !_markedNames.Contains(FieldName(iterator.propertyPath)) || !IsMarked(target.GetType(), iterator.propertyPath))
                    continue;

                Spot? spot = Match(iterator);
                if (spot.HasValue)
                    yield return spot.Value;
            }
        }

        private Spot? Match(SerializedProperty property)
        {
            if (property == null || property.propertyType != SerializedPropertyType.String)
                return null;

            string value = property.stringValue;
            if (string.IsNullOrEmpty(value) || !_pattern.IsMatch(value) || IsInheritedFromProjectPrefab(property))
                return null;

            return new Spot(property.serializedObject.targetObject, property.propertyPath, _pattern.Replace(value, _replacement));
        }

        private static bool IsInheritedFromProjectPrefab(SerializedProperty property)
        {
            Object target = property.serializedObject.targetObject;
            if (property.prefabOverride || !PrefabUtility.IsPartOfPrefabInstance(target))
                return false;

            Object source = PrefabUtility.GetCorrespondingObjectFromSource(target);
            return source != null && ProjectScan.IsInProject(AssetDatabase.GetAssetPath(source));
        }

        private static string FieldName(string propertyPath)
        {
            string path = Regex.Replace(propertyPath, @"(\.Array\.data\[\d+\])+$", string.Empty);
            int dot = path.LastIndexOf('.');
            return dot < 0 ? path : path.Substring(dot + 1);
        }

        private static bool IsMarked(Type root, string propertyPath)
        {
            string path = Regex.Replace(propertyPath, @"\.Array\.data\[\d+\]", ".[]");
            if (MarkedPaths.TryGetValue((root, path), out bool marked))
                return marked;

            Type type = root;
            FieldInfo field = null;
            foreach (string part in path.Split('.'))
            {
                if (part == "[]")
                {
                    type = ElementType(type);
                    continue;
                }

                field = type != null ? FindField(type, part) : null;
                if (field == null)
                    break;
                type = field.FieldType;
            }

            marked = field != null && field.IsDefined(typeof(TypecraftMarkupAttribute), true);
            MarkedPaths[(root, path)] = marked;
            return marked;
        }

        private static FieldInfo FindField(Type type, string name)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;
            }

            return null;
        }

        private static Type ElementType(Type type)
        {
            if (type == null)
                return null;
            if (type.IsArray)
                return type.GetElementType();
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) ? type.GetGenericArguments()[0] : null;
        }

        private Regex LooseFilter()
        {
            string source = _pattern.ToString();
            return source.StartsWith(UnescapedGuard, StringComparison.Ordinal) ? new Regex(source.Substring(UnescapedGuard.Length)) : _pattern;
        }
    }
}
