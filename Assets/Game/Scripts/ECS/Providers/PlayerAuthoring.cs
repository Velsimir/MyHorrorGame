using Game.Scripts.Configs;
using Game.Scripts.ECS.Components;
using Game.Scripts.ECS.Components.Input;
using Game.Scripts.ECS.Components.Interaction;
using Game.Scripts.ECS.Components.MonoBehaviourRefs;
using Game.Scripts.ECS.Components.Movement;
using Game.Scripts.ECS.Components.Tags;
using UnityEngine;
using Zenject;

namespace Game.Scripts.ECS.Providers
{
    public class PlayerAuthoring : MonoBehaviour
    {
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private Transform _headTransform;

        [Inject] private PlayerConfig _playerConfig;
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
            ref var interactor = ref _ecsWorldProvider.World.GetPool<Interactor>().Add(player);
            playerRefs.CharacterController = _characterController;
            playerRefs.HeadTransform = _headTransform;

            interactor.Distance = _playerConfig.InteractionDistance;
            interactor.Mask = _playerConfig.InteractionLayer;
        }
    }
}