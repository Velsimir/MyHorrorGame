using Game.Scripts.Services.Input;
using Game.Scripts.Services.Update;
using Unity.VisualScripting;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Dependencies
{
    public class ProjectInstaller : MonoInstaller
    {
        private GameObject _servicesRootPrefab;
        
        public override void InstallBindings()
        {
            _servicesRootPrefab = new GameObject("ServicesRoot");

            DontDestroyOnLoad(_servicesRootPrefab);

            Container.Bind<InputService>().AsSingle().NonLazy();
            
            CoroutineRunner coroutineRunner = _servicesRootPrefab.AddComponent<CoroutineRunner>();
            Container.Bind<CoroutineRunner>().FromInstance(coroutineRunner).AsSingle().NonLazy();
            
            UpdateService updateService = _servicesRootPrefab.AddComponent<UpdateService>();
            Container.Bind<UpdateService>().FromInstance(updateService).AsSingle().NonLazy();
            
            Debug.Log("Я закончив");
        }
    }
}