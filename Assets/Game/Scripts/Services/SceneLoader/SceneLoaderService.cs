using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Game.Scripts.Services.SceneLoader
{
    public class SceneLoaderService : ISceneLoader
    {
        private readonly ZenjectSceneLoader _sceneLoader;
        
        private HashSet<string> _loadedScenes;
        
        public SceneLoaderService(ZenjectSceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
            CacheBuildScenes();
        }
        
        public async UniTask LoadSceneAddictiveAsync(ScenesName sceneName, Action<DiContainer> callback = null)
        {
            await _sceneLoader.LoadSceneAsync(sceneName.HumanName(), LoadSceneMode.Additive, callback);
        }
        
        public async UniTask LoadSceneSingleAsync(ScenesName sceneName, Action<DiContainer> callback = null)
        {
            await _sceneLoader.LoadSceneAsync(sceneName.HumanName(), LoadSceneMode.Single, callback);
        }

        public async UniTask UnloadSceneAsync(ScenesName sceneName)
        {
            await SceneManager.UnloadSceneAsync(sceneName.HumanName());
        }

        private void CacheBuildScenes()
        {
            _loadedScenes = new HashSet<string>();
            int sceneCount = SceneManager.sceneCountInBuildSettings;

            for (int i = 0; i < sceneCount; i++)
            {
                _loadedScenes.Add(SceneManager.GetSceneByBuildIndex(i).name);
                Debug.Log($"Loaded scene #{SceneManager.GetSceneByBuildIndex(i).name}: {_loadedScenes.Count}");
            }
        }
    }

    public interface ISceneLoader
    {
        UniTask LoadSceneAddictiveAsync(ScenesName sceneName, Action<DiContainer> callback = null);
        UniTask LoadSceneSingleAsync(ScenesName sceneName, Action<DiContainer> callback = null);
        UniTask UnloadSceneAsync(ScenesName sceneName);
    }

    public enum ScenesName
    {
        Init,
        Loading,
        Playroom
    }
}