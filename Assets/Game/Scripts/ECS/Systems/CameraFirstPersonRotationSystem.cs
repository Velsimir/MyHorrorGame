using Game.Scripts.Configs;
using Game.Scripts.ECS.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems
{
    public class CameraFirstPersonRotationSystem : IEcsRunSystem
    {
        private readonly PlayerConfig _playerConfig;
        
        public CameraFirstPersonRotationSystem(PlayerConfig playerConfig)
        {
            _playerConfig = playerConfig;
        }

        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var filer = world.Filter<PlayerTag>().Inc<MouseInputDirection>().Inc<LookRotation>().End();
            var poolLookDirection = world.GetPool<MouseInputDirection>();
            var poolPlayerRef = world.GetPool<PlayerRefs>();
            var poolLookRotation = world.GetPool<LookRotation>();

            foreach (int entity in filer)
            {
                ref var lookDirection = ref poolLookDirection.Get(entity);
                ref var playerRef = ref poolPlayerRef.Get(entity);
                ref var lLookRotation = ref poolLookRotation.Get(entity);
                
                Rotate(ref lookDirection, ref playerRef, ref lLookRotation);
            }
        }
        
        private void Rotate(ref MouseInputDirection mouseInputDirection, ref PlayerRefs playerRef, ref LookRotation lLookRotation)
        {
            float mouseX = mouseInputDirection.Direction.x * _playerConfig.MouseSensitivityX;
            float mouseY = mouseInputDirection.Direction.y * _playerConfig.MouseSensitivityY;
                
            lLookRotation.Pitch -= mouseY;
            lLookRotation.Pitch = Mathf.Clamp(lLookRotation.Pitch, _playerConfig.MinRotationX, _playerConfig.MaxRotationX);
            lLookRotation.Yaw += mouseX;
            
            playerRef.HeadTransform.localEulerAngles = new Vector3(lLookRotation.Pitch, 0f, 0f);
            playerRef.CharacterController.transform.localEulerAngles = new Vector3(0f, lLookRotation.Yaw, 0f);
        }
    }
}