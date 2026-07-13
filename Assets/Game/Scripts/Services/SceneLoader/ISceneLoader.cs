using System;
using Cysharp.Threading.Tasks;
using Zenject;

namespace Game.Scripts.Services.SceneLoader
{
    public interface ISceneLoader
    {
        UniTask LoadSceneAddictiveAsync(ScenesName sceneName, Action<DiContainer> callback = null);
        UniTask LoadSceneSingleAsync(ScenesName sceneName, Action<DiContainer> callback = null);
        UniTask UnloadSceneAsync(ScenesName sceneName);
    }
}