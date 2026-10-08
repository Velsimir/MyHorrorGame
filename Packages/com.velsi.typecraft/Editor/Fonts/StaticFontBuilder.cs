using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Velsi.Typecraft.Editor.Fonts
{
    public static class StaticFontBuilder
    {
        private const int SamplingPointSize = 48;
        private const int Padding = 5;
        private const int AtlasSize = 2048;

        [Serializable]
        private sealed class Recipe
        {
            public string Source;
            public string Locale;
            public string Extra;
        }

        public readonly struct Result
        {
            public Result(TMP_FontAsset font, int characters, string notInSource)
            {
                Font = font;
                Characters = characters;
                NotInSource = notInSource;
            }

            public TMP_FontAsset Font { get; }
            public int Characters { get; }
            public string NotInSource { get; }
        }

        public static string DefaultPath(Font source, string locale)
        {
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(source))?.Replace('\\', '/') ?? "Assets";
            return $"{folder}/{source.name} {locale} SDF.asset";
        }

        public static Result Create(Font source, string locale, TextAsset extra, string assetPath)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (font == null)
            {
                font = TMP_FontAsset.CreateFontAsset(source, SamplingPointSize, Padding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
                font.name = Path.GetFileNameWithoutExtension(assetPath);
                AssetDatabase.CreateAsset(font, assetPath);
                font.material.name = $"{font.name} Material";
                AssetDatabase.AddObjectToAsset(font.material, font);
                AdoptTextures(font);
                AssetDatabase.SaveAssets();
            }

            WriteRecipe(font, new Recipe { Source = Guid(source), Locale = locale, Extra = Guid(extra) });
            return Fill(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath), locale, extra);
        }

        public static bool IsBuilt(TMP_FontAsset font) => ReadRecipe(font) != null;

        public static string LocaleOf(TMP_FontAsset font) => ReadRecipe(font)?.Locale;

        public static Result Rebuild(TMP_FontAsset font)
        {
            Recipe recipe = ReadRecipe(font);
            if (recipe == null)
                return new Result(font, 0, string.Empty);

            var extra = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(recipe.Extra ?? string.Empty));
            return Fill(font, recipe.Locale, extra);
        }

        private static Result Fill(TMP_FontAsset font, string locale, TextAsset extra)
        {
            string characters = FontCharset.Collect(locale, extra != null ? extra.text : null);

            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            SetAtlasSize(font, AtlasSizeFor(characters.Length));
            font.ClearFontAssetData(true);
            font.TryAddCharacters(characters, out string notInSource);
            AdoptTextures(font);
            font.atlasPopulationMode = AtlasPopulationMode.Static;

            EditorUtility.SetDirty(font);
            MaterialPresets.SyncAtlases(Typecraft.Settings != null ? Typecraft.Settings.Fonts : null);
            AssetDatabase.SaveAssets();
            Typecraft.Refresh();
            return new Result(font, font.characterTable.Count, notInSource ?? string.Empty);
        }

        private static int AtlasSizeFor(int characters)
        {
            int cell = SamplingPointSize + Padding * 2;
            int size = 256;
            while (size < AtlasSize && (size / cell) * (size / cell) < characters * 1.2f)
                size *= 2;
            return size;
        }

        private static void SetAtlasSize(TMP_FontAsset font, int size)
        {
            var serialized = new SerializedObject(font);
            serialized.FindProperty("m_AtlasWidth").intValue = size;
            serialized.FindProperty("m_AtlasHeight").intValue = size;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AdoptTextures(TMP_FontAsset font)
        {
            for (int i = 0; i < font.atlasTextures.Length; i++)
            {
                Texture2D texture = font.atlasTextures[i];
                if (texture == null || AssetDatabase.Contains(texture))
                    continue;

                texture.name = i == 0 ? $"{font.name} Atlas" : $"{font.name} Atlas {i}";
                AssetDatabase.AddObjectToAsset(texture, font);
            }
        }

        private static string Guid(UnityEngine.Object asset) =>
            asset != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)) : string.Empty;

        private static Recipe ReadRecipe(TMP_FontAsset font)
        {
            if (font == null)
                return null;

            AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(font));
            if (importer == null || string.IsNullOrEmpty(importer.userData))
                return null;

            try
            {
                var recipe = JsonUtility.FromJson<Recipe>(importer.userData);
                return string.IsNullOrEmpty(recipe?.Locale) ? null : recipe;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static void WriteRecipe(TMP_FontAsset font, Recipe recipe)
        {
            AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(font));
            importer.userData = JsonUtility.ToJson(recipe);
            importer.SaveAndReimport();
        }
    }
}
