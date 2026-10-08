using System.Collections.Generic;

namespace Velsi.Typecraft.Library
{
    public sealed class IconLibrary
    {
        private readonly Dictionary<string, (IconSet Set, IconEntry Entry)> _qualified = new Dictionary<string, (IconSet, IconEntry)>();
        private readonly Dictionary<string, (IconSet Set, IconEntry Entry)> _plain = new Dictionary<string, (IconSet, IconEntry)>();

        public IconLibrary(IEnumerable<IconSet> sets)
        {
            if (sets == null)
                return;

            foreach (IconSet set in sets)
                Add(set);
        }

        public void Add(IconSet set)
        {
            if (set == null)
                return;

            foreach (IconEntry entry in set.Entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Key))
                    continue;

                _qualified[$"{set.SetId}/{entry.Key}"] = (set, entry);
                if (!_plain.ContainsKey(entry.Key))
                    _plain[entry.Key] = (set, entry);
            }
        }

        public bool TryResolve(string reference, out IconSet set, out IconEntry entry)
        {
            if (!string.IsNullOrEmpty(reference)
                && (_qualified.TryGetValue(reference, out var found) || _plain.TryGetValue(reference, out found)))
            {
                set = found.Set;
                entry = found.Entry;
                return true;
            }

            set = null;
            entry = null;
            return false;
        }

        public IEnumerable<string> QualifiedKeys => _qualified.Keys;
    }
}
