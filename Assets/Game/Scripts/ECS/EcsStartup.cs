using System;
using Game.Scripts.Configs;
using Game.Scripts.ECS.Systems;
using Game.Scripts.ECS.Systems.DebugHelpers;
using Game.Scripts.ECS.Systems.Doors;
using Game.Scripts.ECS.Systems.Interaction;
using Game.Scripts.Services.Input;
using Leopotam.EcsLite;
using Zenject;

namespace Game.Scripts.ECS
{
    public class EcsStartup : IInitializable, ITickable, IDisposable
    {
        private readonly IInputService _inputService;

        private readonly IEcsWorldProvider _ecsWorldProvider;
        private readonly GameConfigs _gameConfigs;
        private EcsSystems _systems;

        public EcsStartup(IInputService inputService, IEcsWorldProvider ecsWorldProvider, GameConfigs gameConfigs)
        {
            _inputService = inputService;
            _ecsWorldProvider = ecsWorldProvider;
            _gameConfigs = gameConfigs;
        }

        public void Initialize()
        {
            CreateSystems();
        }
        
        public void Tick()
        {
            _systems.Run();
        }

        public void Dispose()
        {
            _systems.Destroy();
            _ecsWorldProvider.World.Destroy();
        }

        private void CreateSystems()
        {
            _systems = new EcsSystems(_ecsWorldProvider.World);
            
            AddInputSystems();
            AddSimulationSystems();
            AddPresentationSystems();
            AddDebugSystems();
            AddCleanupSystems();
            
            _systems.Init();
        }
        
        private void AddInputSystems()
        {
            _systems.Add(new PlayerInputSystem(_inputService));
        }

        private void AddSimulationSystems()
        {
            _systems
                .Add(new FirstPersonLookRotationSystem(_gameConfigs.PlayerConfig))
                .Add(new InteractionRaycastSystem())
                .Add(new InteractionRequestSystem())
                .Add(new GravitySystem())
                .Add(new DoorInteractionSystem());
            
        }

        private void AddPresentationSystems()
        {
            _systems
                .Add(new PlayerMovementSystem(_gameConfigs.PlayerConfig))
                .Add(new CameraShakeSystem())
                .Add(new OutlineViewSystem())
                .Add(new DoorAnimationSystem());
        }

        private void AddDebugSystems()
        {
#if UNITY_EDITOR
            _systems.Add(new Leopotam.EcsLite.UnityEditor.EcsWorldDebugSystem())
                .Add(new Leopotam.EcsLite.UnityEditor.EcsSystemsDebugSystem())
                .Add(new DebugDrawSystem());
#endif
        }

        private void AddCleanupSystems()
        {
            _systems.Add(new ClearInteractionRequestSystem());
        }
    }
}