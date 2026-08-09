using Game.Scripts.Configs;
using Game.Scripts.ECS.Components;
using Game.Scripts.ECS.Components.Input;
using Game.Scripts.ECS.Components.Movement;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems
{
    public class FirstPersonLookRotationSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly PlayerConfig _playerConfig;

        private EcsFilter _filter;
        private EcsPool<MouseInputDirection> _poolMouseInput;
        private EcsPool<PlayerRefs> _poolPlayerRef;
        private EcsPool<LookRotation> _poolLookRotation;

        public FirstPersonLookRotationSystem(PlayerConfig playerConfig)
        {
            _playerConfig = playerConfig;
        }

        public void Init(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            _filter = world.Filter<PlayerTag>().Inc<PlayerRefs>().Inc<MouseInputDirection>().Inc<LookRotation>().End();
            _poolMouseInput = world.GetPool<MouseInputDirection>();
            _poolPlayerRef = world.GetPool<PlayerRefs>();
            _poolLookRotation = world.GetPool<LookRotation>();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter)
            {
                ref var mouseInput = ref _poolMouseInput.Get(entity);
                ref var playerRef = ref _poolPlayerRef.Get(entity);
                ref var lLookRotation = ref _poolLookRotation.Get(entity);
                
                Rotate(ref mouseInput, ref playerRef, ref lLookRotation);
            }
        }

        private void Rotate(ref MouseInputDirection mouseInputDirection, ref PlayerRefs playerRef, ref LookRotation lookRotation)
        {
            float mouseX = mouseInputDirection.Direction.x * _playerConfig.MouseSensitivityX;
            float mouseY = mouseInputDirection.Direction.y * _playerConfig.MouseSensitivityY;
                
            lookRotation.Pitch -= mouseY;
            lookRotation.Pitch = Mathf.Clamp(lookRotation.Pitch, _playerConfig.MinRotationX, _playerConfig.MaxRotationX);
            lookRotation.Yaw += mouseX;
            
            playerRef.HeadTransform.localEulerAngles = new Vector3(lookRotation.Pitch, 0f, 0f);
            playerRef.CharacterController.transform.localEulerAngles = new Vector3(0f, lookRotation.Yaw, 0f);
        }
    }
}