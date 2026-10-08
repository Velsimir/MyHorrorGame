using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Velsi.Typecraft.Editor.Localization
{
    public static class XlsxReader
    {
        private static readonly Regex EscapedChar = new Regex("_x([0-9A-Fa-f]{4})_");

        public static IReadOnlyList<string> SheetNames(string path)
        {
            using ZipArchive zip = Open(path);
            return Sheets(zip).Select(sheet => sheet.Name).ToList();
        }

        public static List<IReadOnlyList<string>> Read(string path, string sheet = null)
        {
            using ZipArchive zip = Open(path);
            List<(string Name, string Part)> sheets = Sheets(zip);
            if (sheets.Count == 0)
                throw new FormatException("В книге нет листов");

            (string Name, string Part) target = string.IsNullOrEmpty(sheet)
                ? sheets[0]
                : sheets.FirstOrDefault(candidate => candidate.Name == sheet);
            if (target.Part == null)
                throw new FormatException($"В книге нет листа «{sheet}». Листы: {string.Join(", ", sheets.Select(candidate => candidate.Name))}");

            List<string> shared = SharedStrings(zip);
            XDocument document = Load(zip, target.Part) ?? throw new FormatException($"Не найдена часть листа «{target.Name}»: {target.Part}");
            return Rows(document, shared);
        }

        public static int ColumnIndex(string reference)
        {
            int column = 0;
            foreach (char c in reference)
            {
                char upper = char.ToUpperInvariant(c);
                if (upper < 'A' || upper > 'Z')
                    break;
                column = column * 26 + (upper - 'A' + 1);
            }

            return column - 1;
        }

        public static int RowIndex(string reference)
        {
            int digits = 0;
            while (digits < reference.Length && !char.IsDigit(reference[digits]))
                digits++;
            return int.TryParse(reference.Substring(digits), NumberStyles.None, CultureInfo.InvariantCulture, out int row) ? row - 1 : -1;
        }

        private static ZipArchive Open(string path)
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            try
            {
                return new ZipArchive(stream, ZipArchiveMode.Read, false);
            }
            catch (InvalidDataException)
            {
                stream.Dispose();
                throw new FormatException("Файл не похож на .xlsx (это не zip-архив). Старый формат .xls не поддерживается — пересохрани книгу как .xlsx");
            }
        }

        private static List<(string Name, string Part)> Sheets(ZipArchive zip)
        {
            XDocument workbook = Load(zip, "xl/workbook.xml") ?? throw new FormatException("В архиве нет xl/workbook.xml — это не книга Excel");
            Dictionary<string, string> targets = Relationships(zip, "xl/_rels/workbook.xml.rels", "xl/");

            var sheets = new List<(string, string)>();
            foreach (XElement sheet in workbook.Descendants().Where(element => element.Name.LocalName == "sheet"))
            {
                string name = (string)sheet.Attribute("name") ?? string.Empty;
                string id = sheet.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "id" && attribute.Name.NamespaceName.Length > 0)?.Value;
                if (id != null && targets.TryGetValue(id, out string part))
                    sheets.Add((name, part));
            }

            return sheets;
        }

        private static Dictionary<string, string> Relationships(ZipArchive zip, string relsPath, string baseFolder)
        {
            var targets = new Dictionary<string, string>();
            XDocument rels = Load(zip, relsPath);
            if (rels == null)
                return targets;

            foreach (XElement relationship in rels.Descendants().Where(element => element.Name.LocalName == "Relationship"))
            {
                string id = (string)relationship.Attribute("Id");
                string target = (string)relationship.Attribute("Target");
                if (id != null && target != null)
                    targets[id] = ResolvePart(baseFolder, target);
            }

            return targets;
        }

        private static string ResolvePart(string baseFolder, string target)
        {
            string combined = target.StartsWith("/") ? target.TrimStart('/') : baseFolder + target;
            var parts = new List<string>();
            foreach (string piece in combined.Replace('\\', '/').Split('/'))
            {
                if (piece == "..")
                {
                    if (parts.Count > 0)
                        parts.RemoveAt(parts.Count - 1);
                }
                else if (piece.Length > 0 && piece != ".")
                {
                    parts.Add(piece);
                }
            }

            return string.Join("/", parts);
        }

        private static List<string> SharedStrings(ZipArchive zip)
        {
            var strings = new List<string>();
            XDocument document = Load(zip, "xl/sharedStrings.xml");
            if (document?.Root == null)
                return strings;

            foreach (XElement item in document.Root.Elements().Where(element => element.Name.LocalName == "si"))
                strings.Add(RichText(item));
            return strings;
        }

        private static string RichText(XElement container)
        {
            var text = new StringBuilder();
            foreach (XElement child in container.Elements())
            {
                if (child.Name.LocalName == "t")
                    text.Append(child.Value);
                else if (child.Name.LocalName == "r")
                    text.Append(string.Concat(child.Elements().Where(run => run.Name.LocalName == "t").Select(run => run.Value)));
            }

            return Unescape(text.ToString());
        }

        private static List<IReadOnlyList<string>> Rows(XDocument document, List<string> shared)
        {
            var rows = new List<IReadOnlyList<string>>();
            XElement sheetData = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "sheetData");
            if (sheetData == null)
                return rows;

            foreach (XElement row in sheetData.Elements().Where(element => element.Name.LocalName == "row"))
            {
                int rowIndex = int.TryParse((string)row.Attribute("r"), NumberStyles.None, CultureInfo.InvariantCulture, out int number) ? number - 1 : rows.Count;
                while (rows.Count < rowIndex)
                    rows.Add(new List<string>());

                var cells = new List<string>();
                foreach (XElement cell in row.Elements().Where(element => element.Name.LocalName == "c"))
                {
                    string reference = (string)cell.Attribute("r");
                    int column = string.IsNullOrEmpty(reference) ? cells.Count : ColumnIndex(reference);
                    if (column < 0)
                        column = cells.Count;

                    while (cells.Count <= column)
                        cells.Add(string.Empty);
                    cells[column] = Value(cell, shared);
                }

                if (rowIndex < rows.Count)
                    rows[rowIndex] = cells;
                else
                    rows.Add(cells);
            }

            return rows;
        }

        private static string Value(XElement cell, List<string> shared)
        {
            string type = (string)cell.Attribute("t");
            string raw = cell.Elements().FirstOrDefault(element => element.Name.LocalName == "v")?.Value;

            switch (type)
            {
                case "s":
                    return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) && index >= 0 && index < shared.Count ? shared[index] : string.Empty;
                case "inlineStr":
                    XElement inline = cell.Elements().FirstOrDefault(element => element.Name.LocalName == "is");
                    return inline != null ? RichText(inline) : string.Empty;
                case "b":
                    return raw == "1" ? "TRUE" : raw == "0" ? "FALSE" : raw ?? string.Empty;
                case "str":
                    return Unescape(raw ?? string.Empty);
                default:
                    return raw ?? string.Empty;
            }
        }

        private static string Unescape(string text) =>
            text.IndexOf("_x", StringComparison.Ordinal) < 0
                ? text
                : EscapedChar.Replace(text, match => ((char)int.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToString());

        private static XDocument Load(ZipArchive zip, string part)
        {
            ZipArchiveEntry entry = zip.GetEntry(part)
                                    ?? zip.Entries.FirstOrDefault(candidate => string.Equals(candidate.FullName.TrimStart('/').Replace('\\', '/'), part, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
                return null;

            using Stream stream = entry.Open();
            return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        }
    }
}
