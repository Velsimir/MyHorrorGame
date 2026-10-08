using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Editor.Setup;
using Velsi.Typecraft.Editor.Window.Dialogs;
using Velsi.Typecraft.Editor.Window.Markup;
using Velsi.Typecraft.Editor.Window.Preview;
using Velsi.Typecraft.Fonts;
using Velsi.Typecraft.Markup;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Editor.Window
{
    public sealed class LocalizationTab : VisualElement, IDisposable
    {
        private static readonly (string Title, CellIssue Issue)[] Filters =
        {
            ("Все строки", CellIssue.None),
            ("Есть проблемы", CellIssue.Missing | CellIssue.Overflow | CellIssue.MissingGlyph | CellIssue.MarkupMismatch | CellIssue.StaleMarkup),
            ("Только изменившиеся", CellIssue.StaleMarkup),
            ("Нет перевода", CellIssue.Missing),
            ("Не помещается", CellIssue.Overflow),
            ("Нет символа в шрифте", CellIssue.MissingGlyph),
            ("Разметка расходится", CellIssue.MarkupMismatch)
        };

        private readonly VisualElement _body;
        private readonly Label _backendLabel;
        private readonly DropdownField _tableField;
        private readonly DropdownField _filterField;
        private readonly DropdownField _sceneLocaleField;
        private readonly ToolbarSearchField _search;
        private readonly Label _summary;
        private readonly VisualElement _gridHost;
        private readonly VisualElement _cellPanel;
        private readonly Label _cellTitle;
        private readonly Label _baseText;
        private readonly TextField _comment;
        private readonly Label _decorationHint;
        private readonly VisualElement _layerPanel;
        private readonly Label _layerTitle;
        private readonly Label _layerBefore;
        private readonly Label _layerAfter;
        private readonly Label _layerMarkup;
        private readonly Button _transferButton;
        private readonly MarkupField _markupField;
        private readonly Button _syncButton;
        private readonly VisualElement _syncStatus;
        private readonly Label _syncLabel;
        private readonly TextPreview _preview = new TextPreview();
        private readonly List<MarkupDiagnostic> _diagnostics = new List<MarkupDiagnostic>();
        private readonly Dictionary<string, RowReport> _reports = new Dictionary<string, RowReport>();
        private readonly List<string> _rows = new List<string>();

        private MultiColumnListView _grid;
        private ITableBackend _backend;
        private string _table;
        private string _key;
        private string _locale;
        private bool _suppress;
        private GoogleSheetsDownload _download;

        public LocalizationTab()
        {
            AddToClassList("tc-loc");

            var toolbar = new VisualElement();
            toolbar.AddToClassList("tc-toolbar");
            Add(toolbar);

            _backendLabel = new Label();
            _backendLabel.AddToClassList("tc-toolbar__target");
            toolbar.Add(_backendLabel);

            _tableField = new DropdownField { tooltip = "Таблица" };
            _tableField.AddToClassList("tc-loc__table");
            _tableField.RegisterValueChangedCallback(evt =>
            {
                _table = evt.newValue;
                _key = null;
                UpdateSyncButton();
                RecheckAll();
            });
            toolbar.Add(_tableField);
            toolbar.Add(new Button(CreateTable) { text = "+ таблица" });
            toolbar.Add(new Button(PingTable) { text = "▸", tooltip = "Показать ассет таблицы" });

            _search = new ToolbarSearchField();
            _search.AddToClassList("tc-loc__search");
            _search.RegisterValueChangedCallback(_ => ApplyFilter());
            toolbar.Add(_search);

            _filterField = new DropdownField(Filters.Select(filter => filter.Title).ToList(), 0);
            _filterField.AddToClassList("tc-loc__filter");
            _filterField.RegisterValueChangedCallback(_ => ApplyFilter());
            toolbar.Add(_filterField);

            toolbar.Add(Spacer());
            toolbar.Add(new Button(AddKey) { text = "+ ключ" });
            toolbar.Add(new Button(AddLocale) { text = "+ язык" });
            _syncButton = new Button(Sync) { text = "Синхронизировать" };
            toolbar.Add(_syncButton);
            toolbar.Add(new Button(ConfigureSource) { text = "Источник…", tooltip = "Откуда синхронизировать таблицу: CSV/TSV, Excel или Google Sheets" });
            toolbar.Add(new Button(ImportFile) { text = "Импорт…", tooltip = "Разово загрузить CSV, TSV или .xlsx — с отчётом перед применением" });
            toolbar.Add(new Button(Export) { text = "Экспорт…", tooltip = "Выгрузить текущую или все таблицы в .xlsx, CSV, TSV или JSON" });
            toolbar.Add(new Button(() => RecheckAll()) { text = "⟳", tooltip = "Перепроверить все строки" });

            _sceneLocaleField = new DropdownField { tooltip = "Язык, на котором показываются тексты в сцене и в игре" };
            _sceneLocaleField.AddToClassList("tc-toolbar__locale");
            _sceneLocaleField.RegisterValueChangedCallback(evt => Typecraft.Settings?.TextSource?.SelectLocale(evt.newValue));
            toolbar.Add(new Label("Сцена:") { tooltip = _sceneLocaleField.tooltip });
            toolbar.Add(_sceneLocaleField);

            _syncStatus = new VisualElement();
            _syncStatus.AddToClassList("tc-loc__sync");
            _syncLabel = new Label();
            _syncLabel.AddToClassList("tc-loc__sync-label");
            _syncStatus.Add(_syncLabel);
            _syncStatus.Add(new Button(CancelDownload) { text = "Отмена" });
            _syncStatus.style.display = DisplayStyle.None;
            Add(_syncStatus);

            _body = new VisualElement();
            _body.AddToClassList("tc-loc__body");
            Add(_body);

            _gridHost = new VisualElement();
            _gridHost.AddToClassList("tc-loc__grid-host");
            _body.Add(_gridHost);

            _cellPanel = new VisualElement();
            _cellPanel.AddToClassList("tc-loc__cell-panel");
            _body.Add(_cellPanel);

            var editorColumn = new ScrollView();
            editorColumn.AddToClassList("tc-loc__cell-editor");
            _cellPanel.Add(editorColumn);

            var header = new VisualElement();
            header.AddToClassList("tc-loc__cell-header");
            _cellTitle = new Label();
            _cellTitle.AddToClassList("tc-loc__cell-title");
            header.Add(_cellTitle);
            header.Add(new Button(() => ShowUsages(_key)) { text = "Найти в проекте", tooltip = "Где используется ключ: сцены, префабы, скрипты. То же — двойной клик по ключу" });
            header.Add(new Button(RenameKey) { text = "Переименовать" });
            header.Add(new Button(RemoveKey) { text = "Удалить ключ" });
            editorColumn.Add(header);

            _baseText = new Label();
            _baseText.AddToClassList("tc-loc__base");
            editorColumn.Add(_baseText);

            _markupField = new MarkupField(MarkupCandidates.For);
            _markupField.MarkupChanged += OnCellChanged;
            editorColumn.Add(_markupField);

            _decorationHint = new Label("Оформление в таблицу не пишется — в таблицу ушёл текст без стилей и тегов. Стили и теги правь во вкладке «Редактор», они хранятся в слое разметки.");
            _decorationHint.AddToClassList("tc-loc__hint");
            editorColumn.Add(_decorationHint);

            _layerPanel = new VisualElement();
            _layerPanel.AddToClassList("tc-loc__layer");
            var layerHeader = new VisualElement();
            layerHeader.AddToClassList("tc-loc__cell-header");
            _layerTitle = new Label();
            _layerTitle.AddToClassList("tc-loc__layer-title");
            layerHeader.Add(_layerTitle);
            _transferButton = new Button(TransferTags) { text = "Перенести теги", tooltip = "Перенести оформление на новый текст таблицы по совпадающим словам" };
            layerHeader.Add(_transferButton);
            layerHeader.Add(new Button(ResetMarkup) { text = "Сбросить разметку", tooltip = "Удалить запись слоя: строка покажется без оформления" });
            _layerPanel.Add(layerHeader);
            _layerBefore = DiffLabel();
            _layerAfter = DiffLabel();
            _layerMarkup = new Label { enableRichText = false };
            _layerMarkup.AddToClassList("tc-loc__layer-markup");
            _layerPanel.Add(_layerBefore);
            _layerPanel.Add(_layerAfter);
            _layerPanel.Add(_layerMarkup);
            editorColumn.Add(_layerPanel);

            _comment = new TextField("Комментарий") { isDelayed = true, tooltip = "Пояснение для переводчика: где показывается строка, ограничения по длине" };
            _comment.AddToClassList("tc-loc__comment");
            _comment.RegisterValueChangedCallback(evt =>
            {
                if (_suppress || _key == null)
                    return;
                _backend.SetComment(_table, _key, evt.newValue);
                _grid?.RefreshItems();
            });
            editorColumn.Add(_comment);

            var previewHost = new VisualElement();
            previewHost.AddToClassList("tc-loc__preview");
            previewHost.Add(_preview.Element);
            _cellPanel.Add(previewHost);

            Undo.undoRedoPerformed += OnUndoRedo;
            Reload();
        }

        public void Reload()
        {
            _backend = TableBackends.Current;
            _gridHost.Clear();

            if (_backend == null)
            {
                ShowSetup();
                return;
            }

            _backendLabel.text = _backend.Name;
            List<string> tables = _backend.Tables.ToList();
            _tableField.choices = tables;
            if (_table == null || !tables.Contains(_table))
                _table = tables.FirstOrDefault();
            _tableField.SetValueWithoutNotify(_table ?? string.Empty);
            UpdateSyncButton();

            ITextSource source = Typecraft.Settings != null ? Typecraft.Settings.TextSource : null;
            _sceneLocaleField.choices = source != null ? source.Locales.ToList() : new List<string>();
            _sceneLocaleField.SetValueWithoutNotify(Typecraft.Locale);

            if (_table == null)
            {
                _gridHost.Add(new Label("Таблиц пока нет — нажми «+ таблица».") { name = "tc-loc__empty" });
                ShowCell(false);
                return;
            }

            BuildGrid();
            RecheckAll();
        }

        public void Tick(float deltaTime)
        {
            _preview.Tick(deltaTime);
            if (_download != null && _download.IsRunning)
                _syncLabel.text = $"Загрузка Google Sheets для «{_table}»… {_download.DownloadedBytes / 1024f:0.#} КБ";
        }

        public void Dispose()
        {
            CancelDownload();
            Undo.undoRedoPerformed -= OnUndoRedo;
            _preview.Dispose();
        }

        private void ShowSetup()
        {
            _backendLabel.text = "Нет источника строк";
            ShowCell(false);
            var box = new VisualElement();
            box.AddToClassList("tc-loc__setup");
            box.Add(new Label("В TypecraftSettings не задан источник строк, который умеет править окно.\nВстроенные таблицы Typecraft работают без сторонних пакетов; Unity Localization подключается тем же полем Text Source."));
            box.Add(new Button(() =>
            {
                TypecraftSetup.CreateStringTables();
                Reload();
            }) { text = "Создать таблицы Typecraft" });
            _gridHost.Add(box);
        }

        private void BuildGrid()
        {
            var columns = new Columns();
            columns.Add(MakeColumn("key", "Ключ", 220f, null, BindKey));
            foreach (string locale in _backend.Locales)
            {
                string captured = locale;
                string title = locale == _backend.BaseLocale ? locale + " ★" : locale;
                columns.Add(MakeColumn("locale:" + locale, title, 240f, locale, (label, key) => BindValue(label, key, captured)));
            }

            columns.Add(MakeColumn("issues", "Проблемы", 130f, null, BindIssues));
            columns.Add(MakeColumn("comment", "Комментарий", 200f, null, (label, key) => label.text = _backend.Comment(_table, key)));

            _grid = new MultiColumnListView(columns)
            {
                itemsSource = _rows,
                fixedItemHeight = 22f,
                selectionType = SelectionType.None,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly
            };
            _grid.AddToClassList("tc-loc__grid");
            _grid.RegisterCallback<KeyDownEvent>(OnGridKeyDown);
            _gridHost.Add(_grid);
        }

        private Column MakeColumn(string name, string title, float width, string locale, Action<Label, string> bind) => new Column
        {
            name = name,
            title = title,
            width = width,
            stretchable = locale != null,
            makeCell = () =>
            {
                var label = new Label();
                label.AddToClassList("tc-loc__cell");
                label.RegisterCallback<PointerDownEvent>(evt => OnCellClicked(label, evt));
                if (locale == null && name == "key")
                    label.AddManipulator(new ContextualMenuManipulator(evt => FillKeyMenu(label, evt)));
                return label;
            },
            bindCell = (element, index) =>
            {
                var label = (Label)element;
                if (index < 0 || index >= _rows.Count)
                    return;

                string key = _rows[index];
                label.userData = new[] { key, locale };
                label.EnableInClassList("tc-loc__cell--selected", key == _key && (locale == null || locale == _locale));
                bind(label, key);
            }
        };

        private void BindKey(Label label, string key)
        {
            label.text = key;
            label.tooltip = "Двойной клик — где используется · F2 — переименовать · ПКМ — меню";
            ClearIssueClasses(label);
        }

        private void BindValue(Label label, string key, string locale)
        {
            string value = _backend.Get(_table, key, locale);
            CellIssue issue = Report(key)[locale];
            LayerState layer = MarkupLayers.State(_backend, _table, key, locale);
            string marker = layer == LayerState.Stale ? "⚠ " : layer == LayerState.Applied ? "✦ " : string.Empty;
            label.text = marker + (string.IsNullOrEmpty(value) ? "—" : value.Replace("\r", string.Empty).Replace('\n', '⏎'));
            label.tooltip = string.Join("\n", Report(key).Messages.Where(message => message.StartsWith(locale + ":")));
            ClearIssueClasses(label);
            label.EnableInClassList("tc-loc__cell--missing", (issue & CellIssue.Missing) != 0);
            label.EnableInClassList("tc-loc__cell--overflow", (issue & CellIssue.Overflow) != 0);
            label.EnableInClassList("tc-loc__cell--glyph", (issue & CellIssue.MissingGlyph) != 0);
            label.EnableInClassList("tc-loc__cell--mismatch", (issue & CellIssue.MarkupMismatch) != 0);
            label.EnableInClassList("tc-loc__cell--stale", layer == LayerState.Stale);
            if (layer == LayerState.Applied)
                label.tooltip = (label.tooltip.Length > 0 ? label.tooltip + "\n" : string.Empty) + "✦ есть разметка в слое";
        }

        private void BindIssues(Label label, string key)
        {
            RowReport report = Report(key);
            ClearIssueClasses(label);
            label.text = string.Join("  ", new[]
            {
                Badge(report, CellIssue.Missing, "∅"),
                Badge(report, CellIssue.Overflow, "⇲"),
                Badge(report, CellIssue.MissingGlyph, "□"),
                Badge(report, CellIssue.MarkupMismatch, "≠"),
                Badge(report, CellIssue.StaleMarkup, "⚠")
            }.Where(badge => badge != null));
            label.tooltip = report.Messages.Count == 0 ? "Без проблем" : string.Join("\n", report.Messages);
            label.EnableInClassList("tc-loc__cell--warn", report.Messages.Count > 0);
        }

        private static string Badge(RowReport report, CellIssue issue, string symbol)
        {
            int count = report.Cells.Values.Count(cell => (cell & issue) != 0);
            return count == 0 ? null : count == 1 ? symbol : symbol + count;
        }

        private static void ClearIssueClasses(VisualElement label)
        {
            label.RemoveFromClassList("tc-loc__cell--missing");
            label.RemoveFromClassList("tc-loc__cell--overflow");
            label.RemoveFromClassList("tc-loc__cell--glyph");
            label.RemoveFromClassList("tc-loc__cell--mismatch");
            label.RemoveFromClassList("tc-loc__cell--warn");
            label.RemoveFromClassList("tc-loc__cell--stale");
        }

        private RowReport Report(string key)
        {
            if (!_reports.TryGetValue(key, out RowReport report))
                _reports[key] = report = LocalizationChecks.Check(_backend, _table, key);
            return report;
        }

        private void RecheckAll()
        {
            _reports.Clear();
            ApplyFilter();
            if (_key != null && _backend.Keys(_table).Contains(_key))
                Select(_key, _locale);
            else
                ShowCell(false);
        }

        private void ApplyFilter()
        {
            if (_backend == null || _table == null)
                return;

            string query = _search.value?.Trim() ?? string.Empty;
            CellIssue filter = Filters[Mathf.Max(0, _filterField.index)].Issue;
            List<string> keys = _backend.Keys(_table).ToList();

            _rows.Clear();
            foreach (string key in keys)
            {
                if (query.Length > 0 && !Matches(key, query))
                    continue;
                if (filter != CellIssue.None && (Report(key).All & filter) == 0)
                    continue;
                _rows.Add(key);
            }

            int problems = keys.Count(key => Report(key).All != CellIssue.None);
            _grid?.RefreshItems();
            _grid?.Rebuild();
            UpdateSummary(keys.Count, problems);
        }

        private void UpdateSummary(int total, int problems) =>
            _backendLabel.text = $"{_backend.Name} · ключей: {total}" + (problems > 0 ? $" · с проблемами: {problems}" : string.Empty);

        private bool Matches(string key, string query)
        {
            if (key.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (_backend.Comment(_table, key).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return _backend.Locales.Any(locale => _backend.Get(_table, key, locale).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void OnCellClicked(Label label, PointerDownEvent evt)
        {
            if (label.userData is not string[] cell)
                return;

            _grid?.Focus();
            if (cell[1] == null && evt.clickCount >= 2)
            {
                Select(cell[0], _locale);
                string key = cell[0];
                EditorApplication.delayCall += () => ShowUsages(key);
                return;
            }

            Select(cell[0], cell[1] ?? _locale ?? _backend.BaseLocale);
        }

        private void FillKeyMenu(Label label, ContextualMenuPopulateEvent evt)
        {
            if (label.userData is not string[] cell)
                return;

            string key = cell[0];
            Select(key, _locale);
            evt.menu.AppendAction("Где используется", _ => ShowUsages(key));
            evt.menu.AppendAction("Переименовать… (F2)", _ => RenameKey());
            evt.menu.AppendAction("Удалить ключ", _ => RemoveKey());
            evt.StopPropagation();
        }

        private void OnGridKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.F2 || _key == null)
                return;

            evt.StopPropagation();
            RenameKey();
        }

        private void ShowUsages(string key)
        {
            if (_backend == null || _table == null || key == null)
                return;

            List<KeyUsage> usages = KeyUsages.Find(_table, key);
            if (usages == null)
                return;

            if (usages.Count == 0)
                EditorUtility.DisplayDialog("Где используется", $"Ключ «{key}» нигде не используется: ни в сценах, ни в префабах, ни строкой в скриптах Assets/.", "OK");
            else if (usages.Count == 1)
                KeyUsages.Reveal(usages[0]);
            else
                KeyUsagesWindow.Show(_table, key, usages);
        }

        private void Select(string key, string locale)
        {
            _key = key;
            _locale = locale != null && _backend.Locales.Contains(locale) ? locale : _backend.BaseLocale;
            ShowCell(true);

            _suppress = true;
            _cellTitle.text = $"{_key} · {_locale}";
            string baseText = _backend.Get(_table, _key, _backend.BaseLocale);
            _baseText.text = _locale == _backend.BaseLocale ? "Основной язык" : $"{_backend.BaseLocale}: {(string.IsNullOrEmpty(baseText) ? "—" : baseText)}";
            _markupField.SetValue(_backend.Get(_table, _key, _locale), notify: false);
            _comment.SetValueWithoutNotify(_backend.Comment(_table, _key));
            _suppress = false;

            _decorationHint.style.display = DisplayStyle.None;
            UpdateLayerPanel();
            UpdatePreview();
            _grid?.RefreshItems();
        }

        private void ShowCell(bool show) => _cellPanel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

        private void OnCellChanged(string value)
        {
            if (_suppress || _key == null)
                return;

            bool decorated = Decoration.Has(value);
            _backend.Set(_table, _key, _locale, decorated ? Decoration.Strip(value) : value);
            _decorationHint.style.display = decorated ? DisplayStyle.Flex : DisplayStyle.None;
            _reports[_key] = LocalizationChecks.Check(_backend, _table, _key);
            UpdateLayerPanel();
            UpdatePreview();
            _grid?.RefreshItems();
        }

        private void UpdatePreview()
        {
            TypecraftText sceneText = SceneTexts.Localized(_table, _key).FirstOrDefault();
            TypecraftSettings settings = Typecraft.Settings;
            string role = sceneText != null ? sceneText.FontRole : settings?.DefaultFontRole;
            FontVariant variant = settings != null && settings.Fonts != null ? settings.Fonts.Resolve(role, _locale) : null;

            _preview.Configure(sceneText != null ? sceneText.Target : null, variant?.Font, null);
            _diagnostics.Clear();
            Typecraft.RenderPreview(_markupField.Value, _locale, Typecraft.Device, _diagnostics, null);
            _markupField.SetDiagnostics(_diagnostics);
            _preview.SetMarkup(MarkupLayers.Display(_backend, _table, _key, _locale), _locale, Typecraft.Device, new List<MarkupDiagnostic>());
        }

        private static Label DiffLabel()
        {
            var label = new Label { enableRichText = true };
            label.AddToClassList("tc-loc__diff");
            return label;
        }

        private void UpdateLayerPanel()
        {
            MarkupLayerEntry entry = _key != null ? MarkupLayers.Entry(_table, _key, _locale) : null;
            _layerPanel.style.display = entry != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (entry == null)
                return;

            bool stale = MarkupLayers.State(_backend, _table, _key, _locale) == LayerState.Stale;
            _layerPanel.EnableInClassList("tc-loc__layer--stale", stale);
            _layerTitle.text = stale ? "⚠ Строка в таблице изменилась — разметка не применяется" : "✦ Разметка в слое";
            _transferButton.style.display = stale ? DisplayStyle.Flex : DisplayStyle.None;
            _layerBefore.style.display = stale ? DisplayStyle.Flex : DisplayStyle.None;
            _layerAfter.style.display = stale ? DisplayStyle.Flex : DisplayStyle.None;

            if (stale)
            {
                List<DiffPart> parts = Decoration.Diff(entry.Source, _backend.Get(_table, _key, _locale));
                _layerBefore.text = "<b>Было (снимок):</b> " + Decoration.RichDiff(parts, true);
                _layerAfter.text = "<b>Стало (таблица):</b> " + Decoration.RichDiff(parts, false);
            }

            _layerMarkup.text = "Разметка: " + entry.Markup.Replace("\r", string.Empty).Replace('\n', '⏎');
        }

        private void TransferTags()
        {
            MarkupLayerEntry entry = _key != null ? MarkupLayers.Entry(_table, _key, _locale) : null;
            if (entry == null)
                return;

            TransferResult result = Decoration.Transfer(entry.Markup, _backend.Get(_table, _key, _locale));
            if (result.Broken)
            {
                EditorUtility.DisplayDialog("Перенести теги", "Перенести не получилось. " + result.Describe() + "\n\nСбрось разметку и расставь оформление заново во вкладке «Редактор».", "OK");
                return;
            }

            if (!result.Exact && !EditorUtility.DisplayDialog("Перенести теги",
                    $"Не все теги удалось привязать к тем же словам.\n\n{result.Describe()}\n\nРезультат:\n{result.Markup}", "Сохранить так", "Отмена"))
                return;

            MarkupLayers.Set(_backend, _table, _key, _locale, result.Markup);
            _reports[_key] = LocalizationChecks.Check(_backend, _table, _key);
            UpdateLayerPanel();
            UpdatePreview();
            _grid?.RefreshItems();
        }

        private void ResetMarkup()
        {
            if (_key == null || !EditorUtility.DisplayDialog("Сбросить разметку", $"Удалить оформление строки «{_key}» [{_locale}]? Текст таблицы не меняется. Отменить можно через Undo.", "Сбросить", "Отмена"))
                return;

            MarkupLayers.Remove(_table, _key, _locale);
            _reports[_key] = LocalizationChecks.Check(_backend, _table, _key);
            UpdateLayerPanel();
            UpdatePreview();
            _grid?.RefreshItems();
        }

        private void CreateTable()
        {
            if (_backend == null)
                return;

            string name = TextPrompt.Show("Новая таблица", "Имя таблицы (например, UI, Dialogues, Items):", "Main");
            if (name == null)
                return;

            _table = _backend.CreateTable(name) ?? _table;
            Reload();
        }

        private void PingTable()
        {
            UnityEngine.Object asset = _backend?.Asset(_table);
            if (asset == null)
                return;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void AddKey()
        {
            if (_backend == null || _table == null)
                return;

            string key = TextPrompt.Show("Новый ключ", "Ключ строки (например, ui.door.locked):", string.Empty);
            if (key == null)
                return;

            if (!_backend.AddKey(_table, key))
            {
                EditorUtility.DisplayDialog("Typecraft", $"Ключ «{key}» уже есть в таблице.", "OK");
                return;
            }

            _search.SetValueWithoutNotify(string.Empty);
            RecheckAll();
            Select(key, _backend.BaseLocale);
        }

        private void RenameKey()
        {
            if (_key == null)
                return;

            string newKey = TextPrompt.Show("Переименовать ключ", "Новое имя ключа:", _key);
            if (newKey == null || newKey == _key)
                return;

            List<TypecraftText> users = SceneTexts.Localized(_table, _key).ToList();
            if (!_backend.RenameKey(_table, _key, newKey))
            {
                EditorUtility.DisplayDialog("Typecraft", $"Не удалось переименовать: ключ «{newKey}» уже занят.", "OK");
                return;
            }

            MarkupLayers.RenameKey(_table, _key, newKey);
            foreach (TypecraftText text in users)
            {
                Undo.RecordObject(text, "Typecraft: переименование ключа");
                text.SetKey(text.Table, newKey);
                EditorUtility.SetDirty(text);
            }

            if (users.Count > 0)
                TypecraftLog.Info($"Ключ «{_key}» → «{newKey}», обновлено объектов в открытых сценах: {users.Count}. Префабы и закрытые сцены проверь вручную.");

            _key = newKey;
            RecheckAll();
        }

        private void RemoveKey()
        {
            if (_key == null)
                return;

            int users = SceneTexts.Localized(_table, _key).Count();
            string warning = users > 0 ? $"\n\nЕго используют объекты в открытых сценах: {users}." : string.Empty;
            if (!EditorUtility.DisplayDialog("Удалить ключ", $"Удалить «{_key}» со всеми переводами? Отменить можно через Undo.{warning}", "Удалить", "Отмена"))
                return;

            _backend.RemoveKey(_table, _key);
            MarkupLayers.RemoveKey(_table, _key);
            _key = null;
            RecheckAll();
        }

        private void AddLocale()
        {
            if (_backend == null)
                return;

            string locale = TextPrompt.Show("Новый язык", "Код языка: en, ru, zh-Hans, ja, de, fr, es, pt-BR…", string.Empty);
            if (locale == null)
                return;

            if (!_backend.AddLocale(locale))
            {
                EditorUtility.DisplayDialog("Typecraft", $"Язык «{locale}» уже есть.", "OK");
                return;
            }

            Reload();
        }

        private void UpdateSyncButton()
        {
            TableSyncSource source = _table != null ? TableSyncSources.Find(_table) : null;
            _syncButton.tooltip = source != null ? "Синхронизировать из источника: " + source.Describe() : "Источник не настроен — откроется настройка";
            _syncButton.SetEnabled(_download == null);
        }

        private void ConfigureSource()
        {
            if (_backend == null || _table == null)
                return;

            SyncSourceChoice choice = SyncSourceDialog.Show(_table);
            UpdateSyncButton();
            if (choice == SyncSourceChoice.SavedAndSync)
                Sync();
        }

        private void Sync()
        {
            if (_backend == null || _table == null || _download != null)
                return;

            TableSyncSource source = TableSyncSources.Find(_table);
            if (source == null)
            {
                ConfigureSource();
                return;
            }

            if (source.IsFile)
            {
                SheetImport data;
                try
                {
                    data = source.ReadFile();
                }
                catch (Exception exception)
                {
                    EditorUtility.DisplayDialog("Синхронизация", $"Не удалось прочитать источник ({source.Describe()}):\n{exception.Message}", "OK");
                    return;
                }

                Preview(data, source.Describe());
                return;
            }

            if (!GoogleSheets.TryParse(source.Url, out GoogleSheetLink link))
            {
                EditorUtility.DisplayDialog("Синхронизация", "Ссылка на Google Sheets не распознана — поправь её в «Источник…».", "OK");
                return;
            }

            string table = _table;
            string origin = source.Describe();
            _download = GoogleSheetsDownload.Start(link, csv => OnDownloaded(table, csv, origin), OnDownloadFailed);
            _syncLabel.text = $"Загрузка Google Sheets для «{table}»…";
            _syncStatus.style.display = DisplayStyle.Flex;
            UpdateSyncButton();
        }

        private void OnDownloaded(string table, string csv, string origin)
        {
            EndDownload();
            SheetImport data;
            try
            {
                data = SheetImport.FromRows(TableCsv.Parse(csv, ','));
            }
            catch (Exception exception)
            {
                EditorApplication.delayCall += () => EditorUtility.DisplayDialog("Синхронизация", $"Google Sheets отдал данные, но их не удалось разобрать: {exception.Message}", "OK");
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (_backend == null || !_backend.Tables.Contains(table))
                    return;

                if (_table != table)
                {
                    _table = table;
                    _key = null;
                    Reload();
                }

                Preview(data, origin);
            };
        }

        private void OnDownloadFailed(string error)
        {
            EndDownload();
            EditorApplication.delayCall += () => EditorUtility.DisplayDialog("Синхронизация", error, "OK");
        }

        private void CancelDownload()
        {
            _download?.Cancel();
            EndDownload();
        }

        private void EndDownload()
        {
            _download = null;
            _syncStatus.style.display = DisplayStyle.None;
            UpdateSyncButton();
        }

        private void ImportFile()
        {
            if (_backend == null || _table == null)
                return;

            string path = EditorUtility.OpenFilePanelWithFilters("Импорт таблицы", string.Empty, new[] { "Таблицы", "csv,tsv,xlsx", "Все файлы", "*" });
            if (string.IsNullOrEmpty(path))
                return;

            SheetImport data;
            try
            {
                data = new TableSyncSource(_table, TableSyncSource.KindFor(path), path, null, null).ReadFile();
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Импорт", $"Не удалось прочитать файл: {exception.Message}", "OK");
                return;
            }

            Preview(data, System.IO.Path.GetFileName(path));
        }

        private void Preview(SheetImport data, string origin)
        {
            TableSyncPlan plan = TableSync.Plan(_backend, _table, data, origin);
            if (plan.IsEmpty)
            {
                EditorUtility.DisplayDialog("Синхронизация", $"Таблица «{_table}» уже совпадает с источником.", "OK");
                return;
            }

            TableSyncOptions options = TableSyncDialog.Show(plan);
            if (options == null)
                return;

            TableSync.Apply(_backend, plan, options);
            TypecraftLog.Info($"Таблица «{plan.Table}» синхронизирована ({origin}): {TableSync.Summary(plan, options)}.");
            Reload();
        }

        private void Export()
        {
            if (_backend == null || _table == null)
                return;

            ExportDialog.Show(_backend, _table);
        }

        private void OnUndoRedo()
        {
            if (_backend == null || _table == null)
                return;

            _reports.Clear();
            ApplyFilter();
            if (_key != null && _backend.Keys(_table).Contains(_key))
                Select(_key, _locale);
            else
                ShowCell(false);
        }

        private static VisualElement Spacer()
        {
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            return spacer;
        }
    }
}
