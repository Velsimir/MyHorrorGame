using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using Velsi.Typecraft.Fonts;

namespace Velsi.Typecraft.Styles
{
    [Serializable]
    public sealed class TextStyle
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private string _fontRole;
        [SerializeField] private string _materialPreset;
        [SerializeField] private bool _useColor;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private TMP_ColorGradient _gradient;
        [SerializeField] private bool _bold;
        [SerializeField] private bool _italic;
        [SerializeField] private bool _underline;
        [SerializeField] private TextCase _case;
        [SerializeField, Min(0)] private float _sizePercent;
        [SerializeField] private float _characterSpacingEm;
        [SerializeField] private string _effectTag;
        [SerializeField] private List<string> _autoTagWords = new List<string>();

        public TextStyle()
        {
        }

        internal TextStyle(string id, string displayName, Color? color = null, bool italic = false, TextCase textCase = TextCase.None,
            float sizePercent = 0f, float characterSpacingEm = 0f, string effectTag = null)
        {
            _id = id;
            _displayName = displayName;
            _useColor = color.HasValue;
            _color = color ?? Color.white;
            _italic = italic;
            _case = textCase;
            _sizePercent = sizePercent;
            _characterSpacingEm = characterSpacingEm;
            _effectTag = effectTag;
        }

        public string Id => _id;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? _id : _displayName;
        public string FontRole => _fontRole;
        public string MaterialPreset => _materialPreset;
        public TMP_ColorGradient Gradient => _gradient;
        public IReadOnlyList<string> AutoTagWords => _autoTagWords;

        public void AppendOpen(StringBuilder output, FontProfile fonts, string locale)
        {
            if (!string.IsNullOrEmpty(_fontRole) && fonts != null)
            {
                FontVariant variant = fonts.Resolve(_fontRole, locale);
                if (variant?.Font != null)
                {
                    output.Append("<font=\"").Append(variant.Font.name).Append('"');
                    Material material = variant.FindMaterial(_materialPreset);
                    if (material != null)
                        output.Append(" material=\"").Append(material.name).Append('"');
                    output.Append('>');
                }
            }

            if (_gradient != null)
                output.Append("<gradient=\"").Append(_gradient.name).Append("\">");
            else if (_useColor)
                output.Append("<color=#").Append(ColorUtility.ToHtmlStringRGBA(_color)).Append('>');

            if (_sizePercent > 0f)
                output.Append("<size=").Append(_sizePercent.ToString("0.##", CultureInfo.InvariantCulture)).Append("%>");

            if (Mathf.Abs(_characterSpacingEm) > 0.0001f)
                output.Append("<cspace=").Append(_characterSpacingEm.ToString("0.###", CultureInfo.InvariantCulture)).Append("em>");

            if (_bold) output.Append("<b>");
            if (_italic) output.Append("<i>");
            if (_underline) output.Append("<u>");

            switch (_case)
            {
                case TextCase.Upper: output.Append("<uppercase>"); break;
                case TextCase.Lower: output.Append("<lowercase>"); break;
                case TextCase.SmallCaps: output.Append("<smallcaps>"); break;
            }

            if (!string.IsNullOrWhiteSpace(_effectTag))
                output.Append('<').Append(_effectTag.Trim()).Append('>');
        }

        public void AppendClose(StringBuilder output, FontProfile fonts, string locale)
        {
            if (!string.IsNullOrWhiteSpace(_effectTag))
                output.Append("</").Append(EffectTagName()).Append('>');

            switch (_case)
            {
                case TextCase.Upper: output.Append("</uppercase>"); break;
                case TextCase.Lower: output.Append("</lowercase>"); break;
                case TextCase.SmallCaps: output.Append("</smallcaps>"); break;
            }

            if (_underline) output.Append("</u>");
            if (_italic) output.Append("</i>");
            if (_bold) output.Append("</b>");

            if (Mathf.Abs(_characterSpacingEm) > 0.0001f)
                output.Append("</cspace>");

            if (_sizePercent > 0f)
                output.Append("</size>");

            if (_gradient != null)
                output.Append("</gradient>");
            else if (_useColor)
                output.Append("</color>");

            if (!string.IsNullOrEmpty(_fontRole) && fonts != null && fonts.Resolve(_fontRole, locale)?.Font != null)
                output.Append("</font>");
        }

        private string EffectTagName()
        {
            string tag = _effectTag.Trim();
            int end = tag.IndexOfAny(new[] { ' ', '=' });
            return end > 0 ? tag.Substring(0, end) : tag;
        }
    }

    public enum TextCase
    {
        None,
        Upper,
        Lower,
        SmallCaps
    }
}
