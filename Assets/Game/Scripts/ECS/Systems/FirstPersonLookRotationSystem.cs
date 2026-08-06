using Game.Scripts.Configs;
using Game.Scripts.ECS.Components;
using Game.Scripts.ECS.Components.Input;
using Game.Scripts.ECS.Components.Movement;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems
{
    public class FirstPersonLookRotationSystem : IEcsRunSystem
    {
        private readonly PlayerConfig _playerConfig;
        
        public FirstPersonLookRotationSystem(PlayerConfig playerConfig)
        {
            _playerConfig = playerConfig;
        }

        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var filter = world.Filter<PlayerTag>().Inc<PlayerRefs>().Inc<MouseInputDirection>().Inc<LookRotation>().End();
            var poolMouseInput = world.GetPool<MouseInputDirection>();
            var poolPlayerRef = world.GetPool<PlayerRefs>();
            var poolLookRotation = world.GetPool<LookRotation>();

            foreach (int entity in filter)
            {
                ref var mouseInput = ref poolMouseInput.Get(entity);
                ref var playerRef = ref poolPlayerRef.Get(entity);
                ref var lLookRotation = ref poolLookRotation.Get(entity);
                
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