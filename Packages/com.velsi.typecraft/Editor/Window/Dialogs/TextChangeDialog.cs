using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Editor.Window.Layout;

namespace Velsi.Typecraft.Editor.Window.Dialogs
{
    public enum TextChangeChoice
    {
        Cancel,
        WriteTable,
        RestoreWords
    }

    public sealed class TextChangeDialog : EditorWindow
    {
        private static TextChangeChoice _choice;

        public static TextChangeChoice Show(string subject, string before, string after, string restoreRefusal)
        {
            _choice = TextChangeChoice.Cancel;
            var window = CreateInstance<TextChangeDialog>();
            window.titleContent = new GUIContent("Текст локализации изменён");
            window.Build(subject, before, after, restoreRefusal);

            var size = new Vector2(640f, 420f);
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            window.position = new Rect(main.center - size / 2f, size);
            window.minSize = new Vector2(480f, 320f);
            window.ShowModal();
            return _choice;
        }

        private void Build(string subject, string before, string after, string restoreRefusal)
        {
            VisualElement root = rootVisualElement;
            TypecraftLayout.Dialog(root);
            root.style.paddingLeft = root.style.paddingRight = 12;
            root.style.paddingTop = root.style.paddingBottom = 10;

            var header = new Label($"Изменились слова строки {subject}");
            header.AddToClassList("tc-dialog__title");
            root.Add(header);

            var note = new Label("Слова — текст для переводчика, он хранится в таблице. Оформление (стили и теги) хранится в слое разметки и на таблицу не влияет.");
            note.style.whiteSpace = WhiteSpace.Normal;
            note.style.color = new Color(0.65f, 0.65f, 0.65f);
            note.style.marginBottom = 8;
            root.Add(note);

            ScrollView scroll = TypecraftLayout.VerticalScroll();
            scroll.style.flexGrow = 1;
            root.Add(scroll);

            var parts = Decoration.Diff(before, after);
            scroll.Add(Block("Было — в таблице", Decoration.RichDiff(parts, true), new Color(0.55f, 0.2f, 0.2f, 0.25f)));
            scroll.Add(Block("Стало — в редакторе", Decoration.RichDiff(parts, false), new Color(0.2f, 0.5f, 0.25f, 0.25f)));

            if (!string.IsNullOrEmpty(restoreRefusal))
            {
                var refusal = new Label("«Вернуть слова» недоступно: " + restoreRefusal);
                refusal.style.whiteSpace = WhiteSpace.Normal;
                refusal.style.color = new Color(0.9f, 0.62f, 0.25f);
                refusal.style.marginTop = 6;
                root.Add(refusal);
            }

            VisualElement buttons = TypecraftLayout.ButtonRow();
            root.Add(buttons);

            var write = new Button(() => Close(TextChangeChoice.WriteTable)) { text = "Записать в таблицу", tooltip = "Таблица получит новый текст без оформления, слой — оформление" };
            var restore = new Button(() => Close(TextChangeChoice.RestoreWords)) { text = "Вернуть слова", tooltip = "Оставить слова таблицы и сохранить только изменения оформления" };
            restore.SetEnabled(string.IsNullOrEmpty(restoreRefusal));
            var cancel = new Button(() => Close(TextChangeChoice.Cancel)) { text = "Отмена" };
            buttons.Add(write);
            buttons.Add(restore);
            buttons.Add(cancel);

            root.focusable = true;
            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape)
                    Close(TextChangeChoice.Cancel);
            });
        }

        private static VisualElement Block(string title, string richText, Color background)
        {
            var block = new VisualElement();
            block.style.marginBottom = 8;

            var caption = new Label(title);
            caption.style.unityFontStyleAndWeight = FontStyle.Bold;
            caption.style.whiteSpace = WhiteSpace.Normal;
            caption.style.marginBottom = 2;
            block.Add(caption);

            var text = new Label(richText) { enableRichText = true };
            text.style.whiteSpace = WhiteSpace.Normal;
            text.style.fontSize = 15;
            text.style.backgroundColor = background;
            text.style.paddingLeft = text.style.paddingRight = 8;
            text.style.paddingTop = text.style.paddingBottom = 6;
            text.style.borderTopLeftRadius = text.style.borderTopRightRadius = text.style.borderBottomLeftRadius = text.style.borderBottomRightRadius = 4;
            block.Add(text);
            return block;
        }

        private void Close(TextChangeChoice choice)
        {
            _choice = choice;
            Close();
        }
    }
}
