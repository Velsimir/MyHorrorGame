using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using Velsi.Typecraft.Fonts;
using Velsi.Typecraft.Library;
using Velsi.Typecraft.Styles;
using Velsi.Typecraft.Text.Tables;
#if TYPECRAFT_INPUTSYSTEM
using UnityEngine.InputSystem;
using Velsi.Typecraft.InputSystem;
#endif

namespace Velsi.Typecraft.Editor.Setup
{
    public static class TypecraftSetup
    {
        private const string BasePromptsRoot = "Packages/com.velsi.typecraft/Art/Prompts";
        private const string DefaultConfigFolder = "Assets/Typecraft";
        private const string DefaultResourcesFolder = "Assets/Typecraft/Resources";

        [MenuItem("Tools/Typecraft/Create String Tables")]
        public static TableTextSource CreateStringTables()
        {
            TypecraftSettings settings = Typecraft.Settings;
            string folder = settings != null && settings.Styles != null
                ? Path.GetDirectoryName(AssetDatabase.GetAssetPath(settings.Styles))?.Replace('\\', '/')
                : DefaultConfigFolder;

            TableTextSource source = CreateTableSource(folder);
            if (settings != null)
            {
                var serialized = new SerializedObject(settings);
                serialized.FindProperty("_textSource").objectReferenceValue = source;
                serialized.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Typecraft.Refresh();
            }

            TypecraftLog.Info($"Таблицы строк созданы: {AssetDatabase.GetAssetPath(source)}");
            return source;
        }

        [MenuItem("Tools/Typecraft/Create Project Settings")]
        private static void CreateFromMenu()
        {
            TypecraftSettings settings = CreateDefaults(DefaultConfigFolder, DefaultResourcesFolder);
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        public static TypecraftSettings CreateDefaults(string configFolder, string resourcesFolder)
        {
            string settingsPath = $"{resourcesFolder}/{TypecraftSettings.ResourcesPath}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TypecraftSettings>(settingsPath);
            if (existing != null)
            {
                TypecraftLog.Warning($"Settings already exist: {settingsPath}");
                return existing;
            }

            EnsureFolder(configFolder);
            EnsureFolder(resourcesFolder);

            StyleLibrary styles = CreateAsset<StyleLibrary>($"{configFolder}/StyleLibrary.asset");
            foreach (TextStyle style in StarterStyles())
                styles.Add(style);

            FontProfile fonts = CreateAsset<FontProfile>($"{configFolder}/FontProfile.asset");
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            fonts.Add(new FontRole("Body", defaultFont));
            fonts.Add(new FontRole("Header", defaultFont));

            var settings = CreateAsset<TypecraftSettings>(settingsPath);
            settings.Configure(styles, fonts, new List<IconSet>(), CreateInputGlyphs(configFolder), CreateTextSource(configFolder));

            EditorUtility.SetDirty(styles);
            EditorUtility.SetDirty(fonts);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Typecraft.Initialize(settings);
            TypecraftLog.Info($"Project settings created: {settingsPath}");
            return settings;
        }

        private static IEnumerable<TextStyle> StarterStyles()
        {
            yield return new TextStyle("warn", "Warning", new Color(0.89f, 0.36f, 0.29f));
            yield return new TextStyle("whisper", "Whisper", new Color(0.6f, 0.65f, 0.68f), italic: true, characterSpacingEm: 0.04f);
            yield return new TextStyle("title", "Title", textCase: TextCase.Upper, sizePercent: 130f, characterSpacingEm: 0.15f);
            yield return new TextStyle("hint", "Hint", new Color(0.85f, 0.8f, 0.6f), sizePercent: 85f);
        }

        private static ScriptableObject CreateInputGlyphs(string configFolder)
        {
#if TYPECRAFT_INPUTSYSTEM
            var profile = CreateAsset<InputGlyphProfile>($"{configFolder}/InputGlyphProfile.asset");
            profile.Configure(FindActions(), DefaultFamilies(), DefaultControlGlyphs.TouchOverrides());
            EditorUtility.SetDirty(profile);
            return profile;
#else
            return null;
#endif
        }

        private static ScriptableObject CreateTextSource(string configFolder) => CreateTableSource(configFolder);

        private static TableTextSource CreateTableSource(string configFolder)
        {
            EnsureFolder(configFolder);
            string path = $"{configFolder}/TableTextSource.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TableTextSource>(path);
            if (existing != null)
                return existing;

            var table = CreateAsset<StringTable>($"{configFolder}/StringTable_Main.asset");
            table.SetTableId("Main");
            foreach ((string key, string comment, string en, string ru, string zh) in StarterStrings())
            {
                StringEntry entry = table.Add(key);
                entry.Comment = comment;
                entry.Set("en", en);
                entry.Set("ru", ru);
                entry.Set("zh-Hans", zh);
            }

            var source = CreateAsset<TableTextSource>(path);
            source.Configure(new[] { "en", "ru", "zh-Hans" }, "en", new[] { table });
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(source);
            AssetDatabase.SaveAssets();
            return source;
        }

        private static IEnumerable<(string, string, string, string, string)> StarterStrings()
        {
            yield return ("ui.door.locked", "Подсказка у запертой двери",
                "The door is [warn]locked[/warn]. Press {key:Interact} to inspect the lock.",
                "Дверь [warn]заперта[/warn]. Нажми {key:Interact}, чтобы осмотреть замок.",
                "门[warn]锁着[/warn]。按 {key:Interact} 检查门锁。");
            yield return ("ui.whisper.still_here", "Шёпот после закрытия двери",
                "[whisper]It is still here…[/whisper]",
                "[whisper]Он всё ещё здесь…[/whisper]",
                null);
            yield return ("ui.location.basement", "Заголовок локации",
                "[title]Basement[/title]",
                "[title]Подвал[/title]",
                "[title]地下室[/title]");
        }

#if TYPECRAFT_INPUTSYSTEM
        public static List<FamilyGlyphs> DefaultFamilies() => new List<FamilyGlyphs>
        {
            Family(DeviceFamily.KeyboardMouse, "KeyboardMouse_Dark", null),
            Family(DeviceFamily.Xbox, "XboxSeries", null),
            Family(DeviceFamily.PlayStation, "PS5", DeviceFamily.Xbox),
            Family(DeviceFamily.Switch, "Switch", DeviceFamily.Xbox),
            Family(DeviceFamily.SteamDeck, "SteamDeck", DeviceFamily.Xbox),
            new FamilyGlyphs(DeviceFamily.GenericGamepad, null, DeviceFamily.Xbox, new List<ControlGlyph>()),
            Family(DeviceFamily.Touch, "Gestures", null)
        };

        private static FamilyGlyphs Family(DeviceFamily family, string setName, DeviceFamily? fallback) =>
            new FamilyGlyphs(
                family,
                AssetDatabase.LoadAssetAtPath<IconSet>($"{BasePromptsRoot}/{setName}/{setName}.asset"),
                fallback,
                DefaultControlGlyphs.For(family));

        private static InputActionAsset FindActions()
        {
            if (UnityEngine.InputSystem.InputSystem.actions != null)
                return UnityEngine.InputSystem.InputSystem.actions;

            string guid = AssetDatabase.FindAssets("t:InputActionAsset", new[] { "Assets" }).FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guid));
        }
#endif

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

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
