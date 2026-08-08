using Game.Scripts.ECS.Components.Camera;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems.Camera
{
    public class ClearCameraOffsetSystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var filter = world.Filter<PlayerTag>().Inc<CameraOffset>().Inc<CameraNoiseBlend>().End();
            var poolCameraOffset = world.GetPool<CameraOffset>();
            var poolNoiseBlend = world.GetPool<CameraNoiseBlend>();

            foreach (var cinemachineRef in filter)
            {
                ref CameraOffset cameraOffset = ref poolCameraOffset.Get(cinemachineRef);
                cameraOffset.FovDelta = 0;
                cameraOffset.Position = Vector3.zero;
                cameraOffset.Rotation = Vector3.zero;

                ref CameraNoiseBlend cameraNoiseBlend = ref poolNoiseBlend.Get(cinemachineRef);
                cameraNoiseBlend.Idle = 1;
                cameraNoiseBlend.Panic = 0;
                cameraNoiseBlend.Tense = 0;
            }
        }
    }
}