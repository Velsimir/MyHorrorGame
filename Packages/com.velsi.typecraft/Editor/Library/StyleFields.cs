using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Window.Effects;
using Velsi.Typecraft.Fonts;

namespace Velsi.Typecraft.Editor.Library
{
    public sealed class StyleFields : VisualElement
    {
        private const string NoRole = "(как у текста)";
        private const string NoPreset = "(основной материал)";

        private readonly SerializedObject _serialized;
        private readonly SerializedProperty _style;
        private readonly DropdownField _role;
        private readonly DropdownField _preset;

        public StyleFields(SerializedObject serialized, SerializedProperty style)
        {
            _serialized = serialized;
            _style = style;

            _role = new DropdownField("Роль шрифта");
            _role.RegisterValueChangedCallback(evt => SetString("_fontRole", evt.newValue == NoRole ? string.Empty : evt.newValue));
            Add(_role);

            _preset = new DropdownField("Пресет материала");
            _preset.RegisterValueChangedCallback(evt => SetString("_materialPreset", evt.newValue == NoPreset ? string.Empty : evt.newValue));
            Add(_preset);

            Add(Field("_useColor", "Свой цвет"));
            Add(Field("_color", "Цвет"));
            Add(Field("_gradient", "Градиент"));
            Add(Field("_bold", "Жирный"));
            Add(Field("_italic", "Курсив"));
            Add(Field("_underline", "Подчёркнутый"));
            Add(Field("_case", "Регистр"));
            Add(Field("_sizePercent", "Размер, % (0 — как у текста)"));
            Add(Field("_characterSpacingEm", "Интервал, em"));

            var effectRow = new VisualElement();
            effectRow.AddToClassList("tc-lib__row");
            PropertyField effect = Field("_effectTag", "Эффект");
            effect.style.flexGrow = 1;
            effectRow.Add(effect);
            effectRow.Add(new Button(ShowEffectMenu) { text = "▾", tooltip = "Выбрать эффект TMPEffects" });
            Add(effectRow);
        }

        public void RefreshChoices()
        {
            FontProfile fonts = Typecraft.Settings != null ? Typecraft.Settings.Fonts : null;

            var roles = new List<string> { NoRole };
            if (fonts != null)
                roles.AddRange(fonts.Roles.Where(role => role != null && !string.IsNullOrEmpty(role.Id)).Select(role => role.Id));
            string current = _style.FindPropertyRelative("_fontRole").stringValue;
            _role.choices = roles;
            _role.SetValueWithoutNotify(string.IsNullOrEmpty(current) ? NoRole : current);

            var presets = new List<string> { NoPreset };
            FontVariant variant = fonts != null && !string.IsNullOrEmpty(current) ? fonts.Resolve(current, Typecraft.Locale) : null;
            if (variant != null)
                presets.AddRange(variant.Materials.Where(preset => preset != null && !string.IsNullOrEmpty(preset.Id)).Select(preset => preset.Id));
            string preset = _style.FindPropertyRelative("_materialPreset").stringValue;
            _preset.choices = presets;
            _preset.SetValueWithoutNotify(string.IsNullOrEmpty(preset) ? NoPreset : preset);
            _preset.SetEnabled(!string.IsNullOrEmpty(current));
        }

        private PropertyField Field(string name, string label) => new PropertyField(_style.FindPropertyRelative(name), label);

        private void SetString(string field, string value)
        {
            _serialized.Update();
            _style.FindPropertyRelative(field).stringValue = value;
            _serialized.ApplyModifiedProperties();
        }

        private void ShowEffectMenu()
        {
            var menu = new GenericMenu();
            string current = _style.FindPropertyRelative("_effectTag").stringValue;
            menu.AddItem(new UnityEngine.GUIContent("(нет)"), string.IsNullOrEmpty(current), () => SetString("_effectTag", string.Empty));
            foreach (EffectEntry entry in EffectCatalog.Entries.Where(entry => entry.Wraps))
            {
                string tag = entry.TagName;
                menu.AddItem(new UnityEngine.GUIContent($"{entry.Kind}/{entry.Name}"), current == tag, () => SetString("_effectTag", tag));
            }
            menu.ShowAsContext();
        }
    }
}
