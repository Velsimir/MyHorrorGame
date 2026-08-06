using UnityEngine;

namespace Game.Scripts.ECS.Components.Doors
{
    public struct Door
    {
        public Transform Pivot;
        public float OpenAngle;
        public float CloseAngle;
        public float Speed;
        public bool IsOpen;
    }
}