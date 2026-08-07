using UnityEngine;

namespace Game.Scripts.ECS.Components.DebugHelpers
{
    public struct DebugDraw
    {
        public static void WireSphere(Vector3 center, float radius, Color color, int segments = 16)
        {
#if UNITY_EDITOR
            Circle(center, radius, Vector3.right,   Vector3.up,      color, segments);
            Circle(center, radius, Vector3.up,      Vector3.forward, color, segments);
            Circle(center, radius, Vector3.forward, Vector3.right,   color, segments);
#endif
        }

        private static void Circle(Vector3 c, float r, Vector3 a, Vector3 b, Color color, int segments)
        {
            Vector3 prev = c + a * r;
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments * Mathf.PI * 2f;
                Vector3 next = c + (a * Mathf.Cos(t) + b * Mathf.Sin(t)) * r;
                Debug.DrawLine(prev, next, color);
                prev = next;
            }
        }
    }
}