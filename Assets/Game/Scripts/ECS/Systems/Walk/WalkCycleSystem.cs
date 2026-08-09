using Game.Scripts.Configs;
using Game.Scripts.ECS.Components.Movement;
using Game.Scripts.ECS.Components.Player;
using Game.Scripts.ECS.Components.Walk;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems.Walk
{
    public class WalkCycleSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly PlayerConfig _playerConfig;

        private EcsFilter _filter;
        private EcsPool<VerticalMovement> _verticalMovementPool;
        private EcsPool<HorizontalMovement> _horizontalMovementPool;
        private EcsPool<WalkCycle> _walkCyclePool;

        public WalkCycleSystem(PlayerConfig playerConfig)
        {
            _playerConfig = playerConfig;
        }

        public void Init(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            _filter = world.Filter<PlayerTag>().Inc<HorizontalMovement>().Inc<WalkCycle>().Inc<VerticalMovement>().End();
            _verticalMovementPool = world.GetPool<VerticalMovement>();
            _horizontalMovementPool = world.GetPool<HorizontalMovement>();
            _walkCyclePool = world.GetPool<WalkCycle>();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter)
            {
                ref var verticalMovement = ref _verticalMovementPool.Get(entity);
                ref var horizontalMovement = ref _horizontalMovementPool.Get(entity);
                ref var walkCycle = ref _walkCyclePool.Get(entity);

                float input = Mathf.Clamp01(horizontalMovement.Direction.magnitude);
                float speed = input * _playerConfig.Speed;

                float distance = speed * Time.deltaTime;

                if (!verticalMovement.IsGrounded) { distance = 0f; }

                walkCycle.Phase += distance / walkCycle.StrideLength;
                walkCycle.Phase = Mathf.Repeat(walkCycle.Phase, 1f);

                float target = verticalMovement.IsGrounded ? input : 0f;
                walkCycle.Intensity = Mathf.MoveTowards(walkCycle.Intensity, target, Time.deltaTime / walkCycle.SmoothTime);
            }
        }
    }
}
