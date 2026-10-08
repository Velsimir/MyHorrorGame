using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using Velsi.Typecraft.Fonts;
using Velsi.Typecraft.Markup;
#if TYPECRAFT_TMPEFFECTS
using TMPEffects.Components;
#endif

namespace Velsi.Typecraft.Text
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Typecraft/Typecraft Text")]
    public sealed class TypecraftText : MonoBehaviour
    {
        [SerializeField] private TextMode _mode = TextMode.Inline;
        [SerializeField, TextArea(2, 8)] private string _markup;
        [SerializeField] private string _table;
        [SerializeField] private string _key;
        [SerializeField] private string _fontRole;
        [SerializeField] private string _materialPreset;

        private TMP_Text _target;
        private object[] _arguments;
        private int _requestId;
        private bool _dirty;

        public TextMode Mode => _mode;
        public string Markup => _markup;
        public string Table => _table;
        public string Key => _key;
        public TMP_Text Target => _target != null ? _target : _target = GetComponent<TMP_Text>();

        public string FontRole
        {
            get => string.IsNullOrEmpty(_fontRole) && Typecraft.Settings != null ? Typecraft.Settings.DefaultFontRole : _fontRole;
            set
            {
                _fontRole = value;
                Refresh();
            }
        }

        public void SetMarkup(string markup)
        {
            _mode = TextMode.Inline;
            _markup = markup;
            Refresh();
        }

        public void SetKey(string table, string key)
        {
            _mode = TextMode.Localized;
            _table = table;
            _key = key;
            Refresh();
        }

        public void SetArguments(params object[] arguments)
        {
            _arguments = arguments;
            Refresh();
        }

        public void Refresh()
        {
            if (!isActiveAndEnabled || Target == null)
                return;

            _dirty = false;
            ApplyFont();

            int requestId = ++_requestId;

            if (_mode == TextMode.Localized)
            {
                LocalizedText.Request(_table, _key, _arguments, (text, formatted) => Show(text, requestId, formatArguments: !formatted));
                return;
            }

            Show(_markup, requestId, formatArguments: true);
        }

        private void OnEnable()
        {
            Typecraft.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Typecraft.Changed -= Refresh;
            _requestId++;
        }

        private void OnValidate() => _dirty = true;

        private void Update()
        {
            if (_dirty)
                Refresh();
        }

        private void Show(string raw, int requestId, bool formatArguments)
        {
            if (requestId != _requestId || this == null || Target == null)
                return;

            string rendered = Typecraft.Render(raw);

            if (formatArguments && _arguments != null && _arguments.Length > 0)
            {
                try
                {
                    rendered = string.Format(CultureInfo.CurrentCulture, rendered, _arguments);
                }
                catch (FormatException exception)
                {
                    Debug.LogWarning($"[Typecraft] {name}: {exception.Message}", this);
                }
            }

            if (!HasWriter)
                rendered = EffectTags.StripWriterTags(rendered);

            if (Target.text != rendered)
                Target.text = rendered;
        }

#if TYPECRAFT_TMPEFFECTS
        private bool HasWriter => GetComponent<TMPWriter>() != null;
#else
        private bool HasWriter => false;
#endif

        private void ApplyFont()
        {
            TypecraftSettings settings = Typecraft.Settings;
            if (settings == null || settings.Fonts == null)
                return;

            FontVariant variant = settings.Fonts.Resolve(FontRole, Typecraft.Locale);
            if (variant?.Font == null)
                return;

            if (Target.font != variant.Font)
                Target.font = variant.Font;

            Material material = variant.FindMaterial(_materialPreset);
            Material wanted = material != null ? material : variant.Font.material;
            if (Target.fontSharedMaterial != wanted)
                Target.fontSharedMaterial = wanted;
        }
    }

    public enum TextMode
    {
        Inline,
        Localized
    }
}
