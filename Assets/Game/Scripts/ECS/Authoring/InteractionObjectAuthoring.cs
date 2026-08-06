using Game.Scripts.ECS.Components.Interaction;
using Leopotam.EcsLite;
using UnityEngine;
using Zenject;

namespace Game.Scripts.ECS.Authoring
{
    public abstract class InteractionObjectAuthoring : MonoBehaviour
    {
        [SerializeField] private OutlineRefs _outlineRefs;
            
        [Inject] private IEcsWorldProvider EcsWorldProvider;
        private EcsPackedEntity _entity;
        
        public EcsPackedEntity Entity => _entity;

        private void Start()
        {
            int entity = EcsWorldProvider.World.NewEntity();

            _entity = EcsWorldProvider.World.PackEntity(entity);

            EcsWorldProvider.World.GetPool<Interactable>().Add(entity);
            
            ref var poolOutlineRefs = ref EcsWorldProvider.World.GetPool<OutlineRefs>().Add(entity);
            poolOutlineRefs.MeshRenderer = _outlineRefs.MeshRenderer;
            poolOutlineRefs.WithOutline = _outlineRefs.WithOutline;
            poolOutlineRefs.Default = _outlineRefs.Default;

            AddSpecificTag(EcsWorldProvider.World, entity);
            
            gameObject.layer = 6;
        }

        protected abstract void AddSpecificTag(EcsWorld world, int entity);

        private void OnDestroy()
        {
            if (_entity.Unpack(EcsWorldProvider.World, out int entity))
                EcsWorldProvider.World.DelEntity(entity);
        }
    }
}