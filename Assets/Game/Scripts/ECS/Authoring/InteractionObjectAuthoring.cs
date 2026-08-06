using Game.Scripts.ECS.Components.Interaction;
using Leopotam.EcsLite;
using UnityEngine;
using Zenject;

namespace Game.Scripts.ECS.Authoring
{
    public class InteractionObjectAuthoring : MonoBehaviour
    {
        [SerializeField] private OutlineRefs _outlineRefs;
            
        [Inject] private IEcsWorldProvider _ecsWorldProvider;
        private EcsPackedEntity _entity;
        
        public EcsPackedEntity Entity => _entity;

        private void Start()
        {
            int entity = _ecsWorldProvider.World.NewEntity();

            _entity = _ecsWorldProvider.World.PackEntity(entity);

            _ecsWorldProvider.World.GetPool<Interactable>().Add(entity);
            
            ref var poolOutlineRefs = ref _ecsWorldProvider.World.GetPool<OutlineRefs>().Add(entity);
            poolOutlineRefs.MeshRenderer = _outlineRefs.MeshRenderer;
            poolOutlineRefs.WithOutline = _outlineRefs.WithOutline;
            poolOutlineRefs.Default = _outlineRefs.Default;
        }
        
        private void OnDestroy()
        {
            if (_entity.Unpack(_ecsWorldProvider.World, out int entity))
                _ecsWorldProvider.World.DelEntity(entity);
        }
    }
}