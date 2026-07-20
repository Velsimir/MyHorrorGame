using UnityEngine;

namespace Game.Scripts.ECS.Components
{
    public struct PlayerRefs
    {
        public CharacterController CharacterController;
        public Transform PlayerTransform;
        public Transform CameraTransform;
    }
}