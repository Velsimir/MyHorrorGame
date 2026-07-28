using Game.Scripts.ECS.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems
{
    public class CameraShakeSystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var fearFilter = world.Filter<PlayerTag>().Inc<FearFactor>().End();
            
            if (fearFilter.GetEntitiesCount() == 0) { return; }
    
            int playerEntity = fearFilter.GetRawEntities()[0];
            var cameraFilter = world.Filter<CinemachineTag>().Inc<CinemachineRefs>().End();
            var poolCameras = world.GetPool<CinemachineRefs>();
            var poolFearFactors = world.GetPool<FearFactor>();
            FearFactor fear = poolFearFactors.Get(playerEntity);

            foreach (var cameraEntity in cameraFilter)
            {
                var camera = poolCameras.Get(cameraEntity);

                ShakeCamera(camera, fear);
            }
        }

        private void ShakeCamera(CinemachineRefs camera, FearFactor fearFactor)
        {
            camera.Perlin.AmplitudeGain = fearFactor.Value;
            camera.Perlin.FrequencyGain = Mathf.Lerp(0.8f, 2f, fearFactor.Value);
        }
    }
}