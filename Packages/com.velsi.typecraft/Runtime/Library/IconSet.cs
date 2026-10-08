using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Velsi.Typecraft.Library
{
    [CreateAssetMenu(fileName = "IconSet", menuName = "Typecraft/Icon Set")]
    public sealed class IconSet : ScriptableObject
    {
        [SerializeField] private string _setId;
        [SerializeField] private TMP_SpriteAsset _spriteAsset;
        [SerializeField] private List<IconEntry> _entries = new List<IconEntry>();
        [SerializeField] private IconSetBuildSettings _buildSettings = new IconSetBuildSettings();

        private Dictionary<string, IconEntry> _lookup;

        public string SetId => _setId;
        public TMP_SpriteAsset SpriteAsset => _spriteAsset;
        public IReadOnlyList<IconEntry> Entries => _entries;
        public IconSetBuildSettings BuildSettings => _buildSettings;

        public bool TryGetEntry(string key, out IconEntry entry)
        {
            _lookup ??= BuildLookup();
            return _lookup.TryGetValue(key, out entry);
        }

        public bool TryGetGlyphName(string key, out string glyphName)
        {
            if (TryGetEntry(key, out IconEntry entry) && !entry.IsPlaceholder)
            {
                glyphName = entry.GlyphName;
                return true;
            }

            glyphName = null;
            return false;
        }

        internal void Configure(string setId, TMP_SpriteAsset spriteAsset)
        {
            _setId = setId;
            _spriteAsset = spriteAsset;
            _lookup = null;
        }

        internal List<IconEntry> MutableEntries
        {
            get
            {
                _lookup = null;
                return _entries;
            }
        }

        private void OnValidate() => _lookup = null;

        private Dictionary<string, IconEntry> BuildLookup()
        {
            var lookup = new Dictionary<string, IconEntry>(_entries.Count);
            foreach (IconEntry entry in _entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Key))
                    lookup[entry.Key] = entry;
            }

            return lookup;
        }
    }
}
