using Game.Scripts.ECS.Components.Camera;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems.Camera
{
    public class CameraApplySystem : IEcsInitSystem, IEcsRunSystem
    {
        private EcsFilter _cameraFilter;
        private EcsFilter _playerFilter;
        private EcsPool<CinemachineRefs> _cinemachineRefsPool;
        private EcsPool<CameraNoiseBlend> _cameraNoiseBlendPool;
        private EcsPool<PlayerRefs> _playerRefsPool;
        private EcsPool<CameraOffset> _cameraOffsetPool;

        public void Init(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            _cameraFilter = world.Filter<CameraTag>().Inc<CinemachineRefs>().End();
            _playerFilter = world.Filter<PlayerTag>().Inc<PlayerRefs>().Inc<CameraOffset>().Inc<CameraNoiseBlend>().End();
            _cinemachineRefsPool = world.GetPool<CinemachineRefs>();
            _cameraNoiseBlendPool = world.GetPool<CameraNoiseBlend>();
            _playerRefsPool = world.GetPool<PlayerRefs>();
            _cameraOffsetPool = world.GetPool<CameraOffset>();
        }

        public void Run(IEcsSystems systems)
        {
            if (_playerFilter.GetEntitiesCount() == 0) { return; }

            int player = _playerFilter.GetRawEntities()[0];

            ref var noiseBlend = ref _cameraNoiseBlendPool.Get(player);
            ref var playerRefs = ref _playerRefsPool.Get(player);
            ref var cameraOffset = ref _cameraOffsetPool.Get(player);

            playerRefs.CameraView.localPosition = cameraOffset.Position;
            playerRefs.CameraView.localRotation = Quaternion.Euler(cameraOffset.Rotation);

            foreach (int cameraEntity in _cameraFilter)
            {
                ref var cinemachineRefs = ref _cinemachineRefsPool.Get(cameraEntity);

                cinemachineRefs.CameraMixing.Weight0 = noiseBlend.Idle;
                cinemachineRefs.CameraMixing.Weight1 = noiseBlend.Tense;
                cinemachineRefs.CameraMixing.Weight2 = noiseBlend.Panic;
            }
        }
    }
}
