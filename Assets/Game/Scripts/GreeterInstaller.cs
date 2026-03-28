using Reflex.Core;
using UnityEngine;

namespace Game.Scripts
{
    public class GreeterInstaller : MonoBehaviour, IInstaller
    {
        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterValue("World");
        }
    }
}