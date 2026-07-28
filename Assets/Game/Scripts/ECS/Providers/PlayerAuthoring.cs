using Game.Scripts.ECS.Components;
using UnityEngine;
using Zenject;

namespace Game.Scripts.ECS.Providers
{
    public class PlayerAuthoring : MonoBehaviour
    {
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private Transform _headTransform;
        
        [Inject] private IEcsWorldProvider _ecsWorldProvider;
        
        private void Start()
        {
            int player = _ecsWorldProvider.World.NewEntity();
            _ecsWorldProvider.World.GetPool<PlayerTag>().Add(player);
            _ecsWorldProvider.World.GetPool<HorizontalMovement>().Add(player);
            _ecsWorldProvider.World.GetPool<VerticalMovement>().Add(player);
            _ecsWorldProvider.World.GetPool<MouseInputDirection>().Add(player);
            _ecsWorldProvider.World.GetPool<LookRotation>().Add(player);
            _ecsWorldProvider.World.GetPool<FearFactor>().Add(player);
            
            ref var playerRefs = ref _ecsWorldProvider.World.GetPool<PlayerRefs>().Add(player);
            playerRefs.CharacterController = _characterController;
            playerRefs.HeadTransform = _headTransform;
        }
    }
}