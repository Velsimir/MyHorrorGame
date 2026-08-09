using Game.Scripts.Configs;
using Game.Scripts.ECS.Components.Camera;
using Game.Scripts.ECS.Components.Fear;
using Game.Scripts.ECS.Components.Input;
using Game.Scripts.ECS.Components.Interaction;
using Game.Scripts.ECS.Components.Movement;
using Game.Scripts.ECS.Components.Player;
using Game.Scripts.ECS.Components.Walk;
using UnityEngine;
using Zenject;

namespace Game.Scripts.ECS.Authoring
{
    public class PlayerAuthoring : MonoBehaviour
    {
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private Transform _headTransform;
        [SerializeField] private Transform _cameraView;
        
        [Inject] private GameConfigs _gameConfigs;
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
            _ecsWorldProvider.World.GetPool<InteractInput>().Add(player);
            _ecsWorldProvider.World.GetPool<CameraOffset>().Add(player);
            
            ref var playerRefs = ref _ecsWorldProvider.World.GetPool<PlayerRefs>().Add(player);
            playerRefs.CharacterController = _characterController;
            playerRefs.HeadTransform = _headTransform;
            playerRefs.CameraView = _cameraView;

            ref var interactor = ref _ecsWorldProvider.World.GetPool<Interactor>().Add(player);
            interactor.Distance = _gameConfigs.PlayerConfig.InteractionDistance;
            interactor.Mask = _gameConfigs.PlayerConfig.InteractionLayer;
            interactor.SphereCastRadius = _gameConfigs.PlayerConfig.SphereCastRadius;
            
            ref var gravity = ref _ecsWorldProvider.World.GetPool<Gravity>().Add(player);
            gravity.GroundedVelocity = _gameConfigs.GravityConfig.GroundedVelocity;
            gravity.Acceleration = _gameConfigs.GravityConfig.Acceleration;
            
            ref var cameraNoiseBlend = ref _ecsWorldProvider.World.GetPool<CameraNoiseBlend>().Add(player);
            cameraNoiseBlend.Idle = 1;
            cameraNoiseBlend.Tense = 0;
            cameraNoiseBlend.Panic = 0;
            
            ref var walkCycle = ref _ecsWorldProvider.World.GetPool<WalkCycle>().Add(player);
            walkCycle.BobAmplitudeX = _gameConfigs.WalkConfig.BobAmplitudeX;
            walkCycle.BobAmplitudeY = _gameConfigs.WalkConfig.BobAmplitudeY;
            walkCycle.BobRollAngle = _gameConfigs.WalkConfig.BobRollAngle;
            walkCycle.SmoothTime = _gameConfigs.WalkConfig.SmoothTime;
            walkCycle.StrideLength = _gameConfigs.WalkConfig.StrideLength;
        }
    }
}