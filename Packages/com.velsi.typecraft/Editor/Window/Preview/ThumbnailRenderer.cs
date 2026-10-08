using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Velsi.Typecraft.Editor.Window.Preview
{
    public sealed class ThumbnailRenderer : IDisposable
    {
        private readonly PreviewRenderUtility _renderer;
        private readonly GameObject _root;
        private readonly TextMeshPro _text;
        private readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();

        public ThumbnailRenderer()
        {
            _renderer = new PreviewRenderUtility();
            Camera camera = _renderer.camera;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.078f, 0.082f, 0.086f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;

            _root = new GameObject("TypecraftThumbnail") { hideFlags = HideFlags.HideAndDontSave };
            _text = _root.AddComponent<TextMeshPro>();
            _text.fontSize = 36f;
            _text.alignment = TextAlignmentOptions.Center;
            _text.textWrappingMode = TextWrappingModes.NoWrap;
            _text.rectTransform.sizeDelta = new Vector2(40f, 10f);
            _renderer.AddSingleGO(_root);
        }

        public Texture2D Render(string key, string richText, TMP_FontAsset font, Material material, Vector2Int size)
        {
            if (_cache.TryGetValue(key, out Texture2D cached) && cached != null)
                return cached;

            TMP_FontAsset resolved = font != null ? font : TMP_Settings.defaultFontAsset;
            if (resolved != null)
                _text.font = resolved;
            _text.fontSharedMaterial = material != null ? material : resolved != null ? resolved.material : _text.fontSharedMaterial;
            _text.color = new Color(0.91f, 0.88f, 0.83f);
            _text.text = richText;
            _text.ForceMeshUpdate(true, true);

            Bounds bounds = _text.textBounds;
            float aspect = (float)size.x / size.y;
            Camera camera = _renderer.camera;
            camera.orthographicSize = Mathf.Max(0.5f, Mathf.Max(bounds.extents.y, bounds.extents.x / aspect) * 1.25f);
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);

            var rect = new Rect(0, 0, size.x, size.y);
            _renderer.BeginStaticPreview(rect);
            _renderer.Render(true);
            Texture2D texture = _renderer.EndStaticPreview();
            texture.hideFlags = HideFlags.HideAndDontSave;

            _cache[key] = texture;
            return texture;
        }

        public void Clear()
        {
            foreach (Texture2D texture in _cache.Values)
            {
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
            }

            _cache.Clear();
        }

        public void Dispose()
        {
            Clear();
            _renderer.Cleanup();
            if (_root != null)
                UnityEngine.Object.DestroyImmediate(_root);
        }
    }
}
