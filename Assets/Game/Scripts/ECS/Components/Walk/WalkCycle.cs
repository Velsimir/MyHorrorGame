namespace Game.Scripts.ECS.Components.Player
{
    public struct WalkCycle
    {
        public float Phase;
        public float Intensity;
        public float BobAmplitudeY;
        public float BobAmplitudeX;
        public float BobRollAngle;
        public float StrideLength;
        public float SmoothTime;
    }
}