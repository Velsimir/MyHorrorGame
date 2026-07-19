using UnityEngine;

namespace Game.Scripts.ECS.Components
{
    public struct PlayerRefs
    {
        public CharacterController CharacterController;
        public Transform PlayerTransform;
        public Transform CameraTransform;
        
        public PlayerRefs(CharacterController characterController, Transform playerTransform, Transform cameraTransform)
        {
            CharacterController = characterController;
            PlayerTransform = playerTransform;
            CameraTransform = cameraTransform;
        }
    }
}