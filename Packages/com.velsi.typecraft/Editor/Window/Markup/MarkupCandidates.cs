using System.Collections.Generic;
using System.Linq;
using Velsi.Typecraft.Editor.Window.Effects;
using Velsi.Typecraft.Library;
using Velsi.Typecraft.Styles;
#if TYPECRAFT_INPUTSYSTEM
using Velsi.Typecraft.InputSystem;
#endif

namespace Velsi.Typecraft.Editor.Window.Markup
{
    public static class MarkupCandidates
    {
        private static readonly string[] TmpTags =
        {
            "b", "i", "u", "s", "uppercase", "lowercase", "smallcaps", "noparse", "br", "color=#", "size=", "cspace=",
            "line-height=", "voffset=", "mark=#", "link=", "font=\"\"", "material=\"\"", "gradient=\"\"", "sprite name=\"\""
        };

        public static IEnumerable<string> For(char opener)
        {
            TypecraftSettings settings = Typecraft.Settings;
            switch (opener)
            {
                case '{':
                    foreach (string action in ActionNames())
                        yield return "key:" + action;
                    foreach (IconSet set in AllIconSets())
                    foreach (IconEntry entry in set.Entries)
                        yield return $"{set.SetId}/{entry.Key}";
                    if (settings != null)
                    {
                        foreach (IconSet set in settings.IconSets.Where(s => s != null))
                        foreach (IconEntry entry in set.Entries)
                            yield return entry.Key;
                    }
                    break;

                case '[':
                    if (settings?.Styles != null)
                    {
                        foreach (TextStyle style in settings.Styles.Styles.Where(s => s != null && !string.IsNullOrEmpty(s.Id)))
                        {
                            yield return style.Id;
                            yield return "/" + style.Id;
                        }
                    }
                    break;

                case '<':
                    foreach (EffectEntry effect in EffectCatalog.Entries)
                        yield return effect.Open.Substring(1, effect.Open.Length - 2);
                    foreach (string tag in TmpTags)
                        yield return tag;
                    break;
            }
        }

        private static IEnumerable<string> ActionNames()
        {
#if TYPECRAFT_INPUTSYSTEM
            if (Typecraft.Settings?.InputGlyphs is InputGlyphProfile profile && profile.Actions != null)
            {
                foreach (UnityEngine.InputSystem.InputActionMap map in profile.Actions.actionMaps)
                foreach (UnityEngine.InputSystem.InputAction action in map.actions)
                    yield return action.name;
            }
#else
            yield break;
#endif
        }

        public static IEnumerable<IconSet> AllIconSets()
        {
            TypecraftSettings settings = Typecraft.Settings;
            if (settings == null)
                yield break;

            var seen = new HashSet<IconSet>();
            foreach (IconSet set in settings.IconSets)
            {
                if (set != null && seen.Add(set))
                    yield return set;
            }

            if (settings.InputGlyphs == null)
                yield break;

            foreach (IconSet set in settings.InputGlyphs.IconSets)
            {
                if (set != null && seen.Add(set))
                    yield return set;
            }
        }
    }
}
