using System;
using UnityEngine;

namespace Velsi.Typecraft.Library
{
    [Serializable]
    public sealed class IconSetBuildSettings
    {
        [SerializeField] private string _sourceFolder;
        [SerializeField] private bool _includeSubfolders = true;
        [SerializeField, Range(0, 8)] private int _padding = 4;
        [SerializeField] private int _maxAtlasSize = 4096;
        [SerializeField] private FilterMode _filterMode = FilterMode.Bilinear;
        [SerializeField, Range(-0.5f, 0.5f)] private float _verticalOffset;
        [SerializeField] private bool _tintableByDefault;

        public string SourceFolder
        {
            get => _sourceFolder;
            set => _sourceFolder = value;
        }

        public bool IncludeSubfolders => _includeSubfolders;
        public int Padding => _padding;
        public int MaxAtlasSize => _maxAtlasSize;
        public FilterMode FilterMode => _filterMode;
        public float VerticalOffset => _verticalOffset;
        public bool TintableByDefault => _tintableByDefault;
    }
}
