using Game.Scripts.ECS.Components.DebugHelpers;
using Game.Scripts.ECS.Components.Interaction;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems.DebugHelpers
{
    public class DebugDrawSystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var filter = world.Filter<PlayerTag>().Inc<PlayerRefs>().Inc<Interactor>().End();
            var poolPlayerRefs = world.GetPool<PlayerRefs>();
            var poolInteractors = world.GetPool<Interactor>();
            
            if (filter.GetEntitiesCount() == 0)
                return;

            var entity = filter.GetRawEntities()[0];
            var playerRefs = poolPlayerRefs.Get(entity);
            var interactor = poolInteractors.Get(entity);
            
            Vector3 origin = playerRefs.HeadTransform.position;
            Vector3 dir    = playerRefs.HeadTransform.forward;

            bool hasHit = Physics.SphereCast(origin, interactor.SphereCastRadius, dir,
                out RaycastHit hit, interactor.Distance, interactor.Mask);

            float dist = hasHit ? hit.distance : interactor.Distance;
            Color color = hasHit ? Color.green : Color.red;

            DebugDraw.WireSphere(origin, interactor.SphereCastRadius, color);
            DebugDraw.WireSphere(origin + dir * dist, interactor.SphereCastRadius, color);
            Debug.DrawLine(origin, origin + dir * dist, color);
        }
    }
}