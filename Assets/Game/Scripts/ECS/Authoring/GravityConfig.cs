using UnityEngine;

namespace Game.Scripts.ECS.Authoring
{
    [CreateAssetMenu(fileName = "GravityConfig", menuName = "Configs/Gravity")]
    public class GravityConfig : ScriptableObject
    {
        [SerializeField] private Vector3 _acceleration;
        [SerializeField] private Vector3 _groundedVelocity;

        public Vector3 Acceleration => _acceleration;
        public Vector3 GroundedVelocity => _groundedVelocity;
    }
}