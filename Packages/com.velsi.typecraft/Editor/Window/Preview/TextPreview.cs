using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Window.Effects;
using Velsi.Typecraft.Editor.Window.Markup;
using Velsi.Typecraft.Markup;
#if TYPECRAFT_TMPEFFECTS
using TMPEffects.Components;
#endif

namespace Velsi.Typecraft.Editor.Window.Preview
{
    public sealed class TextPreview : IDisposable
    {
        private const float PreviewFontSize = 36f;
        private const float DefaultWidth = 60f;
        private const float FitPadding = 1.18f;

        private static readonly Color Background = new Color(0.05f, 0.06f, 0.065f);
        private static readonly Color SelectionColor = new Color(0.3f, 0.56f, 0.85f, 0.35f);

        private readonly PreviewRenderUtility _renderer;
        private readonly GameObject _root;
        private readonly TextMeshPro _visible;
        private readonly TextMeshPro _measure;
        private readonly List<int> _renderMap = new List<int>();
        private readonly List<int> _measureMap = new List<int>();
#if TYPECRAFT_TMPEFFECTS
        private readonly TMPAnimator _animator;
        private TMPEffects.Databases.AnimationDatabase.TMPAnimationDatabase _databaseOverride;
#endif
        private string _markup = string.Empty;
        private int _anchorCaret = -1;
        private int _selectionFirst = -1;
        private int _selectionLast = -1;
        private bool _dragging;
        private bool _hasView;
        private Rect _view;
        private Vector2 _viewCenter;
        private Vector2 _viewHalf = Vector2.one;
        private readonly List<CommandMark> _commands = new List<CommandMark>();
        private readonly List<(Rect Rect, int Rendered)> _markers = new List<(Rect, int)>();
        private float[] _reveal = Array.Empty<float>();
        private float _typeTime;
        private bool _typing;
        private bool _showMarkers = true;
        private GUIStyle _markerStyle;

        public TextPreview()
        {
            _renderer = new PreviewRenderUtility();
            Camera camera = _renderer.camera;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);

            _root = new GameObject("TypecraftPreview") { hideFlags = HideFlags.HideAndDontSave };
            _visible = CreateText("Visible");
            _measure = CreateText("Measure");
            _measure.GetComponent<MeshRenderer>().enabled = false;
            _renderer.AddSingleGO(_root);

#if TYPECRAFT_TMPEFFECTS
            _animator = _visible.gameObject.AddComponent<TMPAnimator>();
            _animator.SetUpdateFrom(TMPEffects.Components.Animator.UpdateFrom.Script);
            _animator.SetDatabase(EffectLibrary.Current);
#endif

            Element = new IMGUIContainer(OnGUI) { focusable = true };
            Element.AddToClassList("tc-preview");
        }

        public event Action<TextRange> SelectionChanged;
        public event Action SelectionCleared;
        public event Action<int> CommandClicked;

        public IMGUIContainer Element { get; }
        public bool Animate { get; set; } = true;
        public float CharactersPerSecond { get; set; } = 30f;
        public bool IsTypewriting => _typing;

        public bool ShowMarkers
        {
            get => _showMarkers;
            set
            {
                _showMarkers = value;
                Element.MarkDirtyRepaint();
            }
        }
        public Rect? SelectionBounds { get; private set; }
        public TMP_Text VisibleText => _visible;

#if TYPECRAFT_TMPEFFECTS
        public void OverrideDatabase(TMPEffects.Databases.AnimationDatabase.TMPAnimationDatabase database)
        {
            _databaseOverride = database;
            SyncDatabase();
            Rebuild();
        }

        public void ReplayVisibility(bool show)
        {
            if (_animator == null || !_animator.isActiveAndEnabled)
                return;

            try
            {
                if (show)
                {
                    _animator.HideAll(true);
                    _animator.ShowAll();
                }
                else
                {
                    _animator.ShowAll(true);
                    _animator.HideAll();
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void SyncDatabase()
        {
            TMPEffects.Databases.AnimationDatabase.TMPAnimationDatabase wanted = _databaseOverride != null ? _databaseOverride : EffectLibrary.Current;
            if (_animator != null && _animator.Database != wanted)
                _animator.SetDatabase(wanted);
        }
#endif

        public string ExtraEffectTag { get; set; }

        public void Configure(TMP_Text reference, TMP_FontAsset font, Material material)
        {
#if TYPECRAFT_TMPEFFECTS
            SyncDatabase();
#endif
            TMP_FontAsset resolved = font != null ? font : reference != null ? reference.font : TMP_Settings.defaultFontAsset;
            foreach (TextMeshPro text in new[] { _visible, _measure })
            {
                if (resolved != null && text.font != resolved)
                    text.font = resolved;

                Material wanted = material != null ? material : resolved != null ? resolved.material : null;
                if (wanted != null)
                    text.fontSharedMaterial = wanted;

                text.fontSize = PreviewFontSize;
                text.alignment = reference != null ? reference.alignment : TextAlignmentOptions.Center;
                text.textWrappingMode = reference != null ? reference.textWrappingMode : TextWrappingModes.Normal;
                text.characterSpacing = reference != null ? reference.characterSpacing : 0f;
                text.lineSpacing = reference != null ? reference.lineSpacing : 0f;
                text.color = reference != null ? reference.color : new Color(0.91f, 0.88f, 0.83f);
                text.rectTransform.sizeDelta = new Vector2(PreviewWidth(reference), 200f);
            }

            Rebuild();
        }

        public void SetMarkup(string markup, string locale, DeviceFamily device, List<MarkupDiagnostic> diagnostics)
        {
            _markup = markup ?? string.Empty;
            string rendered = Typecraft.RenderPreview(_markup, locale, device, diagnostics, _renderMap);
            _visible.text = StripTags(rendered, EffectCatalog.IsWriterTag, null, null);
            _measure.text = StripTags(rendered, IsEffectTag, _measureMap, _commands);
            Rebuild();
        }

        private bool IsEffectTag(string name) => EffectCatalog.IsEffectTag(name) || (!string.IsNullOrEmpty(ExtraEffectTag) && name == ExtraEffectTag);

        public void RestartTypewriter()
        {
            _typeTime = 0f;
            _typing = true;
            _visible.maxVisibleCharacters = 0;
            Element.MarkDirtyRepaint();
        }

        public void ShowAll()
        {
            _typing = false;
            _visible.maxVisibleCharacters = 99999;
            Element.MarkDirtyRepaint();
        }

        public void HighlightMarkupRange(TextRange range)
        {
            _selectionFirst = _selectionLast = -1;
            TMP_TextInfo info = _measure.textInfo;

            for (int i = 0; i < info.characterCount; i++)
            {
                int source = SourceIndex(i);
                if (source < range.Start || source >= range.End)
                    continue;

                if (_selectionFirst < 0)
                    _selectionFirst = i;
                _selectionLast = i;
            }

            Element.MarkDirtyRepaint();
        }

        public bool Tick(float deltaTime)
        {
            bool repaint = false;

            if (_typing)
            {
                _typeTime += deltaTime;
                int shown = 0;
                while (shown < _reveal.Length && _reveal[shown] <= _typeTime)
                    shown++;

                _typing = shown < _reveal.Length;
                _visible.maxVisibleCharacters = _typing ? shown : 99999;
                repaint = true;
            }

#if TYPECRAFT_TMPEFFECTS
            if (Animate && _animator != null && _animator.isActiveAndEnabled)
            {
                try
                {
                    _animator.UpdateAnimations(deltaTime);
                    repaint = true;
                }
                catch (InvalidOperationException)
                {
                    Animate = false;
                }
            }
#endif

            if (repaint)
                Element.MarkDirtyRepaint();

            return repaint;
        }

        public void Dispose()
        {
#if TYPECRAFT_TMPEFFECTS
            if (_animator != null)
                UnityEngine.Object.DestroyImmediate(_animator);
#endif
            if (_root != null)
                UnityEngine.Object.DestroyImmediate(_root);
            _renderer.Cleanup();
        }

        private TextMeshPro CreateText(string name)
        {
            var child = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            child.transform.SetParent(_root.transform, false);
            var text = child.AddComponent<TextMeshPro>();
            text.fontSize = PreviewFontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(DefaultWidth, 200f);
            return text;
        }

        private static float PreviewWidth(TMP_Text reference)
        {
            if (reference == null || reference.fontSize <= 0f)
                return DefaultWidth;

            float width = reference.rectTransform.rect.width;
            if (reference is TextMeshPro)
                width *= 10f;

            return width <= 0f ? DefaultWidth : width / reference.fontSize * PreviewFontSize / 10f;
        }

        private void Rebuild()
        {
            _visible.ForceMeshUpdate(true, true);
            _measure.ForceMeshUpdate(true, true);
            BuildTimeline();
            _selectionFirst = _selectionLast = -1;
            _anchorCaret = -1;
            SelectionBounds = null;
            Element.MarkDirtyRepaint();
        }

        private void BuildTimeline()
        {
            TMP_TextInfo info = _measure.textInfo;
            _reveal = new float[info.characterCount];
            float time = 0f;
            float delay = 1f / Mathf.Max(1f, CharactersPerSecond);
            int next = 0;

            for (int i = 0; i < info.characterCount; i++)
            {
                while (next < _commands.Count && _commands[next].Position <= info.characterInfo[i].index)
                {
                    CommandMark command = _commands[next++];
                    if (!float.TryParse(command.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                        continue;

                    if (command.Name == "!wait")
                        time += Mathf.Max(0f, value);
                    else if (command.Name == "!delay")
                        delay = Mathf.Max(0f, value);
                }

                _reveal[i] = time;
                time += delay;
            }
        }

        private static string StripTags(string rendered, Func<string, bool> strip, List<int> map, List<CommandMark> commands)
        {
            map?.Clear();
            commands?.Clear();
            var output = new StringBuilder(rendered.Length);
            int i = 0;

            while (i < rendered.Length)
            {
                if (rendered[i] == '<')
                {
                    int close = rendered.IndexOf('>', i + 1);
                    string name = close > i ? TagName(rendered, i, close) : null;
                    if (name != null && strip(name))
                    {
                        if (commands != null && name.StartsWith("!") && rendered[i + 1] != '/')
                        {
                            int equals = rendered.IndexOf('=', i, close - i);
                            string value = equals > 0 ? rendered.Substring(equals + 1, close - equals - 1).Trim('"') : string.Empty;
                            commands.Add(new CommandMark(output.Length, i, name, value));
                        }

                        i = close + 1;
                        continue;
                    }
                }

                output.Append(rendered[i]);
                map?.Add(i);
                i++;
            }

            return output.ToString();
        }

        private static string TagName(string text, int open, int close)
        {
            int start = open + 1;
            if (start < close && text[start] == '/')
                start++;

            int end = start;
            while (end < close && text[end] != '=' && text[end] != ' ')
                end++;

            return text.Substring(start, end - start);
        }

        private int SourceIndex(int characterIndex)
        {
            TMP_TextInfo info = _measure.textInfo;
            if (characterIndex < 0 || characterIndex >= info.characterCount)
                return _markup.Length;

            int measureIndex = info.characterInfo[characterIndex].index;
            int renderedIndex = measureIndex < _measureMap.Count ? _measureMap[measureIndex] : _renderMap.Count;
            return renderedIndex < _renderMap.Count ? _renderMap[renderedIndex] : _markup.Length;
        }

        private TextRange MarkupRangeOfSelection()
        {
            int start = SourceIndex(_selectionFirst);
            int lastSource = SourceIndex(_selectionLast);
            int end = lastSource + TokenLength(lastSource);
            return MarkupEdits.Snap(_markup, new TextRange(start, end));
        }

        private int TokenLength(int index)
        {
            if (index >= _markup.Length)
                return 0;

            char c = _markup[index];
            if (c == '\\')
                return Mathf.Min(2, _markup.Length - index);

            if (c == '{')
            {
                int close = _markup.IndexOf('}', index);
                return close > index ? close - index + 1 : 1;
            }

            return 1;
        }

        private void OnGUI()
        {
            Rect rect = Element.contentRect;
            if (rect.width < 4f || rect.height < 4f)
                return;

            Event current = Event.current;

            if (current.type == EventType.Repaint)
            {
                FitCamera(rect);
                _renderer.BeginPreview(rect, GUIStyle.none);
                _renderer.Render(true);
                Texture texture = _renderer.EndPreview();
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
                DrawSelection();
                DrawMarkers();
            }

            HandleMouse(current);
        }

        private void FitCamera(Rect rect)
        {
            Bounds bounds = _measure.textBounds;
            if (bounds.size.sqrMagnitude < 0.0001f)
                bounds = new Bounds(Vector3.zero, new Vector3(10f, 4f, 0f));

            Vector3 center = _measure.transform.TransformPoint(bounds.center);
            float aspect = rect.width / rect.height;
            float halfHeight = Mathf.Max(0.5f, Mathf.Max(bounds.extents.y, bounds.extents.x / aspect) * FitPadding);

            Camera camera = _renderer.camera;
            camera.orthographicSize = halfHeight;
            camera.transform.position = new Vector3(center.x, center.y, -10f);

            _view = rect;
            _viewCenter = center;
            _viewHalf = new Vector2(halfHeight * aspect, halfHeight);
            _hasView = true;
        }

        private void DrawSelection()
        {
            SelectionBounds = null;
            if (_selectionFirst < 0)
                return;

            TMP_TextInfo info = _measure.textInfo;
            Rect? union = null;

            for (int i = _selectionFirst; i <= _selectionLast && i < info.characterCount; i++)
            {
                if (info.characterInfo[i].character == '\n')
                    continue;

                Rect characterRect = CharacterRect(i);
                EditorGUI.DrawRect(characterRect, SelectionColor);
                union = union.HasValue ? Encapsulate(union.Value, characterRect) : characterRect;
            }

            SelectionBounds = union;
        }

        private void DrawMarkers()
        {
            _markers.Clear();
            TMP_TextInfo info = _measure.textInfo;
            if (!_showMarkers || _commands.Count == 0 || info.characterCount == 0)
                return;

            _markerStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                padding = new RectOffset(3, 3, 0, 0),
                normal = { textColor = new Color(0.08f, 0.08f, 0.08f) }
            };

            int lastCharacter = -1;
            float stack = 0f;

            foreach (CommandMark command in _commands)
            {
                int character = 0;
                while (character < info.characterCount && info.characterInfo[character].index < command.Position)
                    character++;

                bool atEnd = character >= info.characterCount;
                if (atEnd)
                    character = info.characterCount - 1;

                TMP_CharacterInfo target = info.characterInfo[character];
                TMP_LineInfo line = info.lineInfo[target.lineNumber];
                float x = atEnd ? target.xAdvance : target.origin;
                Vector2 top = ToGui(new Vector3(x, line.ascender, 0f));
                Vector2 bottom = ToGui(new Vector3(x, line.descender, 0f));

                Color color = command.Name switch
                {
                    "!wait" => new Color(0.95f, 0.72f, 0.32f),
                    "!delay" => new Color(0.5f, 0.85f, 0.75f),
                    _ => new Color(0.78f, 0.58f, 0.82f)
                };

                stack = character == lastCharacter ? stack + 15f : 0f;
                lastCharacter = character;

                EditorGUI.DrawRect(new Rect(top.x - 1f, top.y, 2f, bottom.y - top.y), color);

                var content = new GUIContent(MarkerLabel(command), "Клик — изменить");
                Vector2 size = _markerStyle.CalcSize(content);
                var badge = new Rect(top.x - size.x / 2f, top.y - size.y - 1f - stack, size.x, size.y);
                EditorGUI.DrawRect(badge, color);
                GUI.Label(badge, content, _markerStyle);
                _markers.Add((badge, command.Rendered));
            }
        }

        private static string MarkerLabel(CommandMark command) => command.Name switch
        {
            "!wait" => $"‖ {command.Value}s",
            "!delay" => $"» {command.Value}s",
            _ => command.Name
        };

        private Rect CharacterRect(int index)
        {
            TMP_TextInfo info = _measure.textInfo;
            TMP_CharacterInfo character = info.characterInfo[index];
            TMP_LineInfo line = info.lineInfo[character.lineNumber];
            Vector2 a = ToGui(new Vector3(character.origin, line.descender, 0f));
            Vector2 b = ToGui(new Vector3(character.xAdvance, line.ascender, 0f));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private Vector2 ToGui(Vector3 local)
        {
            Vector3 world = _measure.transform.TransformPoint(local);
            float x = (world.x - _viewCenter.x) / _viewHalf.x;
            float y = (world.y - _viewCenter.y) / _viewHalf.y;
            return new Vector2(_view.x + (x + 1f) * 0.5f * _view.width, _view.y + (1f - y) * 0.5f * _view.height);
        }

        private void HandleMouse(Event current)
        {
            if (!_hasView)
                return;

            int caret;
            switch (current.type)
            {
                case EventType.MouseDown when current.button == 0 && _view.Contains(current.mousePosition):
                    current.Use();
                    foreach ((Rect markerRect, int rendered) in _markers)
                    {
                        if (markerRect.Contains(current.mousePosition))
                        {
                            CommandClicked?.Invoke(rendered < _renderMap.Count ? _renderMap[rendered] : -1);
                            return;
                        }
                    }

                    caret = HitCaret(current.mousePosition);
                    if (caret < 0)
                    {
                        ClearSelection();
                        break;
                    }

                    if (current.clickCount == 2)
                    {
                        SelectWord(caret);
                        NotifySelection();
                        break;
                    }

                    if (current.clickCount >= 3)
                    {
                        SelectLine(caret);
                        NotifySelection();
                        break;
                    }

                    if (!current.shift || _anchorCaret < 0)
                        _anchorCaret = caret;
                    _dragging = true;
                    SelectCarets(_anchorCaret, caret);
                    break;

                case EventType.MouseDrag when _dragging:
                    current.Use();
                    caret = HitCaret(current.mousePosition);
                    if (caret >= 0)
                        SelectCarets(_anchorCaret, caret);
                    break;

                case EventType.MouseUp when _dragging:
                    current.Use();
                    _dragging = false;
                    if (_selectionFirst < 0)
                        ClearSelection();
                    else
                        NotifySelection();
                    break;
            }
        }

        private void SelectCarets(int a, int b)
        {
            int from = Mathf.Min(a, b);
            int to = Mathf.Max(a, b);
            _selectionFirst = from < to ? from : -1;
            _selectionLast = from < to ? to - 1 : -1;
            Element.MarkDirtyRepaint();
        }

        private void ClearSelection()
        {
            _selectionFirst = _selectionLast = -1;
            SelectionCleared?.Invoke();
            Element.MarkDirtyRepaint();
        }

        private void NotifySelection()
        {
            SelectionChanged?.Invoke(MarkupRangeOfSelection());
            Element.MarkDirtyRepaint();
        }

        private void SelectWord(int caret)
        {
            TMP_TextInfo info = _measure.textInfo;
            int index = Mathf.Min(caret, info.characterCount - 1);
            int word = FindWord(index);
            if (word < 0 && index > 0)
                word = FindWord(index - 1);

            if (word >= 0)
            {
                _selectionFirst = info.wordInfo[word].firstCharacterIndex;
                _selectionLast = info.wordInfo[word].lastCharacterIndex;
            }
            else
            {
                _selectionFirst = _selectionLast = index;
            }

            _anchorCaret = _selectionFirst;
        }

        private int FindWord(int index)
        {
            TMP_TextInfo info = _measure.textInfo;
            for (int w = 0; w < info.wordCount; w++)
            {
                if (index >= info.wordInfo[w].firstCharacterIndex && index <= info.wordInfo[w].lastCharacterIndex)
                    return w;
            }

            return -1;
        }

        private void SelectLine(int caret)
        {
            TMP_TextInfo info = _measure.textInfo;
            TMP_LineInfo line = info.lineInfo[info.characterInfo[Mathf.Min(caret, info.characterCount - 1)].lineNumber];
            int last = line.lastCharacterIndex;
            while (last > line.firstCharacterIndex && char.IsWhiteSpace(info.characterInfo[last].character))
                last--;

            _selectionFirst = line.firstCharacterIndex;
            _selectionLast = last;
            _anchorCaret = _selectionFirst;
        }

        private int HitCaret(Vector2 mouse)
        {
            TMP_TextInfo info = _measure.textInfo;
            if (info.characterCount == 0 || info.lineCount == 0)
                return -1;

            int line = 0;
            float best = float.MaxValue;
            for (int l = 0; l < info.lineCount; l++)
            {
                float top = ToGui(new Vector3(0f, info.lineInfo[l].ascender, 0f)).y;
                float bottom = ToGui(new Vector3(0f, info.lineInfo[l].descender, 0f)).y;
                float distance = mouse.y < top ? top - mouse.y : mouse.y > bottom ? mouse.y - bottom : 0f;
                if (distance < best)
                {
                    best = distance;
                    line = l;
                }
            }

            TMP_LineInfo lineInfo = info.lineInfo[line];
            for (int i = lineInfo.firstCharacterIndex; i <= lineInfo.lastCharacterIndex && i < info.characterCount; i++)
            {
                if (info.characterInfo[i].character == '\n' || mouse.x < CharacterRect(i).center.x)
                    return i;
            }

            return Mathf.Min(lineInfo.lastCharacterIndex + 1, info.characterCount);
        }

        private readonly struct CommandMark
        {
            public CommandMark(int position, int rendered, string name, string value)
            {
                Position = position;
                Rendered = rendered;
                Name = name;
                Value = value;
            }

            public int Position { get; }
            public int Rendered { get; }
            public string Name { get; }
            public string Value { get; }
        }

        private static Rect Encapsulate(Rect a, Rect b) =>
            Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
    }
}
