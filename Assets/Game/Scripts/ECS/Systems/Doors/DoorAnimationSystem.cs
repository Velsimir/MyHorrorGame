using Game.Scripts.ECS.Components.Doors;
using Game.Scripts.ECS.Components.Interaction;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems.Doors
{
    public class DoorAnimationSystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var filter = world.Filter<Door>().Inc<Acting>().End();
            var pool = world.GetPool<Door>();
            var poolActing = world.GetPool<Acting>();

            foreach (var entity in filter)
            {
                ref var door = ref pool.Get(entity);
                float targetAngle = door.IsOpen 
                    ? door.OpenAngle 
                    : door.CloseAngle;
                
                door.CurrentAngle = Mathf.MoveTowardsAngle(door.CurrentAngle, targetAngle, door.Speed * Time.deltaTime);
                door.Pivot.localRotation = door.InitialRotation * Quaternion.Euler(0, door.CurrentAngle, 0);
                
                if (targetAngle == door.CurrentAngle)
                    poolActing.Del(entity);
            }
        }
    }
}