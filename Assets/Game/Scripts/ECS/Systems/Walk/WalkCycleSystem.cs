using Game.Scripts.Configs;
using Game.Scripts.ECS.Components.Movement;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems.Walk
{
    public class WalkCycleSystem : IEcsRunSystem
    {
        private PlayerConfig _playerConfig;

        public WalkCycleSystem(PlayerConfig playerConfig)
        {
            _playerConfig = playerConfig;
        }

        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var filter = world.Filter<PlayerTag>().Inc<HorizontalMovement>().Inc<WalkCycle>().Inc<VerticalMovement>().End();

            if (filter.GetEntitiesCount() == 0) return;
            var player = filter.GetRawEntities()[0];
            
            var verticalMovementPool = world.GetPool<VerticalMovement>();
            ref var verticalMovement = ref verticalMovementPool.Get(player);
            
            var horizontalMovementPool = world.GetPool<HorizontalMovement>();
            ref var horizontalMovement = ref horizontalMovementPool.Get(player);
            
            var walkCyclePool = world.GetPool<WalkCycle>();
            ref var walkCycle = ref walkCyclePool.Get(player);
            
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