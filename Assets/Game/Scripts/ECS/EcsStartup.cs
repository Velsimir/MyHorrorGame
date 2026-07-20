using System;
using Game.Scripts.Configs;
using Game.Scripts.ECS.Components;
using Game.Scripts.ECS.Systems;
using Game.Scripts.Services.Input;
using Leopotam.EcsLite;
using Zenject;

namespace Game.Scripts.ECS
{
    public class EcsStartup : IInitializable, ITickable, IDisposable
    {
        private readonly IInputService _inputService;

        private IEcsWorldProvider _ecsWorldProvider;
        private readonly PlayerConfig _playerConfig;
        private EcsSystems _systems;

        public EcsStartup(IInputService inputService, IEcsWorldProvider ecsWorldProvider, PlayerConfig playerConfig)
        {
            _inputService = inputService;
            _ecsWorldProvider = ecsWorldProvider;
            _playerConfig = playerConfig;
        }

        public void Initialize()
        {
            CreateSystems();
        }

        private void CreateSystems()
        {
            _systems = new EcsSystems(_ecsWorldProvider.World);

            _systems
                .Add(new PlayerInputSystem(_inputService))
                .Add(new CameraFirstPersonRotationSystem(_playerConfig))
                .Add(new GravitySystem())
                .Add(new PlayerMovementSystem(_playerConfig))
                .Init();
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
    }
}