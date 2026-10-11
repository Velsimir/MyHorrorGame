using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Velsi.Typecraft.Editor.Window.Layout
{
    public sealed class ColumnsLayout : VisualElement
    {
        private const float OverlayShare = 0.85f;

        private readonly SplitPane _outer;
        private readonly SplitPane _inner;
        private readonly VisualElement _list;
        private readonly VisualElement _overlay;
        private readonly VisualElement _scrim;
        private readonly float _narrowWidth;
        private readonly float _listWidth;
        private readonly string _listKey;
        private bool _narrow;

        public ColumnsLayout(string key, VisualElement list, VisualElement center, VisualElement side, float listWidth, float sideWidth, float sideHeight, float narrowWidth = TypecraftLayout.NarrowWidth)
        {
            _list = list;
            _narrowWidth = narrowWidth;
            _listWidth = listWidth;
            _listKey = key + ".List";
            AddToClassList("tc-columns");

            VisualElement rest = center;
            if (side != null)
            {
                _inner = new SplitPane(key + ".Side", center, side, false, sideWidth, sideHeight, 200f, 160f);
                rest = _inner;
            }

            _outer = new SplitPane(_listKey, list, rest, true, listWidth, -1f, 120f, 220f);
            Add(_outer);

            _scrim = new VisualElement();
            _scrim.AddToClassList("tc-columns__scrim");
            _scrim.RegisterCallback<PointerDownEvent>(_ => HideList());
            Add(_scrim);

            _overlay = new VisualElement();
            _overlay.AddToClassList("tc-columns__overlay");
            Add(_overlay);

            MenuButton = new Button(ToggleList) { text = "☰", tooltip = "Список" };
            MenuButton.AddToClassList("tc-columns__menu");

            focusable = false;
            RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Escape || !ListShown)
                    return;
                HideList();
                evt.StopPropagation();
            });
            RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (evt.newRect.width > 0f && !float.IsNaN(evt.newRect.width))
                    SetNarrow(evt.newRect.width < _narrowWidth);
            });
            HideList();
            MenuButton.style.display = DisplayStyle.None;
        }

        public Button MenuButton { get; }
        public bool IsNarrow => _narrow;
        public bool ListShown => _overlay.style.display == DisplayStyle.Flex;

        public void ShowList()
        {
            if (!_narrow)
                return;

            float width = EditorPrefs.GetFloat("Typecraft.Split." + _listKey + ".w", _listWidth);
            float available = layout.width;
            _overlay.style.width = float.IsNaN(available) || available <= 0f ? width : Mathf.Min(width, available * OverlayShare);
            _scrim.style.display = DisplayStyle.Flex;
            _overlay.style.display = DisplayStyle.Flex;
            _scrim.BringToFront();
            _overlay.BringToFront();
            MenuButton.AddToClassList("tc-columns__menu--on");
        }

        public void HideList()
        {
            _scrim.style.display = DisplayStyle.None;
            _overlay.style.display = DisplayStyle.None;
            MenuButton.RemoveFromClassList("tc-columns__menu--on");
        }

        private void ToggleList()
        {
            if (ListShown)
                HideList();
            else
                ShowList();
        }

        private void SetNarrow(bool narrow)
        {
            if (narrow == _narrow)
                return;

            _narrow = narrow;
            HideList();
            if (narrow)
            {
                _outer.ReleaseFixed();
                _list.style.width = StyleKeyword.Auto;
                _overlay.Add(_list);
            }
            else
            {
                _list.style.width = StyleKeyword.Null;
                _outer.RestoreFixed();
            }

            if (_inner != null)
                _inner.Vertical = narrow;
            MenuButton.style.display = narrow ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
