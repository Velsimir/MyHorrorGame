using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    public sealed class DialogueVariables : IDialogueVariables
    {
        private readonly Dictionary<string, DialogueValue> _values = new Dictionary<string, DialogueValue>(StringComparer.Ordinal);

        public event Action<string> Changed;

        public int Count => _values.Count;
        public IEnumerable<string> Names => _values.Keys;

        public bool TryGet(string name, out DialogueValue value)
        {
            if (string.IsNullOrEmpty(name))
            {
                value = default;
                return false;
            }

            return _values.TryGetValue(name, out value);
        }

        public void Set(string name, DialogueValue value)
        {
            if (string.IsNullOrEmpty(name))
                return;

            if (_values.TryGetValue(name, out DialogueValue current) && Same(current, value))
                return;

            _values[name] = value;
            Changed?.Invoke(name);
        }

        public bool Remove(string name)
        {
            if (string.IsNullOrEmpty(name) || !_values.Remove(name))
                return false;

            Changed?.Invoke(name);
            return true;
        }

        public void Clear()
        {
            if (_values.Count == 0)
                return;

            var names = new List<string>(_values.Keys);
            _values.Clear();
            foreach (string name in names)
                Changed?.Invoke(name);
        }

        public string ToJson(bool prettyPrint = false)
        {
            var snapshot = new Snapshot();
            foreach (KeyValuePair<string, DialogueValue> pair in _values)
                snapshot.Entries.Add(new Entry { Name = pair.Key, Value = pair.Value });

            return JsonUtility.ToJson(snapshot, prettyPrint);
        }

        public void FromJson(string json)
        {
            Snapshot snapshot = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<Snapshot>(json);
            var loaded = new Dictionary<string, DialogueValue>(StringComparer.Ordinal);
            if (snapshot?.Entries != null)
            {
                foreach (Entry entry in snapshot.Entries)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.Name))
                        loaded[entry.Name] = entry.Value;
                }
            }

            var changed = new List<string>();
            foreach (KeyValuePair<string, DialogueValue> pair in _values)
            {
                if (!loaded.TryGetValue(pair.Key, out DialogueValue value) || !Same(value, pair.Value))
                    changed.Add(pair.Key);
            }

            foreach (string name in loaded.Keys)
            {
                if (!_values.ContainsKey(name))
                    changed.Add(name);
            }

            _values.Clear();
            foreach (KeyValuePair<string, DialogueValue> pair in loaded)
                _values[pair.Key] = pair.Value;

            foreach (string name in changed)
                Changed?.Invoke(name);
        }

        private static bool Same(DialogueValue left, DialogueValue right) => left.Type == right.Type && left == right;

        [Serializable]
        private sealed class Snapshot
        {
            public List<Entry> Entries = new List<Entry>();
        }

        [Serializable]
        private sealed class Entry
        {
            public string Name;
            public DialogueValue Value;
        }
    }
}
