using Game.Scripts.Services.Loading;
using Game.Scripts.Services.Loading.Operations;
using Game.Scripts.Services.SceneLoader;
using Zenject;

namespace Game.Scripts.Services.Booststrap
{
    public class GameBootstrapper : IInitializable
    {
        private readonly ILoadingService _loadingService;
        private readonly ISceneLoader _sceneLoader;

        public GameBootstrapper(ILoadingService loadingService, ISceneLoader sceneLoader)
        {
            _loadingService = loadingService;
            _sceneLoader = sceneLoader;
        }

        public void Initialize()
        {
            LoadGameScene();
        }

        private void LoadGameScene()
        {
            ILoadingOperation[] operations =
            {
                new LoadingSceneOperation(_sceneLoader, ScenesName.Playroom) 
            };

            _loadingService.BeginLoading(operations);
        }
    }
}