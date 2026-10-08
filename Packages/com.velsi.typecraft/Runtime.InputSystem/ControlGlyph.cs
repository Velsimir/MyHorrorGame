using System;
using System.Collections.Generic;
using UnityEngine;
using Velsi.Typecraft.Library;

namespace Velsi.Typecraft.InputSystem
{
    [Serializable]
    public sealed class ControlGlyph
    {
        [SerializeField] private string _control;
        [SerializeField] private string _iconKey;

        public ControlGlyph(string control, string iconKey)
        {
            _control = control;
            _iconKey = iconKey;
        }

        public string Control => _control;
        public string IconKey => _iconKey;
    }

    [Serializable]
    public sealed class FamilyGlyphs
    {
        [SerializeField] private DeviceFamily _family;
        [SerializeField] private IconSet _icons;
        [SerializeField] private bool _hasFallback;
        [SerializeField] private DeviceFamily _fallback;
        [SerializeField] private List<ControlGlyph> _controls = new List<ControlGlyph>();

        public FamilyGlyphs(DeviceFamily family, IconSet icons, DeviceFamily? fallback, List<ControlGlyph> controls)
        {
            _family = family;
            _icons = icons;
            _hasFallback = fallback.HasValue;
            _fallback = fallback ?? default;
            _controls = controls;
        }

        public DeviceFamily Family => _family;
        public IconSet Icons => _icons;
        public DeviceFamily? Fallback => _hasFallback ? _fallback : null;
        public IReadOnlyList<ControlGlyph> Controls => _controls;

        public string FindIconKey(string control)
        {
            foreach (ControlGlyph glyph in _controls)
            {
                if (glyph != null && string.Equals(glyph.Control, control, StringComparison.OrdinalIgnoreCase))
                    return glyph.IconKey;
            }

            return null;
        }
    }

    [Serializable]
    public sealed class ActionGlyphOverride
    {
        [SerializeField] private DeviceFamily _family;
        [SerializeField] private string _action;
        [SerializeField] private string _markup;

        public ActionGlyphOverride(DeviceFamily family, string action, string markup)
        {
            _family = family;
            _action = action;
            _markup = markup;
        }

        public DeviceFamily Family => _family;
        public string Action => _action;
        public string Markup => _markup;
    }
}
