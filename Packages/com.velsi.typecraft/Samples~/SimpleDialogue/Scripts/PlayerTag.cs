using System.Text;
using UnityEngine;
using Velsi.Typecraft.Markup;

namespace Velsi.Typecraft.Samples.SimpleDialogue
{
    public sealed class PlayerTag : IMarkupTagHandler
    {
        public static string Name { get; set; } = "Alex";

        public string Prefix => "player";

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register() => Typecraft.Register(new PlayerTag());

        public bool TryRender(string argument, MarkupContext context, StringBuilder output)
        {
            if (argument != "name")
                return false;

            output.Append(Name);
            return true;
        }
    }
}
