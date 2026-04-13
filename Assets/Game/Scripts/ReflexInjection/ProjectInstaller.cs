using Game.Scripts.Services.Input;
using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using Resolution = Reflex.Enums.Resolution;

namespace Game.Scripts.ReflexInjection
{
    public class ProjectInstaller : MonoBehaviour, IInstaller
    {
        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType(typeof(InputService), Lifetime.Singleton, Resolution.Lazy);
        }
    }
}
