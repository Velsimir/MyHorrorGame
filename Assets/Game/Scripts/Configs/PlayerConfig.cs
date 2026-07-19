using UnityEngine;

namespace Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Configs/PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [SerializeField] private float _speed;

        [Space] 
        [Header("Mouse sensitivity")] 
        [SerializeField] private float _mouseSensitivityX;
        [SerializeField] private float _mouseSensitivityY;
        
        [Space] 
        [Header("Rotation restriction")] 
        [SerializeField] private float _maxRotationX = 80f;
        [SerializeField] private float _minRotationX = -80f;
        
        public float Speed => _speed;
        public float MaxRotationX => _maxRotationX;
        public float MinRotationX => _minRotationX;
        public float MouseSensitivityX => _mouseSensitivityX;
        public float MouseSensitivityY => _mouseSensitivityY;
    }
}