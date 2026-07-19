using Game.Scripts.Configs;
using Game.Scripts.ECS.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems
{
    public class PlayerHorizontalMovementSystem : IEcsRunSystem
    {
        private readonly PlayerConfig _playerConfig;

        public PlayerHorizontalMovementSystem(PlayerConfig playerConfig)
        {
            _playerConfig = playerConfig;
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            var filter = world.Filter<PlayerTag>().Inc<HorizontalMovement>().End();
            var poolHorizontalMovement = world.GetPool<HorizontalMovement>();
            var poolPlayerRef = world.GetPool<PlayerRefs>();

            foreach (int entity in filter)
            {
                ref var horizontalMovement = ref poolHorizontalMovement.Get(entity);
                ref var playerRef = ref poolPlayerRef.Get(entity);
                
                Move(ref horizontalMovement, ref playerRef);
            }
        }
        
        private void Move(ref HorizontalMovement horizontalMovement, ref PlayerRefs playerRef)
        {
            Vector3 move = new Vector3(horizontalMovement.Direction.x, 0, horizontalMovement.Direction.y);
            move = Quaternion.Euler(0, playerRef.PlayerTransform.eulerAngles.y, 0) * move;
            move = Vector3.ClampMagnitude(move, 1f);
            move *= _playerConfig.Speed;
            playerRef.CharacterController.Move(move * Time.deltaTime);
        }
    }
}