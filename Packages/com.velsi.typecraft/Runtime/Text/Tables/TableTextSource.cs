using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Text.Tables
{
    [CreateAssetMenu(fileName = "TableTextSource", menuName = "Typecraft/Text Sources/String Tables")]
    public sealed class TableTextSource : ScriptableObject, ITextSource
    {
        [SerializeField] private List<string> _locales = new List<string> { "en" };
        [SerializeField] private string _baseLocale = "en";
        [SerializeField] private bool _pseudoLocale = true;
        [SerializeField] private List<StringTable> _tables = new List<StringTable>();

        private readonly List<string> _available = new List<string>();

        public IReadOnlyList<string> Locales
        {
            get
            {
                _available.Clear();
                _available.AddRange(_locales);
                if (_pseudoLocale)
                    _available.Add(Pseudolocalizer.Locale);
                return _available;
            }
        }

        public IReadOnlyList<string> ProjectLocales => _locales;
        public string BaseLocale => string.IsNullOrEmpty(_baseLocale) ? (_locales.Count > 0 ? _locales[0] : "en") : _baseLocale;
        public IReadOnlyList<StringTable> Tables => _tables;

        public void Activate()
        {
        }

        public void Request(string table, string key, string locale, Action<TextEntry> onReady) =>
            onReady(Lookup(table, key, locale));

        public void SelectLocale(string localeCode) => Typecraft.SetLocale(localeCode);

        public TextEntry Lookup(string table, string key, string locale)
        {
            StringTable source = FindTable(table, key);
            if (source == null)
                return TextEntry.Missing(table, key, locale);

            if (source.TryGet(key, locale, out string value))
                return new TextEntry(source.TableId, key, locale, value);

            return source.TryGet(key, BaseLocale, out value)
                ? new TextEntry(source.TableId, key, BaseLocale, value)
                : TextEntry.Missing(source.TableId, key, locale);
        }

        public StringTable FindTable(string table, string key = null)
        {
            foreach (StringTable candidate in _tables)
            {
                if (candidate == null)
                    continue;

                if (!string.IsNullOrEmpty(table))
                {
                    if (candidate.TableId == table)
                        return candidate;
                }
                else if (key == null || candidate.Contains(key))
                {
                    return candidate;
                }
            }

            return null;
        }

        internal void AddLocale(string locale)
        {
            if (!string.IsNullOrEmpty(locale) && !_locales.Contains(locale))
                _locales.Add(locale);
        }

        internal void AddTable(StringTable table)
        {
            if (table != null && !_tables.Contains(table))
                _tables.Add(table);
        }

        internal void Configure(IEnumerable<string> locales, string baseLocale, IEnumerable<StringTable> tables)
        {
            _locales = new List<string>(locales);
            _baseLocale = baseLocale;
            _tables = new List<StringTable>(tables);
        }

        private void OnValidate()
        {
            if (Typecraft.Settings != null && ReferenceEquals(Typecraft.Settings.TextSource, this))
                Typecraft.Refresh();
        }
    }
}
