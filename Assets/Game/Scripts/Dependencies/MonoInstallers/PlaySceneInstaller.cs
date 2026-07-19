using Game.Scripts.ECS;
using Zenject;

namespace Game.Scripts.Dependencies.MonoInstallers
{
    public class PlaySceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<EcsStartup>().AsSingle().NonLazy();
        }
    }
}