using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Velsi.Typecraft.Fonts
{
    [Serializable]
    public sealed class FontRole
    {
        [SerializeField] private string _id;
        [SerializeField] private FontVariant _default = new FontVariant();
        [SerializeField] private List<LocaleFontOverride> _localeOverrides = new List<LocaleFontOverride>();

        public FontRole()
        {
        }

        internal FontRole(string id, TMP_FontAsset font)
        {
            _id = id;
            _default = new FontVariant(font);
        }

        public string Id => _id;
        public FontVariant Default => _default;
        public IReadOnlyList<LocaleFontOverride> LocaleOverrides => _localeOverrides;

        public FontVariant Resolve(string locale)
        {
            if (string.IsNullOrEmpty(locale))
                return _default;

            FontVariant languageMatch = null;
            string language = LocaleCodes.Language(locale);

            foreach (LocaleFontOverride localeOverride in _localeOverrides)
            {
                if (localeOverride?.Variant?.Font == null)
                    continue;

                if (string.Equals(localeOverride.Locale, locale, StringComparison.OrdinalIgnoreCase))
                    return localeOverride.Variant;

                if (languageMatch == null && string.Equals(LocaleCodes.Language(localeOverride.Locale), language, StringComparison.OrdinalIgnoreCase))
                    languageMatch = localeOverride.Variant;
            }

            return languageMatch ?? _default;
        }
    }

    [Serializable]
    public sealed class LocaleFontOverride
    {
        [SerializeField] private string _locale;
        [SerializeField] private FontVariant _variant = new FontVariant();

        public string Locale => _locale;
        public FontVariant Variant => _variant;
    }
}
