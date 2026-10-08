using UnityEngine;
using UnityEngine.InputSystem;

namespace Velsi.Typecraft.Dialogue.InputSystem
{
    [AddComponentMenu("Typecraft/Dialogue/Dialogue Input Advance")]
    public sealed class DialogueInputAdvance : MonoBehaviour
    {
        [SerializeField] private DialoguePlayer _player;
        [SerializeField] private InputActionReference _advance;
        [Tooltip("Enable the action while this component is enabled, if nothing else did")]
        [SerializeField] private bool _enableAction = true;

        private InputAction _bound;
        private bool _enabledByThis;

        public DialoguePlayer Player
        {
            get => _player;
            set => _player = value;
        }

        public InputActionReference Advance
        {
            get => _advance;
            set
            {
                _advance = value;
                if (isActiveAndEnabled)
                {
                    Unbind();
                    Bind();
                }
            }
        }

        private void Reset() => _player = GetComponent<DialoguePlayer>();

        private void OnEnable() => Bind();

        private void OnDisable() => Unbind();

        private void Bind()
        {
            InputAction action = _advance != null ? _advance.action : null;
            if (action == null)
                return;

            _bound = action;
            _bound.performed += OnPerformed;
            if (_enableAction && !_bound.enabled)
            {
                _bound.Enable();
                _enabledByThis = true;
            }
        }

        private void Unbind()
        {
            if (_bound == null)
                return;

            _bound.performed -= OnPerformed;
            if (_enabledByThis)
                _bound.Disable();

            _bound = null;
            _enabledByThis = false;
        }

        private void OnPerformed(InputAction.CallbackContext context)
        {
            if (_player != null && _player.IsPlaying)
                _player.Advance();
        }
    }
}
