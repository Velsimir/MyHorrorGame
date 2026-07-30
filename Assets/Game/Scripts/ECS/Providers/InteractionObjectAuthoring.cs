using Game.Scripts.Configs;
using Game.Scripts.ECS.Components.Interaction;
using Leopotam.EcsLite;
using UnityEngine;
using Zenject;

namespace Game.Scripts.ECS.Providers
{
    public class InteractionObjectAuthoring : MonoBehaviour
    {
        [Inject] private IEcsWorldProvider _ecsWorldProvider;
        private EcsPackedEntity _entity;
        
        public EcsPackedEntity Entity => _entity;
        
        private void Start()
        {
            int entity = _ecsWorldProvider.World.NewEntity();

            _entity = _ecsWorldProvider.World.PackEntity(entity);

            _ecsWorldProvider.World.GetPool<Interactable>().Add(entity);
        }
        
        private void OnDestroy()
        {
            if (_entity.Unpack(_ecsWorldProvider.World, out int entity))
                _ecsWorldProvider.World.DelEntity(entity);
        }
    }
}