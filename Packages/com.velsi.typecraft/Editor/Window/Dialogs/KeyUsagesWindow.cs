using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Editor.Window.Layout;

namespace Velsi.Typecraft.Editor.Window.Dialogs
{
    public sealed class KeyUsagesWindow : EditorWindow
    {
        private readonly List<KeyUsage> _usages = new List<KeyUsage>();

        public static void Show(string table, string key, IReadOnlyList<KeyUsage> usages)
        {
            var window = GetWindow<KeyUsagesWindow>(true, "Использования ключа", true);
            window.Build(table, key, usages);
            window.minSize = new Vector2(420f, 160f);
        }

        private void Build(string table, string key, IReadOnlyList<KeyUsage> usages)
        {
            _usages.Clear();
            _usages.AddRange(usages);

            VisualElement root = rootVisualElement;
            root.Clear();
            TypecraftLayout.Dialog(root);
            root.style.paddingLeft = root.style.paddingRight = 8;
            root.style.paddingTop = root.style.paddingBottom = 6;

            var header = new Label($"«{key}» ({table}) — мест: {usages.Count}. Клик — перейти.");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 4;
            header.style.whiteSpace = WhiteSpace.Normal;
            root.Add(header);

            var list = new ListView(_usages, 34f, MakeItem, BindItem)
            {
                selectionType = SelectionType.Single
            };
            list.style.flexGrow = 1;
            root.Add(list);
        }

        private static VisualElement MakeItem()
        {
            var item = new VisualElement();
            item.style.justifyContent = Justify.Center;
            item.style.paddingLeft = 4;
            item.RegisterCallback<ClickEvent>(_ =>
            {
                if (item.userData is KeyUsage usage)
                    KeyUsages.Reveal(usage);
            });

            var title = new Label { name = "title" };
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            item.Add(title);

            title.AddToClassList("tc-ellipsis");
            var location = new Label { name = "location" };
            location.AddToClassList("tc-ellipsis");
            location.style.color = new Color(0.65f, 0.65f, 0.65f);
            item.Add(location);
            return item;
        }

        private void BindItem(VisualElement element, int index)
        {
            KeyUsage usage = _usages[index];
            element.userData = usage;
            element.Q<Label>("title").text = $"{usage.KindTitle} · {(string.IsNullOrEmpty(usage.AssetPath) ? "(не сохранена)" : usage.AssetPath)}";
            element.Q<Label>("location").text = usage.Location;
            element.tooltip = usage.ToString();
        }
    }
}
