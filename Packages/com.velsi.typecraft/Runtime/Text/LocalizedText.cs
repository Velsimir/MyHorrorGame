using System;
using System.Collections.Generic;

namespace Velsi.Typecraft.Text
{
    public static class LocalizedText
    {
        public static void Request(string table, string key, IList<object> arguments, Action<string, bool> onReady)
        {
            ITextSource source = Typecraft.Settings != null ? Typecraft.Settings.TextSource : null;
            string locale = Typecraft.Locale;
            if (source == null || string.IsNullOrEmpty(key))
            {
                onReady(key, true);
                return;
            }

            bool pseudo = Pseudolocalizer.IsPseudo(locale);
            source.Request(table, key, pseudo ? source.BaseLocale : locale, entry =>
            {
                if (!entry.Found)
                {
                    onReady(key, true);
                    return;
                }

                string text = Decorate(entry);
                bool formatted = entry.Format != null;
                if (formatted)
                    text = entry.Format(text, arguments);

                onReady(pseudo ? Pseudolocalizer.Transform(text) : text, formatted);
            });
        }

        public static string Decorate(TextEntry entry)
        {
            if (!entry.Found)
                return entry.Text;

            MarkupLayer layer = Typecraft.Settings != null ? Typecraft.Settings.FindMarkupLayer(entry.Table) : null;
            return layer != null ? layer.Resolve(entry.Key, entry.Locale, entry.Text) : entry.Text;
        }
    }
}
