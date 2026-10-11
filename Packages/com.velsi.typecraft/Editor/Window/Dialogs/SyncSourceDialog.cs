using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Editor.Window.Layout;

namespace Velsi.Typecraft.Editor.Window.Dialogs
{
    public enum SyncSourceChoice
    {
        Cancel,
        Saved,
        SavedAndSync,
        Removed
    }

    public sealed class SyncSourceDialog : EditorWindow
    {
        private const string FirstSheet = "(первый лист)";

        private static SyncSourceChoice _choice;

        private string _table;
        private EnumField _kind;
        private VisualElement _fileRow;
        private TextField _path;
        private DropdownField _sheet;
        private TextField _url;
        private Label _hint;
        private Button _save;
        private Button _saveAndSync;

        public static SyncSourceChoice Show(string table)
        {
            _choice = SyncSourceChoice.Cancel;
            var window = CreateInstance<SyncSourceDialog>();
            window.titleContent = new GUIContent("Источник синхронизации");
            window.Build(table, TableSyncSources.Find(table));

            var size = new Vector2(620f, 250f);
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            window.position = new Rect(main.center - size / 2f, size);
            window.minSize = new Vector2(480f, 230f);
            window.ShowModal();
            return _choice;
        }

        private void Build(string table, TableSyncSource current)
        {
            _table = table;
            VisualElement root = rootVisualElement;
            TypecraftLayout.Dialog(root);
            root.style.paddingLeft = root.style.paddingRight = 12;
            root.style.paddingTop = root.style.paddingBottom = 10;

            var header = new Label($"Откуда синхронизировать таблицу «{table}»");
            header.AddToClassList("tc-dialog__title");
            root.Add(header);

            var note = new Label("Первая строка — заголовки: key, необязательная comment, дальше коды языков (en, ru, zh-Hans…). Настройка хранится в ProjectSettings/TypecraftTableSync.asset.");
            note.style.whiteSpace = WhiteSpace.Normal;
            note.style.color = new Color(0.65f, 0.65f, 0.65f);
            note.style.marginBottom = 8;
            root.Add(note);

            _kind = new EnumField("Тип", current?.Kind ?? TableSyncKind.GoogleSheets);
            _kind.RegisterValueChangedCallback(_ => Refresh());
            root.Add(_kind);

            _fileRow = new VisualElement();
            _fileRow.AddToClassList("tc-lib__row");
            _path = new TextField("Файл") { value = current?.Path ?? string.Empty, isDelayed = true };
            _path.RegisterValueChangedCallback(_ => Refresh());
            _fileRow.Add(_path);
            _fileRow.Add(new Button(Browse) { text = "…", tooltip = "Выбрать файл" });
            root.Add(_fileRow);

            _sheet = new DropdownField("Лист", new List<string> { FirstSheet }, 0);
            if (!string.IsNullOrEmpty(current?.Sheet))
            {
                _sheet.choices = new List<string> { FirstSheet, current.Sheet };
                _sheet.SetValueWithoutNotify(current.Sheet);
            }
            root.Add(_sheet);

            _url = new TextField("Ссылка") { value = current?.Url ?? string.Empty };
            _url.RegisterValueChangedCallback(_ => Refresh());
            root.Add(_url);

            _hint = new Label();
            _hint.style.whiteSpace = WhiteSpace.Normal;
            _hint.style.marginTop = 4;
            root.Add(_hint);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            root.Add(spacer);

            VisualElement buttons = TypecraftLayout.ButtonRow();
            root.Add(buttons);

            if (current != null)
                buttons.Add(new Button(() => Finish(SyncSourceChoice.Removed)) { text = "Убрать источник" });
            _saveAndSync = new Button(() => Finish(SyncSourceChoice.SavedAndSync)) { text = "Сохранить и синхронизировать" };
            _save = new Button(() => Finish(SyncSourceChoice.Saved)) { text = "Сохранить" };
            buttons.Add(_saveAndSync);
            buttons.Add(_save);
            buttons.Add(new Button(() => Finish(SyncSourceChoice.Cancel)) { text = "Отмена" });

            root.focusable = true;
            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape)
                    Finish(SyncSourceChoice.Cancel);
            });

            Refresh();
        }

        private TableSyncKind Kind => (TableSyncKind)_kind.value;

        private void Refresh()
        {
            bool google = Kind == TableSyncKind.GoogleSheets;
            _fileRow.style.display = google ? DisplayStyle.None : DisplayStyle.Flex;
            _sheet.style.display = Kind == TableSyncKind.Xlsx ? DisplayStyle.Flex : DisplayStyle.None;
            _url.style.display = google ? DisplayStyle.Flex : DisplayStyle.None;

            string error = google ? CheckUrl() : CheckFile();
            bool valid = error == null;
            _hint.style.color = valid ? new Color(0.65f, 0.65f, 0.65f) : new Color(0.9f, 0.62f, 0.25f);
            _hint.text = valid ? Describe() : error;
            _save.SetEnabled(valid);
            _saveAndSync.SetEnabled(valid);
        }

        private string Describe()
        {
            if (Kind != TableSyncKind.GoogleSheets)
                return "Файл найден.";

            GoogleSheets.TryParse(_url.value, out GoogleSheetLink link);
            return $"Таблица {link.Id.Substring(0, Math.Min(8, link.Id.Length))}…, лист gid={link.Gid}" + (link.Published ? " (опубликованная)" : string.Empty)
                   + ". Нужен доступ «Все, у кого есть ссылка — читатель». Лист берётся по gid из ссылки — открой нужный лист и скопируй адрес.";
        }

        private string CheckUrl() =>
            GoogleSheets.TryParse(_url.value, out _) ? null : "Вставь ссылку вида https://docs.google.com/spreadsheets/d/<SHEET_ID>/edit#gid=<GID>";

        private string CheckFile()
        {
            string path = FullPath(_path.value);
            if (string.IsNullOrEmpty(path))
                return "Выбери файл.";
            if (!File.Exists(path))
                return $"Файл не найден: {path}";
            if (Kind != TableSyncKind.Xlsx)
                return null;

            try
            {
                string selected = _sheet.value;
                List<string> choices = new List<string> { FirstSheet }.Concat(XlsxReader.SheetNames(path)).ToList();
                _sheet.choices = choices;
                _sheet.SetValueWithoutNotify(choices.Contains(selected) ? selected : FirstSheet);
                return null;
            }
            catch (Exception exception)
            {
                return $"Не удалось открыть книгу: {exception.Message}";
            }
        }

        private void Browse()
        {
            string current = FullPath(_path.value);
            string folder = string.IsNullOrEmpty(current) ? string.Empty : Path.GetDirectoryName(current);
            string path = EditorUtility.OpenFilePanelWithFilters("Файл таблицы", folder, new[] { "Таблицы", "csv,tsv,xlsx", "Все файлы", "*" });
            if (string.IsNullOrEmpty(path))
                return;

            _kind.SetValueWithoutNotify(TableSyncSource.KindFor(path));
            _path.SetValueWithoutNotify(new TableSyncSource(_table, Kind, path, null, null).Path);
            Refresh();
        }

        private static string FullPath(string path) =>
            string.IsNullOrWhiteSpace(path) ? string.Empty : new TableSyncSource(null, TableSyncKind.Csv, path.Trim(), null, null).FullPath;

        private void Finish(SyncSourceChoice choice)
        {
            if (choice == SyncSourceChoice.Removed)
            {
                TableSyncSources.Remove(_table);
            }
            else if (choice != SyncSourceChoice.Cancel)
            {
                bool google = Kind == TableSyncKind.GoogleSheets;
                string sheet = _sheet.value == FirstSheet ? null : _sheet.value;
                TableSyncSources.Set(new TableSyncSource(_table, Kind, google ? null : _path.value.Trim(), Kind == TableSyncKind.Xlsx ? sheet : null, google ? _url.value.Trim() : null));
            }

            _choice = choice;
            Close();
        }
    }
}
