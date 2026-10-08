using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Text
{
    [CreateAssetMenu(fileName = "MarkupLayer", menuName = "Typecraft/Markup Layer")]
    public sealed class MarkupLayer : ScriptableObject
    {
        [SerializeField] private string _tableId;
        [SerializeField] private List<MarkupLayerEntry> _entries = new List<MarkupLayerEntry>();

        private Dictionary<(string Key, string Locale), MarkupLayerEntry> _lookup;
        private int _lookupCount = -1;

        public string TableId => _tableId;
        public IReadOnlyList<MarkupLayerEntry> Entries => _entries;

        public MarkupLayerEntry Find(string key, string locale)
        {
            if (string.IsNullOrEmpty(key) || locale == null)
                return null;

            if (Lookup().TryGetValue((key, locale), out MarkupLayerEntry entry) && entry.Key == key && entry.Locale == locale)
                return entry;

            if (entry == null)
                return null;

            _lookup = null;
            return Lookup().TryGetValue((key, locale), out entry) ? entry : null;
        }

        public string Resolve(string key, string locale, string tableText)
        {
            MarkupLayerEntry entry = Find(key, locale);
            return entry != null && entry.Source == tableText && !string.IsNullOrEmpty(entry.Markup) ? entry.Markup : tableText;
        }

        internal void SetTableId(string tableId) => _tableId = tableId;

        internal void Invalidate() => _lookup = null;

        internal void Set(string key, string locale, string markup, string source)
        {
            MarkupLayerEntry entry = Find(key, locale);
            if (entry == null)
            {
                entry = new MarkupLayerEntry(key, locale);
                _entries.Add(entry);
                _lookup = null;
            }

            entry.Markup = markup;
            entry.Source = source;
        }

        internal bool Remove(string key, string locale)
        {
            int removed = _entries.RemoveAll(entry => entry != null && entry.Key == key && entry.Locale == locale);
            _lookup = null;
            return removed > 0;
        }

        internal int RemoveKey(string key)
        {
            int removed = _entries.RemoveAll(entry => entry != null && entry.Key == key);
            _lookup = null;
            return removed;
        }

        internal int RenameKey(string key, string newKey)
        {
            int renamed = 0;
            foreach (MarkupLayerEntry entry in _entries)
            {
                if (entry == null || entry.Key != key)
                    continue;

                entry.Key = newKey;
                renamed++;
            }

            _lookup = null;
            return renamed;
        }

        private Dictionary<(string Key, string Locale), MarkupLayerEntry> Lookup()
        {
            if (_lookup != null && _lookupCount == _entries.Count)
                return _lookup;

            _lookup = new Dictionary<(string, string), MarkupLayerEntry>();
            foreach (MarkupLayerEntry entry in _entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Key) && entry.Locale != null)
                    _lookup[(entry.Key, entry.Locale)] = entry;
            }

            _lookupCount = _entries.Count;
            return _lookup;
        }

        private void OnValidate()
        {
            _lookup = null;
            TypecraftSettings settings = Typecraft.Settings;
            if (settings != null && settings.FindMarkupLayer(_tableId) == this)
                Typecraft.Refresh();
        }
    }

    [Serializable]
    public sealed class MarkupLayerEntry
    {
        [SerializeField] private string _key;
        [SerializeField] private string _locale;
        [SerializeField, TextArea(1, 6)] private string _markup;
        [SerializeField, TextArea(1, 6)] private string _source;

        public MarkupLayerEntry(string key, string locale)
        {
            _key = key;
            _locale = locale;
        }

        public string Key
        {
            get => _key;
            internal set => _key = value;
        }

        public string Locale => _locale;

        public string Markup
        {
            get => _markup;
            internal set => _markup = value;
        }

        public string Source
        {
            get => _source;
            internal set => _source = value;
        }
    }
}
