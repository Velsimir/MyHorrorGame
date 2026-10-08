using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Text.Tables
{
    [CreateAssetMenu(fileName = "StringTable", menuName = "Typecraft/String Table")]
    public sealed class StringTable : ScriptableObject
    {
        [SerializeField] private string _tableId;
        [SerializeField] private List<StringEntry> _entries = new List<StringEntry>();

        private Dictionary<string, StringEntry> _lookup;

        public string TableId => string.IsNullOrEmpty(_tableId) ? name : _tableId;
        public IReadOnlyList<StringEntry> Entries => _entries;

        public bool TryGet(string key, string locale, out string value)
        {
            value = null;
            return !string.IsNullOrEmpty(key) && Lookup().TryGetValue(key, out StringEntry entry) && entry.TryGet(locale, out value);
        }

        public bool Contains(string key) => !string.IsNullOrEmpty(key) && Lookup().ContainsKey(key);

        internal void SetTableId(string tableId) => _tableId = tableId;

        internal StringEntry Find(string key) => !string.IsNullOrEmpty(key) && Lookup().TryGetValue(key, out StringEntry entry) ? entry : null;

        internal StringEntry Add(string key)
        {
            StringEntry existing = Find(key);
            if (existing != null)
                return existing;

            var entry = new StringEntry(key);
            _entries.Add(entry);
            _lookup = null;
            return entry;
        }

        internal bool Remove(string key)
        {
            int removed = _entries.RemoveAll(entry => entry.Key == key);
            _lookup = null;
            return removed > 0;
        }

        internal bool Rename(string key, string newKey)
        {
            StringEntry entry = Find(key);
            if (entry == null || string.IsNullOrEmpty(newKey) || Contains(newKey))
                return false;

            entry.Key = newKey;
            _lookup = null;
            return true;
        }

        private Dictionary<string, StringEntry> Lookup()
        {
            if (_lookup != null)
                return _lookup;

            _lookup = new Dictionary<string, StringEntry>(StringComparer.Ordinal);
            foreach (StringEntry entry in _entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Key))
                    _lookup[entry.Key] = entry;
            }

            return _lookup;
        }

        private void OnValidate() => _lookup = null;
    }

    [Serializable]
    public sealed class StringEntry
    {
        [SerializeField] private string _key;
        [SerializeField] private string _comment;
        [SerializeField] private List<LocalizedValue> _values = new List<LocalizedValue>();

        public StringEntry(string key)
        {
            _key = key;
        }

        public string Key
        {
            get => _key;
            internal set => _key = value;
        }

        public string Comment
        {
            get => _comment;
            internal set => _comment = value;
        }

        public bool TryGet(string locale, out string value)
        {
            foreach (LocalizedValue localized in _values)
            {
                if (localized.Locale == locale && !string.IsNullOrEmpty(localized.Text))
                {
                    value = localized.Text;
                    return true;
                }
            }

            value = null;
            return false;
        }

        internal void Set(string locale, string text)
        {
            int index = _values.FindIndex(value => value.Locale == locale);
            if (string.IsNullOrEmpty(text))
            {
                if (index >= 0)
                    _values.RemoveAt(index);
                return;
            }

            if (index >= 0)
                _values[index] = new LocalizedValue(locale, text);
            else
                _values.Add(new LocalizedValue(locale, text));
        }
    }

    [Serializable]
    public struct LocalizedValue
    {
        [SerializeField] private string _locale;
        [SerializeField, TextArea(1, 6)] private string _text;

        public LocalizedValue(string locale, string text)
        {
            _locale = locale;
            _text = text;
        }

        public string Locale => _locale;
        public string Text => _text;
    }
}
