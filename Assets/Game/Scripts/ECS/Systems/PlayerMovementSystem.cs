using Game.Scripts.Configs;
using Game.Scripts.ECS.Components.Movement;
using Game.Scripts.ECS.Components.Player;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Systems
{
    public class PlayerMovementSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly PlayerConfig _playerConfig;

        private EcsFilter _filter;
        private EcsPool<HorizontalMovement> _poolHorizontalMovement;
        private EcsPool<PlayerRefs> _poolPlayerRef;
        private EcsPool<VerticalMovement> _poolVerticalMovement;
        private EcsPool<LookRotation> _poolLookRotation;

        public PlayerMovementSystem(PlayerConfig playerConfig)
        {
            _playerConfig = playerConfig;
        }

        public void Init(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            _filter = world.Filter<PlayerTag>().Inc<PlayerRefs>().Inc<HorizontalMovement>().Inc<VerticalMovement>().Inc<LookRotation>().End();
            _poolHorizontalMovement = world.GetPool<HorizontalMovement>();
            _poolPlayerRef = world.GetPool<PlayerRefs>();
            _poolVerticalMovement = world.GetPool<VerticalMovement>();
            _poolLookRotation = world.GetPool<LookRotation>();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter)
            {
                ref var horizontalMovement = ref _poolHorizontalMovement.Get(entity);
                ref var playerRef = ref _poolPlayerRef.Get(entity);
                ref var verticalMovement = ref _poolVerticalMovement.Get(entity);
                ref var lookRotation = ref _poolLookRotation.Get(entity);
                
                Move(ref horizontalMovement, ref verticalMovement,ref lookRotation, ref playerRef);
            }
        }

        private void Move(ref HorizontalMovement horizontalMovement, ref VerticalMovement verticalMovement, ref LookRotation lookRotation, ref PlayerRefs playerRef)
        {
            Vector3 move = new Vector3(horizontalMovement.Direction.x, 0, horizontalMovement.Direction.y);
            move = Quaternion.Euler(0, lookRotation.Yaw, 0) * move;
            move = Vector3.ClampMagnitude(move, 1f);
            move *= _playerConfig.Speed;
            move += verticalMovement.Velocity;
            playerRef.CharacterController.Move(move * Time.deltaTime);
            verticalMovement.IsGrounded = playerRef.CharacterController.isGrounded;
        }
    }
}