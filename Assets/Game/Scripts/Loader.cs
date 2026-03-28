using Reflex.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Game.Scripts
{
    public class Loader : MonoBehaviour
    {
        private void Awake()
        {
            ContainerScope.OnSceneContainerBuilding += InstallExtra;

            var loadHandle = Addressables.LoadSceneAsync("PlayRoom", LoadSceneMode.Single);

            loadHandle.Completed += OnSceneLoaded;
        }

        private void InstallExtra(UnityEngine.SceneManagement.Scene scene, ContainerBuilder builder)
        {
            builder.RegisterValue("of Developers");
        }

        private void OnSceneLoaded(AsyncOperationHandle<SceneInstance> obj)
        {
            ContainerScope.OnSceneContainerBuilding -= InstallExtra;
        }
    }
}