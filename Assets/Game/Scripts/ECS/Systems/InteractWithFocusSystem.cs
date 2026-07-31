using Game.Scripts.ECS.Components.Interaction;
using Game.Scripts.ECS.Components.Tags;
using Leopotam.EcsLite;

namespace Game.Scripts.ECS.Systems
{
    public class InteractWithFocusSystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var filterFocused = world.Filter<Focused>().End();
            var filterPlayer = world.Filter<PlayerTag>().End();

            foreach (var VARIABLE in filterFocused)
            {
                
            }
        }
    }
}