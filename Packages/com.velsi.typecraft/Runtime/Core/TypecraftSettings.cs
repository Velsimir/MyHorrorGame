using System.Collections.Generic;
using UnityEngine;
using Velsi.Typecraft.AutoTags;
using Velsi.Typecraft.Fonts;
using Velsi.Typecraft.Input;
using Velsi.Typecraft.Library;
using Velsi.Typecraft.Styles;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft
{
    [CreateAssetMenu(fileName = "TypecraftSettings", menuName = "Typecraft/Settings")]
    public sealed class TypecraftSettings : ScriptableObject
    {
        public const string ResourcesPath = "TypecraftSettings";

        [SerializeField] private StyleLibrary _styles;
        [SerializeField] private FontProfile _fonts;
        [SerializeField] private List<IconSet> _iconSets = new List<IconSet>();
        [SerializeField] private ScriptableObject _inputGlyphs;
        [SerializeField] private ScriptableObject _textSource;
        [SerializeField] private List<MarkupLayer> _markupLayers = new List<MarkupLayer>();
        [SerializeField] private List<AutoTagRule> _autoTags = new List<AutoTagRule>();
        [SerializeField] private ScriptableObject _animationDatabase;
        [SerializeField] private string _defaultLocale = "en";
        [SerializeField] private string _defaultFontRole = "Body";
        [SerializeField] private DeviceFamily _defaultDesktopDevice = DeviceFamily.KeyboardMouse;
        [SerializeField] private DeviceFamily _defaultMobileDevice = DeviceFamily.Touch;

        public StyleLibrary Styles => _styles;
        public FontProfile Fonts => _fonts;
        public IReadOnlyList<IconSet> IconSets => _iconSets;
        public IInputGlyphResolver InputGlyphs => _inputGlyphs as IInputGlyphResolver;
        public ITextSource TextSource => _textSource as ITextSource;
        public IReadOnlyList<MarkupLayer> MarkupLayers => _markupLayers;
        public IReadOnlyList<AutoTagRule> AutoTags => _autoTags;
        public ScriptableObject AnimationDatabase => _animationDatabase;
        public string DefaultLocale => _defaultLocale;
        public string DefaultFontRole => _defaultFontRole;
        public DeviceFamily DefaultDevice => Application.isMobilePlatform ? _defaultMobileDevice : _defaultDesktopDevice;

        public MarkupLayer FindMarkupLayer(string tableId)
        {
            if (string.IsNullOrEmpty(tableId))
                return null;

            foreach (MarkupLayer layer in _markupLayers)
            {
                if (layer != null && layer.TableId == tableId)
                    return layer;
            }

            return null;
        }

        internal void AddMarkupLayer(MarkupLayer layer)
        {
            if (layer != null && !_markupLayers.Contains(layer))
                _markupLayers.Add(layer);
        }

        internal void Configure(StyleLibrary styles, FontProfile fonts, IEnumerable<IconSet> iconSets, ScriptableObject inputGlyphs, ScriptableObject textSource)
        {
            _styles = styles;
            _fonts = fonts;
            _iconSets = new List<IconSet>(iconSets);
            _inputGlyphs = inputGlyphs;
            _textSource = textSource;
        }

        private void OnValidate()
        {
            if (_inputGlyphs != null && _inputGlyphs is not IInputGlyphResolver)
            {
                Debug.LogWarning($"[Typecraft] {_inputGlyphs.name} does not implement {nameof(IInputGlyphResolver)}", this);
                _inputGlyphs = null;
            }

            if (_textSource != null && _textSource is not ITextSource)
            {
                Debug.LogWarning($"[Typecraft] {_textSource.name} does not implement {nameof(ITextSource)}", this);
                _textSource = null;
            }

            if (Typecraft.Settings == this)
                Typecraft.Refresh();
        }
    }
}
