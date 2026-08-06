using Game.Scripts.ECS.Components.Interaction;
using Leopotam.EcsLite;

namespace Game.Scripts.ECS.Systems.Interaction
{
    public class OutlineViewSystem : IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var filterNeedEnableOutline = world.Filter<Focused>().Inc<OutlineRefs>().Exc<OutlineEnabled>().End();
            var filterNeedDisableOutline = world.Filter<OutlineEnabled>().Inc<OutlineRefs>().Exc<Focused>().End();
            
            var poolOutlineEnabled = world.GetPool<OutlineEnabled>();
            var poolOutlineRefs = world.GetPool<OutlineRefs>();

            foreach (var entity in filterNeedDisableOutline)
            {
                ref var outlineRefs = ref poolOutlineRefs.Get(entity);
                outlineRefs.MeshRenderer.sharedMaterials = outlineRefs.Default;
                poolOutlineEnabled.Del(entity);
            }

            foreach (var entity in filterNeedEnableOutline)
            {
                ref var outlineRefs = ref poolOutlineRefs.Get(entity);
                outlineRefs.MeshRenderer.sharedMaterials = outlineRefs.WithOutline;
                poolOutlineEnabled.Add(entity);
            }
        }
    }
}