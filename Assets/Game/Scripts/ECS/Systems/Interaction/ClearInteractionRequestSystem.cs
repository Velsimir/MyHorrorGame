using Game.Scripts.ECS.Components.Interaction;
using Leopotam.EcsLite;

namespace Game.Scripts.ECS.Systems.Interaction
{
    public class ClearInteractionRequestSystem : IEcsInitSystem, IEcsRunSystem
    {
        private EcsFilter _filter;
        private EcsPool<InteractionRequest> _pool;

        public void Init(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();

            _filter = world.Filter<InteractionRequest>().End();
            _pool = world.GetPool<InteractionRequest>();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter)
                _pool.Del(entity);
        }
    }
}