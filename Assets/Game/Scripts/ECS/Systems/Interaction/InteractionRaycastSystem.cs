using Game.Scripts.ECS.Authoring;
using Game.Scripts.ECS.Components.Interaction;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems.Interaction
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
            var poolInteractable = world.GetPool<Interactable>();
            
            foreach (var entityPlayer in filterPlayer)
            {
                foreach (int entityFocused in filterFocused) poolFocused.Del(entityFocused);
                
                PlayerRefs playerRefs = poolPlayerRefs.Get(entityPlayer);
                Interactor interactor = poolInteractorRefs.Get(entityPlayer);

                if (Physics.SphereCast(playerRefs.HeadTransform.position, interactor.SphereCastRadius,
                        playerRefs.HeadTransform.forward, out RaycastHit hit, 
                        interactor.Distance, interactor.Mask))
                {
                    if (hit.collider.TryGetComponent(out InteractionObjectAuthoring authoring)
                        && authoring.Entity.Unpack(world, out int targetEntity) && poolInteractable.Has(targetEntity))
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