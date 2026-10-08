using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Editor.Window.Effects;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Editor.Window.Targets
{
    public sealed class LocalizedTextTarget : ITextEditTarget
    {
        private readonly TypecraftText _text;
        private readonly Func<string> _locale;

        public LocalizedTextTarget(TypecraftText text, Func<string> locale)
        {
            _text = text;
            _locale = locale;
        }

        private ITableBackend Backend => TableBackends.Current;
        private string Locale => _locale() ?? Typecraft.Locale;
        private bool IsPseudo => Pseudolocalizer.IsPseudo(Locale);

        public string Title
        {
            get
            {
                if (_text == null)
                    return "—";

                ITableBackend backend = Backend;
                string table = Table;
                bool stale = backend != null && table != null && !IsPseudo && MarkupLayers.State(backend, table, _text.Key, Locale) == LayerState.Stale;
                return $"{_text.name} · {table ?? "?"}/{_text.Key} [{Locale}]" + (stale ? " · ⚠ строка в таблице изменилась, разметка не применяется" : string.Empty);
            }
        }

        public string Markup
        {
            get
            {
                ITableBackend backend = Backend;
                if (_text == null || backend == null || Table == null)
                    return _text != null ? _text.Key ?? string.Empty : string.Empty;

                return IsPseudo
                    ? Pseudolocalizer.Transform(MarkupLayers.Display(backend, Table, _text.Key, backend.BaseLocale))
                    : MarkupLayers.Display(backend, Table, _text.Key, Locale);
            }
        }

        public string TableText => _text != null && Backend != null && Table != null ? Backend.Get(Table, _text.Key, Locale) : string.Empty;

        public string Subject => _text != null ? $"«{_text.Key}» [{Locale}]" : string.Empty;

        public bool IsValid => _text != null && _text.Mode == TextMode.Localized;
        public bool CanWrite => ReadOnlyReason == null;

        public string ReadOnlyReason
        {
            get
            {
                if (_text == null)
                    return null;
                if (Backend == null)
                    return "Источник строк не поддерживает правку из окна — открой его таблицы напрямую.";
                if (Table == null)
                    return "Нет ни одной таблицы — создай её во вкладке «Локализация».";
                if (IsPseudo)
                    return "Псевдолокаль собирается из основного языка автоматически — выбери настоящий язык.";
                return null;
            }
        }

        public string FontRole => _text != null ? _text.FontRole : null;
        public TMP_Text Reference => _text != null ? _text.Target : null;

        private string Table
        {
            get
            {
                ITableBackend backend = Backend;
                if (_text == null || backend == null)
                    return null;
                if (!string.IsNullOrEmpty(_text.Table))
                    return backend.Tables.Contains(_text.Table) ? _text.Table : null;

                return backend.Tables.FirstOrDefault(table => backend.Keys(table).Contains(_text.Key)) ?? backend.Tables.FirstOrDefault();
            }
        }

        public bool ChangesWords(string markup) => CanWrite && Decoration.Strip(markup) != TableText;

        public TransferResult RestoreWords(string markup) => Decoration.Transfer(markup, TableText);

        public void Write(string markup)
        {
            if (!CanWrite)
                return;

            MarkupLayers.Write(Backend, Table, _text.Key, Locale, markup);
            EffectComponents.Ensure(_text.Target, AllMarkups().Append(markup));
            _text.Refresh();
        }

        public IEnumerable<string> AllMarkups()
        {
            ITableBackend backend = Backend;
            string table = Table;
            if (_text == null || backend == null || table == null)
                return Array.Empty<string>();

            return backend.Locales.Select(locale => MarkupLayers.Display(backend, table, _text.Key, locale)).ToList();
        }

        public void SetFontRole(string role)
        {
            if (_text == null)
                return;

            Undo.RecordObject(_text, "Typecraft Font Role");
            var serialized = new SerializedObject(_text);
            serialized.FindProperty("_fontRole").stringValue = role;
            serialized.ApplyModifiedProperties();
            _text.Refresh();
        }
    }
}
