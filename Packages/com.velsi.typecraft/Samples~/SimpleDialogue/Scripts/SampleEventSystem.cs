using UnityEngine;
using UnityEngine.EventSystems;

namespace Velsi.Typecraft.Samples.SimpleDialogue
{
    public sealed class SampleEventSystem : MonoBehaviour
    {
        private void Awake()
        {
            if (EventSystem.current != null)
                return;

            var host = new GameObject("EventSystem", typeof(EventSystem));
#if TYPECRAFT_SAMPLE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            host.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            host.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
