using Game.Scripts.ECS.Components.DebugHelpers;
using Game.Scripts.ECS.Components.Interaction;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems.DebugHelpers
{
    public class DebugDrawSystem : IEcsInitSystem, IEcsRunSystem
    {
        private EcsFilter _filter;
        private EcsPool<PlayerRefs> _poolPlayerRefs;
        private EcsPool<Interactor> _poolInteractors;

        public void Init(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            _filter = world.Filter<PlayerTag>().Inc<PlayerRefs>().Inc<Interactor>().End();
            _poolPlayerRefs = world.GetPool<PlayerRefs>();
            _poolInteractors = world.GetPool<Interactor>();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter)
            {
                ref PlayerRefs playerRefs = ref _poolPlayerRefs.Get(entity);
                ref Interactor interactor = ref _poolInteractors.Get(entity);

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
}
