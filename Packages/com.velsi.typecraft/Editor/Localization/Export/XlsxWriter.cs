using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Velsi.Typecraft.Editor.Localization
{
    public static class XlsxWriter
    {
        private const string MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private const string PackageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";
        private const string ContentTypesNs = "http://schemas.openxmlformats.org/package/2006/content-types";
        private const string XmlNs = "http://www.w3.org/XML/1998/namespace";
        private const int MaxSheetName = 31;
        private const double MinWidth = 10;
        private const double MaxWidth = 80;

        private const int StyleHeader = 1;
        private const int StyleText = 2;
        private const int StyleMissing = 3;
        private const int StyleStale = 4;

        private static readonly char[] ForbiddenSheetChars = { ':', '\\', '/', '?', '*', '[', ']' };
        private static readonly Regex EscapeLike = new Regex("_(x[0-9A-Fa-f]{4})_");

        public static void Write(string path, IReadOnlyList<ExportTable> tables)
        {
            if (tables.Count == 0)
                throw new ArgumentException("Нечего выгружать: нет таблиц", nameof(tables));

            List<string> names = SheetNames(tables.Select(table => table.Id));
            var strings = new SharedStrings();
            List<byte[]> sheets = tables.Select((table, index) => Sheet(table, strings, index == 0)).ToList();

            string temp = path + ".tmp";
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Add(zip, "[Content_Types].xml", ContentTypes(sheets.Count));
                Add(zip, "_rels/.rels", RootRels());
                Add(zip, "xl/workbook.xml", Workbook(names));
                Add(zip, "xl/_rels/workbook.xml.rels", WorkbookRels(sheets.Count));
                Add(zip, "xl/styles.xml", Styles());
                Add(zip, "xl/sharedStrings.xml", strings.Xml());
                for (int i = 0; i < sheets.Count; i++)
                    Add(zip, $"xl/worksheets/sheet{i + 1}.xml", sheets[i]);
            }

            if (File.Exists(path))
                File.Delete(path);
            File.Move(temp, path);
        }

        public static List<string> SheetNames(IEnumerable<string> ids)
        {
            var names = new List<string>();
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string id in ids)
            {
                string name = new string((id ?? string.Empty).Select(c => Array.IndexOf(ForbiddenSheetChars, c) >= 0 || char.IsControl(c) ? '_' : c).ToArray()).Trim('\'').Trim();
                if (name.Length == 0 || string.Equals(name, "History", StringComparison.OrdinalIgnoreCase))
                    name = "Sheet" + (names.Count + 1);
                name = Truncate(name, MaxSheetName);

                string candidate = name;
                for (int index = 2; !used.Add(candidate); index++)
                {
                    string suffix = $" ({index})";
                    candidate = Truncate(name, MaxSheetName - suffix.Length).TrimEnd('\'') + suffix;
                }

                names.Add(candidate);
            }

            return names;
        }

        private static string Truncate(string text, int length)
        {
            if (text.Length <= length)
                return text;
            int cut = length;
            if (char.IsHighSurrogate(text[cut - 1]))
                cut--;
            return text.Substring(0, cut);
        }

        private static byte[] Sheet(ExportTable table, SharedStrings strings, bool selected)
        {
            List<string> header = table.Header.ToList();
            int columns = header.Count;
            int rows = table.Rows.Count + 1;
            double[] widths = header.Select(Width).ToArray();
            foreach (ExportRow row in table.Rows)
            {
                int column = 0;
                foreach (string cell in table.Cells(row))
                    widths[column] = Math.Max(widths[column++], Width(cell));
            }

            return Xml(writer =>
            {
                writer.WriteStartElement("worksheet", MainNs);
                writer.WriteAttributeString("xmlns", "r", null, RelNs);

                writer.WriteStartElement("dimension", MainNs);
                writer.WriteAttributeString("ref", "A1:" + Reference(columns - 1, rows - 1));
                writer.WriteEndElement();

                writer.WriteStartElement("sheetViews", MainNs);
                writer.WriteStartElement("sheetView", MainNs);
                if (selected)
                    writer.WriteAttributeString("tabSelected", "1");
                writer.WriteAttributeString("workbookViewId", "0");
                writer.WriteStartElement("pane", MainNs);
                writer.WriteAttributeString("ySplit", "1");
                writer.WriteAttributeString("topLeftCell", "A2");
                writer.WriteAttributeString("activePane", "bottomLeft");
                writer.WriteAttributeString("state", "frozen");
                writer.WriteEndElement();
                writer.WriteStartElement("selection", MainNs);
                writer.WriteAttributeString("pane", "bottomLeft");
                writer.WriteAttributeString("activeCell", "A2");
                writer.WriteAttributeString("sqref", "A2");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("sheetFormatPr", MainNs);
                writer.WriteAttributeString("defaultRowHeight", "15");
                writer.WriteEndElement();

                writer.WriteStartElement("cols", MainNs);
                for (int i = 0; i < columns; i++)
                {
                    writer.WriteStartElement("col", MainNs);
                    writer.WriteAttributeString("min", (i + 1).ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("max", (i + 1).ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("width", widths[i].ToString("0.##", CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("customWidth", "1");
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                writer.WriteStartElement("sheetData", MainNs);
                WriteRow(writer, strings, 0, header, _ => StyleHeader);
                for (int r = 0; r < table.Rows.Count; r++)
                {
                    ExportRow row = table.Rows[r];
                    WriteRow(writer, strings, r + 1, table.Cells(row).ToList(), column => CellStyle(row, column, table));
                }
                writer.WriteEndElement();

                writer.WriteEndElement();
            });
        }

        private static int CellStyle(ExportRow row, int column, ExportTable table)
        {
            int localeIndex = column - 2;
            if (localeIndex >= 0 && string.IsNullOrEmpty(row.Values.TryGetValue(table.Locales[localeIndex], out string value) ? value : null))
                return StyleMissing;
            return row.StaleMarkup ? StyleStale : StyleText;
        }

        private static void WriteRow(XmlWriter writer, SharedStrings strings, int rowIndex, IReadOnlyList<string> cells, Func<int, int> style)
        {
            writer.WriteStartElement("row", MainNs);
            writer.WriteAttributeString("r", (rowIndex + 1).ToString(CultureInfo.InvariantCulture));
            for (int column = 0; column < cells.Count; column++)
            {
                writer.WriteStartElement("c", MainNs);
                writer.WriteAttributeString("r", Reference(column, rowIndex));
                writer.WriteAttributeString("s", style(column).ToString(CultureInfo.InvariantCulture));
                string text = cells[column];
                if (!string.IsNullOrEmpty(text))
                {
                    writer.WriteAttributeString("t", "s");
                    writer.WriteElementString("v", MainNs, strings.Index(text).ToString(CultureInfo.InvariantCulture));
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private static double Width(string text)
        {
            if (string.IsNullOrEmpty(text))
                return MinWidth;

            double longest = text.Split('\n').Max(line => line.Sum(c => c >= 0x1100 && !char.IsSurrogate(c) && IsWide(c) ? 2.0 : char.IsLowSurrogate(c) ? 0.0 : 1.0));
            return Math.Max(MinWidth, Math.Min(MaxWidth, longest + 2));
        }

        private static bool IsWide(char c) =>
            (c >= 0x1100 && c <= 0x115F) || (c >= 0x2E80 && c <= 0xA4CF) || (c >= 0xAC00 && c <= 0xD7A3) || (c >= 0xF900 && c <= 0xFAFF) || (c >= 0xFE30 && c <= 0xFE4F) || (c >= 0xFF00 && c <= 0xFF60) || (c >= 0xFFE0 && c <= 0xFFE6);

        public static string Reference(int column, int row)
        {
            var letters = new StringBuilder();
            for (int value = column + 1; value > 0; value = (value - 1) / 26)
                letters.Insert(0, (char)('A' + (value - 1) % 26));
            return letters + (row + 1).ToString(CultureInfo.InvariantCulture);
        }

        private static string Escape(string text)
        {
            string escaped = EscapeLike.Replace(text, "_x005F_$1_");
            var output = new StringBuilder(escaped.Length);
            for (int i = 0; i < escaped.Length; i++)
            {
                char c = escaped[i];
                bool lone = char.IsHighSurrogate(c)
                    ? i + 1 >= escaped.Length || !char.IsLowSurrogate(escaped[i + 1])
                    : char.IsLowSurrogate(c) && (i == 0 || !char.IsHighSurrogate(escaped[i - 1]));
                if ((c < 0x20 && c != '\t' && c != '\n') || c == 0xFFFE || c == 0xFFFF || lone)
                    output.Append("_x").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture)).Append('_');
                else
                    output.Append(c);
            }

            return output.ToString();
        }

        private static byte[] ContentTypes(int sheets) => Xml(writer =>
        {
            writer.WriteStartElement("Types", ContentTypesNs);
            Default(writer, "rels", "application/vnd.openxmlformats-package.relationships+xml");
            Default(writer, "xml", "application/xml");
            Override(writer, "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
            for (int i = 1; i <= sheets; i++)
                Override(writer, $"/xl/worksheets/sheet{i}.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
            Override(writer, "/xl/styles.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
            Override(writer, "/xl/sharedStrings.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml");
            writer.WriteEndElement();

            static void Default(XmlWriter w, string extension, string type)
            {
                w.WriteStartElement("Default", ContentTypesNs);
                w.WriteAttributeString("Extension", extension);
                w.WriteAttributeString("ContentType", type);
                w.WriteEndElement();
            }

            static void Override(XmlWriter w, string part, string type)
            {
                w.WriteStartElement("Override", ContentTypesNs);
                w.WriteAttributeString("PartName", part);
                w.WriteAttributeString("ContentType", type);
                w.WriteEndElement();
            }
        });

        private static byte[] RootRels() => Xml(writer =>
        {
            writer.WriteStartElement("Relationships", PackageRelNs);
            Relationship(writer, "rId1", RelNs + "/officeDocument", "xl/workbook.xml");
            writer.WriteEndElement();
        });

        private static byte[] Workbook(IReadOnlyList<string> names) => Xml(writer =>
        {
            writer.WriteStartElement("workbook", MainNs);
            writer.WriteAttributeString("xmlns", "r", null, RelNs);
            writer.WriteStartElement("bookViews", MainNs);
            writer.WriteStartElement("workbookView", MainNs);
            writer.WriteAttributeString("activeTab", "0");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteStartElement("sheets", MainNs);
            for (int i = 0; i < names.Count; i++)
            {
                writer.WriteStartElement("sheet", MainNs);
                writer.WriteAttributeString("name", names[i]);
                writer.WriteAttributeString("sheetId", (i + 1).ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("id", RelNs, "rId" + (i + 1).ToString(CultureInfo.InvariantCulture));
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
        });

        private static byte[] WorkbookRels(int sheets) => Xml(writer =>
        {
            writer.WriteStartElement("Relationships", PackageRelNs);
            for (int i = 1; i <= sheets; i++)
                Relationship(writer, "rId" + i, RelNs + "/worksheet", $"worksheets/sheet{i}.xml");
            Relationship(writer, "rId" + (sheets + 1), RelNs + "/styles", "styles.xml");
            Relationship(writer, "rId" + (sheets + 2), RelNs + "/sharedStrings", "sharedStrings.xml");
            writer.WriteEndElement();
        });

        private static void Relationship(XmlWriter writer, string id, string type, string target)
        {
            writer.WriteStartElement("Relationship", PackageRelNs);
            writer.WriteAttributeString("Id", id);
            writer.WriteAttributeString("Type", type);
            writer.WriteAttributeString("Target", target);
            writer.WriteEndElement();
        }

        private static byte[] Styles() => Xml(writer =>
        {
            writer.WriteStartElement("styleSheet", MainNs);

            writer.WriteStartElement("fonts", MainNs);
            writer.WriteAttributeString("count", "2");
            Font(writer, false);
            Font(writer, true);
            writer.WriteEndElement();

            writer.WriteStartElement("fills", MainNs);
            writer.WriteAttributeString("count", "5");
            PatternFill(writer, "none", null);
            PatternFill(writer, "gray125", null);
            PatternFill(writer, "solid", "FFD9E1F2");
            PatternFill(writer, "solid", "FFFFC7CE");
            PatternFill(writer, "solid", "FFFCE4C4");
            writer.WriteEndElement();

            writer.WriteStartElement("borders", MainNs);
            writer.WriteAttributeString("count", "1");
            writer.WriteStartElement("border", MainNs);
            foreach (string side in new[] { "left", "right", "top", "bottom", "diagonal" })
            {
                writer.WriteStartElement(side, MainNs);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("cellStyleXfs", MainNs);
            writer.WriteAttributeString("count", "1");
            Xf(writer, 0, 0, false, false);
            writer.WriteEndElement();

            writer.WriteStartElement("cellXfs", MainNs);
            writer.WriteAttributeString("count", "5");
            Xf(writer, 0, 0, false, true);
            Xf(writer, 1, 2, true, true);
            Xf(writer, 0, 0, true, true);
            Xf(writer, 0, 3, true, true);
            Xf(writer, 0, 4, true, true);
            writer.WriteEndElement();

            writer.WriteStartElement("cellStyles", MainNs);
            writer.WriteAttributeString("count", "1");
            writer.WriteStartElement("cellStyle", MainNs);
            writer.WriteAttributeString("name", "Normal");
            writer.WriteAttributeString("xfId", "0");
            writer.WriteAttributeString("builtinId", "0");
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteEndElement();
        });

        private static void Font(XmlWriter writer, bool bold)
        {
            writer.WriteStartElement("font", MainNs);
            if (bold)
            {
                writer.WriteStartElement("b", MainNs);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("sz", MainNs);
            writer.WriteAttributeString("val", "11");
            writer.WriteEndElement();
            writer.WriteStartElement("name", MainNs);
            writer.WriteAttributeString("val", "Calibri");
            writer.WriteEndElement();
            writer.WriteStartElement("family", MainNs);
            writer.WriteAttributeString("val", "2");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private static void PatternFill(XmlWriter writer, string pattern, string color)
        {
            writer.WriteStartElement("fill", MainNs);
            writer.WriteStartElement("patternFill", MainNs);
            writer.WriteAttributeString("patternType", pattern);
            if (color != null)
            {
                writer.WriteStartElement("fgColor", MainNs);
                writer.WriteAttributeString("rgb", color);
                writer.WriteEndElement();
                writer.WriteStartElement("bgColor", MainNs);
                writer.WriteAttributeString("indexed", "64");
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private static void Xf(XmlWriter writer, int font, int fill, bool wrap, bool cellXf)
        {
            writer.WriteStartElement("xf", MainNs);
            writer.WriteAttributeString("numFmtId", "0");
            writer.WriteAttributeString("fontId", font.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("fillId", fill.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("borderId", "0");
            if (cellXf)
            {
                writer.WriteAttributeString("xfId", "0");
                if (font != 0)
                    writer.WriteAttributeString("applyFont", "1");
                if (fill != 0)
                    writer.WriteAttributeString("applyFill", "1");
                if (wrap)
                    writer.WriteAttributeString("applyAlignment", "1");
            }

            if (wrap)
            {
                writer.WriteStartElement("alignment", MainNs);
                writer.WriteAttributeString("vertical", "top");
                writer.WriteAttributeString("wrapText", "1");
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private static void Add(ZipArchive zip, string name, byte[] content)
        {
            ZipArchiveEntry entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using Stream stream = entry.Open();
            stream.Write(content, 0, content.Length);
        }

        private static byte[] Xml(Action<XmlWriter> write)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = false,
                NewLineHandling = NewLineHandling.Entitize
            };

            using var memory = new MemoryStream();
            using (XmlWriter writer = XmlWriter.Create(memory, settings))
            {
                writer.WriteStartDocument(true);
                write(writer);
                writer.WriteEndDocument();
            }

            return memory.ToArray();
        }

        private sealed class SharedStrings
        {
            private readonly Dictionary<string, int> _indices = new Dictionary<string, int>(StringComparer.Ordinal);
            private readonly List<string> _items = new List<string>();
            private int _references;

            public int Index(string text)
            {
                _references++;
                if (_indices.TryGetValue(text, out int index))
                    return index;

                index = _items.Count;
                _items.Add(text);
                _indices[text] = index;
                return index;
            }

            public byte[] Xml() => XlsxWriter.Xml(writer =>
            {
                writer.WriteStartElement("sst", MainNs);
                writer.WriteAttributeString("count", _references.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("uniqueCount", _items.Count.ToString(CultureInfo.InvariantCulture));
                foreach (string item in _items)
                {
                    writer.WriteStartElement("si", MainNs);
                    writer.WriteStartElement("t", MainNs);
                    if (item.Length > 0 && (char.IsWhiteSpace(item[0]) || char.IsWhiteSpace(item[item.Length - 1]) || item.IndexOf('\n') >= 0 || item.IndexOf('\t') >= 0))
                        writer.WriteAttributeString("xml", "space", XmlNs, "preserve");
                    writer.WriteString(Escape(item));
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            });
        }
    }
}
