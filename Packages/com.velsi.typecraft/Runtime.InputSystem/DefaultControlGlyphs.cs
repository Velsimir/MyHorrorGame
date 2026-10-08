using System.Collections.Generic;

namespace Velsi.Typecraft.InputSystem
{
    public static class DefaultControlGlyphs
    {
        public static List<ControlGlyph> For(DeviceFamily family)
        {
            switch (family)
            {
                case DeviceFamily.KeyboardMouse: return Keyboard();
                case DeviceFamily.Xbox: return Gamepad("a", "b", "x", "y", "lb", "rb", "lt", "rt", "menu", "view");
                case DeviceFamily.PlayStation: return Gamepad("cross", "circle", "square", "triangle", "l1", "r1", "l2", "r2", "options", "share", ("touchpadButton", "touch_pad"));
                case DeviceFamily.Switch: return Gamepad("b", "a", "y", "x", "lb", "rb", "lt", "rt", "plus", "minus", ("home", "home"));
                case DeviceFamily.SteamDeck: return Gamepad("a", "b", "x", "y", "l1", "r1", "l2", "r2", "menu", "square");
                default: return new List<ControlGlyph>();
            }
        }

        public static List<ActionGlyphOverride> TouchOverrides() => new List<ActionGlyphOverride>
        {
            new ActionGlyphOverride(DeviceFamily.Touch, "Interact", "{gestures/double_tap}"),
            new ActionGlyphOverride(DeviceFamily.Touch, "Attack", "{gestures/tap}"),
            new ActionGlyphOverride(DeviceFamily.Touch, "Jump", "{gestures/swipe_up}"),
            new ActionGlyphOverride(DeviceFamily.Touch, "Crouch", "{gestures/swipe_bottom}"),
            new ActionGlyphOverride(DeviceFamily.Touch, "Sprint", "{gestures/hold}"),
            new ActionGlyphOverride(DeviceFamily.Touch, "Move", "{gestures/finger_front}"),
            new ActionGlyphOverride(DeviceFamily.Touch, "Look", "{gestures/finger_side}"),
            new ActionGlyphOverride(DeviceFamily.Touch, "Previous", "{gestures/swipe_right}"),
            new ActionGlyphOverride(DeviceFamily.Touch, "Next", "{gestures/swipe_left}")
        };

        private static List<ControlGlyph> Gamepad(
            string south, string east, string west, string north,
            string leftShoulder, string rightShoulder, string leftTrigger, string rightTrigger,
            string start, string select, params (string Control, string Key)[] extra)
        {
            var glyphs = new List<ControlGlyph>
            {
                new ControlGlyph("buttonSouth", south),
                new ControlGlyph("buttonEast", east),
                new ControlGlyph("buttonWest", west),
                new ControlGlyph("buttonNorth", north),
                new ControlGlyph("leftShoulder", leftShoulder),
                new ControlGlyph("rightShoulder", rightShoulder),
                new ControlGlyph("leftTrigger", leftTrigger),
                new ControlGlyph("rightTrigger", rightTrigger),
                new ControlGlyph("start", start),
                new ControlGlyph("select", select),
                new ControlGlyph("leftStick", "left_stick"),
                new ControlGlyph("rightStick", "right_stick"),
                new ControlGlyph("leftStickPress", "left_stick_click"),
                new ControlGlyph("rightStickPress", "right_stick_click"),
                new ControlGlyph("dpad", "dpad"),
                new ControlGlyph("dpad/up", "dpad_up"),
                new ControlGlyph("dpad/down", "dpad_down"),
                new ControlGlyph("dpad/left", "dpad_left"),
                new ControlGlyph("dpad/right", "dpad_right")
            };

            foreach ((string control, string key) in extra)
                glyphs.Add(new ControlGlyph(control, key));

            return glyphs;
        }

        private static List<ControlGlyph> Keyboard()
        {
            var glyphs = new List<ControlGlyph>
            {
                new ControlGlyph("leftShift", "shift"),
                new ControlGlyph("rightShift", "shift"),
                new ControlGlyph("shift", "shift"),
                new ControlGlyph("leftCtrl", "ctrl"),
                new ControlGlyph("rightCtrl", "ctrl"),
                new ControlGlyph("ctrl", "ctrl"),
                new ControlGlyph("leftAlt", "alt"),
                new ControlGlyph("rightAlt", "alt"),
                new ControlGlyph("alt", "alt"),
                new ControlGlyph("leftMeta", "win"),
                new ControlGlyph("rightMeta", "win"),
                new ControlGlyph("escape", "esc"),
                new ControlGlyph("delete", "del"),
                new ControlGlyph("upArrow", "arrow_up"),
                new ControlGlyph("downArrow", "arrow_down"),
                new ControlGlyph("leftArrow", "arrow_left"),
                new ControlGlyph("rightArrow", "arrow_right"),
                new ControlGlyph("pageUp", "page_up"),
                new ControlGlyph("pageDown", "page_down"),
                new ControlGlyph("capsLock", "caps_lock"),
                new ControlGlyph("numLock", "num_lock"),
                new ControlGlyph("printScreen", "print_screen"),
                new ControlGlyph("leftBracket", "bracket_left"),
                new ControlGlyph("rightBracket", "bracket_right"),
                new ControlGlyph("backquote", "tilda"),
                new ControlGlyph("equals", "plus"),
                new ControlGlyph("numpadPlus", "plus"),
                new ControlGlyph("numpadMinus", "minus"),
                new ControlGlyph("numpadMultiply", "asterisk"),
                new ControlGlyph("numpadEnter", "enter"),
                new ControlGlyph("leftButton", "mouse_left"),
                new ControlGlyph("rightButton", "mouse_right"),
                new ControlGlyph("middleButton", "mouse_middle"),
                new ControlGlyph("delta", "mouse_simple"),
                new ControlGlyph("position", "mouse_simple"),
                new ControlGlyph("scroll", "mouse_middle")
            };

            for (int digit = 0; digit <= 9; digit++)
                glyphs.Add(new ControlGlyph($"numpad{digit}", digit.ToString()));

            return glyphs;
        }
    }
}
