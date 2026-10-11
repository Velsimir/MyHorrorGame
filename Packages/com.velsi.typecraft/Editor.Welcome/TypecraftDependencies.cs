using System;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Velsi.Typecraft.Editor.Welcome
{
    internal static class TypecraftDependencies
    {
        public const string PackageName = "com.velsi.typecraft";
        public const string TmpEffectsVersion = "0.3.2";
        public const string InputSystemPackage = "com.unity.inputsystem";
        public const string LocalizationPackage = "com.unity.localization";

        private const string TmpEffectsArchive = "com.luca3317.tmpeffects-" + TmpEffectsVersion + ".tgz";
        private const string TmpEffectsGitUrl = "https://github.com/Luca3317/TMPEffects.git?path=/Package#" + TmpEffectsVersion;
        private const string TmpEffectsResources = "Packages/com.luca3317.tmpeffects/Resources/TMPEffects Required Resources.unitypackage";

        private static AddRequest _request;

        public static event Action Changed;

        public static PackageInfo Self => PackageInfo.FindForAssembly(typeof(TypecraftDependencies).Assembly);
        public static string Version => Self?.version ?? "?";
        public static string PackageRoot => Self?.resolvedPath;

        public static bool HasTmpEssentials => AssetDatabase.FindAssets("t:TMP_Settings").Length > 0;
        public static bool HasTmpEffects => HasType("TMPEffects.Components.TMPAnimator, TMPEffects");
        public static bool HasTmpEffectsResources => AssetDatabase.FindAssets("t:TMPEffectsSettings").Length > 0;
        public static bool HasInputSystem => HasType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
        public static bool HasLocalization => HasType("UnityEngine.Localization.Settings.LocalizationSettings, Unity.Localization");
        public static bool HasSettings => AssetDatabase.FindAssets("t:TypecraftSettings").Length > 0;

        public static bool Busy => _request != null;
        public static string Installing { get; private set; }
        public static string LastError { get; private set; }

        public static void ImportTmpEssentials()
        {
            TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);
            Changed?.Invoke();
        }

        public static void ImportTmpEffectsResources()
        {
            AssetDatabase.ImportPackage(TmpEffectsResources, false);
            Changed?.Invoke();
        }

        public static void InstallTmpEffects()
        {
            string bundled = PackageRoot == null ? null : Path.Combine(PackageRoot, "Vendor~", TmpEffectsArchive);
            if (bundled == null || !File.Exists(bundled))
            {
                Install(TmpEffectsGitUrl, "TMPEffects");
                return;
            }

            string target = Path.Combine(ProjectPackagesFolder, TmpEffectsArchive);
            if (!File.Exists(target))
                File.Copy(bundled, target);

            Install("file:" + TmpEffectsArchive, "TMPEffects");
        }

        public static void Install(string packageId, string displayName)
        {
            if (Busy)
                return;

            LastError = null;
            Installing = displayName;
            _request = Client.Add(packageId);
            EditorApplication.update += Poll;
            Changed?.Invoke();
        }

        private static string ProjectPackagesFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages"));

        private static void Poll()
        {
            if (_request == null || !_request.IsCompleted)
                return;

            EditorApplication.update -= Poll;
            if (_request.Status == StatusCode.Failure)
            {
                LastError = $"{Installing}: {_request.Error?.message}";
                Debug.LogError($"[Typecraft] {LastError}");
            }

            _request = null;
            Installing = null;
            Changed?.Invoke();
        }

        private static bool HasType(string assemblyQualifiedName) => Type.GetType(assemblyQualifiedName) != null;
    }
}
