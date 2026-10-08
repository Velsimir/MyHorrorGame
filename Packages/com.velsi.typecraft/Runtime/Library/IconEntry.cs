using System;
using UnityEngine;

namespace Velsi.Typecraft.Library
{
    [Serializable]
    public sealed class IconEntry
    {
        public const float MinScale = 0.25f;
        public const float MaxScale = 3f;

        [SerializeField] private string _key;
        [SerializeField] private string _glyphName;
        [SerializeField] private bool _tintable;
        [SerializeField] private Vector2 _offset;
        [SerializeField] private float _scale = 1f;

        public IconEntry(string key, string glyphName, bool tintable)
        {
            _key = key;
            _glyphName = glyphName;
            _tintable = tintable;
        }

        public string Key => _key;
        public string GlyphName => _glyphName;
        public bool Tintable => _tintable;
        public bool IsPlaceholder => string.IsNullOrEmpty(_glyphName);
        public Vector2 Offset => _offset;
        public float Scale => _scale > 0f ? _scale : 1f;

        internal void Bind(string glyphName) => _glyphName = glyphName;
        internal void Unbind() => _glyphName = null;

        internal void NormalizeScale()
        {
            if (_scale <= 0f)
                _scale = 1f;
        }
    }
}
