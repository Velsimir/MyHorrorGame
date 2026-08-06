using Game.Scripts.ECS.Components.Input;
using Game.Scripts.ECS.Components.Interaction;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems
{
    public class InteractionRequestSystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var filterFocusedObjects = world.Filter<Focused>().Inc<Interactable>().End();
            var filterPlayerInteractInput = world.Filter<PlayerTag>().Inc<InteractInput>().End();

            if (filterPlayerInteractInput.GetEntitiesCount() == 0)
                return;
            
            var inputEntity = filterPlayerInteractInput.GetRawEntities()[0];
            
            var interactInputPool = world.GetPool<InteractInput>();
            var interactionRequest = world.GetPool<InteractionRequest>();
            ref var interactInput = ref interactInputPool.Get(inputEntity);

            if (interactInput.IsInteractPressed)
            {
                foreach (var objectEntity in filterFocusedObjects)
                {
                    {
                        interactionRequest.Add(objectEntity);
                        Debug.Log("interacted with " + objectEntity);
                    }
                }
            }
        }
    }
}