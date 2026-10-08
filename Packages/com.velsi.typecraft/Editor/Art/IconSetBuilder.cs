using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using Velsi.Typecraft.Library;

namespace Velsi.Typecraft.Editor.Art
{
    public static class IconSetBuilder
    {
        public static bool Build(IconSet set)
        {
            IconSetBuildSettings settings = set.BuildSettings;
            string sourceFolder = Path.GetFullPath(settings.SourceFolder ?? string.Empty);

            if (string.IsNullOrEmpty(settings.SourceFolder) || !Directory.Exists(sourceFolder))
            {
                TypecraftLog.Error($"Source folder not found for {set.name}: {settings.SourceFolder}");
                return false;
            }

            SearchOption option = settings.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            List<string> files = Directory.GetFiles(sourceFolder, "*.png", option).OrderBy(f => f).ToList();
            List<string> uniqueFiles = DropDuplicateNames(files, set.name);
            if (!BasePromptSets.IsBaseSet(set))
                IconImageCheck.Report(uniqueFiles, set.name);

            string setPath = AssetDatabase.GetAssetPath(set);
            string folder = Path.GetDirectoryName(setPath).Replace('\\', '/');
            string atlasPath = $"{folder}/{set.name}_Atlas.png";
            string spriteAssetPath = $"{folder}/{set.name}_Sprites.asset";

            Dictionary<string, Sprite> sprites = SpriteAtlasBuilder.Build(
                uniqueFiles, atlasPath, settings.Padding, settings.MaxAtlasSize, settings.FilterMode);

            if (sprites == null)
                return false;

            TMP_SpriteAsset spriteAsset = set.SpriteAsset;
            if (spriteAsset == null)
                spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(spriteAssetPath);
            if (spriteAsset == null)
                spriteAsset = TmpSpriteAssetSync.CreateSpriteAsset(spriteAssetPath);

            TmpSpriteAssetSync.AssignSpriteSheet(spriteAsset, AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath));
            TmpSpriteAssetSync.Sync(spriteAsset, sprites);

            string setId = string.IsNullOrEmpty(set.SetId) ? IconKeyNaming.ToKey(set.name) : set.SetId;
            set.Configure(setId, spriteAsset);
            SyncEntries(set, sprites.Keys.ToList(), settings.TintableByDefault);
            TmpSpriteAssetSync.ApplyPlacement(set);

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();

            int placeholders = set.Entries.Count(e => e.IsPlaceholder);
            TypecraftLog.Info($"{set.name}: {sprites.Count} icons, {placeholders} placeholders");
            return true;
        }

        private static void SyncEntries(IconSet set, IReadOnlyCollection<string> glyphNames, bool tintable)
        {
            List<IconEntry> entries = set.MutableEntries;
            Dictionary<string, string> keys = IconKeyNaming.KeysFor(glyphNames);
            var boundGlyphs = new HashSet<string>(glyphNames);

            foreach (IconEntry entry in entries.Where(e => !e.IsPlaceholder && !boundGlyphs.Contains(e.GlyphName)))
                entry.Unbind();

            foreach (KeyValuePair<string, string> pair in keys.OrderBy(p => p.Value))
            {
                IconEntry existing = entries.FirstOrDefault(e => e.GlyphName == pair.Key)
                    ?? entries.FirstOrDefault(e => e.Key == pair.Value);

                if (existing != null)
                    existing.Bind(pair.Key);
                else
                    entries.Add(new IconEntry(pair.Value, pair.Key, tintable));
            }

            foreach (IconEntry entry in entries)
                entry.NormalizeScale();
        }

        private static List<string> DropDuplicateNames(List<string> files, string setName)
        {
            var seen = new HashSet<string>();
            var unique = new List<string>(files.Count);

            foreach (string file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (seen.Add(name))
                    unique.Add(file);
                else
                    TypecraftLog.Warning($"{setName}: duplicate icon name '{name}' skipped ({file})");
            }

            return unique;
        }
    }
}
