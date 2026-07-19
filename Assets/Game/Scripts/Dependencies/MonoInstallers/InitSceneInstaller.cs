using Game.Scripts.Services.Booststraps;
using Zenject;

namespace Game.Scripts.Dependencies.MonoInstallers
{
    public class InitSceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<GameBootstrapper>().AsSingle().NonLazy();
        }
    }
}