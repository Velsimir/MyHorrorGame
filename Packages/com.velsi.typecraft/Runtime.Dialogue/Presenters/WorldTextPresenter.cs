using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [AddComponentMenu("Typecraft/Dialogue/World Text Presenter")]
    public sealed class WorldTextPresenter : TextDialoguePresenter
    {
        [SerializeField] private bool _faceCamera = true;
        [Tooltip("Empty: Camera.main")]
        [SerializeField] private Camera _camera;
        [Tooltip("Rotated to face the camera. Empty: the text object")]
        [SerializeField] private Transform _billboard;

        public bool FaceCamera
        {
            get => _faceCamera;
            set => _faceCamera = value;
        }

        public Camera Camera
        {
            get => _camera;
            set => _camera = value;
        }

        protected override void Reset()
        {
            base.Reset();
            HideAfter = 3f;
        }

        private void LateUpdate()
        {
            if (!_faceCamera || !IsVisible)
                return;

            Camera target = _camera != null ? _camera : Camera.main;
            Transform billboard = _billboard != null ? _billboard : Text != null ? Text.transform : transform;
            if (target == null)
                return;

            Transform view = target.transform;
            billboard.rotation = Quaternion.LookRotation(view.forward, view.up);
        }
    }
}
