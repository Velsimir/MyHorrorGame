using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Velsi.Typecraft.Fonts;
using Velsi.Typecraft.Styles;

namespace Velsi.Typecraft
{
    internal static class TmpAssetRegistry
    {
        private static readonly Dictionary<int, TMP_FontAsset> Fonts = new Dictionary<int, TMP_FontAsset>();
        private static readonly Dictionary<int, TMP_SpriteAsset> SpriteAssets = new Dictionary<int, TMP_SpriteAsset>();
        private static bool _hooked;

        public static void Rebuild(TypecraftSettings settings)
        {
            Hook();
            Fonts.Clear();

            if (settings == null)
                return;

            RegisterFonts(settings.Fonts);
            RegisterGradients(settings.Styles);
        }

        public static void Track(TMP_SpriteAsset spriteAsset)
        {
            if (spriteAsset == null)
                return;

            Hook();
            SpriteAssets[TMP_TextUtilities.GetHashCode(spriteAsset.name)] = spriteAsset;
        }

        public static void Reset()
        {
            Fonts.Clear();
            SpriteAssets.Clear();
        }

        private static void Hook()
        {
            if (_hooked)
                return;

            TMP_Text.OnFontAssetRequest += ResolveFont;
            TMP_Text.OnSpriteAssetRequest += ResolveSpriteAsset;
            _hooked = true;
        }

        private static TMP_FontAsset ResolveFont(int hashCode, string name) =>
            Fonts.TryGetValue(hashCode, out TMP_FontAsset font) ? font : null;

        private static TMP_SpriteAsset ResolveSpriteAsset(int hashCode, string name) =>
            SpriteAssets.TryGetValue(hashCode, out TMP_SpriteAsset asset) ? asset : null;

        private static void RegisterFonts(FontProfile profile)
        {
            if (profile == null)
                return;

            foreach (FontRole role in profile.Roles)
            {
                if (role == null)
                    continue;

                RegisterVariant(role.Default);
                foreach (LocaleFontOverride localeOverride in role.LocaleOverrides)
                    RegisterVariant(localeOverride?.Variant);
            }
        }

        private static void RegisterVariant(FontVariant variant)
        {
            if (variant?.Font == null)
                return;

            Fonts[TMP_TextUtilities.GetHashCode(variant.Font.name)] = variant.Font;

            foreach (FontMaterialPreset preset in variant.Materials)
            {
                if (preset?.Material == null)
                    continue;

                int hash = TMP_TextUtilities.GetHashCode(preset.Material.name);
                if (!MaterialReferenceManager.TryGetMaterial(hash, out Material registered))
                    MaterialReferenceManager.AddFontMaterial(hash, preset.Material);
#if UNITY_EDITOR
                else if (registered != preset.Material)
                    ReplaceMaterial(hash, preset.Material);
#endif
            }
        }

#if UNITY_EDITOR
        // MaterialReferenceManager умеет только добавлять: пресет, пересозданный под именем удалённого материала,
        // иначе так и резолвился бы в уничтоженный объект до перезапуска редактора.
        private static void ReplaceMaterial(int hash, Material material)
        {
            var field = typeof(MaterialReferenceManager).GetField("m_FontMaterialReferenceLookup",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field?.GetValue(MaterialReferenceManager.instance) is Dictionary<int, Material> lookup)
                lookup[hash] = material;
        }
#endif

        private static void RegisterGradients(StyleLibrary styles)
        {
            if (styles == null)
                return;

            foreach (TextStyle style in styles.Styles)
            {
                if (style?.Gradient == null)
                    continue;

                int hash = TMP_TextUtilities.GetHashCode(style.Gradient.name);
                if (!MaterialReferenceManager.TryGetColorGradientPreset(hash, out TMP_ColorGradient _))
                    MaterialReferenceManager.AddColorGradientPreset(hash, style.Gradient);
            }
        }
    }
}
