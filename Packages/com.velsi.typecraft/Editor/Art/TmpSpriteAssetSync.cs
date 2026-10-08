using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using Velsi.Typecraft.Library;

namespace Velsi.Typecraft.Editor.Art
{
    public static class TmpSpriteAssetSync
    {
        private const string SpriteAssetVersion = "1.1.0";

        public static TMP_SpriteAsset CreateSpriteAsset(string assetPath)
        {
            var spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            AssetDatabase.CreateAsset(spriteAsset, assetPath);
            StampVersion(spriteAsset);
            return spriteAsset;
        }

        public static TMP_SpriteAsset CreateTransient(Texture2D texture, string glyphName)
        {
            var spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            spriteAsset.name = "TypecraftTransient";
            spriteAsset.hideFlags = HideFlags.HideAndDontSave;
            StampVersion(spriteAsset);

            ShaderUtilities.GetShaderPropertyIDs();
            var material = new Material(Shader.Find("TextMeshPro/Sprite")) { hideFlags = HideFlags.HideAndDontSave };
            material.SetTexture(ShaderUtilities.ID_MainTex, texture);
            spriteAsset.spriteSheet = texture;
            spriteAsset.material = material;

            var rect = new Rect(0f, 0f, texture.width, texture.height);
            var glyph = new TMP_SpriteGlyph(0, MetricsOf(rect, Vector2.zero), new GlyphRect(rect), 1f, 0);
            spriteAsset.spriteGlyphTable.Add(glyph);
            spriteAsset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xFFFE, glyph) { name = glyphName });
            spriteAsset.UpdateLookupTables();
            return spriteAsset;
        }

        public static void DestroyTransient(TMP_SpriteAsset spriteAsset)
        {
            if (spriteAsset == null)
                return;

            if (spriteAsset.material != null)
                Object.DestroyImmediate(spriteAsset.material);
            Object.DestroyImmediate(spriteAsset);
        }

        public static void SetPlacement(TMP_SpriteAsset target, string glyphName, Vector2 offset, float scale)
        {
            TMP_SpriteGlyph glyph = GlyphOf(target, glyphName);
            if (glyph == null)
                return;

            glyph.metrics = MetricsOf(glyph.glyphRect, offset);
            glyph.scale = scale;
        }

        public static void ApplyPlacement(IconSet set)
        {
            TMP_SpriteAsset target = set.SpriteAsset;
            if (target == null)
                return;

            float setOffset = set.BuildSettings.VerticalOffset;
            foreach (IconEntry entry in set.Entries)
            {
                if (entry != null && !entry.IsPlaceholder)
                    SetPlacement(target, entry.GlyphName, entry.Offset + new Vector2(0f, setOffset), entry.Scale);
            }

            EditorUtility.SetDirty(target);
            TMPro_EventManager.ON_SPRITE_ASSET_PROPERTY_CHANGED(true, target);
        }

        private static TMP_SpriteGlyph GlyphOf(TMP_SpriteAsset target, string glyphName)
        {
            TMP_SpriteCharacter character = target.spriteCharacterTable.FirstOrDefault(c => c.name == glyphName);
            return character == null ? null : target.spriteGlyphTable.FirstOrDefault(g => g.index == character.glyphIndex);
        }

        private static void StampVersion(TMP_SpriteAsset spriteAsset)
        {
            var serialized = new SerializedObject(spriteAsset);
            SerializedProperty version = serialized.FindProperty("m_Version");
            if (version != null)
                version.stringValue = SpriteAssetVersion;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void AssignSpriteSheet(TMP_SpriteAsset target, Texture2D atlasTexture)
        {
            target.spriteSheet = atlasTexture;
            ShaderUtilities.GetShaderPropertyIDs();

            if (target.material == null)
            {
                var material = new Material(Shader.Find("TextMeshPro/Sprite")) { name = target.name + " Material" };
                material.SetTexture(ShaderUtilities.ID_MainTex, atlasTexture);
                target.material = material;
                AssetDatabase.AddObjectToAsset(material, target);
            }
            else
            {
                target.material.SetTexture(ShaderUtilities.ID_MainTex, atlasTexture);
            }

            EditorUtility.SetDirty(target);
        }

        public static void Sync(TMP_SpriteAsset target, IReadOnlyDictionary<string, Sprite> sprites)
        {
            List<TMP_SpriteCharacter> characters = target.spriteCharacterTable;
            List<TMP_SpriteGlyph> glyphs = target.spriteGlyphTable;

            foreach (TMP_SpriteCharacter stale in characters.Where(c => !sprites.ContainsKey(c.name)).ToList())
            {
                glyphs.RemoveAll(g => g.index == stale.glyphIndex);
                characters.Remove(stale);
            }

            uint nextIndex = glyphs.Count > 0 ? glyphs.Max(g => g.index) + 1 : 0;

            foreach (KeyValuePair<string, Sprite> pair in sprites.OrderBy(p => p.Key))
            {
                Sprite sprite = pair.Value;
                GlyphMetrics metrics = MetricsOf(sprite.rect, Vector2.zero);
                TMP_SpriteCharacter character = characters.FirstOrDefault(c => c.name == pair.Key);
                TMP_SpriteGlyph glyph = character == null ? null : glyphs.FirstOrDefault(g => g.index == character.glyphIndex);

                if (glyph != null)
                {
                    glyph.sprite = sprite;
                    glyph.glyphRect = new GlyphRect(sprite.rect);
                    glyph.metrics = metrics;
                    continue;
                }

                glyph = new TMP_SpriteGlyph(nextIndex++, metrics, new GlyphRect(sprite.rect), 1f, 0, sprite);
                glyphs.Add(glyph);

                if (character != null)
                    characters.Remove(character);

                characters.Add(new TMP_SpriteCharacter(0xFFFE, glyph) { name = pair.Key });
            }

            target.SortGlyphTable();
            target.UpdateLookupTables();
            EditorUtility.SetDirty(target);
            TMPro_EventManager.ON_SPRITE_ASSET_PROPERTY_CHANGED(true, target);
        }

        private static GlyphMetrics MetricsOf(GlyphRect rect, Vector2 offset) =>
            MetricsOf(new Rect(rect.x, rect.y, rect.width, rect.height), offset);

        private static GlyphMetrics MetricsOf(Rect rect, Vector2 offset) =>
            new GlyphMetrics(
                rect.width,
                rect.height,
                offset.x * rect.height,
                rect.height * (1f + offset.y),
                rect.width);
    }
}
