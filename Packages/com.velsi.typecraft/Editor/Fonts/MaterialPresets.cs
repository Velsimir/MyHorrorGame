using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using Velsi.Typecraft.Editor.Library;
using Velsi.Typecraft.Fonts;

namespace Velsi.Typecraft.Editor.Fonts
{
    [Serializable]
    public sealed class SdfMaterialSettings
    {
        public const string FullShader = "TextMeshPro/Distance Field";

        public float FaceDilate;
        public float Softness;
        public Color OutlineColor = Color.black;
        public float OutlineWidth;
        public bool Underlay;
        public Color UnderlayColor = new Color(0f, 0f, 0f, 0.5f);
        public float UnderlayOffsetX = 0.5f;
        public float UnderlayOffsetY = -0.5f;
        public float UnderlayDilate;
        public float UnderlaySoftness = 0.3f;
        public bool Glow;
        public Color GlowColor = new Color(1f, 0.55f, 0.2f, 0.5f);
        public float GlowOffset;
        public float GlowInner = 0.05f;
        public float GlowOuter = 0.25f;
        public float GlowPower = 0.75f;

        public static SdfMaterialSettings From(Material material)
        {
            var settings = new SdfMaterialSettings();
            if (material == null)
                return settings;

            settings.FaceDilate = Get(material, "_FaceDilate", settings.FaceDilate);
            settings.Softness = Get(material, "_OutlineSoftness", settings.Softness);
            settings.OutlineColor = Get(material, "_OutlineColor", settings.OutlineColor);
            settings.OutlineWidth = Get(material, "_OutlineWidth", settings.OutlineWidth);
            settings.Underlay = material.IsKeywordEnabled(ShaderUtilities.Keyword_Underlay);
            if (settings.Underlay)
            {
                settings.UnderlayColor = Get(material, "_UnderlayColor", settings.UnderlayColor);
                settings.UnderlayOffsetX = Get(material, "_UnderlayOffsetX", settings.UnderlayOffsetX);
                settings.UnderlayOffsetY = Get(material, "_UnderlayOffsetY", settings.UnderlayOffsetY);
                settings.UnderlayDilate = Get(material, "_UnderlayDilate", settings.UnderlayDilate);
                settings.UnderlaySoftness = Get(material, "_UnderlaySoftness", settings.UnderlaySoftness);
            }

            settings.Glow = material.IsKeywordEnabled(ShaderUtilities.Keyword_Glow);
            if (settings.Glow)
            {
                settings.GlowColor = Get(material, "_GlowColor", settings.GlowColor);
                settings.GlowOffset = Get(material, "_GlowOffset", settings.GlowOffset);
                settings.GlowInner = Get(material, "_GlowInner", settings.GlowInner);
                settings.GlowOuter = Get(material, "_GlowOuter", settings.GlowOuter);
                settings.GlowPower = Get(material, "_GlowPower", settings.GlowPower);
            }

            return settings;
        }

        public static bool SupportsGlow(Material material) => material != null && material.HasProperty("_GlowColor");

        public void ApplyTo(Material material)
        {
            if (Glow && !SupportsGlow(material))
            {
                Shader full = Shader.Find(FullShader);
                if (full != null)
                    material.shader = full;
            }

            Set(material, "_FaceDilate", FaceDilate);
            Set(material, "_OutlineSoftness", Softness);
            Set(material, "_OutlineColor", OutlineColor);
            Set(material, "_OutlineWidth", OutlineWidth);
            Set(material, "_UnderlayColor", UnderlayColor);
            Set(material, "_UnderlayOffsetX", UnderlayOffsetX);
            Set(material, "_UnderlayOffsetY", UnderlayOffsetY);
            Set(material, "_UnderlayDilate", UnderlayDilate);
            Set(material, "_UnderlaySoftness", UnderlaySoftness);
            Set(material, "_GlowColor", GlowColor);
            Set(material, "_GlowOffset", GlowOffset);
            Set(material, "_GlowInner", GlowInner);
            Set(material, "_GlowOuter", GlowOuter);
            Set(material, "_GlowPower", GlowPower);

            Keyword(material, ShaderUtilities.Keyword_Outline, OutlineWidth > 0f);
            Keyword(material, ShaderUtilities.Keyword_Underlay, Underlay);
            if (Underlay)
                material.DisableKeyword("UNDERLAY_INNER");
            Keyword(material, ShaderUtilities.Keyword_Glow, Glow && SupportsGlow(material));

            ShaderUtilities.GetShaderPropertyIDs();
            ShaderUtilities.UpdateShaderRatios(material);
        }

        private static float Get(Material material, string property, float fallback) =>
            material.HasProperty(property) ? material.GetFloat(property) : fallback;

        private static Color Get(Material material, string property, Color fallback) =>
            material.HasProperty(property) ? material.GetColor(property) : fallback;

        private static void Set(Material material, string property, float value)
        {
            if (material.HasProperty(property))
                material.SetFloat(property, value);
        }

        private static void Set(Material material, string property, Color value)
        {
            if (material.HasProperty(property))
                material.SetColor(property, value);
        }

        private static void Keyword(Material material, string keyword, bool on)
        {
            if (on)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }
    }

    public static class MaterialPresets
    {
        public const string FolderName = "Materials";

        public static string Folder(FontProfile fonts) =>
            $"{Path.GetDirectoryName(AssetDatabase.GetAssetPath(fonts))?.Replace('\\', '/')}/{FolderName}";

        public static IReadOnlyList<(string Locale, FontVariant Variant)> Variants(FontRole role)
        {
            var variants = new List<(string, FontVariant)>();
            if (role == null)
                return variants;

            if (role.Default?.Font != null)
                variants.Add((null, role.Default));

            foreach (LocaleFontOverride localeOverride in role.LocaleOverrides)
            {
                if (localeOverride?.Variant?.Font != null)
                    variants.Add((localeOverride.Locale, localeOverride.Variant));
            }

            return variants;
        }

        public static string MaterialName(TMP_FontAsset font, string roleId, string presetId) => $"{font.name} {roleId} {presetId}";

        public static string Validate(FontProfile fonts, string roleId, string presetId)
        {
            if (fonts == null)
                return "Нет профиля шрифтов";
            if (!fonts.TryGetRole(roleId, out FontRole role))
                return "Выбери роль шрифта";
            if (Variants(role).Count == 0)
                return $"У роли «{roleId}» не назначен шрифт";
            if (string.IsNullOrEmpty(presetId) || !StylePanel.ValidId.IsMatch(presetId))
                return "Имя пресета: латинская буква первой, дальше буквы, цифры, «_», «.», «-»";
            if (Variants(role).Any(v => v.Variant.Materials.Any(p => p != null && p.Id == presetId)))
                return $"У роли «{roleId}» уже есть пресет «{presetId}»";

            string folder = Folder(fonts);
            foreach ((string _, FontVariant variant) in Variants(role))
            {
                string path = $"{folder}/{MaterialName(variant.Font, roleId, presetId)}.mat";
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                    return $"Файл {path} уже существует";
            }

            return null;
        }

        public static Material CreatePreviewMaterial(TMP_FontAsset font, SdfMaterialSettings settings)
        {
            if (font == null || font.material == null)
                return null;

            var material = new Material(font.material) { hideFlags = HideFlags.HideAndDontSave, name = "Typecraft preview" };
            settings.ApplyTo(material);
            return material;
        }

        public static IReadOnlyList<Material> Create(FontProfile fonts, string roleId, string presetId, SdfMaterialSettings settings)
        {
            string error = Validate(fonts, roleId, presetId);
            if (error != null)
                throw new ArgumentException(error);

            string folder = Folder(fonts);
            EnsureFolder(folder);

            Undo.SetCurrentGroupName("Typecraft: пресет материала");
            int group = Undo.GetCurrentGroup();

            var created = new List<Material>();
            var serialized = new SerializedObject(fonts);
            foreach (SerializedProperty variant in VariantProperties(serialized, roleId))
            {
                var font = variant.FindPropertyRelative("_font").objectReferenceValue as TMP_FontAsset;
                if (font == null || font.material == null)
                    continue;

                var material = new Material(font.material) { name = MaterialName(font, roleId, presetId) };
                settings.ApplyTo(material);
                AssetDatabase.CreateAsset(material, $"{folder}/{material.name}.mat");
                Undo.RegisterCreatedObjectUndo(material, "Typecraft: пресет материала");
                created.Add(material);

                SerializedProperty materials = variant.FindPropertyRelative("_materials");
                materials.arraySize++;
                SerializedProperty added = materials.GetArrayElementAtIndex(materials.arraySize - 1);
                added.FindPropertyRelative("_id").stringValue = presetId;
                added.FindPropertyRelative("_material").objectReferenceValue = material;
            }

            serialized.ApplyModifiedProperties();
            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();
            Typecraft.Refresh();
            return created;
        }

        public static IReadOnlyList<string> PresetIds(FontProfile fonts, string roleId) =>
            fonts != null && fonts.TryGetRole(roleId, out FontRole role)
                ? Variants(role).SelectMany(v => v.Variant.Materials).Where(p => p != null && !string.IsNullOrEmpty(p.Id)).Select(p => p.Id).Distinct().ToList()
                : new List<string>();

        public static bool IsOwned(FontProfile fonts, Material material)
        {
            string path = material != null ? AssetDatabase.GetAssetPath(material) : null;
            return !string.IsNullOrEmpty(path) && path.StartsWith(Folder(fonts) + "/", StringComparison.Ordinal);
        }

        public static bool Remove(FontProfile fonts, string roleId, string presetId)
        {
            if (fonts == null)
                return false;

            var removed = new List<Material>();
            var serialized = new SerializedObject(fonts);
            foreach (SerializedProperty variant in VariantProperties(serialized, roleId))
            {
                SerializedProperty materials = variant.FindPropertyRelative("_materials");
                for (int i = materials.arraySize - 1; i >= 0; i--)
                {
                    SerializedProperty preset = materials.GetArrayElementAtIndex(i);
                    if (preset.FindPropertyRelative("_id").stringValue != presetId)
                        continue;

                    if (preset.FindPropertyRelative("_material").objectReferenceValue is Material material)
                        removed.Add(material);
                    materials.DeleteArrayElementAtIndex(i);
                }
            }

            if (removed.Count == 0 && !serialized.hasModifiedProperties)
                return false;

            serialized.ApplyModifiedProperties();

            var stillUsed = new HashSet<Material>(fonts.Roles.Where(r => r != null).SelectMany(Variants).SelectMany(v => v.Variant.Materials)
                .Where(p => p?.Material != null).Select(p => p.Material));
            foreach (Material material in removed.Distinct())
            {
                if (!stillUsed.Contains(material) && IsOwned(fonts, material))
                    AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(material));
            }

            AssetDatabase.SaveAssets();
            Typecraft.Refresh();
            return true;
        }

        public static int SyncAtlases(FontProfile fonts)
        {
            if (fonts == null)
                return 0;

            ShaderUtilities.GetShaderPropertyIDs();
            int changed = 0;
            foreach (FontRole role in fonts.Roles)
            {
                foreach ((string _, FontVariant variant) in Variants(role))
                {
                    Material source = variant.Font.material;
                    if (source == null)
                        continue;

                    foreach (FontMaterialPreset preset in variant.Materials)
                    {
                        Material material = preset?.Material;
                        if (material == null || material == source)
                            continue;

                        bool dirty = false;
                        if (material.HasProperty(ShaderUtilities.ID_MainTex) && material.GetTexture(ShaderUtilities.ID_MainTex) != source.GetTexture(ShaderUtilities.ID_MainTex))
                        {
                            material.SetTexture(ShaderUtilities.ID_MainTex, source.GetTexture(ShaderUtilities.ID_MainTex));
                            dirty = true;
                        }

                        foreach (string property in new[] { "_TextureWidth", "_TextureHeight", "_GradientScale" })
                        {
                            if (!material.HasProperty(property) || !source.HasProperty(property) || Mathf.Approximately(material.GetFloat(property), source.GetFloat(property)))
                                continue;

                            material.SetFloat(property, source.GetFloat(property));
                            dirty = true;
                        }

                        if (!dirty)
                            continue;

                        ShaderUtilities.UpdateShaderRatios(material);
                        EditorUtility.SetDirty(material);
                        changed++;
                    }
                }
            }

            return changed;
        }

        private static IEnumerable<SerializedProperty> VariantProperties(SerializedObject serialized, string roleId)
        {
            SerializedProperty roles = serialized.FindProperty("_roles");
            for (int r = 0; r < roles.arraySize; r++)
            {
                SerializedProperty role = roles.GetArrayElementAtIndex(r);
                if (role.FindPropertyRelative("_id").stringValue != roleId)
                    continue;

                yield return role.FindPropertyRelative("_default");
                SerializedProperty overrides = role.FindPropertyRelative("_localeOverrides");
                for (int o = 0; o < overrides.arraySize; o++)
                    yield return overrides.GetArrayElementAtIndex(o).FindPropertyRelative("_variant");
            }
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
