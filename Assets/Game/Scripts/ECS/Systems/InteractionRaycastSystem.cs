using Game.Scripts.ECS.Components.Interaction;
using Game.Scripts.ECS.Components.MonoBehaviourRefs;
using Game.Scripts.ECS.Components.Tags;
using Game.Scripts.ECS.Providers;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems
{
    public class InteractionRaycastSystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var filterPlayer = world.Filter<PlayerTag>().Inc<PlayerRefs>().Inc<Interactor>().End();
            var filterFocused = world.Filter<Focused>().End();
            var poolPlayerRefs = world.GetPool<PlayerRefs>();
            var poolInteractorRefs = world.GetPool<Interactor>();
            var poolFocused = world.GetPool<Focused>();
            
            foreach (int e in filterFocused) poolFocused.Del(e);
            
            foreach (var entity in filterPlayer)
            {
                PlayerRefs playerRefs = poolPlayerRefs.Get(entity);
                Interactor interactor = poolInteractorRefs.Get(entity);
                
                if (Physics.Raycast(playerRefs.HeadTransform.position,
                        playerRefs.HeadTransform.forward, out RaycastHit hit, 
                        interactor.Distance, interactor.Mask))
                {
                    if (hit.collider.TryGetComponent(out InteractionObjectAuthoring authoring)
                        && authoring.Entity.Unpack(world, out int targetEntity))
                    {
                        poolFocused.Add(targetEntity);
                    }
                }

                Debug.DrawRay(playerRefs.HeadTransform.position, playerRefs.HeadTransform.forward * interactor.Distance,
                    Color.red);
            }
        }
    }
}