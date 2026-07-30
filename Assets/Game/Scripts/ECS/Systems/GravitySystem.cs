using Game.Scripts.ECS.Components;
using Game.Scripts.ECS.Components.Movement;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems
{
    public class GravitySystem : IEcsRunSystem
    {
        private readonly float _gravity = -9.81f;
        private readonly float _gravityOnGround = -2f;

        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var filter = world.Filter<VerticalMovement>().End();
            var pool = world.GetPool<VerticalMovement>();

            foreach (var entity in filter)
            {
                ref var verticalMovement = ref pool.Get(entity);

                if (verticalMovement.IsGrounded)
                    verticalMovement.Velocity = _gravityOnGround;

                else
                    verticalMovement.Velocity += _gravity * Time.deltaTime;
            }
        }
    }
}