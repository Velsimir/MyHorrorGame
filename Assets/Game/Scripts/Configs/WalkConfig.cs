using UnityEngine;

namespace Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "WalkConfig", menuName = "Configs/WalkConfig")]
    public class WalkConfig : ScriptableObject
    {
        [SerializeField] private float _bobAmplitudeX = 0.02f;
        [SerializeField] private float _bobAmplitudeY = 0.04f;
        [SerializeField] private float _bobRollAngle = 0.7f;
        [SerializeField] private float _smoothTime = 0.15f;
        [SerializeField] private float _strideLength = 1.5f;
        
        public float BobAmplitudeX => _bobAmplitudeX;
        public float BobAmplitudeY => _bobAmplitudeY;
        public float BobRollAngle => _bobRollAngle;
        public float SmoothTime => _smoothTime;
        public float StrideLength => _strideLength;
    }
}