using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using Velsi.Typecraft.Dialogue.Editor.Model;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Editor.Window.Preview;
using Velsi.Typecraft.Fonts;
using Velsi.Typecraft.Markup;

namespace Velsi.Typecraft.Dialogue.Editor.Inspector
{
    public sealed class KeyField : VisualElement, IDisposable
    {
        private readonly IDialogueEditorContext _context;
        private readonly Func<string> _table;
        private readonly Func<string> _prefix;
        private readonly PickField _key;
        private readonly Label _text;
        private readonly TextField _newText;
        private readonly TextPreview _preview;
        private readonly VisualElement _previewHost;

        public KeyField(string label, IDialogueEditorContext context, Func<string> table, Func<string> prefix, bool preview)
        {
            _context = context;
            _table = table;
            _prefix = prefix;
            AddToClassList("dlg-key");

            _key = new PickField(label, "Ключи таблицы", Items);
            _key.Changed += key =>
            {
                Changed?.Invoke(key);
                Refresh();
            };
            Add(_key);

            _text = new Label { enableRichText = false };
            _text.AddToClassList("dlg-key__text");
            Add(_text);

            if (preview)
            {
                _preview = new TextPreview { Animate = true };
                _previewHost = new VisualElement();
                _previewHost.AddToClassList("dlg-key__preview");
                _preview.Element.style.flexGrow = 1;
                _previewHost.Add(_preview.Element);
                Add(_previewHost);
            }

            var create = new VisualElement();
            create.AddToClassList("dlg-row");
            create.AddToClassList("dlg-key__create");
            _newText = new TextField { multiline = true, tooltip = "Текст новой строки на основном языке таблицы. Разметка [стиль] и <тег> уйдёт в слой разметки" };
            _newText.textEdition.placeholder = "Новый текст — «Создать ключ» заведёт строку в таблице";
            _newText.AddToClassList("dlg-key__new");
            create.Add(_newText);
            var button = new Button(() => Create()) { text = "Создать ключ", tooltip = "Ключ вида " + SafePrefix() + ".NNN в таблице, текст — на основном языке" };
            create.Add(button);
            Add(create);
        }

        public event Action<string> Changed;

        public string Value
        {
            get => _key.Value;
            set
            {
                _key.Value = value;
                Refresh();
            }
        }

        public string NewText
        {
            get => _newText.value;
            set => _newText.value = value ?? string.Empty;
        }

        public string Create()
        {
            ITableBackend backend = DialogueTexts.Backend;
            string table = _table();
            if (backend == null || string.IsNullOrEmpty(table) || !backend.Tables.Contains(table))
            {
                EditorUtility.DisplayDialog("Typecraft", string.IsNullOrEmpty(table) ? "У диалога не задана таблица по умолчанию." : $"Таблицы «{table}» нет в источнике строк.", "OK");
                return null;
            }

            string text = _newText.value?.Trim() ?? string.Empty;
            if (text.Length == 0)
            {
                _newText.Focus();
                return null;
            }

            string key = DialogueTexts.NextKey(table, SafePrefix());
            if (!backend.AddKey(table, key))
                return null;

            MarkupLayers.Write(backend, table, key, backend.BaseLocale, text);
            _newText.SetValueWithoutNotify(string.Empty);
            _key.Pick(key);
            return key;
        }

        public void Refresh()
        {
            string table = _table();
            string key = _key.Value;
            string locale = _context.Locale;

            if (string.IsNullOrEmpty(key))
                _text.text = "Ключ не задан";
            else if (!DialogueTexts.HasKey(table, key))
                _text.text = $"⚠ Ключа «{key}» нет в таблице «{table}»";
            else
            {
                string plain = DialogueTexts.Plain(table, key, locale, 400);
                _text.text = string.IsNullOrEmpty(plain) ? $"(пусто на языке {locale})" : plain;
            }

            _text.EnableInClassList("dlg-key__text--warn", !string.IsNullOrEmpty(key) && !DialogueTexts.HasKey(table, key));

            if (_preview == null)
                return;

            string markup = DialogueTexts.Markup(table, key, locale);
            _previewHost.style.display = string.IsNullOrEmpty(markup) ? DisplayStyle.None : DisplayStyle.Flex;
            TypecraftSettings settings = Typecraft.Settings;
            FontVariant variant = settings != null && settings.Fonts != null ? settings.Fonts.Resolve(settings.DefaultFontRole, locale) : null;
            _preview.Configure(null, variant?.Font, null);
            _preview.SetMarkup(markup, locale, Typecraft.Device, new List<MarkupDiagnostic>());
        }

        public void Tick(float deltaTime) => _preview?.Tick(deltaTime);

        public void Dispose() => _preview?.Dispose();

        private string SafePrefix()
        {
            string prefix = _prefix?.Invoke();
            return string.IsNullOrEmpty(prefix) ? "dlg" : prefix;
        }

        private IEnumerable<PickItem> Items()
        {
            ITableBackend backend = DialogueTexts.Backend;
            string table = _table();
            if (backend == null || string.IsNullOrEmpty(table) || !backend.Tables.Contains(table))
                return Enumerable.Empty<PickItem>();

            string locale = _context.Locale;
            return backend.Keys(table).Where(key => !string.IsNullOrEmpty(key)).Select(key => new PickItem(key, $"{key}   —   {DialogueTexts.Plain(table, key, locale, 60)}")).ToList();
        }
    }
}
