using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Velsi.Typecraft.Editor.Window.Layout
{
    public sealed class SplitPane : VisualElement
    {
        private const string PrefPrefix = "Typecraft.Split.";
        private const float HandleSize = 4f;

        private readonly string _key;
        private readonly VisualElement _fixed;
        private readonly VisualElement _flex;
        private readonly VisualElement _handle;
        private readonly bool _fixedFirst;
        private readonly float _defaultWidth;
        private readonly float _defaultHeight;
        private readonly float _minFixed;
        private readonly float _minFlex;

        private bool _vertical;
        private bool _released;
        private bool _hidden;
        private bool _auto;
        private float _narrowWidth;
        private float _dragStart;
        private float _dragSize;

        public SplitPane(string key, VisualElement first, VisualElement second, bool fixedFirst, float defaultWidth, float defaultHeight = -1f, float minFixed = 80f, float minFlex = 120f)
        {
            _key = key;
            _fixedFirst = fixedFirst;
            _fixed = fixedFirst ? first : second;
            _flex = fixedFirst ? second : first;
            _defaultWidth = defaultWidth;
            _defaultHeight = defaultHeight > 0f ? defaultHeight : defaultWidth;
            _minFixed = minFixed;
            _minFlex = minFlex;
            AddToClassList("tc-split");

            _fixed.AddToClassList("tc-split__fixed");
            _flex.AddToClassList("tc-split__flex");

            _handle = new VisualElement { tooltip = "Перетащи, чтобы изменить размер" };
            _handle.AddToClassList("tc-split__handle");
            _handle.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _handle.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _handle.RegisterCallback<PointerUpEvent>(OnPointerUp);

            Add(first);
            Add(_handle);
            Add(second);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            Refresh();
        }

        public bool Vertical
        {
            get => _vertical;
            set
            {
                if (_vertical == value)
                    return;
                _vertical = value;
                Refresh();
            }
        }

        public float NarrowWidth
        {
            get => _narrowWidth;
            set => _narrowWidth = value;
        }

        public VisualElement FixedPane => _fixed;

        public void ReleaseFixed()
        {
            if (_released)
                return;

            _released = true;
            _fixed.RemoveFromClassList("tc-split__fixed");
            _fixed.style.width = StyleKeyword.Null;
            _fixed.style.height = StyleKeyword.Null;
            _fixed.RemoveFromHierarchy();
            Refresh();
        }

        public void RestoreFixed()
        {
            if (!_released)
                return;

            _released = false;
            _fixed.AddToClassList("tc-split__fixed");
            Insert(_fixedFirst ? 0 : childCount, _fixed);
            Refresh();
        }

        public void SetFixedVisible(bool visible)
        {
            _hidden = !visible;
            Refresh();
        }

        public void SetFixedAuto(bool auto)
        {
            _auto = auto;
            Refresh();
        }

        private string PrefKey => PrefPrefix + _key + (_vertical ? ".h" : ".w");

        private float Stored => EditorPrefs.GetFloat(PrefKey, _vertical ? _defaultHeight : _defaultWidth);

        private float Total => _vertical ? layout.height : layout.width;

        private void Refresh()
        {
            EnableInClassList("tc-split--vertical", _vertical);
            bool handle = !_released && !_hidden && !_auto;
            _handle.style.display = handle ? DisplayStyle.Flex : DisplayStyle.None;
            if (!_released)
                _fixed.style.display = _hidden ? DisplayStyle.None : DisplayStyle.Flex;

            if (_released || _hidden)
                return;

            if (_auto)
            {
                _fixed.style.width = StyleKeyword.Null;
                _fixed.style.height = StyleKeyword.Null;
                return;
            }

            Apply(Stored);
        }

        private float Clamp(float size)
        {
            float total = Total;
            if (float.IsNaN(total) || total <= 0f)
                return Mathf.Max(_minFixed, size);

            float floor = Mathf.Min(_minFixed, total * 0.5f);
            return Mathf.Clamp(size, floor, Mathf.Max(floor, total - _minFlex - HandleSize));
        }

        private void Apply(float size)
        {
            size = Clamp(size);
            if (_vertical)
            {
                _fixed.style.width = StyleKeyword.Null;
                _fixed.style.height = size;
            }
            else
            {
                _fixed.style.height = StyleKeyword.Null;
                _fixed.style.width = size;
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (_narrowWidth > 0f && evt.newRect.width > 0f)
            {
                bool vertical = evt.newRect.width < _narrowWidth;
                if (vertical != _vertical)
                {
                    _vertical = vertical;
                    Refresh();
                    return;
                }
            }

            if (!_released && !_hidden && !_auto)
                Apply(Stored);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
                return;

            _dragStart = _vertical ? evt.position.y : evt.position.x;
            _dragSize = _vertical ? _fixed.layout.height : _fixed.layout.width;
            _handle.CapturePointer(evt.pointerId);
            _handle.AddToClassList("tc-split__handle--drag");
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_handle.HasPointerCapture(evt.pointerId))
                return;

            float delta = (_vertical ? evt.position.y : evt.position.x) - _dragStart;
            Apply(_dragSize + (_fixedFirst ? delta : -delta));
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_handle.HasPointerCapture(evt.pointerId))
                return;

            _handle.ReleasePointer(evt.pointerId);
            _handle.RemoveFromClassList("tc-split__handle--drag");
            float size = _vertical ? _fixed.resolvedStyle.height : _fixed.resolvedStyle.width;
            if (!float.IsNaN(size) && size > 0f)
                EditorPrefs.SetFloat(PrefKey, size);
            evt.StopPropagation();
        }
    }
}
