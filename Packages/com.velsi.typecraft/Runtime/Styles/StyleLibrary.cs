using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Styles
{
    [CreateAssetMenu(fileName = "StyleLibrary", menuName = "Typecraft/Style Library")]
    public sealed class StyleLibrary : ScriptableObject
    {
        [SerializeField] private List<TextStyle> _styles = new List<TextStyle>();

        private Dictionary<string, TextStyle> _lookup;

        public IReadOnlyList<TextStyle> Styles => _styles;

        public bool TryGet(string id, out TextStyle style)
        {
            _lookup ??= BuildLookup();
            return _lookup.TryGetValue(id, out style);
        }

        internal void Add(TextStyle style)
        {
            _styles.Add(style);
            _lookup = null;
        }

        private void OnValidate() => _lookup = null;

        private Dictionary<string, TextStyle> BuildLookup()
        {
            var lookup = new Dictionary<string, TextStyle>(_styles.Count);
            foreach (TextStyle style in _styles)
            {
                if (style != null && !string.IsNullOrEmpty(style.Id))
                    lookup[style.Id] = style;
            }

            return lookup;
        }
    }
}
