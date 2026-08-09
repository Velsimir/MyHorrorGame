using Game.Scripts.ECS.Components.Camera;
using Game.Scripts.ECS.Components.Player;
using Game.Scripts.ECS.Components.Walk;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems.Camera
{
    public class CameraWalkBobSystem : IEcsInitSystem, IEcsRunSystem
    {
        private EcsFilter _filterPlayer;
        private EcsPool<WalkCycle> _walkCyclePool;
        private EcsPool<CameraOffset> _cameraOffsetPool;

        public void Init(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            _filterPlayer = world.Filter<PlayerTag>().Inc<CameraOffset>().Inc<WalkCycle>().Exc<CutsceneControlled>().End();
            _walkCyclePool = world.GetPool<WalkCycle>();
            _cameraOffsetPool = world.GetPool<CameraOffset>();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filterPlayer)
            {
                ref var walkCycle = ref _walkCyclePool.Get(entity);
                ref var cameraOffset = ref _cameraOffsetPool.Get(entity);

                float bob = -walkCycle.BobAmplitudeY * Mathf.Cos(Mathf.PI * 4f * walkCycle.Phase);
                float sway = walkCycle.BobAmplitudeX * Mathf.Sin(Mathf.PI * 2f * walkCycle.Phase);
                float roll = walkCycle.BobRollAngle * Mathf.Sin(Mathf.PI * 2f * walkCycle.Phase);

                cameraOffset.Position += new Vector3(sway, bob, 0f) * walkCycle.Intensity;
                cameraOffset.Rotation += new Vector3(0f, 0f, roll) * walkCycle.Intensity;
            }
        }
    }
}
