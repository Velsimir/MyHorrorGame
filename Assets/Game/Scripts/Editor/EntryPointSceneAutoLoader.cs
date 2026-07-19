using UnityEditor;
using UnityEditor.SceneManagement;

namespace Game.Scripts.Editor
{
    [InitializeOnLoad]
    public class EntryPointSceneAutoLoader
    {
        private const string MenuPath = "PlayFromBootstrap/Enebled";
        private const string PlayFromBootstrapKey = "PlayFromBootstrapKey";
        private const int BootSceneIndex = 0;

        static EntryPointSceneAutoLoader()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            bool result = EditorPrefs.GetBool(PlayFromBootstrapKey);
            EditorPrefs.SetBool(PlayFromBootstrapKey, !result);
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(PlayFromBootstrapKey, !EditorPrefs.GetBool(PlayFromBootstrapKey));
            return true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (EditorPrefs.GetBool(PlayFromBootstrapKey) == false)
                {
                    EditorSceneManager.playModeStartScene = null;
                    return;
                }

                if (EditorBuildSettings.scenes.Length == 0)
                    return;

                EditorSceneManager.playModeStartScene =
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[BootSceneIndex].path);
            }
        }
    }
}