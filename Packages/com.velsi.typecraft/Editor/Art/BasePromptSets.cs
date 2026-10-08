using System.IO;
using UnityEditor;
using UnityEngine;
using Velsi.Typecraft.Library;

namespace Velsi.Typecraft.Editor.Art
{
    public static class BasePromptSets
    {
        private const string PackageRoot = "Packages/com.velsi.typecraft";
        private const string SourceRoot = PackageRoot + "/Art~/Prompts";
        private const string OutputRoot = PackageRoot + "/Art/Prompts";

        public static bool IsBaseSet(IconSet set) =>
            AssetDatabase.GetAssetPath(set).StartsWith(OutputRoot + "/");

        [MenuItem("Tools/Typecraft/Rebuild Base Prompt Sets")]
        public static void RebuildAll()
        {
            string[] sourceDirs = Directory.GetDirectories(Path.GetFullPath(SourceRoot));
            int built = 0;

            try
            {
                for (int i = 0; i < sourceDirs.Length; i++)
                {
                    string setName = Path.GetFileName(sourceDirs[i]);
                    EditorUtility.DisplayProgressBar("Typecraft", $"Building {setName}", (float)i / sourceDirs.Length);

                    IconSet set = LoadOrCreate(setName);
                    set.BuildSettings.SourceFolder = $"{SourceRoot}/{setName}";

                    if (IconSetBuilder.Build(set))
                        built++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            TypecraftLog.Info($"Base prompt sets rebuilt: {built}/{sourceDirs.Length}");
        }

        [MenuItem("CONTEXT/IconSet/Rebuild Atlas")]
        private static void RebuildFromContext(MenuCommand command)
        {
            if (command.context is IconSet set)
                IconSetBuilder.Build(set);
        }

        private static IconSet LoadOrCreate(string setName)
        {
            string folder = $"{OutputRoot}/{setName}";
            string path = $"{folder}/{setName}.asset";

            IconSet set = AssetDatabase.LoadAssetAtPath<IconSet>(path);
            if (set != null)
                return set;

            if (!AssetDatabase.IsValidFolder(folder))
            {
                Directory.CreateDirectory(Path.GetFullPath(folder));
                AssetDatabase.Refresh();
            }

            set = ScriptableObject.CreateInstance<IconSet>();
            AssetDatabase.CreateAsset(set, path);
            return set;
        }
    }
}
