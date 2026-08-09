using Game.Scripts.ECS.Components.Camera;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;

namespace Game.Scripts.ECS.Systems.Camera
{
    public class CameraCutsceneSystem : IEcsInitSystem, IEcsRunSystem
    {
        private EcsFilter _filter;
        private EcsPool<CameraOffset> _cameraOffsetPool;

        public void Init(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            _filter = world.Filter<PlayerTag>().Inc<CameraOffset>().Inc<CutsceneControlled>().End();
            _cameraOffsetPool = world.GetPool<CameraOffset>();
        }

        public void Run(IEcsSystems systems)
        {
        }
    }
}
