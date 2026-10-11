using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Editor.Window.Layout;

namespace Velsi.Typecraft.Editor.Window.Dialogs
{
    public sealed class ExportDialog : EditorWindow
    {
        private const string FormatPref = "Velsi.Typecraft.Export.Format";
        private const string ScopePref = "Velsi.Typecraft.Export.AllTables";
        private const string FolderPref = "Velsi.Typecraft.Export.Folder";
        private const string XlsxNote = "Шапка закреплена, пустой перевод подсвечен красным, строка с устаревшей разметкой (⚠) — оранжевым. Оформление разметки в файл не попадает.";

        private static readonly (TableExportFormat Format, string Title)[] Formats =
        {
            (TableExportFormat.Xlsx, "Excel (.xlsx)"),
            (TableExportFormat.Csv, "CSV"),
            (TableExportFormat.Tsv, "TSV"),
            (TableExportFormat.Json, "JSON")
        };

        private ITableBackend _backend;
        private string _table;
        private DropdownField _format;
        private RadioButtonGroup _scope;
        private Label _hint;

        public static void Show(ITableBackend backend, string table)
        {
            var window = CreateInstance<ExportDialog>();
            window.titleContent = new GUIContent("Экспорт таблиц");
            window.Build(backend, table);

            var size = new Vector2(480f, 220f);
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            window.position = new Rect(main.center - size / 2f, size);
            window.minSize = new Vector2(400f, 200f);
            window.ShowModal();
        }

        private TableExportFormat Format => Formats[Mathf.Clamp(_format.index, 0, Formats.Length - 1)].Format;
        private bool AllTables => _scope.value == 1;
        private IReadOnlyList<string> Tables => AllTables ? _backend.Tables.Distinct().ToList() : new List<string> { _table };

        private void Build(ITableBackend backend, string table)
        {
            _backend = backend;
            _table = table;

            VisualElement root = rootVisualElement;
            TypecraftLayout.Dialog(root);
            root.style.paddingLeft = root.style.paddingRight = 12;
            root.style.paddingTop = root.style.paddingBottom = 10;

            var header = new Label("Выгрузить строки таблиц");
            header.AddToClassList("tc-dialog__title");
            header.style.marginBottom = 6;
            root.Add(header);

            int format = Mathf.Clamp(EditorPrefs.GetInt(FormatPref, 0), 0, Formats.Length - 1);
            _format = new DropdownField("Формат", Formats.Select(entry => entry.Title).ToList(), format);
            _format.RegisterValueChangedCallback(_ => Refresh());
            root.Add(_format);

            int tables = backend.Tables.Distinct().Count();
            _scope = new RadioButtonGroup("Что выгрузить", new List<string> { $"Текущая таблица «{table}»", $"Все таблицы ({tables})" })
            {
                value = EditorPrefs.GetBool(ScopePref, false) && tables > 1 ? 1 : 0
            };
            _scope.SetEnabled(tables > 1);
            _scope.RegisterValueChangedCallback(_ => Refresh());
            root.Add(_scope);

            _hint = new Label();
            _hint.style.whiteSpace = WhiteSpace.Normal;
            _hint.style.color = new Color(0.65f, 0.65f, 0.65f);
            _hint.style.marginTop = 6;
            root.Add(_hint);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            root.Add(spacer);

            VisualElement buttons = TypecraftLayout.ButtonRow();
            root.Add(buttons);
            buttons.Add(new Button(Export) { text = "Экспорт…" });
            buttons.Add(new Button(Close) { text = "Отмена" });

            root.focusable = true;
            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape)
                    Close();
            });

            Refresh();
        }

        private void Refresh()
        {
            _hint.text = Format switch
            {
                TableExportFormat.Xlsx => AllTables ? "Одна книга, лист на таблицу (имя листа — id таблицы). " + XlsxNote : XlsxNote,
                TableExportFormat.Json => "Один файл UTF-8: { \"tables\": [ { \"id\", \"locales\", \"entries\": [ { \"key\", \"comment\", \"values\" } ] } ] }.",
                _ => (AllTables ? "Папка: по файлу на таблицу, имя файла — id таблицы. " : string.Empty) + "UTF-8, колонки key, comment и языки — файл читается обратно синхронизацией."
            };
        }

        private void Export()
        {
            TableExportFormat format = Format;
            IReadOnlyList<string> tables = Tables;
            string extension = TableExport.Extension(format);
            string folder = EditorPrefs.GetString(FolderPref, string.Empty);
            string path = TableExport.WritesFolder(format, tables.Count)
                ? EditorUtility.SaveFolderPanel("Папка для таблиц", folder, string.Empty)
                : EditorUtility.SaveFilePanel("Экспорт таблиц", folder, (AllTables ? "Tables" : _table) + "." + extension, extension);
            if (string.IsNullOrEmpty(path))
                return;

            EditorPrefs.SetInt(FormatPref, _format.index);
            EditorPrefs.SetBool(ScopePref, AllTables);
            EditorPrefs.SetString(FolderPref, TableExport.WritesFolder(format, tables.Count) ? path : Path.GetDirectoryName(path));

            List<string> written;
            try
            {
                written = TableExport.Export(_backend, tables, format, path);
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Экспорт", $"Не удалось выгрузить: {exception.Message}", "OK");
                return;
            }

            TypecraftLog.Info($"Выгружено таблиц: {tables.Count} → {string.Join(", ", written)}");
            Close();
        }
    }
}
