using Game.Scripts.Services.SceneLoader;
using Zenject;

namespace Game.Scripts.Dependencies
{
    public class BootstrapInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<SceneLoaderService>().AsSingle().NonLazy();
        }
    }
}