using Game.Scripts.ECS.Components.Doors;
using Game.Scripts.ECS.Components.Interaction;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems.Doors
{
    public class DoorAnimationSystem : IEcsInitSystem, IEcsRunSystem
    {
        private EcsFilter _filter;
        private EcsPool<Door> _pool;
        private EcsPool<Acting> _poolActing;

        public void Init(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            _filter = world.Filter<Door>().Inc<Acting>().End();
            _pool = world.GetPool<Door>();
            _poolActing = world.GetPool<Acting>();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter)
            {
                ref var door = ref _pool.Get(entity);
                float targetAngle = door.IsOpen 
                    ? door.OpenAngle 
                    : door.CloseAngle;
                
                door.CurrentAngle = Mathf.MoveTowardsAngle(door.CurrentAngle, targetAngle, door.Speed * Time.deltaTime);
                door.Pivot.localRotation = door.InitialRotation * Quaternion.Euler(0, door.CurrentAngle, 0);
                
                if (targetAngle == door.CurrentAngle)
                    _poolActing.Del(entity);
            }
        }
    }
}