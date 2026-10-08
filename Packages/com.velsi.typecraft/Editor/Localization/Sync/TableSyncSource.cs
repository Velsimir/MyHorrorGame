using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Velsi.Typecraft.Editor.Localization
{
    public enum TableSyncKind
    {
        Csv,
        Tsv,
        Xlsx,
        GoogleSheets
    }

    [Serializable]
    public sealed class TableSyncSource
    {
        [SerializeField] private string _table;
        [SerializeField] private TableSyncKind _kind;
        [SerializeField] private string _path;
        [SerializeField] private string _sheet;
        [SerializeField] private string _url;

        public TableSyncSource(string table, TableSyncKind kind, string path, string sheet, string url)
        {
            _table = table;
            _kind = kind;
            _path = StoredPath(path);
            _sheet = sheet;
            _url = url;
        }

        public string Table => _table;
        public TableSyncKind Kind => _kind;
        public string Path => _path;
        public string Sheet => _sheet;
        public string Url => _url;
        public bool IsFile => _kind != TableSyncKind.GoogleSheets;
        public string FullPath => string.IsNullOrEmpty(_path) ? string.Empty : System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectRoot, _path));

        public string Describe()
        {
            switch (_kind)
            {
                case TableSyncKind.Xlsx:
                    return $"Excel: {_path}" + (string.IsNullOrEmpty(_sheet) ? string.Empty : $" · лист «{_sheet}»");
                case TableSyncKind.GoogleSheets:
                    return GoogleSheets.TryParse(_url, out GoogleSheetLink link) ? $"Google Sheets · gid={link.Gid}" : "Google Sheets · ссылка не распознана";
                default:
                    return $"{_kind.ToString().ToUpperInvariant()}: {_path}";
            }
        }

        public SheetImport ReadFile()
        {
            string path = FullPath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new TableSyncException($"Файл не найден: {(string.IsNullOrEmpty(_path) ? "путь не задан" : path)}");

            switch (_kind)
            {
                case TableSyncKind.Csv:
                    return TableCsv.Read(path, ',');
                case TableSyncKind.Tsv:
                    return TableCsv.Read(path, '\t');
                case TableSyncKind.Xlsx:
                    return SheetImport.FromRows(XlsxReader.Read(path, _sheet));
                default:
                    throw new TableSyncException("Google Sheets загружается асинхронно, а не как файл");
            }
        }

        public static TableSyncKind KindFor(string path)
        {
            string extension = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
            return extension == ".xlsx" ? TableSyncKind.Xlsx : extension == ".tsv" || extension == ".tab" ? TableSyncKind.Tsv : TableSyncKind.Csv;
        }

        private static string ProjectRoot => System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));

        private static string StoredPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            string full = System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectRoot, path)).Replace('\\', '/');
            string root = ProjectRoot.Replace('\\', '/').TrimEnd('/') + "/";
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full.Substring(root.Length) : full;
        }
    }

    [FilePath("ProjectSettings/TypecraftTableSync.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class TableSyncSources : ScriptableSingleton<TableSyncSources>
    {
        [SerializeField] private List<TableSyncSource> _sources = new List<TableSyncSource>();

        public static TableSyncSource Find(string table) =>
            string.IsNullOrEmpty(table) ? null : instance._sources.Find(source => source != null && source.Table == table);

        public static void Set(TableSyncSource source)
        {
            if (source == null || string.IsNullOrEmpty(source.Table))
                return;

            instance._sources.RemoveAll(existing => existing == null || existing.Table == source.Table);
            instance._sources.Add(source);
            instance.Save(true);
        }

        public static void Remove(string table)
        {
            if (instance._sources.RemoveAll(existing => existing == null || existing.Table == table) > 0)
                instance.Save(true);
        }
    }
}
