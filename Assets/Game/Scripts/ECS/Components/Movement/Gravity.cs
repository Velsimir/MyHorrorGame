
using UnityEngine;

namespace Game.Scripts.ECS.Components.Movement
{
    public struct Gravity
    {
        public Vector3 Acceleration;
        public Vector3 GroundedVelocity;
    }
}