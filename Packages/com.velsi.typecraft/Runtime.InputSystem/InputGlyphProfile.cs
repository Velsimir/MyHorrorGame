using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using Velsi.Typecraft.Input;
using Velsi.Typecraft.Library;
using Velsi.Typecraft.Markup;
using InputSystemApi = UnityEngine.InputSystem.InputSystem;

namespace Velsi.Typecraft.InputSystem
{
    [CreateAssetMenu(fileName = "InputGlyphProfile", menuName = "Typecraft/Input Glyph Profile")]
    public sealed class InputGlyphProfile : ScriptableObject, IInputGlyphResolver
    {
        private const int MaxFallbackDepth = 4;
        private static readonly HashSet<string> PartNames = new HashSet<string>();

        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private List<FamilyGlyphs> _families = new List<FamilyGlyphs>();
        [SerializeField] private List<ActionGlyphOverride> _overrides = new List<ActionGlyphOverride>();

        private readonly List<string> _controls = new List<string>();

        public InputActionAsset Actions => _actions;
        public IReadOnlyList<FamilyGlyphs> Families => _families;

        public IEnumerable<IconSet> IconSets
        {
            get
            {
                foreach (FamilyGlyphs glyphs in _families)
                {
                    if (glyphs?.Icons != null)
                        yield return glyphs.Icons;
                }
            }
        }

        public void Activate() => DeviceTracker.Start();

        public bool TryAppendGlyph(string actionName, DeviceFamily device, StringBuilder output)
        {
            DeviceFamily? family = device;

            for (int depth = 0; family.HasValue && depth < MaxFallbackDepth; depth++)
            {
                if (TryAppendOverride(actionName, family.Value, output))
                    return true;

                FamilyGlyphs glyphs = Find(family.Value);
                if (glyphs != null && TryAppendFromBindings(actionName, glyphs, output))
                    return true;

                family = glyphs?.Fallback;
            }

            return false;
        }

        public void Configure(InputActionAsset actions, List<FamilyGlyphs> families, List<ActionGlyphOverride> overrides)
        {
            _actions = actions;
            _families = families;
            _overrides = overrides;
        }

        private bool TryAppendOverride(string actionName, DeviceFamily family, StringBuilder output)
        {
            foreach (ActionGlyphOverride glyphOverride in _overrides)
            {
                if (glyphOverride != null && glyphOverride.Family == family && glyphOverride.Action == actionName)
                {
                    output.Append(Typecraft.Render(glyphOverride.Markup));
                    return true;
                }
            }

            return false;
        }

        private bool TryAppendFromBindings(string actionName, FamilyGlyphs glyphs, StringBuilder output)
        {
            InputAction action = _actions != null ? _actions.FindAction(actionName) : null;
            if (action == null || glyphs.Icons == null)
                return false;

            if (!CollectControls(action, glyphs.Family, _controls))
                return false;

            int start = output.Length;
            foreach (string control in _controls)
            {
                string iconKey = glyphs.FindIconKey(control) ?? control.ToLowerInvariant();
                if (!glyphs.Icons.TryGetEntry(iconKey, out IconEntry entry) || entry.IsPlaceholder)
                {
                    output.Length = start;
                    return false;
                }

                MarkupContext.AppendIcon(glyphs.Icons, entry, output);
            }

            return true;
        }

        private static bool CollectControls(InputAction action, DeviceFamily family, List<string> controls)
        {
            controls.Clear();
            var bindings = action.bindings;

            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];

                if (binding.isComposite)
                {
                    int part = i + 1;
                    PartNames.Clear();
                    while (part < bindings.Count && bindings[part].isPartOfComposite)
                    {
                        if (TryGetControl(bindings[part], family, out string control)
                            && PartNames.Add(bindings[part].name ?? string.Empty)
                            && !controls.Contains(control))
                            controls.Add(control);
                        part++;
                    }

                    if (controls.Count > 0)
                    {
                        SortCompositeParts(bindings, i + 1, part, family, controls);
                        return true;
                    }

                    i = part - 1;
                    continue;
                }

                if (!binding.isPartOfComposite && TryGetControl(binding, family, out string single))
                {
                    controls.Add(single);
                    return true;
                }
            }

            return false;
        }

        private static void SortCompositeParts(ReadOnlyArray<InputBinding> bindings, int from, int to, DeviceFamily family, List<string> controls)
        {
            var order = new Dictionary<string, int>();
            for (int i = from; i < to; i++)
            {
                if (TryGetControl(bindings[i], family, out string control) && !order.ContainsKey(control))
                    order[control] = PartRank(bindings[i].name);
            }

            controls.Sort((a, b) => order[a].CompareTo(order[b]));
        }

        private static int PartRank(string partName)
        {
            switch (partName?.ToLowerInvariant())
            {
                case "up": return 0;
                case "left": return 1;
                case "down": return 2;
                case "right": return 3;
                default: return 4;
            }
        }

        private static bool TryGetControl(InputBinding binding, DeviceFamily family, out string control)
        {
            control = null;
            string path = binding.effectivePath;
            if (string.IsNullOrEmpty(path))
                return false;

            string layout = InputControlPath.TryGetDeviceLayout(path);
            if (string.IsNullOrEmpty(layout) || !Accepts(family, layout))
                return false;

            int slash = path.IndexOf('/');
            if (slash < 0 || slash == path.Length - 1)
                return false;

            control = path.Substring(slash + 1);
            return true;
        }

        private static bool Accepts(DeviceFamily family, string layout)
        {
            switch (family)
            {
                case DeviceFamily.KeyboardMouse:
                    return IsBasedOn(layout, "Keyboard") || IsBasedOn(layout, "Mouse") || layout == "Pointer";
                case DeviceFamily.Touch:
                    return IsBasedOn(layout, "Touchscreen");
                default:
                    return IsBasedOn(layout, "Gamepad");
            }
        }

        private static bool IsBasedOn(string layout, string baseLayout)
        {
            if (layout == baseLayout)
                return true;

            try
            {
                return InputSystemApi.IsFirstLayoutBasedOnSecond(layout, baseLayout);
            }
            catch (System.ArgumentException)
            {
                return false;
            }
        }

        private FamilyGlyphs Find(DeviceFamily family)
        {
            foreach (FamilyGlyphs glyphs in _families)
            {
                if (glyphs != null && glyphs.Family == family)
                    return glyphs;
            }

            return null;
        }

        private void OnValidate()
        {
            if (Typecraft.Settings != null && ReferenceEquals(Typecraft.Settings.InputGlyphs, this))
                Typecraft.Refresh();
        }
    }
}
