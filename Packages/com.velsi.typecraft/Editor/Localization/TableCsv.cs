using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Velsi.Typecraft.Editor.Localization
{
    public static class TableCsv
    {
        public static char DelimiterFor(string path)
        {
            string extension = Path.GetExtension(path)?.ToLowerInvariant();
            return extension == ".tsv" || extension == ".tab" ? '\t' : ',';
        }

        public static void Export(ITableBackend backend, string table, string path) => Export(backend, table, path, DelimiterFor(path));

        public static void Export(ITableBackend backend, string table, string path, char delimiter) =>
            Write(TableExport.Collect(backend, table), path, delimiter);

        public static void Write(ExportTable table, string path, char delimiter)
        {
            var output = new StringBuilder();
            WriteRow(output, table.Header, delimiter);
            foreach (ExportRow row in table.Rows)
                WriteRow(output, table.Cells(row), delimiter);

            File.WriteAllText(path, output.ToString(), new UTF8Encoding(true));
        }

        public static SheetImport Read(string path) => Read(path, DelimiterFor(path));

        public static SheetImport Read(string path, char delimiter) => SheetImport.FromRows(Parse(ReadShared(path), delimiter));

        public static List<IReadOnlyList<string>> Parse(string text, char delimiter)
        {
            var rows = new List<IReadOnlyList<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            int i = 0;
            while (i < text.Length && text[i] == '﻿')
                i++;

            for (; i < text.Length; i++)
            {
                char c = text[i];

                if (quoted)
                {
                    if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        cell.Append(c);
                    }

                    continue;
                }

                if (c == delimiter)
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                    continue;
                }

                switch (c)
                {
                    case '"' when cell.Length == 0:
                        quoted = true;
                        break;
                    case '\r':
                        break;
                    case '\n':
                        row.Add(cell.ToString());
                        cell.Clear();
                        rows.Add(row);
                        row = new List<string>();
                        break;
                    default:
                        cell.Append(c);
                        break;
                }
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                rows.Add(row);
            }

            return rows;
        }

        internal static string ReadShared(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            return reader.ReadToEnd();
        }

        private static void WriteRow(StringBuilder output, IEnumerable<string> cells, char delimiter)
        {
            output.Append(string.Join(delimiter.ToString(), cells.Select(cell => Quote(cell, delimiter))));
            output.Append("\r\n");
        }

        private static string Quote(string cell, char delimiter)
        {
            cell ??= string.Empty;
            bool needsQuotes = cell.IndexOfAny(new[] { delimiter, '"', '\n', '\r' }) >= 0 || cell.StartsWith(" ") || cell.EndsWith(" ");
            return needsQuotes ? "\"" + cell.Replace("\"", "\"\"") + "\"" : cell;
        }
    }
}
