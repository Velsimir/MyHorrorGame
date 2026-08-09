using Game.Scripts.ECS.Components.Camera;
using Game.Scripts.ECS.Components.Fear;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;

namespace Game.Scripts.ECS.Systems.Camera
{
    public class CameraFearShakeSystem : IEcsInitSystem, IEcsRunSystem
    {
        private EcsFilter _fearFilter;
        private EcsPool<CameraNoiseBlend> _noiseBlendPool;
        private EcsPool<FearFactor> _fearFactorPool;

        public void Init(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            _fearFilter = world.Filter<PlayerTag>().Inc<FearFactor>().Inc<CameraNoiseBlend>().Exc<CutsceneControlled>().End();
            _noiseBlendPool = world.GetPool<CameraNoiseBlend>();
            _fearFactorPool = world.GetPool<FearFactor>();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _fearFilter)
            {
                ref CameraNoiseBlend noiseBlend = ref _noiseBlendPool.Get(entity);
                ref FearFactor fearFactor = ref _fearFactorPool.Get(entity);

                noiseBlend.Tense += fearFactor.Value * 3;
                noiseBlend.Panic += fearFactor.Value * fearFactor.Value * 6;
            }
        }
    }
}
