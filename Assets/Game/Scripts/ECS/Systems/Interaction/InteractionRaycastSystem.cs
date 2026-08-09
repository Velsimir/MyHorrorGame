using Game.Scripts.ECS.Authoring;
using Game.Scripts.ECS.Components.Interaction;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems.Interaction
{
    public class InteractionRaycastSystem : IEcsInitSystem, IEcsRunSystem
    {
        private EcsWorld _world;
        private EcsFilter _filterPlayer;
        private EcsFilter _filterFocused;
        private EcsPool<PlayerRefs> _poolPlayerRefs;
        private EcsPool<Interactor> _poolInteractorRefs;
        private EcsPool<Focused> _poolFocused;
        private EcsPool<Interactable> _poolInteractable;

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _filterPlayer = _world.Filter<PlayerTag>().Inc<PlayerRefs>().Inc<Interactor>().End();
            _filterFocused = _world.Filter<Focused>().End();

            _poolPlayerRefs = _world.GetPool<PlayerRefs>();
            _poolInteractorRefs = _world.GetPool<Interactor>();
            _poolFocused = _world.GetPool<Focused>();
            _poolInteractable = _world.GetPool<Interactable>();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int entityFocused in _filterFocused) _poolFocused.Del(entityFocused);

            foreach (int entityPlayer in _filterPlayer)
            {
                ref PlayerRefs playerRefs = ref _poolPlayerRefs.Get(entityPlayer);
                ref Interactor interactor = ref _poolInteractorRefs.Get(entityPlayer);

                if (Physics.SphereCast(playerRefs.HeadTransform.position, interactor.SphereCastRadius,
                        playerRefs.HeadTransform.forward, out RaycastHit hit,
                        interactor.Distance, interactor.Mask))
                {
                    InteractionObjectAuthoring authoring = hit.collider.GetComponentInParent<InteractionObjectAuthoring>();

                    if (authoring != null && authoring.Entity.Unpack(_world, out int targetEntity) && _poolInteractable.Has(targetEntity))
                    {
                        if (_poolFocused.Has(targetEntity) == false)
                            _poolFocused.Add(targetEntity);
                    }
                }
            }
        }
    }
}
