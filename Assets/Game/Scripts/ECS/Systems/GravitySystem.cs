using Game.Scripts.ECS.Components.Movement;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems
{
    public class GravitySystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var filterVerticalMovement = world.Filter<VerticalMovement>().End();
            var poolVerticalMovement = world.GetPool<VerticalMovement>();
            
            var filterGravity = world.Filter<Gravity>().End();
            var poolGravity = world.GetPool<Gravity>();
            
            var gravity = poolGravity.Get(filterGravity.GetRawEntities()[0]);
            
            foreach (var entity in filterVerticalMovement)
            {
                ref var verticalMovement = ref poolVerticalMovement.Get(entity);

                if (verticalMovement.IsGrounded)
                    verticalMovement.Velocity = gravity.GroundedVelocity;
                else
                    verticalMovement.Velocity += gravity.Acceleration * Time.deltaTime;
            }
        }
    }
}