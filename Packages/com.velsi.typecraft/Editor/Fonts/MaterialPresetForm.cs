using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Window.Dialogs;
using Velsi.Typecraft.Editor.Window.Preview;
using Velsi.Typecraft.Fonts;

namespace Velsi.Typecraft.Editor.Fonts
{
    public sealed class MaterialPresetForm : TypecraftForm
    {
        private const string DefaultVariant = "основной";
        private const string LatinSample = "Подвал заперт — The door is locked";

        private readonly FontProfile _fonts;
        private readonly SdfMaterialSettings _settings;
        private readonly TextPreview _preview = new TextPreview();
        private readonly DropdownField _role;
        private readonly DropdownField _variant;
        private readonly TextField _id;
        private readonly VisualElement _glow;
        private readonly Label _glowNote;
        private Material _previewMaterial;
        private TMP_FontAsset _previewFont;

        public MaterialPresetForm(FontProfile fonts, string roleId, string locale) : base("Новый пресет материала")
        {
            _fonts = fonts;

            List<string> roles = fonts != null
                ? fonts.Roles.Where(role => role != null && !string.IsNullOrEmpty(role.Id) && MaterialPresets.Variants(role).Count > 0).Select(role => role.Id).ToList()
                : new List<string>();
            string initialRole = roles.Contains(roleId) ? roleId : roles.FirstOrDefault();

            _role = new DropdownField("Роль шрифта", roles, initialRole ?? string.Empty);
            _role.RegisterValueChangedCallback(_ => OnRoleChanged(null));
            Fields.Add(_role);

            _id = new TextField("Имя пресета") { value = "Outline", tooltip = "Id пресета в профиле шрифтов: его выбирают в стилях" };
            _id.RegisterValueChangedCallback(_ => Validate());
            Fields.Add(_id);

            _variant = new DropdownField("Превью для") { tooltip = "Пресет создаётся для каждого шрифта роли: у каждого языка свой атлас и своя копия материала" };
            _variant.RegisterValueChangedCallback(_ => RebuildPreviewMaterial());
            Fields.Add(_variant);

            _settings = SdfMaterialSettings.From(DefaultFont(initialRole)?.material);

            Fields.Add(Section("Лицо"));
            Fields.Add(MakeSlider("Face Dilate", -1f, 1f, _settings.FaceDilate, v => _settings.FaceDilate = v));
            Fields.Add(MakeSlider("Softness", 0f, 1f, _settings.Softness, v => _settings.Softness = v));

            Fields.Add(Section("Обводка"));
            Fields.Add(MakeColor("Цвет", _settings.OutlineColor, false, c => _settings.OutlineColor = c));
            Fields.Add(MakeSlider("Толщина", 0f, 1f, _settings.OutlineWidth, v => _settings.OutlineWidth = v));

            Fields.Add(Section("Подложка (Underlay)"));
            var underlay = new VisualElement();
            Fields.Add(MakeToggle("Включить", _settings.Underlay, on =>
            {
                _settings.Underlay = on;
                underlay.SetEnabled(on);
            }));
            underlay.Add(MakeColor("Цвет", _settings.UnderlayColor, false, c => _settings.UnderlayColor = c));
            underlay.Add(MakeSlider("Смещение X", -1f, 1f, _settings.UnderlayOffsetX, v => _settings.UnderlayOffsetX = v));
            underlay.Add(MakeSlider("Смещение Y", -1f, 1f, _settings.UnderlayOffsetY, v => _settings.UnderlayOffsetY = v));
            underlay.Add(MakeSlider("Dilate", -1f, 1f, _settings.UnderlayDilate, v => _settings.UnderlayDilate = v));
            underlay.Add(MakeSlider("Softness", 0f, 1f, _settings.UnderlaySoftness, v => _settings.UnderlaySoftness = v));
            underlay.SetEnabled(_settings.Underlay);
            Fields.Add(underlay);

            Fields.Add(Section("Свечение (Glow)"));
            _glow = new VisualElement();
            Fields.Add(MakeToggle("Включить", _settings.Glow, on =>
            {
                _settings.Glow = on;
                _glow.SetEnabled(on);
            }));
            _glowNote = new Label($"Мобильный SDF-шейдер свечение не умеет — пресет получит «{SdfMaterialSettings.FullShader}».");
            _glowNote.AddToClassList("tc-lib__hint");
            Fields.Add(_glowNote);
            _glow.Add(MakeColor("Цвет", _settings.GlowColor, true, c => _settings.GlowColor = c));
            _glow.Add(MakeSlider("Offset", -1f, 1f, _settings.GlowOffset, v => _settings.GlowOffset = v));
            _glow.Add(MakeSlider("Inner", 0f, 1f, _settings.GlowInner, v => _settings.GlowInner = v));
            _glow.Add(MakeSlider("Outer", 0f, 1f, _settings.GlowOuter, v => _settings.GlowOuter = v));
            _glow.Add(MakeSlider("Power", 0f, 1f, _settings.GlowPower, v => _settings.GlowPower = v));
            _glow.SetEnabled(_settings.Glow);
            Fields.Add(_glow);

            _preview.Element.AddToClassList("tc-form__preview");
            Side.Add(_preview.Element);

            OnRoleChanged(locale);
        }

        public event Action<string, string> Saved;

        public SdfMaterialSettings Settings => _settings;
        public Material PreviewMaterial => _previewMaterial;
        public TextPreview Preview => _preview;
        public IReadOnlyList<Material> Created { get; private set; }

        public void SetRole(string roleId)
        {
            _role.SetValueWithoutNotify(roleId);
            OnRoleChanged(null);
        }

        public void SetId(string id)
        {
            _id.SetValueWithoutNotify(id);
            Validate();
        }

        public void SetPreviewVariant(string label)
        {
            _variant.SetValueWithoutNotify(label);
            RebuildPreviewMaterial();
        }
        public IReadOnlyList<string> PreviewVariants => _variant.choices;

        public void ApplySettings()
        {
            if (_previewMaterial != null)
                _settings.ApplyTo(_previewMaterial);
            _glowNote.style.display = _settings.Glow && !SdfMaterialSettings.SupportsGlow(_previewFont != null ? _previewFont.material : null) ? DisplayStyle.Flex : DisplayStyle.None;
            UpdatePreview();
        }

        public override void Tick(float deltaTime) => _preview.Tick(deltaTime);

        public override void Dispose()
        {
            _preview.Dispose();
            DestroyPreviewMaterial();
        }

        protected override bool Save()
        {
            try
            {
                Created = MaterialPresets.Create(_fonts, _role.value, _id.value?.Trim(), _settings);
            }
            catch (ArgumentException error)
            {
                SetError(error.Message);
                return false;
            }

            Saved?.Invoke(_role.value, _id.value?.Trim());
            return true;
        }

        private void OnRoleChanged(string locale)
        {
            var labels = new List<string>();
            string selected = null;
            if (_fonts != null && _fonts.TryGetRole(_role.value, out FontRole role))
            {
                FontVariant resolved = locale != null ? role.Resolve(locale) : null;
                foreach ((string variantLocale, FontVariant variant) in MaterialPresets.Variants(role))
                {
                    string label = VariantLabel(variantLocale, variant);
                    labels.Add(label);
                    if (variant == resolved)
                        selected = label;
                }
            }

            _variant.choices = labels;
            _variant.SetValueWithoutNotify(selected ?? labels.FirstOrDefault() ?? string.Empty);
            RebuildPreviewMaterial();
            Validate();
        }

        private void RebuildPreviewMaterial()
        {
            DestroyPreviewMaterial();
            _previewFont = SelectedFont();
            _previewMaterial = MaterialPresets.CreatePreviewMaterial(_previewFont, _settings);
            ApplySettings();
        }

        private void UpdatePreview()
        {
            _preview.Configure(null, _previewFont, _previewMaterial);
            _preview.SetMarkup(SampleFor(_previewFont), Typecraft.Locale, Typecraft.Device, null);
        }

        private void Validate() => SetError(MaterialPresets.Validate(_fonts, _role.value, _id.value?.Trim()));

        private TMP_FontAsset SelectedFont()
        {
            if (_fonts == null || !_fonts.TryGetRole(_role.value, out FontRole role))
                return null;

            foreach ((string variantLocale, FontVariant variant) in MaterialPresets.Variants(role))
            {
                if (VariantLabel(variantLocale, variant) == _variant.value)
                    return variant.Font;
            }

            return MaterialPresets.Variants(role).FirstOrDefault().Variant?.Font;
        }

        private TMP_FontAsset DefaultFont(string roleId) =>
            _fonts != null && _fonts.TryGetRole(roleId, out FontRole role) ? MaterialPresets.Variants(role).FirstOrDefault().Variant?.Font : null;

        private void DestroyPreviewMaterial()
        {
            if (_previewMaterial != null)
                UnityEngine.Object.DestroyImmediate(_previewMaterial);
            _previewMaterial = null;
        }

        private static string VariantLabel(string locale, FontVariant variant) => $"{locale ?? DefaultVariant} · {variant.Font.name}";

        private static string SampleFor(TMP_FontAsset font)
        {
            if (font == null)
                return LatinSample;

            bool latin = LatinSample.Where(c => !char.IsWhiteSpace(c)).All(c => font.HasCharacter(c));
            if (latin)
                return LatinSample;

            string own = new string(font.characterTable
                .Select(character => character.unicode)
                .Where(code => code < 0x10000 && char.IsLetter((char)code))
                .OrderByDescending(code => code >= 0x2E80)
                .Take(12)
                .Select(code => (char)code)
                .ToArray());
            return string.IsNullOrEmpty(own) ? LatinSample : own;
        }

        private Slider MakeSlider(string label, float min, float max, float value, Action<float> set)
        {
            var slider = new Slider(label, min, max) { value = value, showInputField = true };
            slider.RegisterValueChangedCallback(evt =>
            {
                set(evt.newValue);
                ApplySettings();
            });
            return slider;
        }

        private ColorField MakeColor(string label, Color value, bool hdr, Action<Color> set)
        {
            var field = new ColorField(label) { value = value, hdr = hdr, showAlpha = true };
            field.RegisterValueChangedCallback(evt =>
            {
                set(evt.newValue);
                ApplySettings();
            });
            return field;
        }

        private Toggle MakeToggle(string label, bool value, Action<bool> set)
        {
            var toggle = new Toggle(label) { value = value };
            toggle.RegisterValueChangedCallback(evt =>
            {
                set(evt.newValue);
                ApplySettings();
            });
            return toggle;
        }
    }
}
