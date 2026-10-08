using System.Collections.Generic;

namespace Velsi.Typecraft.Editor.Localization
{
    public interface ITableBackend
    {
        string Name { get; }
        IReadOnlyList<string> Locales { get; }
        string BaseLocale { get; }
        IReadOnlyList<string> Tables { get; }
        IReadOnlyList<string> Keys(string table);
        string Get(string table, string key, string locale);
        string Comment(string table, string key);
        void Set(string table, string key, string locale, string text);
        void SetComment(string table, string key, string comment);
        bool AddKey(string table, string key);
        bool RemoveKey(string table, string key);
        bool RenameKey(string table, string key, string newKey);
        bool AddLocale(string locale);
        string CreateTable(string name);
        UnityEngine.Object Asset(string table);
    }
}
