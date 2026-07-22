using Game.Scripts.ECS.Components;
using Game.Scripts.Services.Input;
using Leopotam.EcsLite;

namespace Game.Scripts.ECS.Systems
{
    public class PlayerInputSystem : IEcsRunSystem
    {
        private readonly IInputService _inputService;
        private float _xRotation;

        public PlayerInputSystem(IInputService inputService)
        {
            _inputService = inputService;
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            EcsFilter filter = world.Filter<PlayerTag>().Inc<HorizontalMovement>().Inc<MouseInputDirection>().End();
            EcsPool<HorizontalMovement> horizontalPool = world.GetPool<HorizontalMovement>();
            EcsPool<MouseInputDirection> lookPool = world.GetPool<MouseInputDirection>();
            
            foreach (int entity in filter)
            {
                ref HorizontalMovement horizontalMovement = ref horizontalPool.Get(entity);
                ref MouseInputDirection mouseInputDirection = ref lookPool.Get(entity);

                mouseInputDirection.Direction = _inputService.Look.CurrentValue;
                horizontalMovement.Direction = _inputService.Move.CurrentValue;
            }
        }
    }
}