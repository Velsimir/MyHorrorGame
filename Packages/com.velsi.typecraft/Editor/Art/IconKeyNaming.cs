using System.Collections.Generic;
using System.Linq;

namespace Velsi.Typecraft.Editor.Art
{
    public static class IconKeyNaming
    {
        private const float SharedAffixShare = 0.8f;

        public static Dictionary<string, string> KeysFor(IReadOnlyCollection<string> glyphNames)
        {
            string prefix = SharedAffix(glyphNames, name => FirstToken(name), suffix: false);
            string suffix = SharedAffix(glyphNames, name => LastTokens(name), suffix: true);

            var keys = glyphNames.ToDictionary(name => name, name => ToKey(Strip(name, prefix, suffix)));

            foreach (IGrouping<string, string> clash in keys.GroupBy(p => p.Value, p => p.Key).Where(g => g.Count() > 1).ToList())
            {
                foreach (string name in clash)
                    keys[name] = ToKey(name);
            }

            return keys;
        }

        public static string ToKey(string raw) =>
            raw.Trim().Replace(' ', '_').Replace('-', '_').ToLowerInvariant();

        private static string Strip(string name, string prefix, string suffix)
        {
            string result = name;
            if (prefix != null && result.StartsWith(prefix) && result.Length > prefix.Length)
                result = result.Substring(prefix.Length);
            if (suffix != null && result.EndsWith(suffix) && result.Length > suffix.Length)
                result = result.Substring(0, result.Length - suffix.Length);
            return result;
        }

        private static string SharedAffix(IReadOnlyCollection<string> names, System.Func<string, IEnumerable<string>> candidates, bool suffix)
        {
            if (names.Count < 2)
                return null;

            return names
                .SelectMany(candidates)
                .GroupBy(affix => affix)
                .Where(group => group.Count() >= names.Count * SharedAffixShare)
                .OrderByDescending(group => group.Key.Length)
                .Select(group => group.Key)
                .FirstOrDefault();
        }

        private static IEnumerable<string> FirstToken(string name)
        {
            int index = name.IndexOf('_');
            if (index > 0)
                yield return name.Substring(0, index + 1);
        }

        private static IEnumerable<string> LastTokens(string name)
        {
            int last = name.LastIndexOf('_');
            if (last <= 0)
                yield break;

            yield return name.Substring(last);

            int previous = name.LastIndexOf('_', last - 1);
            if (previous > 0)
                yield return name.Substring(previous);
        }
    }
}
