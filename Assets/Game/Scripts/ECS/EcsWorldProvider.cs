using Leopotam.EcsLite;

namespace Game.Scripts.ECS
{
    public class EcsWorldProvider : IEcsWorldProvider
    {
        private EcsWorld _world;
        
        public EcsWorld World {
            get
            {
                if (_world == null)
                    _world = new EcsWorld();
                
                return _world;
            }
        }
    }
}