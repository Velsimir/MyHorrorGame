using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Fonts;

namespace Velsi.Typecraft.Editor.Window.Preview
{
    public sealed class IconLinePreview : IDisposable
    {
        private const float FontSize = 36f;
        private const float FitPadding = 1.35f;

        private static readonly Color Background = new Color(0.05f, 0.06f, 0.065f);
        private static readonly Color BaselineColor = new Color(0.95f, 0.45f, 0.35f, 0.9f);
        private static readonly Color GuideColor = new Color(0.5f, 0.6f, 0.7f, 0.45f);

        private readonly PreviewRenderUtility _renderer;
        private readonly GameObject _root;
        private readonly TextMeshPro _text;
        private Rect _view;
        private Vector2 _viewCenter;
        private Vector2 _viewHalf = Vector2.one;

        public IconLinePreview()
        {
            _renderer = new PreviewRenderUtility();
            Camera camera = _renderer.camera;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;

            _root = new GameObject("TypecraftIconLine") { hideFlags = HideFlags.HideAndDontSave };
            _text = _root.AddComponent<TextMeshPro>();
            _text.fontSize = FontSize;
            _text.alignment = TextAlignmentOptions.Center;
            _text.textWrappingMode = TextWrappingModes.NoWrap;
            _text.color = new Color(0.91f, 0.88f, 0.83f);
            _text.rectTransform.sizeDelta = new Vector2(60f, 10f);
            _renderer.AddSingleGO(_root);

            TypecraftSettings settings = Typecraft.Settings;
            FontVariant body = settings != null && settings.Fonts != null ? settings.Fonts.Resolve(settings.DefaultFontRole, Typecraft.Locale) : null;
            if (body != null && body.Font != null)
                _text.font = body.Font;

            Element = new IMGUIContainer(OnGUI);
            Element.AddToClassList("tc-icon-line");
        }

        public IMGUIContainer Element { get; }

        public void Show(TMP_SpriteAsset spriteAsset, string glyphName, bool tint)
        {
            _text.spriteAsset = spriteAsset;
            string sprite = spriteAsset == null || string.IsNullOrEmpty(glyphName)
                ? "?"
                : $"<sprite name=\"{glyphName}\"{(tint ? " tint=1" : string.Empty)}>";
            _text.text = $"Press {sprite} to open";
            Refresh();
        }

        public void Refresh()
        {
            _text.havePropertiesChanged = true;
            _text.ForceMeshUpdate(true, true);
            Element.MarkDirtyRepaint();
        }

        public void Dispose()
        {
            if (_root != null)
                UnityEngine.Object.DestroyImmediate(_root);
            _renderer.Cleanup();
        }

        private void OnGUI()
        {
            Rect rect = Element.contentRect;
            if (rect.width < 4f || rect.height < 4f || Event.current.type != EventType.Repaint)
                return;

            FitCamera(rect);
            _renderer.BeginPreview(rect, GUIStyle.none);
            _renderer.Render(true);
            GUI.DrawTexture(rect, _renderer.EndPreview(), ScaleMode.StretchToFill, false);
            DrawGuides();
        }

        private void FitCamera(Rect rect)
        {
            Bounds bounds = _text.textBounds;
            if (bounds.size.sqrMagnitude < 0.0001f)
                bounds = new Bounds(Vector3.zero, new Vector3(10f, 4f, 0f));

            Vector3 center = _text.transform.TransformPoint(bounds.center);
            float aspect = rect.width / rect.height;
            float halfHeight = Mathf.Max(0.5f, Mathf.Max(bounds.extents.y, bounds.extents.x / aspect) * FitPadding);

            Camera camera = _renderer.camera;
            camera.orthographicSize = halfHeight;
            camera.transform.SetPositionAndRotation(new Vector3(center.x, center.y, -10f), Quaternion.identity);

            _view = rect;
            _viewCenter = center;
            _viewHalf = new Vector2(halfHeight * aspect, halfHeight);
        }

        private void DrawGuides()
        {
            TMP_TextInfo info = _text.textInfo;
            if (info.lineCount == 0)
                return;

            TMP_LineInfo line = info.lineInfo[0];
            DrawHorizontal(line.ascender, GuideColor);
            DrawHorizontal(line.descender, GuideColor);
            DrawHorizontal(line.baseline, BaselineColor);
        }

        private void DrawHorizontal(float localY, Color color)
        {
            Vector3 world = _text.transform.TransformPoint(new Vector3(0f, localY, 0f));
            float y = _view.y + (1f - (world.y - _viewCenter.y) / _viewHalf.y) * 0.5f * _view.height;
            EditorGUI.DrawRect(new Rect(_view.x, Mathf.Round(y), _view.width, 1f), color);
        }
    }
}
