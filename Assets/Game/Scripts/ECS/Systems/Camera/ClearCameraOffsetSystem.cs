using Game.Scripts.ECS.Components.Camera;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems.Camera
{
    public class ClearCameraOffsetSystem : IEcsInitSystem, IEcsRunSystem
    {
        private EcsFilter _filter;
        private EcsPool<CameraOffset> _poolCameraOffset;
        private EcsPool<CameraNoiseBlend> _poolNoiseBlend;

        public void Init(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            _filter = world.Filter<PlayerTag>().Inc<CameraOffset>().Inc<CameraNoiseBlend>().End();
            _poolCameraOffset = world.GetPool<CameraOffset>();
            _poolNoiseBlend = world.GetPool<CameraNoiseBlend>();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter)
            {
                ref CameraOffset cameraOffset = ref _poolCameraOffset.Get(entity);
                cameraOffset.FovDelta = 0;
                cameraOffset.Position = Vector3.zero;
                cameraOffset.Rotation = Vector3.zero;

                ref CameraNoiseBlend cameraNoiseBlend = ref _poolNoiseBlend.Get(entity);
                cameraNoiseBlend.Idle = 1;
                cameraNoiseBlend.Panic = 0;
                cameraNoiseBlend.Tense = 0;
            }
        }
    }
}