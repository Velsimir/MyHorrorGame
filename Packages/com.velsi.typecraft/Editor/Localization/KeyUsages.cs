using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Velsi.Typecraft.Text;
using Object = UnityEngine.Object;

namespace Velsi.Typecraft.Editor.Localization
{
    public enum KeyUsageKind
    {
        Scene,
        Prefab,
        Script,
        Asset
    }

    public sealed class KeyUsage
    {
        private readonly string _title;
        private readonly Action _reveal;

        public KeyUsage(KeyUsageKind kind, string assetPath, string objectPath, int[] siblings, int line, GameObject live)
        {
            Kind = kind;
            AssetPath = assetPath;
            ObjectPath = objectPath;
            Siblings = siblings;
            Line = line;
            Live = live;
        }

        public KeyUsage(string assetPath, string title, string location, Action reveal)
        {
            Kind = KeyUsageKind.Asset;
            AssetPath = assetPath;
            ObjectPath = location;
            _title = title;
            _reveal = reveal;
        }

        public KeyUsageKind Kind { get; }
        public string AssetPath { get; }
        public string ObjectPath { get; }
        public int[] Siblings { get; }
        public int Line { get; }
        public GameObject Live { get; }

        public string KindTitle => Kind switch
        {
            KeyUsageKind.Scene => "Сцена",
            KeyUsageKind.Prefab => "Префаб",
            KeyUsageKind.Asset => _title ?? "Ассет",
            _ => "Скрипт"
        };

        internal void RevealAsset() => _reveal?.Invoke();

        public string Location => Kind == KeyUsageKind.Script ? $"строка {Line}" : ObjectPath;

        public override string ToString() => $"{KindTitle} · {(string.IsNullOrEmpty(AssetPath) ? "(не сохранена)" : AssetPath)} · {Location}";
    }

    public static class KeyUsages
    {
        private const string ProgressTitle = "Typecraft: поиск использований ключа";

        private static readonly List<Func<string, string, IEnumerable<KeyUsage>>> Sources = new List<Func<string, string, IEnumerable<KeyUsage>>>();

        public static void RegisterSource(Func<string, string, IEnumerable<KeyUsage>> source)
        {
            if (source != null && !Sources.Contains(source))
                Sources.Add(source);
        }

        public static List<KeyUsage> Find(string table, string key)
        {
            var found = new List<KeyUsage>();
            var prefabsInScenes = new HashSet<string>();

            foreach (Scene scene in ProjectScan.LoadedScenes())
            foreach (TypecraftText text in Texts(scene.GetRootGameObjects(), table, key))
            {
                found.Add(new KeyUsage(KeyUsageKind.Scene, scene.path, ProjectScan.HierarchyPath(text.transform), ProjectScan.SiblingPath(text.transform), 0, text.gameObject));
                prefabsInScenes.UnionWith(SourcePrefabs(text));
            }

            GameObject stageRoot = ProjectScan.OpenPrefabRoot();
            if (stageRoot != null)
            {
                foreach (TypecraftText text in Texts(new[] { stageRoot }, table, key))
                    found.Add(new KeyUsage(KeyUsageKind.Prefab, ProjectScan.OpenPrefabPath(), ProjectScan.HierarchyPath(text.transform, stageRoot.transform), ProjectScan.SiblingPath(text.transform, stageRoot.transform), 0, text.gameObject));
            }

            foreach (Func<string, string, IEnumerable<KeyUsage>> source in Sources)
            {
                try
                {
                    found.AddRange(source(table, key) ?? Enumerable.Empty<KeyUsage>());
                }
                catch (Exception exception)
                {
                    TypecraftLog.Error($"Поиск использований ключа: {exception}");
                }
            }

            var filter = new Regex(Regex.Escape(key));
            List<string> prefabs = ProjectScan.PrefabPaths();
            List<string> scenes = ProjectScan.ClosedScenePaths();
            List<string> scripts = ProjectScan.AssetPaths("t:Script").Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToList();
            int total = prefabs.Count + scenes.Count + scripts.Count;
            int done = 0;

            try
            {
                foreach (string path in prefabs)
                {
                    if (Cancelled(path, done++, total))
                        return null;
                    if (prefabsInScenes.Contains(path) || !ProjectScan.MayContain(path, filter))
                        continue;

                    found.AddRange(ProjectScan.InPrefab(path, root => Texts(new[] { root }, table, key)
                        .Select(text => new KeyUsage(KeyUsageKind.Prefab, path, ProjectScan.HierarchyPath(text.transform, root.transform), ProjectScan.SiblingPath(text.transform, root.transform), 0, null))
                        .ToList()));
                }

                foreach (string path in scenes)
                {
                    if (Cancelled(path, done++, total))
                        return null;
                    if (!ProjectScan.MayContain(path, filter))
                        continue;

                    found.AddRange(ProjectScan.InClosedScene(path, scene => Texts(scene.GetRootGameObjects(), table, key)
                        .Select(text => new KeyUsage(KeyUsageKind.Scene, path, ProjectScan.HierarchyPath(text.transform), ProjectScan.SiblingPath(text.transform), 0, null))
                        .ToList()));
                }

                string literal = "\"" + key + "\"";
                foreach (string path in scripts)
                {
                    if (Cancelled(path, done++, total))
                        return null;
                    found.AddRange(LiteralLines(path, literal).Select(line => new KeyUsage(KeyUsageKind.Script, path, null, null, line, null)));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return found;
        }

        public static void Reveal(KeyUsage usage)
        {
            switch (usage.Kind)
            {
                case KeyUsageKind.Script:
                    RevealScript(usage);
                    break;
                case KeyUsageKind.Prefab:
                    RevealPrefab(usage);
                    break;
                case KeyUsageKind.Asset:
                    usage.RevealAsset();
                    break;
                default:
                    RevealScene(usage);
                    break;
            }
        }

        private static IEnumerable<TypecraftText> Texts(IEnumerable<GameObject> roots, string table, string key) =>
            roots.SelectMany(root => root.GetComponentsInChildren<TypecraftText>(true)).Where(text => SceneTexts.Uses(text, table, key));

        private static IEnumerable<string> SourcePrefabs(Object target)
        {
            for (Object source = PrefabUtility.GetCorrespondingObjectFromSource(target); source != null; source = PrefabUtility.GetCorrespondingObjectFromSource(source))
                yield return AssetDatabase.GetAssetPath(source);
        }

        private static IEnumerable<int> LiteralLines(string path, string literal)
        {
            string text = File.ReadAllText(Path.GetFullPath(path));
            int line = 1;
            int scanned = 0;
            for (int index = text.IndexOf(literal, StringComparison.Ordinal); index >= 0; index = text.IndexOf(literal, index + literal.Length, StringComparison.Ordinal))
            {
                for (; scanned < index; scanned++)
                {
                    if (text[scanned] == '\n')
                        line++;
                }

                yield return line;
            }
        }

        private static bool Cancelled(string path, int done, int total) =>
            EditorUtility.DisplayCancelableProgressBar(ProgressTitle, path, total == 0 ? 1f : (float)done / total);

        private static void RevealScript(KeyUsage usage)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(usage.AssetPath);
            if (script != null)
                AssetDatabase.OpenAsset(script, usage.Line);
        }

        private static void RevealPrefab(KeyUsage usage)
        {
            if (usage.Live != null)
            {
                Select(usage.Live);
                return;
            }

            PrefabStage stage = PrefabStageUtility.OpenPrefab(usage.AssetPath);
            GameObject root = stage != null ? stage.prefabContentsRoot : null;
            if (root == null)
                return;

            Select(ProjectScan.Find(new[] { root }, usage.Siblings, usage.ObjectPath) ?? root);
        }

        private static void RevealScene(KeyUsage usage)
        {
            if (usage.Live != null)
            {
                if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                    StageUtility.GoToMainStage();
                Select(usage.Live);
                return;
            }

            if (string.IsNullOrEmpty(usage.AssetPath))
                return;

            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                StageUtility.GoToMainStage();

            Scene scene = SceneManager.GetSceneByPath(usage.AssetPath);
            if (!scene.isLoaded)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                scene = EditorSceneManager.OpenScene(usage.AssetPath, OpenSceneMode.Single);
            }

            GameObject target = ProjectScan.Find(scene.GetRootGameObjects(), usage.Siblings, usage.ObjectPath);
            if (target == null)
            {
                TypecraftLog.Warning($"В сцене {usage.AssetPath} не найден объект {usage.ObjectPath}");
                return;
            }

            Select(target);
        }

        private static void Select(GameObject target)
        {
            Selection.activeGameObject = target;
            EditorGUIUtility.PingObject(target);
            EditorApplication.delayCall += () =>
            {
                if (target != null && SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.FrameSelected();
            };
        }
    }
}
