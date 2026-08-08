using Game.Scripts.ECS.Components.Camera;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems.Camera
{
    public class CameraApplySystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var cameraFilter = world.Filter<CameraTag>().Inc<CinemachineRefs>().End();
            var playerFilter =  world.Filter<PlayerTag>().Inc<PlayerRefs>().Inc<CameraOffset>().Inc<CameraNoiseBlend>().End();
            
            if (playerFilter.GetEntitiesCount() == 0) { return; }

            var player = playerFilter.GetRawEntities()[0];
            var poolCameraCinemachineRefs = world.GetPool<CinemachineRefs>();
            var cameraNoiseBlendPool = world.GetPool<CameraNoiseBlend>();
            var playerRefsPool = world.GetPool<PlayerRefs>();
            var cameraOffsetPool = world.GetPool<CameraOffset>();
            
            ref var playerCondition = ref cameraNoiseBlendPool.Get(player);
            ref var playerRefs = ref playerRefsPool.Get(player);
            ref var cameraOffset = ref cameraOffsetPool.Get(player);
                
            foreach (var cameraEntity in cameraFilter)
            {
                ref var cinemachineRef = ref poolCameraCinemachineRefs.Get(cameraEntity);

                cinemachineRef.CameraMixing.Weight0 = playerCondition.Idle;
                cinemachineRef.CameraMixing.Weight1 = playerCondition.Tense;
                cinemachineRef.CameraMixing.Weight2 = playerCondition.Panic;
                
                playerRefs.CameraView.localPosition = cameraOffset.Position;
                playerRefs.CameraView.localRotation = Quaternion.Euler(cameraOffset.Rotation);
            }
        }
    }
}