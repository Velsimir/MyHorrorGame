using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Editor.Window.Layout;

namespace Velsi.Typecraft.Editor.Window.Dialogs
{
    public sealed class TableSyncDialog : EditorWindow
    {
        private const int ListLimit = 200;

        private static readonly Color Warning = new Color(0.9f, 0.62f, 0.25f);
        private static readonly Color Muted = new Color(0.65f, 0.65f, 0.65f);

        private static TableSyncOptions _result;

        private TableSyncPlan _plan;
        private TableSyncOptions _options;
        private VisualElement _content;
        private Button _apply;

        public static TableSyncOptions Show(TableSyncPlan plan)
        {
            _result = null;
            var window = CreateInstance<TableSyncDialog>();
            window.titleContent = new GUIContent("Синхронизация таблицы");
            window.Build(plan);

            var size = new Vector2(760f, 560f);
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            window.position = new Rect(main.center - size / 2f, size);
            window.minSize = new Vector2(520f, 360f);
            window.ShowModal();
            return _result;
        }

        private void Build(TableSyncPlan plan)
        {
            _plan = plan;
            _options = new TableSyncOptions();

            VisualElement root = rootVisualElement;
            TypecraftLayout.Dialog(root);
            root.style.paddingLeft = root.style.paddingRight = 12;
            root.style.paddingTop = root.style.paddingBottom = 10;

            var header = new Label($"Таблица «{plan.Table}»" + (string.IsNullOrEmpty(plan.Origin) ? string.Empty : $" ← {plan.Origin}"));
            header.AddToClassList("tc-dialog__title");
            root.Add(header);

            ScrollView scroll = TypecraftLayout.VerticalScroll();
            scroll.style.flexGrow = 1;
            root.Add(scroll);
            _content = scroll.contentContainer;

            VisualElement buttons = TypecraftLayout.ButtonRow();
            root.Add(buttons);

            _apply = new Button(() => Close(_options)) { text = "Применить" };
            buttons.Add(_apply);
            buttons.Add(new Button(() => Close(null)) { text = "Отмена" });

            root.focusable = true;
            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape)
                    Close(null);
            });

            Rebuild();
        }

        private void Rebuild()
        {
            _content.Clear();
            List<CellChange> changes = _plan.EffectiveChanges(_options).ToList();
            List<CommentChange> comments = _plan.EffectiveComments(_options).ToList();
            List<CellChange> markupLost = changes.Where(change => change.LosesMarkup).ToList();
            int removed = _options.RemoveMissing ? _plan.MissingKeys.Count : 0;

            _content.Add(Note("Будет: " + TableSync.Summary(_plan, _options) + ".", Muted));
            if (markupLost.Count > 0)
                _content.Add(Note($"⚠ Разметка перестанет применяться: {markupLost.Count} строк. Оформление останется в слое, ячейки пометятся ⚠ — «Перенести теги» во вкладке «Локализация» переставит его на новый текст.", Warning));
            if (_plan.DuplicateKeys.Count > 0)
                _content.Add(Note($"Ключи повторяются в источнике, взята последняя строка: {string.Join(", ", _plan.DuplicateKeys.Take(20))}", Warning));
            if (_plan.IgnoredColumns.Count > 0)
                _content.Add(Note($"Пропущены колонки (не код языка): {string.Join(", ", _plan.IgnoredColumns)}", Muted));

            if (_plan.NewLocales.Count > 0)
                _content.Add(Toggle($"Добавить языки: {string.Join(", ", _plan.NewLocales)}", _options.AddLocales, value => _options.AddLocales = value));

            int clears = _plan.Changes.Count(change => change.Clears) + _plan.Comments.Count(change => change.Clears);
            if (clears > 0)
                _content.Add(Toggle($"Стирать строки и комментарии, пустые в источнике ({clears})", _options.ClearEmpty, value => _options.ClearEmpty = value));

            if (_plan.MissingKeys.Count > 0)
            {
                string markup = _plan.MissingKeysWithMarkup.Count > 0 ? $"; у {_plan.MissingKeysWithMarkup.Count} удалится и разметка" : string.Empty;
                _content.Add(Toggle($"Удалить ключи, которых нет в источнике ({_plan.MissingKeys.Count}{markup})", _options.RemoveMissing, value => _options.RemoveMissing = value));
            }

            if (_plan.AddedKeys.Count > 0)
            {
                Foldout added = Section($"Новые ключи ({_plan.AddedKeys.Count})", true);
                foreach (SheetImport.Row row in _plan.AddedKeys.Take(ListLimit))
                {
                    string values = string.Join("   ", _plan.AddedValues(row, _options).Select(pair => $"{pair.Key}: {Line(pair.Value)}"));
                    added.Add(Item($"<b>{Escape(row.Key)}</b>   {Escape(values)}"));
                }
                More(added, _plan.AddedKeys.Count);
            }

            if (markupLost.Count > 0)
            {
                Foldout lost = Section($"⚠ Разметка перестанет применяться ({markupLost.Count})", true);
                foreach (CellChange change in markupLost.Take(ListLimit))
                    lost.Add(Item($"<b>{Escape(change.Key)}</b> [{change.Locale}]"));
                More(lost, markupLost.Count);
            }

            foreach (IGrouping<string, CellChange> locale in changes.GroupBy(change => change.Locale))
            {
                List<CellChange> items = locale.ToList();
                Foldout section = Section($"Изменённые строки · {locale.Key} ({items.Count})", items.Count <= 30);
                foreach (CellChange change in items.Take(ListLimit))
                    section.Add(Change(change));
                More(section, items.Count);
            }

            if (comments.Count > 0)
            {
                Foldout section = Section($"Комментарии ({comments.Count})", false);
                foreach (CommentChange change in comments.Take(ListLimit))
                    section.Add(Item($"<b>{Escape(change.Key)}</b>   {Escape(Line(change.Before))} → {Escape(Line(change.After))}"));
                More(section, comments.Count);
            }

            if (_plan.MissingKeys.Count > 0)
            {
                Foldout section = Section($"Нет в источнике ({_plan.MissingKeys.Count}) — " + (_options.RemoveMissing ? "будут удалены" : "останутся как есть"), false);
                foreach (string key in _plan.MissingKeys.Take(ListLimit))
                    section.Add(Item(Escape(key) + (_plan.MissingKeysWithMarkup.Contains(key) ? "   ✦ есть разметка" : string.Empty)));
                More(section, _plan.MissingKeys.Count);
            }

            bool nothing = changes.Count == 0 && comments.Count == 0 && _plan.AddedKeys.Count == 0 && removed == 0 && !(_options.AddLocales && _plan.NewLocales.Count > 0);
            if (nothing)
                _content.Add(Note("Применять нечего: таблица уже совпадает с источником.", Muted));
            _apply.SetEnabled(!nothing);
        }

        private VisualElement Toggle(string label, bool value, System.Action<bool> changed)
        {
            var toggle = new Toggle(label) { value = value };
            toggle.AddToClassList("tc-toggle-line");
            toggle.style.marginTop = 4;
            toggle.RegisterValueChangedCallback(evt =>
            {
                changed(evt.newValue);
                Rebuild();
            });
            return toggle;
        }

        private Foldout Section(string title, bool open)
        {
            var foldout = new Foldout { text = title, value = open };
            foldout.style.marginTop = 8;
            _content.Add(foldout);
            return foldout;
        }

        private static VisualElement Change(CellChange change)
        {
            var block = new VisualElement();
            block.style.marginBottom = 6;

            string badge = change.LosesMarkup ? "   <color=#E0A43C>⚠ разметка перестанет применяться</color>" : change.NewLocale ? "   <color=#8AF0A0>новый язык</color>" : string.Empty;
            block.Add(Item($"<b>{Escape(change.Key)}</b>{badge}"));

            List<DiffPart> parts = Decoration.Diff(change.Before, change.After);
            block.Add(Item("было: " + (string.IsNullOrEmpty(change.Before) ? "—" : Decoration.RichDiff(parts, true)), new Color(0.55f, 0.2f, 0.2f, 0.2f)));
            block.Add(Item("стало: " + (string.IsNullOrEmpty(change.After) ? "— (строка будет стёрта)" : Decoration.RichDiff(parts, false)), new Color(0.2f, 0.5f, 0.25f, 0.2f)));
            return block;
        }

        private static Label Item(string richText, Color? background = null)
        {
            var label = new Label(richText) { enableRichText = true };
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.paddingLeft = 6;
            if (background.HasValue)
            {
                label.style.backgroundColor = background.Value;
                label.style.paddingTop = label.style.paddingBottom = 2;
                label.style.marginLeft = 12;
            }

            return label;
        }

        private static Label Note(string text, Color color)
        {
            var label = new Label(text) { enableRichText = false };
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = color;
            label.style.marginTop = 6;
            return label;
        }

        private static void More(VisualElement section, int total)
        {
            if (total > ListLimit)
                section.Add(Item($"… и ещё {total - ListLimit}"));
        }

        private static string Line(string text)
        {
            string line = (text ?? string.Empty).Replace("\r", string.Empty).Replace('\n', '⏎');
            return line.Length <= 80 ? line : line.Substring(0, 80) + "…";
        }

        private static string Escape(string text) => "<noparse>" + (text ?? string.Empty).Replace("</noparse>", "</ noparse>") + "</noparse>";

        private void Close(TableSyncOptions result)
        {
            _result = result;
            Close();
        }
    }
}
