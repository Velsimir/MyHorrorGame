using System;
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
        
        public SceneLoaderService(ZenjectSceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
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
    }

    public enum ScenesName
    {
        Init,
        Playroom
    }
}