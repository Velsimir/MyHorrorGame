using Game.Scripts.ECS.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems
{
    public class DebugFearSystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var filter = world.Filter<PlayerTag>().Inc<FearFactor>().End();
            var pool = world.GetPool<FearFactor>();

            foreach (var fearEntity in filter)
            {
                ref var fear = ref pool.Get(fearEntity);
                fear.Value = (Mathf.Sin(Time.time * 2f) + 1f) * 0.5f;
            }
        }
    }
}