using UnityEngine;

namespace Game.Scripts.ECS.Components.Camera
{
    public struct CameraOffset
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public float FovDelta;
    }
}