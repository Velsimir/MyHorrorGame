using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Velsi.Typecraft.Editor.Localization
{
    public sealed class SheetImport
    {
        public const string KeyColumn = "key";
        public const string CommentColumn = "comment";

        private static readonly Regex LocaleCode = new Regex(@"^[A-Za-z]{2,3}([-_][A-Za-z0-9]{2,8})*$");

        public List<string> Locales { get; } = new List<string>();
        public List<Row> Rows { get; } = new List<Row>();
        public List<string> IgnoredColumns { get; } = new List<string>();
        public List<string> DuplicateKeys { get; } = new List<string>();
        public bool HasComments { get; private set; }

        public static SheetImport FromRows(IReadOnlyList<IReadOnlyList<string>> rows)
        {
            int headerIndex = 0;
            while (headerIndex < rows.Count && rows[headerIndex].All(string.IsNullOrWhiteSpace))
                headerIndex++;
            if (headerIndex == rows.Count)
                throw new FormatException("Источник пустой");

            List<string> header = rows[headerIndex].Select(cell => (cell ?? string.Empty).Trim()).ToList();
            int keyIndex = header.FindIndex(cell => string.Equals(cell, KeyColumn, StringComparison.OrdinalIgnoreCase));
            if (keyIndex < 0)
                throw new FormatException("В первой строке нет колонки «key»");

            int commentIndex = header.FindIndex(cell => string.Equals(cell, CommentColumn, StringComparison.OrdinalIgnoreCase));
            var import = new SheetImport { HasComments = commentIndex >= 0 };
            var localeColumns = new List<(int Column, string Locale)>();

            for (int column = 0; column < header.Count; column++)
            {
                string name = header[column];
                if (column == keyIndex || column == commentIndex || name.Length == 0)
                    continue;

                if (!LocaleCode.IsMatch(name))
                {
                    import.IgnoredColumns.Add(name);
                    continue;
                }

                if (import.Locales.Contains(name, StringComparer.OrdinalIgnoreCase))
                    throw new FormatException($"Колонка языка «{name}» встречается дважды");

                import.Locales.Add(name);
                localeColumns.Add((column, name));
            }

            var byKey = new Dictionary<string, Row>(StringComparer.Ordinal);
            for (int index = headerIndex + 1; index < rows.Count; index++)
            {
                IReadOnlyList<string> cells = rows[index];
                string key = Cell(cells, keyIndex).Trim();
                if (key.Length == 0)
                    continue;

                var row = new Row { Key = key, Line = index + 1, Comment = commentIndex >= 0 ? Normalize(Cell(cells, commentIndex)) : null };
                foreach ((int column, string locale) in localeColumns)
                    row.Values[locale] = Normalize(Cell(cells, column));

                if (byKey.TryGetValue(key, out Row previous))
                {
                    if (!import.DuplicateKeys.Contains(key))
                        import.DuplicateKeys.Add(key);
                    import.Rows[import.Rows.IndexOf(previous)] = row;
                }
                else
                {
                    import.Rows.Add(row);
                }

                byKey[key] = row;
            }

            return import;
        }

        private static string Cell(IReadOnlyList<string> row, int index) => index >= 0 && index < row.Count ? row[index] ?? string.Empty : string.Empty;

        private static string Normalize(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');

        public sealed class Row
        {
            public string Key;
            public string Comment;
            public int Line;
            public Dictionary<string, string> Values = new Dictionary<string, string>();
        }
    }
}
