using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [CreateAssetMenu(fileName = "Speaker", menuName = "Typecraft/Dialogue Speaker")]
    public sealed class SpeakerProfile : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _nameTable;
        [SerializeField] private string _nameKey;
        [SerializeField] private string _styleId;
        [SerializeField] private List<SpeakerPortrait> _portraits = new List<SpeakerPortrait>();
        [SerializeField] private Sprite _defaultPortrait;
        [SerializeField] private AudioClip _voiceBlip;
        [SerializeField] private SpeakerSide _side = SpeakerSide.None;

        public string Id
        {
            get => string.IsNullOrEmpty(_id) ? name : _id;
            set => _id = value;
        }

        public string NameTable
        {
            get => _nameTable;
            set => _nameTable = value;
        }

        public string NameKey
        {
            get => _nameKey;
            set => _nameKey = value;
        }

        public string StyleId
        {
            get => _styleId;
            set => _styleId = value;
        }

        public List<SpeakerPortrait> Portraits => _portraits ??= new List<SpeakerPortrait>();

        public Sprite DefaultPortrait
        {
            get => _defaultPortrait;
            set => _defaultPortrait = value;
        }

        public AudioClip VoiceBlip
        {
            get => _voiceBlip;
            set => _voiceBlip = value;
        }

        public SpeakerSide Side
        {
            get => _side;
            set => _side = value;
        }

        public string ResolveNameTable(string fallbackTable) => string.IsNullOrEmpty(_nameTable) ? fallbackTable : _nameTable;

        public Sprite PortraitFor(string emotion)
        {
            if (!string.IsNullOrEmpty(emotion) && _portraits != null)
            {
                foreach (SpeakerPortrait portrait in _portraits)
                {
                    if (portrait != null && portrait.Sprite != null && string.Equals(portrait.Emotion, emotion, StringComparison.OrdinalIgnoreCase))
                        return portrait.Sprite;
                }
            }

            return _defaultPortrait;
        }
    }

    [Serializable]
    public sealed class SpeakerPortrait
    {
        [SerializeField] private string _emotion;
        [SerializeField] private Sprite _sprite;

        public SpeakerPortrait()
        {
        }

        public SpeakerPortrait(string emotion, Sprite sprite)
        {
            _emotion = emotion;
            _sprite = sprite;
        }

        public string Emotion
        {
            get => _emotion;
            set => _emotion = value;
        }

        public Sprite Sprite
        {
            get => _sprite;
            set => _sprite = value;
        }
    }

    public enum SpeakerSide
    {
        None,
        Left,
        Right
    }
}
