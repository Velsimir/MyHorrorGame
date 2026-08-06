using Game.Scripts.ECS.Components.Interaction;
using Leopotam.EcsLite;

namespace Game.Scripts.ECS.Systems
{
    public class ClearInteractionRequestSystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            
            var filter = world.Filter<InteractionRequest>().End();
            var pool = world.GetPool<InteractionRequest>();
            
            foreach (var entity in filter)
                pool.Del(entity);
        }
    }
}