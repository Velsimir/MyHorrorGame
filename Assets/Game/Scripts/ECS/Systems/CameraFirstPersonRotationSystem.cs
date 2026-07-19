using Game.Scripts.Configs;
using Game.Scripts.ECS.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems
{
    public class CameraFirstPersonRotationSystem : IEcsRunSystem
    {
        private readonly PlayerConfig _playerConfig;

        private float _yaw;
        private float _pitch;
        
        public CameraFirstPersonRotationSystem(PlayerConfig playerConfig)
        {
            _playerConfig = playerConfig;
        }

        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var filer = world.Filter<PlayerTag>().Inc<LookDirection>().End();
            var poolLookRotation = world.GetPool<LookDirection>();
            var poolPlayerRef = world.GetPool<PlayerRefs>();

            foreach (int entity in filer)
            {
                ref var lookDirection = ref poolLookRotation.Get(entity);
                ref var playerRef = ref poolPlayerRef.Get(entity);
                
                Rotate(ref lookDirection, ref playerRef);
            }
        }
        
        private void Rotate(ref LookDirection lookDirection,  ref PlayerRefs playerRef)
        {
            float mouseX = lookDirection.Direction.x * _playerConfig.MouseSensitivityX;
            float mouseY = lookDirection.Direction.y * _playerConfig.MouseSensitivityY;
                
            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, _playerConfig.MinRotationX, _playerConfig.MaxRotationX);
            _yaw += mouseX;
            
            playerRef.CameraTransform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
            playerRef.CharacterController.transform.localEulerAngles = new Vector3(0f, _yaw, 0f);
        }
    }
}