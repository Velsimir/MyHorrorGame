using UnityEngine;

namespace Velsi.Typecraft.Samples.BranchingDialogue
{
    public sealed class SampleDoor : MonoBehaviour
    {
        [SerializeField] private string _eventName = "door";
        [SerializeField] private SpriteRenderer _leaf;
        [SerializeField] private Color _closedColor = new Color(0.45f, 0.3f, 0.2f);
        [SerializeField] private Color _openColor = new Color(0.95f, 0.8f, 0.35f);
        [SerializeField] private Vector3 _openOffset = new Vector3(0.9f, 0f, 0f);

        private Vector3 _closedPosition;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (_leaf != null)
                _closedPosition = _leaf.transform.localPosition;
            Apply();
        }

        public void OnDialogueEvent(string eventName, string argument)
        {
            if (eventName != _eventName)
                return;

            if (argument == "open")
                Open();
            else if (argument == "close")
                Close();
        }

        public void Open()
        {
            IsOpen = true;
            Apply();
        }

        public void Close()
        {
            IsOpen = false;
            Apply();
        }

        private void Apply()
        {
            if (_leaf == null)
                return;

            _leaf.color = IsOpen ? _openColor : _closedColor;
            _leaf.transform.localPosition = IsOpen ? _closedPosition + _openOffset : _closedPosition;
        }
    }
}
