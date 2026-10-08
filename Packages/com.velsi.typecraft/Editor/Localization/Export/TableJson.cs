using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Velsi.Typecraft.Editor.Localization
{
    public static class TableJson
    {
        public static void Write(string path, IReadOnlyList<ExportTable> tables) =>
            File.WriteAllText(path, Serialize(tables), new UTF8Encoding(false));

        public static string Serialize(IReadOnlyList<ExportTable> tables)
        {
            var json = new StringBuilder();
            json.Append("{\n  \"tables\": [");
            for (int t = 0; t < tables.Count; t++)
            {
                ExportTable table = tables[t];
                json.Append(t == 0 ? "\n" : ",\n");
                json.Append("    {\n");
                json.Append("      \"id\": ").Append(Quote(table.Id)).Append(",\n");
                json.Append("      \"locales\": [");
                for (int l = 0; l < table.Locales.Count; l++)
                    json.Append(l == 0 ? string.Empty : ", ").Append(Quote(table.Locales[l]));
                json.Append("],\n");
                json.Append("      \"entries\": [");

                for (int r = 0; r < table.Rows.Count; r++)
                {
                    ExportRow row = table.Rows[r];
                    json.Append(r == 0 ? "\n" : ",\n");
                    json.Append("        {\n");
                    json.Append("          \"key\": ").Append(Quote(row.Key)).Append(",\n");
                    json.Append("          \"comment\": ").Append(Quote(row.Comment)).Append(",\n");
                    json.Append("          \"values\": {");
                    for (int l = 0; l < table.Locales.Count; l++)
                    {
                        string locale = table.Locales[l];
                        json.Append(l == 0 ? "\n" : ",\n");
                        json.Append("            ").Append(Quote(locale)).Append(": ").Append(Quote(row.Values.TryGetValue(locale, out string value) ? value : string.Empty));
                    }

                    json.Append(table.Locales.Count > 0 ? "\n          }\n" : "}\n");
                    json.Append("        }");
                }

                json.Append(table.Rows.Count > 0 ? "\n      ]\n" : "]\n");
                json.Append("    }");
            }

            json.Append(tables.Count > 0 ? "\n  ]\n}\n" : "]\n}\n");
            return json.ToString();
        }

        public static string Quote(string text)
        {
            text ??= string.Empty;
            var output = new StringBuilder(text.Length + 2);
            output.Append('"');
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                switch (c)
                {
                    case '"':
                        output.Append("\\\"");
                        break;
                    case '\\':
                        output.Append("\\\\");
                        break;
                    case '\n':
                        output.Append("\\n");
                        break;
                    case '\r':
                        output.Append("\\r");
                        break;
                    case '\t':
                        output.Append("\\t");
                        break;
                    case '\b':
                        output.Append("\\b");
                        break;
                    case '\f':
                        output.Append("\\f");
                        break;
                    default:
                        if (c < 0x20 || c == '\u2028' || c == '\u2029' || IsLoneSurrogate(text, i))
                            output.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            output.Append(c);
                        break;
                }
            }

            output.Append('"');
            return output.ToString();
        }

        private static bool IsLoneSurrogate(string text, int i)
        {
            if (char.IsHighSurrogate(text[i]))
                return i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1]);
            return char.IsLowSurrogate(text[i]) && (i == 0 || !char.IsHighSurrogate(text[i - 1]));
        }
    }
}
