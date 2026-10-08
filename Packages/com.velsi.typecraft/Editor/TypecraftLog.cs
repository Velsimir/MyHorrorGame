using UnityEngine;

namespace Velsi.Typecraft.Editor
{
    internal static class TypecraftLog
    {
        private const string Prefix = "[Typecraft] ";

        public static void Info(string message) => Debug.Log(Prefix + message);
        public static void Warning(string message) => Debug.LogWarning(Prefix + message);
        public static void Error(string message) => Debug.LogError(Prefix + message);
    }
}
