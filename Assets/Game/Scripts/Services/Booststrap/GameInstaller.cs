using Zenject;

namespace Game.Scripts.Services.Booststrap
{
    public class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<GameBootstrapper>().AsSingle().NonLazy();
        }
    }
}