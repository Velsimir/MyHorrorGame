using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Velsi.Typecraft.Editor.Window.Effects
{
    public enum EffectKind
    {
        Basic,
        Show,
        Hide,
        Command
    }

    public readonly struct EffectEntry
    {
        public EffectEntry(EffectKind kind, string name)
        {
            Kind = kind;
            Name = name;
        }

        public EffectKind Kind { get; }
        public string Name { get; }

        public string TagName => Kind switch
        {
            EffectKind.Show => "+" + Name,
            EffectKind.Hide => "-" + Name,
            EffectKind.Command => "!" + Name,
            _ => Name
        };

        public bool Wraps => Kind != EffectKind.Command;
        public string Open => Kind == EffectKind.Command ? $"<!{Name}={DefaultCommandValue(Name)}>" : $"<{TagName}>";
        public string Close => Wraps ? $"</{TagName}>" : string.Empty;

        private static string DefaultCommandValue(string name) => name switch
        {
            "wait" => "0.5",
            "delay" => "0.05",
            "skippable" => "true",
            _ => "1"
        };
    }

    public static class EffectCatalog
    {
        private static List<EffectEntry> _entries;
        private static HashSet<string> _tagNames;

        public static IReadOnlyList<EffectEntry> Entries
        {
            get
            {
                if (_entries == null)
                    Load();
                return _entries;
            }
        }

        public static bool IsEffectTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return false;

            if (tagName[0] == '+' || tagName[0] == '-' || IsWriterTag(tagName))
                return true;

            if (_tagNames == null)
                Load();

            return _tagNames.Contains(tagName);
        }

#if TYPECRAFT_TMPEFFECTS
        public static TMPEffects.Databases.AnimationDatabase.TMPAnimationDatabase DefaultAnimationDatabase =>
            DefaultDatabase("defaultAnimationDatabase") as TMPEffects.Databases.AnimationDatabase.TMPAnimationDatabase;

        public static TMPEffects.Databases.CommandDatabase.TMPCommandDatabase DefaultCommandDatabase =>
            DefaultDatabase("defaultCommandDatabase") as TMPEffects.Databases.CommandDatabase.TMPCommandDatabase;

        private static Object DefaultDatabase(string property)
        {
            ScriptableObject settings = Resources.Load<ScriptableObject>("TMPEffects Settings");
            return settings != null ? new SerializedObject(settings).FindProperty(property)?.objectReferenceValue : null;
        }

#endif
        public static bool IsWriterTag(string tagName) => Velsi.Typecraft.Markup.EffectTags.IsWriterTag(tagName);

        public static void Invalidate()
        {
            _entries = null;
            _tagNames = null;
        }

        private static void Load()
        {
            _entries = new List<EffectEntry>();
            _tagNames = new HashSet<string>();

#if TYPECRAFT_TMPEFFECTS
            ScriptableObject settings = Resources.Load<ScriptableObject>("TMPEffects Settings");
            if (settings == null)
                return;

            var serializedSettings = new SerializedObject(settings);
            TMPEffects.Databases.AnimationDatabase.TMPAnimationDatabase animationDatabase = EffectLibrary.Current;
            if (animationDatabase != null)
            {
                Collect(animationDatabase.BasicAnimationDatabase, EffectKind.Basic);
                Collect(animationDatabase.ShowAnimationDatabase, EffectKind.Show);
                Collect(animationDatabase.HideAnimationDatabase, EffectKind.Hide);
            }

            Collect(serializedSettings.FindProperty("defaultCommandDatabase")?.objectReferenceValue, EffectKind.Command);
#endif
        }

        private static void Collect(Object database, EffectKind kind)
        {
            if (database == null)
                return;

            var serialized = new SerializedObject(database);
            SerializedProperty iterator = serialized.GetIterator();
            var seen = new HashSet<string>();

            while (iterator.Next(true))
            {
                if (iterator.propertyType != SerializedPropertyType.String || iterator.name != "Key")
                    continue;

                string name = iterator.stringValue;
                if (string.IsNullOrEmpty(name) || !seen.Add(name))
                    continue;

                _entries.Add(new EffectEntry(kind, name));
                if (kind == EffectKind.Basic)
                    _tagNames.Add(name);
            }
        }
    }
}
