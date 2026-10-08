using System;
using System.Collections.Generic;

namespace Velsi.Typecraft.Text
{
    public readonly struct TextEntry
    {
        public TextEntry(string table, string key, string locale, string text, Func<string, IList<object>, string> format = null)
        {
            Table = table;
            Key = key;
            Locale = locale;
            Text = text;
            Format = format;
        }

        public string Table { get; }
        public string Key { get; }
        public string Locale { get; }
        public string Text { get; }
        public Func<string, IList<object>, string> Format { get; }
        public bool Found => Text != null;

        public static TextEntry Missing(string table, string key, string locale) => new TextEntry(table, key, locale, null);
    }
}
