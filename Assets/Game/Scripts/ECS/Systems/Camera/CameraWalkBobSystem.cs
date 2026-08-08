using Game.Scripts.ECS.Components.Camera;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems.Camera
{
    public class CameraWalkBobSystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var filterPlayer = world.Filter<PlayerTag>().Inc<CameraOffset>().Inc<WalkCycle>().Exc<CutsceneControlled>().End();

            if (filterPlayer.GetEntitiesCount() == 0) return;

            var player = filterPlayer.GetRawEntities()[0];
            
            var walkCyclePool = world.GetPool<WalkCycle>();
            ref var walkCycle = ref walkCyclePool.Get(player);
            
            var cameraOffsetPool = world.GetPool<CameraOffset>();
            ref var cameraOffset = ref cameraOffsetPool.Get(player);

            float bob = -walkCycle.BobAmplitudeY * Mathf.Cos(Mathf.PI * 4f * walkCycle.Phase);
            float sway = walkCycle.BobAmplitudeX * Mathf.Sin(Mathf.PI * 2f * walkCycle.Phase);
            float roll = walkCycle.BobRollAngle * Mathf.Sin(Mathf.PI * 2f * walkCycle.Phase);

            cameraOffset.Position += new Vector3(sway, bob, 0f) * walkCycle.Intensity;
            cameraOffset.Rotation += new Vector3(0f, 0f, roll) * walkCycle.Intensity;
        }
    }
}