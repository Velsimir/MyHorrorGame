using Game.Scripts.ECS.Components.Camera;
using Game.Scripts.ECS.Components.Fear;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;

namespace Game.Scripts.ECS.Systems.Camera
{
    public class CameraFearShakeSystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var fearFilter = world.Filter<PlayerTag>().Inc<FearFactor>().Inc<CameraNoiseBlend>().Exc<CutsceneControlled>().End();
            
            if (fearFilter.GetEntitiesCount() == 0) { return; }
            int playerEntity = fearFilter.GetRawEntities()[0];

            var poolPlayerCondition = world.GetPool<CameraNoiseBlend>();
            ref CameraNoiseBlend noiseBlend = ref poolPlayerCondition.Get(playerEntity);
            
            var poolFearFactors = world.GetPool<FearFactor>();
            ref FearFactor fearFactor = ref poolFearFactors.Get(playerEntity);
            
            noiseBlend.Tense += fearFactor.Value * 3;
            noiseBlend.Panic += fearFactor.Value * fearFactor.Value * 6;
        }
    }
}