using System;
using System.Collections.Generic;

namespace Velsi.Typecraft.Text
{
    public interface ITextSource
    {
        IReadOnlyList<string> Locales { get; }
        string BaseLocale { get; }
        void Activate();
        void Request(string table, string key, string locale, Action<TextEntry> onReady);
        void SelectLocale(string localeCode);
    }
}
