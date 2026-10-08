using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Velsi.Typecraft.Editor
{
    public static class ProjectScan
    {
        public const string ProjectRoot = "Assets";

        public static IEnumerable<Scene> LoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    yield return scene;
            }
        }

        public static GameObject OpenPrefabRoot()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            return stage != null ? stage.prefabContentsRoot : null;
        }

        public static string OpenPrefabPath()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            return stage != null ? stage.assetPath : null;
        }

        public static List<string> PrefabPaths() =>
            AssetPaths("t:Prefab")
                .Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) && path != OpenPrefabPath())
                .ToList();

        public static List<string> ClosedScenePaths() =>
            AssetPaths("t:Scene")
                .Where(path => !SceneManager.GetSceneByPath(path).isLoaded)
                .ToList();

        public static T InPrefab<T>(string path, Func<GameObject, T> read)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                return read(root);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static T InClosedScene<T>(string path, Func<Scene, T> read)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                return read(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static bool MayContain(string assetPath, Regex filter)
        {
            string text = File.ReadAllText(Path.GetFullPath(assetPath));
            if (!text.StartsWith("%YAML", StringComparison.Ordinal))
                return true;

            if (filter.IsMatch(text))
                return true;

            string unescaped = Regex.Replace(text, @"\\u([0-9A-Fa-f]{4})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
            return unescaped != text && filter.IsMatch(unescaped);
        }

        public static IEnumerable<string> AssetPaths(string filter) =>
            AssetDatabase.FindAssets(filter, new[] { ProjectRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(IsInProject)
                .Distinct();

        public static bool IsInProject(string path) =>
            !string.IsNullOrEmpty(path) && path.StartsWith(ProjectRoot + "/", StringComparison.Ordinal);

        public static string HierarchyPath(Transform transform, Transform top = null)
        {
            var names = new List<string>();
            for (Transform current = transform; current != null; current = current == top ? null : current.parent)
                names.Add(current.name);
            names.Reverse();
            return string.Join("/", names);
        }

        public static int[] SiblingPath(Transform transform, Transform top = null)
        {
            var indices = new List<int>();
            for (Transform current = transform; current != null; current = current == top ? null : current.parent)
                indices.Add(current == top && current.parent != null ? 0 : current.GetSiblingIndex());
            indices.Reverse();
            return indices.ToArray();
        }

        public static GameObject Find(IReadOnlyList<GameObject> roots, int[] siblings, string hierarchyPath)
        {
            GameObject bySiblings = FindBySiblings(roots, siblings);
            Transform top = bySiblings != null ? roots.Select(root => root.transform).FirstOrDefault(bySiblings.transform.IsChildOf) : null;
            if (bySiblings != null && HierarchyPath(bySiblings.transform, top) == hierarchyPath)
                return bySiblings;

            string[] names = hierarchyPath.Split('/');
            foreach (GameObject root in roots)
            {
                if (root.name != names[0])
                    continue;

                Transform current = root.transform;
                for (int i = 1; i < names.Length && current != null; i++)
                    current = current.Cast<Transform>().FirstOrDefault(child => child.name == names[i]);
                if (current != null)
                    return current.gameObject;
            }

            return bySiblings;
        }

        private static GameObject FindBySiblings(IReadOnlyList<GameObject> roots, int[] siblings)
        {
            if (siblings == null || siblings.Length == 0)
                return null;

            Transform current = siblings[0] >= 0 && siblings[0] < roots.Count ? roots[siblings[0]].transform : null;
            for (int i = 1; i < siblings.Length && current != null; i++)
                current = siblings[i] < current.childCount ? current.GetChild(siblings[i]) : null;
            return current != null ? current.gameObject : null;
        }
    }
}
