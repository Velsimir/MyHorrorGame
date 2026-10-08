using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [AddComponentMenu("Typecraft/Dialogue/Subtitle Presenter")]
    public sealed class SubtitlePresenter : TextDialoguePresenter
    {
        private const string NameToken = "{name}";

        [SerializeField] private bool _showSpeakerName = true;
        [Tooltip("Prefix before the line, {name} is replaced with the styled speaker name")]
        [SerializeField] private string _namePrefix = NameToken + ": ";
        [Tooltip("Style of the name when the speaker has none")]
        [SerializeField] private string _nameStyle;

        private int _request;

        public bool ShowSpeakerName
        {
            get => _showSpeakerName;
            set => _showSpeakerName = value;
        }

        public string NamePrefix
        {
            get => _namePrefix;
            set => _namePrefix = value;
        }

        protected override void ApplyLine(DialogueLine line)
        {
            int request = ++_request;
            if (Text == null)
                return;

            if (!_showSpeakerName || line.Speaker == null)
            {
                Text.SetKey(line.ResolveTable(), line.Key);
                return;
            }

            string name = null;
            string body = null;
            bool nameReady = false;
            bool bodyReady = false;

            void TryShow()
            {
                if (!nameReady || !bodyReady || request != _request || this == null)
                    return;

                string prefix = string.IsNullOrEmpty(name) || string.IsNullOrEmpty(_namePrefix) ? string.Empty : _namePrefix.Replace(NameToken, name);
                Text.SetMarkup(prefix + body);
            }

            DialogueSpeakerText.RequestName(line.Speaker, line.ResolveTable(), _nameStyle, markup =>
            {
                name = markup;
                nameReady = true;
                TryShow();
            });

            DialogueSpeakerText.RequestLine(line, markup =>
            {
                body = markup;
                bodyReady = true;
                TryShow();
            });
        }

        protected override void OnHidden() => _request++;

        protected override void OnTypecraftChanged()
        {
            if (_showSpeakerName && CurrentLine?.Speaker != null)
                ApplyLine(CurrentLine);
        }

        protected override void Reset()
        {
            base.Reset();
            HideAfter = 2f;
        }
    }
}
