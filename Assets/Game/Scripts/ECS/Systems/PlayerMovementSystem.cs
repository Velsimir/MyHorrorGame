using Game.Scripts.Configs;
using Game.Scripts.ECS.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems
{
    public class PlayerMovementSystem : IEcsRunSystem
    {
        private readonly PlayerConfig _playerConfig;

        public PlayerMovementSystem(PlayerConfig playerConfig)
        {
            _playerConfig = playerConfig;
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            var filter = world.Filter<PlayerTag>().Inc<HorizontalMovement>().Inc<VerticalMovement>().End();
            var poolHorizontalMovement = world.GetPool<HorizontalMovement>();
            var poolPlayerRef = world.GetPool<PlayerRefs>();
            var poolVerticalMovement = world.GetPool<VerticalMovement>();

            foreach (int entity in filter)
            {
                ref var horizontalMovement = ref poolHorizontalMovement.Get(entity);
                ref var playerRef = ref poolPlayerRef.Get(entity);
                ref var verticalMovement = ref poolVerticalMovement.Get(entity);
                
                Move(ref horizontalMovement, ref verticalMovement,ref playerRef);
            }
        }
        
        private void Move(ref HorizontalMovement horizontalMovement, ref VerticalMovement verticalMovement,ref PlayerRefs playerRef)
        {
            Vector3 move = new Vector3(horizontalMovement.Direction.x, 0, horizontalMovement.Direction.y);
            move = Quaternion.Euler(0, playerRef.HeadTransform.eulerAngles.y, 0) * move;
            move = Vector3.ClampMagnitude(move, 1f);
            move *= _playerConfig.Speed;
            move.y += verticalMovement.Velocity;
            playerRef.CharacterController.Move(move * Time.deltaTime);
            verticalMovement.IsGrounded = playerRef.CharacterController.isGrounded;
        }
    }
}