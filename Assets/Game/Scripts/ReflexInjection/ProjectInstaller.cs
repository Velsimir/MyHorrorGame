using Game.Scripts.Services;
using Game.Scripts.Services.Input;
using Game.Scripts.Services.Update;
using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using Resolution = Reflex.Enums.Resolution;

namespace Game.Scripts.ReflexInjection
{
    public class ProjectInstaller : MonoBehaviour, IInstaller
    {
        public void InstallBindings(ContainerBuilder builder)
        {
            var go = new GameObject("[MonoServices]");
            DontDestroyOnLoad(go);

            IUpdateService updateService = go.AddComponent<UpdateService>();
            ICoroutineRunnerService coroutineRunnerService = go.AddComponent<CoroutineRunnerService>();

            builder.RegisterValue(updateService,
                new[] 
                    { 
                        typeof(IUpdateService), 
                        typeof(IUpdateService) 
                    });
            
            builder.RegisterValue(coroutineRunnerService, 
                new []
                {
                    typeof(ICoroutineRunnerService), 
                    typeof(CoroutineRunnerService)
                });

            builder.RegisterType(typeof(InputService), new[] { typeof(InputService), typeof(IInputService) },
                Lifetime.Singleton, Resolution.Lazy);
        }
    }
}