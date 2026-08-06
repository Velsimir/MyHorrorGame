using UnityEngine;

namespace Game.Scripts.ECS.Components.Interaction
{
    public struct Interactor
    {
        public float Distance;
        public float SphereCastRadius;
        public LayerMask Mask;
    }
}