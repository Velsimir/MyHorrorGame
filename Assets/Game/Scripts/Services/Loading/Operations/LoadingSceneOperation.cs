using Cysharp.Threading.Tasks;
using Game.Scripts.Services.SceneLoader;

namespace Game.Scripts.Services.Loading.Operations
{
    public class LoadingSceneOperation : ILoadingOperation
    {
        private readonly ISceneLoader _loadingService;
        private readonly ScenesName _scenesName;
        
        public LoadingSceneOperation(ISceneLoader sceneLoader, ScenesName scenesName)
        {
            _loadingService = sceneLoader;
            _scenesName = scenesName;
        }

        public UniTask ExecuteAsync()
        {
            return _loadingService.LoadSceneSingleAsync(_scenesName);
        }
    }
}