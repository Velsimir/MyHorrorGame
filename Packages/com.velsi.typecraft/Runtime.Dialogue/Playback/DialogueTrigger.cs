using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [AddComponentMenu("Typecraft/Dialogue/Dialogue Trigger")]
    public sealed class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private DialoguePlayer _player;
        [SerializeField] private DialogueTriggerMode _mode = DialogueTriggerMode.Manual;
        [Tooltip("OnTriggerEnter: only colliders with this tag. Empty: any collider")]
        [SerializeField] private string _tag = "Player";
        [Tooltip("Fire only once per lifetime of this component")]
        [SerializeField] private bool _once;

        private bool _fired;

        public DialoguePlayer Player
        {
            get => _player;
            set => _player = value;
        }

        public DialogueTriggerMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        public string Tag
        {
            get => _tag;
            set => _tag = value;
        }

        public bool Once
        {
            get => _once;
            set => _once = value;
        }

        public bool HasFired => _fired;

        public bool Fire()
        {
            if (_once && _fired)
                return false;

            if (_player == null)
            {
                Debug.LogWarning($"[Typecraft] Dialogue: trigger '{name}' has no DialoguePlayer", this);
                return false;
            }

            if (_player.IsPlaying || !_player.Play())
                return false;

            _fired = true;
            return true;
        }

        public void ResetFired() => _fired = false;

        private void Reset() => _player = GetComponent<DialoguePlayer>();

        private void Start()
        {
            if (_mode == DialogueTriggerMode.OnStart)
                Fire();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_mode == DialogueTriggerMode.OnTriggerEnter && Matches(other.gameObject))
                Fire();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_mode == DialogueTriggerMode.OnTriggerEnter && Matches(other.gameObject))
                Fire();
        }

        private bool Matches(GameObject other) => string.IsNullOrEmpty(_tag) || other.CompareTag(_tag);
    }

    public enum DialogueTriggerMode
    {
        Manual,
        OnStart,
        OnTriggerEnter
    }
}
