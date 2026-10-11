using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Velsi.Typecraft.Editor.Window.Layout
{
    public static class TypecraftLayout
    {
        public const float NarrowWidth = 700f;
        public const string FitClass = "tc-fit";

        public static ScrollView VerticalScroll(string className = null)
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            FitScroll(scroll);
            if (className != null)
                scroll.AddToClassList(className);
            return scroll;
        }

        public static void FitScroll(ScrollView scroll)
        {
            scroll.mode = ScrollViewMode.Vertical;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
            scroll.AddToClassList("tc-vscroll");
        }

        public static void Responsive(VisualElement element, float narrowWidth, string narrowClass)
        {
            element.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                float width = evt.newRect.width;
                if (width > 0f && !float.IsNaN(width))
                    element.EnableInClassList(narrowClass, width < narrowWidth);
            });
        }

        public static void Dialog(VisualElement root)
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(TypecraftWindow.StylePath);
            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
                root.styleSheets.Add(styleSheet);
            root.AddToClassList("tc-dialog");
            root.AddToClassList(FitClass);
            TruncatedTooltips(root);
        }

        public static VisualElement ButtonRow()
        {
            var row = new VisualElement();
            row.AddToClassList("tc-button-row");
            return row;
        }

        public static void TruncatedTooltips(VisualElement root)
        {
            root.RegisterCallback<TooltipEvent>(OnTooltip, TrickleDown.TrickleDown);
        }

        private static void OnTooltip(TooltipEvent evt)
        {
            if (evt.target is not TextElement text || string.IsNullOrEmpty(text.text) || !IsTruncated(text))
                return;

            string explanation = null;
            for (VisualElement parent = text; parent != null; parent = parent.hierarchy.parent)
            {
                if (string.IsNullOrEmpty(parent.tooltip))
                    continue;
                explanation = parent.tooltip;
                break;
            }

            evt.tooltip = string.IsNullOrEmpty(explanation) || explanation == text.text ? text.text : text.text + "\n\n" + explanation;
            evt.rect = text.worldBound;
            evt.StopImmediatePropagation();
        }

        private static bool IsTruncated(TextElement text)
        {
            if (text.resolvedStyle.textOverflow != TextOverflow.Ellipsis)
                return false;

            float available = text.contentRect.width;
            if (available <= 0f || float.IsNaN(available))
                return false;

            Vector2 size = text.MeasureTextSize(text.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
            return size.x > available + 0.5f;
        }
    }
}
