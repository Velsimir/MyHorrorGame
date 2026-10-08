using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Velsi.Typecraft.Editor.Localization
{
    public enum TableExportFormat
    {
        Xlsx,
        Csv,
        Tsv,
        Json
    }

    public sealed class ExportRow
    {
        public string Key;
        public string Comment;
        public bool StaleMarkup;
        public Dictionary<string, string> Values = new Dictionary<string, string>();
    }

    public sealed class ExportTable
    {
        public string Id;
        public List<string> Locales = new List<string>();
        public List<ExportRow> Rows = new List<ExportRow>();

        public IEnumerable<string> Header => new[] { SheetImport.KeyColumn, SheetImport.CommentColumn }.Concat(Locales);

        public IEnumerable<string> Cells(ExportRow row) =>
            new[] { row.Key, row.Comment }.Concat(Locales.Select(locale => row.Values.TryGetValue(locale, out string value) ? value : string.Empty));
    }

    public static class TableExport
    {
        public static string Extension(TableExportFormat format) => format switch
        {
            TableExportFormat.Xlsx => "xlsx",
            TableExportFormat.Tsv => "tsv",
            TableExportFormat.Json => "json",
            _ => "csv"
        };

        public static bool WritesFolder(TableExportFormat format, int tables) =>
            tables > 1 && (format == TableExportFormat.Csv || format == TableExportFormat.Tsv);

        public static ExportTable Collect(ITableBackend backend, string table)
        {
            var export = new ExportTable { Id = table };
            export.Locales.Add(backend.BaseLocale);
            export.Locales.AddRange(backend.Locales.Where(locale => locale != backend.BaseLocale));

            foreach (string key in backend.Keys(table))
            {
                var row = new ExportRow { Key = key, Comment = Normalize(backend.Comment(table, key)) };
                foreach (string locale in export.Locales)
                {
                    row.Values[locale] = Normalize(backend.Get(table, key, locale));
                    row.StaleMarkup |= MarkupLayers.State(backend, table, key, locale) == LayerState.Stale;
                }

                export.Rows.Add(row);
            }

            return export;
        }

        public static List<string> Export(ITableBackend backend, IReadOnlyList<string> tables, TableExportFormat format, string path)
        {
            List<ExportTable> data = tables.Distinct().Select(table => Collect(backend, table)).ToList();
            switch (format)
            {
                case TableExportFormat.Xlsx:
                    XlsxWriter.Write(path, data);
                    return new List<string> { path };
                case TableExportFormat.Json:
                    TableJson.Write(path, data);
                    return new List<string> { path };
                default:
                    char delimiter = format == TableExportFormat.Tsv ? '\t' : ',';
                    if (!WritesFolder(format, data.Count))
                    {
                        TableCsv.Write(data[0], path, delimiter);
                        return new List<string> { path };
                    }

                    Directory.CreateDirectory(path);
                    var written = new List<string>();
                    var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (ExportTable table in data)
                    {
                        string file = Path.Combine(path, UniqueFileName(table.Id, used) + "." + Extension(format));
                        TableCsv.Write(table, file, delimiter);
                        written.Add(file);
                    }

                    return written;
            }
        }

        private static string UniqueFileName(string id, HashSet<string> used)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            string name = new string((id ?? string.Empty).Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
            if (name.Length == 0)
                name = "table";

            string candidate = name;
            for (int index = 2; !used.Add(candidate); index++)
                candidate = $"{name} ({index})";
            return candidate;
        }

        private static string Normalize(string text) => (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
    }
}
