using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
#if TYPECRAFT_TMPEFFECTS
using TMPEffects.Components;
using TMPEffects.Databases.AnimationDatabase;
using TMPEffects.TMPAnimations;
using TMPEffects.TMPAnimations.Animations;
using TMPEffects.TMPAnimations.HideAnimations;
using TMPEffects.TMPAnimations.ShowAnimations;
#endif

namespace Velsi.Typecraft.Editor.Window.Effects
{
    public readonly struct EffectBase
    {
        public EffectBase(string label, UnityEngine.Object source, Type type)
        {
            Label = label;
            Source = source;
            Type = type;
        }

        public string Label { get; }
        public UnityEngine.Object Source { get; }
        public Type Type { get; }
    }

    public static class EffectLibrary
    {
        public const string DatabaseName = "TypecraftAnimationDatabase";
        public const string FolderName = "Effects";

        private static readonly Regex ValidName = new Regex(@"^[a-z][a-z0-9_]*$");

        private static readonly HashSet<string> Reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "align", "allcaps", "alpha", "b", "br", "color", "cspace", "font", "font-weight", "gradient", "i", "indent",
            "line-height", "line-indent", "link", "lowercase", "margin", "mark", "material", "mspace", "nobr", "noparse",
            "page", "pos", "rotate", "s", "size", "smallcaps", "space", "sprite", "strikethrough", "style", "sub", "sup",
            "u", "underline", "uppercase", "voffset", "width", "zwsp", "nbsp", "shy", "cr", "zwj"
        };

        public static bool Available
        {
            get
            {
#if TYPECRAFT_TMPEFFECTS
                return EffectCatalog.DefaultAnimationDatabase != null || Project != null;
#else
                return false;
#endif
            }
        }

        public static string Folder(TypecraftSettings settings)
        {
            UnityEngine.Object anchor = settings.Styles != null ? settings.Styles : settings.Fonts != null ? settings.Fonts : (UnityEngine.Object)settings;
            string path = AssetDatabase.GetAssetPath(anchor);
            return $"{Path.GetDirectoryName(path)?.Replace('\\', '/')}/{FolderName}";
        }

        public static string ValidateName(EffectKind kind, string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Введи имя тега";
            if (!ValidName.IsMatch(name))
                return "Только строчные латинские буквы, цифры и «_», первой — буква";
            if (Reserved.Contains(name))
                return $"«{name}» — тег TextMeshPro";
            if (EffectCatalog.Entries.Any(entry => entry.Kind == kind && entry.Name == name))
                return $"Эффект «{TagOf(kind, name)}» уже есть";
            return null;
        }

        public static string TagOf(EffectKind kind, string name) => new EffectEntry(kind, name).TagName;

#if TYPECRAFT_TMPEFFECTS
        public static TMPAnimationDatabase Project =>
            Typecraft.Settings != null ? Typecraft.Settings.AnimationDatabase as TMPAnimationDatabase : null;

        public static TMPAnimationDatabase Current => Project != null ? Project : EffectCatalog.DefaultAnimationDatabase;

        public static bool IsCustom(EffectKind kind, string name)
        {
            TMPAnimationDatabase builtIn = EffectCatalog.DefaultAnimationDatabase;
            TMPAnimationDatabase project = Project;
            if (project == null || kind == EffectKind.Command)
                return false;

            return Entries(SubDatabase(project, kind)).Any(entry => entry.Key == name) &&
                   !Entries(builtIn != null ? SubDatabase(builtIn, kind) : null).Any(entry => entry.Key == name);
        }

        public static UnityEngine.Object AssetOf(EffectKind kind, string name) =>
            Entries(Current != null ? SubDatabase(Current, kind) : null).FirstOrDefault(entry => entry.Key == name).Value;

        public static IReadOnlyList<EffectBase> Bases(EffectKind kind)
        {
            var bases = new List<EffectBase>();
            if (Current != null)
            {
                foreach ((string key, UnityEngine.Object value) in Entries(SubDatabase(Current, kind)).OrderBy(entry => entry.Key))
                {
                    if (value != null)
                        bases.Add(new EffectBase(key, value, value.GetType()));
                }
            }

            switch (kind)
            {
                case EffectKind.Show:
                    bases.Add(new EffectBase("Generic (дорожки)", null, typeof(GenericShowAnimation)));
                    bases.Add(new EffectBase("Stack (стопка)", null, typeof(ShowAnimationStackObject)));
                    break;
                case EffectKind.Hide:
                    bases.Add(new EffectBase("Generic (дорожки)", null, typeof(GenericHideAnimation)));
                    bases.Add(new EffectBase("Stack (стопка)", null, typeof(HideAnimationStackObject)));
                    break;
                default:
                    bases.Add(new EffectBase("Generic (дорожки)", null, typeof(GenericAnimation)));
                    bases.Add(new EffectBase("Stack (стопка)", null, typeof(AnimationStackObject)));
                    break;
            }

            return bases;
        }

        public static ScriptableObject CreateDraft(EffectBase source)
        {
            ScriptableObject draft = source.Source is ScriptableObject asset
                ? UnityEngine.Object.Instantiate(asset)
                : ScriptableObject.CreateInstance(source.Type);
            draft.hideFlags = HideFlags.DontSave;
            draft.name = "Typecraft draft";
            return draft;
        }

        public static TMPAnimationDatabase EnsureProject()
        {
            TypecraftSettings settings = Typecraft.Settings;
            if (settings == null)
                return null;

            TMPAnimationDatabase project = Project;
            if (project == null)
            {
                string folder = Folder(settings);
                EnsureFolder(folder);

                project = ScriptableObject.CreateInstance<TMPAnimationDatabase>();
                project.name = DatabaseName;
                AssetDatabase.CreateAsset(project, $"{folder}/{DatabaseName}.asset");

                var basic = Child<TMPBasicAnimationDatabase>(project, "Basic Animations");
                var show = Child<TMPShowAnimationDatabase>(project, "Show Animations");
                var hide = Child<TMPHideAnimationDatabase>(project, "Hide Animations");

                var serialized = new SerializedObject(project);
                serialized.FindProperty("basicAnimationDatabase").objectReferenceValue = basic;
                serialized.FindProperty("showAnimationDatabase").objectReferenceValue = show;
                serialized.FindProperty("hideAnimationDatabase").objectReferenceValue = hide;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var serializedSettings = new SerializedObject(settings);
                serializedSettings.FindProperty("_animationDatabase").objectReferenceValue = project;
                serializedSettings.ApplyModifiedProperties();
                TypecraftLog.Info($"Создана база анимаций проекта: {AssetDatabase.GetAssetPath(project)}");
            }

            MergeBuiltIns(project);
            AssetDatabase.SaveAssets();
            return project;
        }

        public static ScriptableObject Save(EffectKind kind, string name, ScriptableObject draft)
        {
            string error = ValidateName(kind, name);
            if (error != null)
                throw new ArgumentException(error);

            TMPAnimationDatabase project = EnsureProject();
            if (project == null)
                throw new InvalidOperationException("Нет TypecraftSettings");

            string folder = Folder(Typecraft.Settings);
            EnsureFolder(folder);

            ScriptableObject asset = UnityEngine.Object.Instantiate(draft);
            asset.hideFlags = HideFlags.None;
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{kind}_{name}.asset");
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            Undo.RegisterCreatedObjectUndo(asset, "Typecraft: новый эффект");

            SetEntry(SubDatabase(project, kind), name, asset, "Typecraft: новый эффект");
            AssetDatabase.SaveAssets();
            Changed();
            return asset;
        }

        public static bool Remove(EffectKind kind, string name)
        {
            TMPAnimationDatabase project = Project;
            if (project == null || !IsCustom(kind, name))
                return false;

            ScriptableObject database = SubDatabase(project, kind);
            UnityEngine.Object asset = Entries(database).FirstOrDefault(entry => entry.Key == name).Value;
            RemoveEntry(database, name);

            string path = asset != null ? AssetDatabase.GetAssetPath(asset) : null;
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Folder(Typecraft.Settings) + "/", StringComparison.Ordinal) && !IsReferenced(asset))
                AssetDatabase.MoveAssetToTrash(path);

            AssetDatabase.SaveAssets();
            Changed();
            return true;
        }

        public static TMPAnimationDatabase CreatePreviewDatabase(EffectKind kind, string name, ScriptableObject draft)
        {
            TMPAnimationDatabase source = Current;
            var main = ScriptableObject.CreateInstance<TMPAnimationDatabase>();
            main.hideFlags = HideFlags.HideAndDontSave;

            var serialized = new SerializedObject(main);
            foreach (EffectKind each in new[] { EffectKind.Basic, EffectKind.Show, EffectKind.Hide })
            {
                ScriptableObject child = each switch
                {
                    EffectKind.Show => ScriptableObject.CreateInstance<TMPShowAnimationDatabase>(),
                    EffectKind.Hide => ScriptableObject.CreateInstance<TMPHideAnimationDatabase>(),
                    _ => ScriptableObject.CreateInstance<TMPBasicAnimationDatabase>()
                };
                child.hideFlags = HideFlags.HideAndDontSave;

                var entries = Entries(source != null ? SubDatabase(source, each) : null).ToList();
                if (each == kind && draft != null && !string.IsNullOrEmpty(name))
                {
                    entries.RemoveAll(entry => entry.Key == name);
                    entries.Add((name, draft));
                }

                WriteEntries(child, entries);
                serialized.FindProperty(SubDatabaseField(each)).objectReferenceValue = child;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return main;
        }

        public static void DestroyPreviewDatabase(TMPAnimationDatabase database)
        {
            if (database == null)
                return;

            foreach (EffectKind kind in new[] { EffectKind.Basic, EffectKind.Show, EffectKind.Hide })
            {
                ScriptableObject child = SubDatabase(database, kind);
                if (child != null)
                    UnityEngine.Object.DestroyImmediate(child);
            }

            UnityEngine.Object.DestroyImmediate(database);
        }

        public static void Configure(TMPAnimator animator)
        {
            TMPAnimationDatabase database = Current;
            if (animator == null || database == null || animator.Database == database)
                return;

            var serialized = new SerializedObject(animator);
            serialized.FindProperty("database").objectReferenceValue = database;
            serialized.FindProperty("useDefaultDatabase").boolValue = database == EffectCatalog.DefaultAnimationDatabase;
            serialized.FindProperty("initDatabase").boolValue = true;
            serialized.ApplyModifiedProperties();

            if (animator.isActiveAndEnabled)
                animator.SetDatabase(database);
        }

        public static bool UsesDefault(TMPAnimator animator) =>
            animator != null && (animator.Database == null || animator.Database == EffectCatalog.DefaultAnimationDatabase);

        internal static ScriptableObject SubDatabase(TMPAnimationDatabase database, EffectKind kind) => kind switch
        {
            EffectKind.Basic => database.BasicAnimationDatabase,
            EffectKind.Show => database.ShowAnimationDatabase,
            EffectKind.Hide => database.HideAnimationDatabase,
            _ => null
        };

        private static string SubDatabaseField(EffectKind kind) => kind switch
        {
            EffectKind.Show => "showAnimationDatabase",
            EffectKind.Hide => "hideAnimationDatabase",
            _ => "basicAnimationDatabase"
        };

        private static string DictionaryField(ScriptableObject database) => database switch
        {
            TMPShowAnimationDatabase _ => "showAnimations",
            TMPHideAnimationDatabase _ => "hideAnimations",
            _ => "animations"
        };

        private static T Child<T>(TMPAnimationDatabase parent, string name) where T : ScriptableObject
        {
            var child = ScriptableObject.CreateInstance<T>();
            child.name = name;
            AssetDatabase.AddObjectToAsset(child, parent);
            return child;
        }

        private static void MergeBuiltIns(TMPAnimationDatabase project)
        {
            TMPAnimationDatabase builtIn = EffectCatalog.DefaultAnimationDatabase;
            if (builtIn == null || builtIn == project)
                return;

            foreach (EffectKind kind in new[] { EffectKind.Basic, EffectKind.Show, EffectKind.Hide })
            {
                ScriptableObject target = SubDatabase(project, kind);
                if (target == null)
                    continue;

                var existing = new HashSet<string>(Entries(target).Select(entry => entry.Key));
                List<(string Key, UnityEngine.Object Value)> missing = Entries(SubDatabase(builtIn, kind)).Where(entry => !existing.Contains(entry.Key)).ToList();
                if (missing.Count == 0)
                    continue;

                WriteEntries(target, Entries(target).Concat(missing).ToList());
                EditorUtility.SetDirty(target);
            }
        }

        private static IEnumerable<(string Key, UnityEngine.Object Value)> Entries(ScriptableObject database)
        {
            if (database == null)
                yield break;

            SerializedProperty list = new SerializedObject(database).FindProperty($"{DictionaryField(database)}._serializedList");
            if (list == null)
                yield break;

            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                string key = element.FindPropertyRelative("Key").stringValue;
                if (!string.IsNullOrEmpty(key))
                    yield return (key, element.FindPropertyRelative("Value").objectReferenceValue);
            }
        }

        private static void WriteEntries(ScriptableObject database, IReadOnlyList<(string Key, UnityEngine.Object Value)> entries)
        {
            var serialized = new SerializedObject(database);
            SerializedProperty list = serialized.FindProperty($"{DictionaryField(database)}._serializedList");
            list.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Key").stringValue = entries[i].Key;
                element.FindPropertyRelative("Value").objectReferenceValue = entries[i].Value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEntry(ScriptableObject database, string key, UnityEngine.Object value, string undo)
        {
            var serialized = new SerializedObject(database);
            SerializedProperty list = serialized.FindProperty($"{DictionaryField(database)}._serializedList");
            list.arraySize++;
            SerializedProperty element = list.GetArrayElementAtIndex(list.arraySize - 1);
            element.FindPropertyRelative("Key").stringValue = key;
            element.FindPropertyRelative("Value").objectReferenceValue = value;
            Undo.SetCurrentGroupName(undo);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
        }

        private static void RemoveEntry(ScriptableObject database, string key)
        {
            var serialized = new SerializedObject(database);
            SerializedProperty list = serialized.FindProperty($"{DictionaryField(database)}._serializedList");
            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                if (list.GetArrayElementAtIndex(i).FindPropertyRelative("Key").stringValue == key)
                    list.DeleteArrayElementAtIndex(i);
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
        }

        private static bool IsReferenced(UnityEngine.Object asset)
        {
            TMPAnimationDatabase project = Project;
            return project != null && new[] { EffectKind.Basic, EffectKind.Show, EffectKind.Hide }
                .Any(kind => Entries(SubDatabase(project, kind)).Any(entry => entry.Value == asset));
        }

        private static void Changed()
        {
            EffectCatalog.Invalidate();
            Typecraft.Refresh();
        }
#endif

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
