using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Art;
using Velsi.Typecraft.Editor.Window.Layout;
using Velsi.Typecraft.Editor.Window.Preview;

namespace Velsi.Typecraft.Editor.Library
{
    public enum IconCheckChoice
    {
        Cancel,
        Add,
        Fit
    }

    public sealed class IconCheckDialog : EditorWindow
    {
        private Texture2D _texture;
        private string _fileName;
        private IReadOnlyList<string> _problems;
        private TMP_SpriteAsset _spriteAsset;
        private IconLinePreview _preview;
        private IconCheckChoice _choice;

        public static IconCheckChoice Show(Texture2D texture, string fileName, IReadOnlyList<string> problems)
        {
            var window = CreateInstance<IconCheckDialog>();
            window.titleContent = new GUIContent("Typecraft: проверка иконки");
            window._texture = texture;
            window._fileName = fileName;
            window._problems = problems;
            window.minSize = new Vector2(460f, 300f);
            window.ShowModalUtility();
            return window._choice;
        }

        private void CreateGUI()
        {
            TypecraftLayout.Dialog(rootVisualElement);
            rootVisualElement.AddToClassList("tc-check");

            var title = new Label($"«{_fileName}» может некорректно отображаться в тексте");
            title.AddToClassList("tc-lib__title");
            rootVisualElement.Add(title);

            ScrollView problems = TypecraftLayout.VerticalScroll("tc-check__problems");
            foreach (string problem in _problems)
                problems.Add(new Label("• " + problem) { name = "tc-check__problem" });
            rootVisualElement.Add(problems);

            var hint = new Label($"Стандарт: PNG {IconImageCheck.StandardSize}×{IconImageCheck.StandardSize} с прозрачностью, рисунок в центре, поля по {IconImageCheck.StandardMargin} px.");
            hint.AddToClassList("tc-lib__hint");
            rootVisualElement.Add(hint);

            _spriteAsset = TmpSpriteAssetSync.CreateTransient(_texture, _fileName);
            _preview = new IconLinePreview();
            _preview.Show(_spriteAsset, _fileName, false);
            rootVisualElement.Add(_preview.Element);

            var buttons = new VisualElement();
            buttons.AddToClassList("tc-lib__row");
            buttons.AddToClassList("tc-check__buttons");
            buttons.Add(new Button(() => Choose(IconCheckChoice.Add)) { text = "Всё равно добавить" });
            buttons.Add(new Button(() => Choose(IconCheckChoice.Fit)) { text = "Подогнать", tooltip = "Добавить и сразу открыть ползунки смещения и масштаба" });
            buttons.Add(new Button(() => Choose(IconCheckChoice.Cancel)) { text = "Отмена" });
            rootVisualElement.Add(buttons);
        }

        private void Choose(IconCheckChoice choice)
        {
            _choice = choice;
            Close();
        }

        private void OnDestroy()
        {
            _preview?.Dispose();
            TmpSpriteAssetSync.DestroyTransient(_spriteAsset);
        }
    }
}
