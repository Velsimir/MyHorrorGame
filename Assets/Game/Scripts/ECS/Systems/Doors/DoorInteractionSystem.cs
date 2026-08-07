using Game.Scripts.ECS.Components.Doors;
using Game.Scripts.ECS.Components.Interaction;
using Leopotam.EcsLite;

namespace Game.Scripts.ECS.Systems.Doors
{
    public class DoorInteractionSystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();

            var filter = world.Filter<Door>().Inc<InteractionRequest>().End();
            var pool = world.GetPool<Door>();
            var poolActing = world.GetPool<Acting>();

            foreach (var entity in filter)
            {
                ref var door = ref pool.Get(entity);
                door.IsOpen = !door.IsOpen;
                
                if (poolActing.Has(entity) == false)
                    poolActing.Add(entity);
            }
        }
    }
}