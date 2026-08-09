using Game.Scripts.ECS.Components.Movement;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems
{
    public class GravitySystem : IEcsInitSystem, IEcsRunSystem
    {
        private EcsFilter _filterVerticalMovement;
        private EcsPool<VerticalMovement> _poolVerticalMovement;
        private EcsPool<Gravity> _poolGravity;

        public void Init(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            _filterVerticalMovement = world.Filter<VerticalMovement>().Inc<Gravity>().End();
            _poolVerticalMovement = world.GetPool<VerticalMovement>();
            _poolGravity = world.GetPool<Gravity>();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filterVerticalMovement)
            {
                ref var verticalMovement = ref _poolVerticalMovement.Get(entity);
                ref var gravity = ref _poolGravity.Get(entity);
                
                if (verticalMovement.IsGrounded)
                    verticalMovement.Velocity = gravity.GroundedVelocity;
                else
                    verticalMovement.Velocity += gravity.Acceleration * Time.deltaTime;
            }
        }
    }
}