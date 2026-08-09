using Game.Scripts.ECS.Components.Input;
using Game.Scripts.ECS.Components.Interaction;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;

namespace Game.Scripts.ECS.Systems.Interaction
{
    public class InteractionRequestSystem : IEcsInitSystem, IEcsRunSystem
    {
        private EcsFilter _filterFocusedObjects;
        private EcsFilter _filterPlayerInteractInput;
        private EcsPool<InteractInput> _interactInputPool;
        private EcsPool<InteractionRequest> _interactionRequestPool;

        public void Init(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            _filterFocusedObjects = world.Filter<Focused>().Inc<Interactable>().End();
            _filterPlayerInteractInput = world.Filter<PlayerTag>().Inc<InteractInput>().End();
            _interactInputPool = world.GetPool<InteractInput>();
            _interactionRequestPool = world.GetPool<InteractionRequest>();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int playerEntity in _filterPlayerInteractInput)
            {
                ref var interactInput = ref _interactInputPool.Get(playerEntity);

                if (interactInput.IsInteractPressed == false)
                    continue;

                foreach (int objectEntity in _filterFocusedObjects)
                {
                    if (_interactionRequestPool.Has(objectEntity) == false)
                        _interactionRequestPool.Add(objectEntity);
                }
            }
        }
    }
}
