using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using Velsi.Typecraft.Editor.Art;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Editor.Window.Effects;
using Velsi.Typecraft.Editor.Window.Markup;
using Velsi.Typecraft.Fonts;
using Velsi.Typecraft.Styles;

namespace Velsi.Typecraft.Editor.Library
{
    public static class StyleFromText
    {
        private static readonly Regex MaterialAttribute = new Regex(@"\bmaterial\s*=\s*(?:""([^""]*)""|([^\s>]+))", RegexOptions.IgnoreCase);

        public static IReadOnlyList<string> Prefill(SerializedProperty style, IEnumerable<MarkupModule> modules, FontProfile fonts)
        {
            var consumed = new List<string>();
            bool hasEffect = false;
            string fontRole = null;
            string preset = null;

            foreach (MarkupModule module in modules)
            {
                if (module.IsStyle)
                    continue;

                string slot = module.Slot;
                string value = FirstValue(module.Open);
                bool used = true;

                switch (slot)
                {
                    case "b":
                        style.FindPropertyRelative("_bold").boolValue = true;
                        break;
                    case "i":
                        style.FindPropertyRelative("_italic").boolValue = true;
                        break;
                    case "u":
                        style.FindPropertyRelative("_underline").boolValue = true;
                        break;
                    case "uppercase":
                    case "allcaps":
                        style.FindPropertyRelative("_case").enumValueIndex = (int)TextCase.Upper;
                        break;
                    case "lowercase":
                        style.FindPropertyRelative("_case").enumValueIndex = (int)TextCase.Lower;
                        break;
                    case "smallcaps":
                        style.FindPropertyRelative("_case").enumValueIndex = (int)TextCase.SmallCaps;
                        break;
                    case "color":
                        used = ColorUtility.TryParseHtmlString(value, out Color color);
                        if (used)
                        {
                            style.FindPropertyRelative("_useColor").boolValue = true;
                            style.FindPropertyRelative("_color").colorValue = color;
                        }
                        break;
                    case "cspace":
                        used = value.EndsWith("em") && float.TryParse(value.Substring(0, value.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out float _);
                        if (used)
                            style.FindPropertyRelative("_characterSpacingEm").floatValue = float.Parse(value.Substring(0, value.Length - 2), CultureInfo.InvariantCulture);
                        break;
                    case "size":
                        used = value.EndsWith("%") && float.TryParse(value.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out float percent) && percent > 0f;
                        if (used)
                            style.FindPropertyRelative("_sizePercent").floatValue = float.Parse(value.TrimEnd('%'), CultureInfo.InvariantCulture);
                        break;
                    case "gradient":
                        TMP_ColorGradient gradient = FindGradient(value);
                        used = gradient != null;
                        if (used)
                            style.FindPropertyRelative("_gradient").objectReferenceValue = gradient;
                        break;
                    case "font":
                        used = TryFindFont(fonts, value, MaterialName(module.Open), out fontRole, out preset);
                        break;
                    case "material":
                        used = fontRole == null && TryFindFont(fonts, null, value, out fontRole, out preset);
                        break;
                    default:
                        used = !hasEffect && (slot[0] == '+' || slot[0] == '-' || EffectCatalog.IsEffectTag(slot));
                        if (used)
                        {
                            hasEffect = true;
                            style.FindPropertyRelative("_effectTag").stringValue = module.Open.Substring(1, module.Open.Length - 2);
                        }
                        break;
                }

                if (used)
                    consumed.Add(slot);
            }

            style.FindPropertyRelative("_fontRole").stringValue = fontRole ?? string.Empty;
            style.FindPropertyRelative("_materialPreset").stringValue = preset ?? string.Empty;
            return consumed;
        }

        private static string FirstValue(string open)
        {
            int equals = open.IndexOf('=');
            if (equals < 0)
                return string.Empty;

            string rest = open.Substring(equals + 1, open.Length - equals - 2).TrimStart();
            if (rest.StartsWith("\""))
            {
                int close = rest.IndexOf('"', 1);
                return close > 0 ? rest.Substring(1, close - 1) : rest.Substring(1);
            }

            int end = rest.IndexOf(' ');
            return (end > 0 ? rest.Substring(0, end) : rest).Trim();
        }

        private static string MaterialName(string open)
        {
            Match match = MaterialAttribute.Match(open);
            return match.Success ? (match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value) : null;
        }

        private static bool TryFindFont(FontProfile fonts, string fontName, string materialName, out string role, out string preset)
        {
            role = null;
            preset = null;
            if (fonts == null)
                return false;

            foreach (FontRole candidate in fonts.Roles)
            {
                if (candidate == null)
                    continue;

                var variants = new List<FontVariant> { candidate.Default };
                variants.AddRange(candidate.LocaleOverrides.Select(o => o?.Variant));
                foreach (FontVariant variant in variants)
                {
                    if (variant?.Font == null || (fontName != null && variant.Font.name != fontName))
                        continue;

                    FontMaterialPreset match = materialName == null ? null : variant.Materials.FirstOrDefault(p => p?.Material != null && p.Material.name == materialName);
                    if (materialName != null && match == null)
                        continue;

                    role = candidate.Id;
                    preset = match?.Id;
                    return true;
                }
            }

            return false;
        }

        private static TMP_ColorGradient FindGradient(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(TMP_ColorGradient)} {name}"))
            {
                var gradient = AssetDatabase.LoadAssetAtPath<TMP_ColorGradient>(AssetDatabase.GUIDToAssetPath(guid));
                if (gradient != null && gradient.name == name)
                    return gradient;
            }

            return null;
        }

        public static string Create(StyleLibrary library, FontProfile fonts, TMP_Text text)
        {
            string suggested = IconKeyNaming.ToKey(new string(text.name.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '_').ToArray()));
            if (!StylePanel.ValidId.IsMatch(suggested))
                suggested = "style";

            string id = TextPrompt.Show("Стиль из TMP_Text", $"Id нового стиля по «{text.name}»:", suggested);
            if (id == null)
                return null;

            id = id.Trim();
            if (!StylePanel.ValidId.IsMatch(id) || library.TryGet(id, out _))
            {
                EditorUtility.DisplayDialog("Typecraft", $"Id «{id}» недопустим или уже занят.", "OK");
                return null;
            }

            Undo.SetCurrentGroupName("Typecraft: стиль из TMP_Text");
            int group = Undo.GetCurrentGroup();

            string role = fonts != null ? RoleFor(fonts, text.font) : null;
            string preset = role != null ? PresetFor(fonts, text.font, text.fontSharedMaterial) : null;

            var serialized = new SerializedObject(library);
            SerializedProperty styles = serialized.FindProperty("_styles");
            styles.arraySize++;
            SerializedProperty style = styles.GetArrayElementAtIndex(styles.arraySize - 1);

            FontStyles flags = text.fontStyle;
            style.FindPropertyRelative("_id").stringValue = id;
            style.FindPropertyRelative("_displayName").stringValue = text.name;
            style.FindPropertyRelative("_fontRole").stringValue = role ?? string.Empty;
            style.FindPropertyRelative("_materialPreset").stringValue = preset ?? string.Empty;
            style.FindPropertyRelative("_useColor").boolValue = text.color != Color.white;
            style.FindPropertyRelative("_color").colorValue = text.color;
            style.FindPropertyRelative("_gradient").objectReferenceValue = text.enableVertexGradient ? text.colorGradientPreset : null;
            style.FindPropertyRelative("_bold").boolValue = (flags & FontStyles.Bold) != 0;
            style.FindPropertyRelative("_italic").boolValue = (flags & FontStyles.Italic) != 0;
            style.FindPropertyRelative("_underline").boolValue = (flags & FontStyles.Underline) != 0;
            style.FindPropertyRelative("_case").enumValueIndex = (int)CaseOf(flags);
            style.FindPropertyRelative("_sizePercent").floatValue = 0f;
            style.FindPropertyRelative("_characterSpacingEm").floatValue = text.characterSpacing / 100f;
            style.FindPropertyRelative("_effectTag").stringValue = string.Empty;
            style.FindPropertyRelative("_autoTagWords").arraySize = 0;
            serialized.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(group);
            Typecraft.Refresh();
            return id;
        }

        private static TextCase CaseOf(FontStyles flags)
        {
            if ((flags & FontStyles.UpperCase) != 0)
                return TextCase.Upper;
            if ((flags & FontStyles.LowerCase) != 0)
                return TextCase.Lower;
            if ((flags & FontStyles.SmallCaps) != 0)
                return TextCase.SmallCaps;
            return TextCase.None;
        }

        private static string RoleFor(FontProfile fonts, TMP_FontAsset font)
        {
            if (font == null)
                return null;

            FontRole match = fonts.Roles.FirstOrDefault(role => role?.Default?.Font == font)
                ?? fonts.Roles.FirstOrDefault(role => role != null && role.LocaleOverrides.Any(o => o?.Variant?.Font == font));
            if (match != null)
                return match.Id;

            string id = new string(font.name.Replace(" SDF", string.Empty).Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrEmpty(id) || fonts.TryGetRole(id, out _))
                id = $"Role{fonts.Roles.Count + 1}";

            if (!EditorUtility.DisplayDialog("Typecraft", $"Шрифта «{font.name}» нет в профиле шрифтов. Создать роль «{id}»?", "Создать", "Без роли"))
                return null;

            Undo.RecordObject(fonts, "Typecraft: роль шрифта");
            fonts.Add(new FontRole(id, font));
            EditorUtility.SetDirty(fonts);
            return id;
        }

        private static string PresetFor(FontProfile fonts, TMP_FontAsset font, Material material)
        {
            if (material == null || font == null || material == font.material)
                return null;

            var serialized = new SerializedObject(fonts);
            SerializedProperty roles = serialized.FindProperty("_roles");
            for (int r = 0; r < roles.arraySize; r++)
            {
                SerializedProperty role = roles.GetArrayElementAtIndex(r);
                SerializedProperty variant = role.FindPropertyRelative("_default");
                if (variant.FindPropertyRelative("_font").objectReferenceValue != font)
                {
                    variant = null;
                    SerializedProperty overrides = role.FindPropertyRelative("_localeOverrides");
                    for (int o = 0; o < overrides.arraySize && variant == null; o++)
                    {
                        SerializedProperty candidate = overrides.GetArrayElementAtIndex(o).FindPropertyRelative("_variant");
                        if (candidate.FindPropertyRelative("_font").objectReferenceValue == font)
                            variant = candidate;
                    }
                }

                if (variant == null)
                    continue;

                SerializedProperty materials = variant.FindPropertyRelative("_materials");
                for (int m = 0; m < materials.arraySize; m++)
                {
                    SerializedProperty existing = materials.GetArrayElementAtIndex(m);
                    if (existing.FindPropertyRelative("_material").objectReferenceValue == material)
                        return existing.FindPropertyRelative("_id").stringValue;
                }

                materials.arraySize++;
                SerializedProperty added = materials.GetArrayElementAtIndex(materials.arraySize - 1);
                added.FindPropertyRelative("_id").stringValue = material.name;
                added.FindPropertyRelative("_material").objectReferenceValue = material;
                serialized.ApplyModifiedProperties();
                return material.name;
            }

            return null;
        }
    }
}
