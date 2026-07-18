using Game.Scripts.Services.CoroutineRunner;
using Game.Scripts.Services.Input;
using Game.Scripts.Services.Loading;
using Game.Scripts.Services.SceneLoader;
using Game.Scripts.Views.CurtainLogic;
using Game.Scripts.Services.Update;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Dependencies
{
    public class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private Curtain _curtain;
        
        private GameObject _servicesRootPrefab;
        
        public override void InstallBindings()
        {
            _servicesRootPrefab = new GameObject("ServicesRoot");
            DontDestroyOnLoad(_servicesRootPrefab);
            
            _curtain = Instantiate(_curtain);
            DontDestroyOnLoad(_curtain);
            Container.BindInterfacesAndSelfTo<ICurtain>().FromInstance(_curtain).AsSingle();
            
            Container.BindInterfacesTo<InputService>().AsSingle().NonLazy();
            
            CoroutineRunnerService coroutineRunner = _servicesRootPrefab.AddComponent<CoroutineRunnerService>();
            Container.Bind<ICoroutineRunnerService>().FromInstance(coroutineRunner).AsSingle().NonLazy();
            
            UpdateService updateService = _servicesRootPrefab.AddComponent<UpdateService>();
            Container.Bind<UpdateService>().FromInstance(updateService).AsSingle().NonLazy();
            
            Container.BindInterfacesTo<SceneLoaderService>().AsSingle().NonLazy();
            Container.BindInterfacesTo<LoadingService>().AsSingle().NonLazy();
        }
    }
}