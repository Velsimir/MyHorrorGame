using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Dialogue.Editor.Board;
using Velsi.Typecraft.Dialogue.Editor.Inspector;
using Velsi.Typecraft.Dialogue.Editor.Model;
using Velsi.Typecraft.Dialogue.Editor.Playback;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Editor.Window;
using Velsi.Typecraft.Editor.Window.Layout;
using Object = UnityEngine.Object;

namespace Velsi.Typecraft.Dialogue.Editor
{
    public sealed class DialoguesTab : VisualElement, ITypecraftTab, IDialogueBoardHost, IDialogueEditorContext, IDialoguePlaybackHost
    {
        private const string DockSession = "Typecraft.Dialogues.Dock";
        private const string StylePath = "Packages/com.velsi.typecraft/Editor.Dialogue/DialoguesTab.uss";
        private const string AssetSession = "Typecraft.Dialogues.Asset";
        private const string BoardSession = "Typecraft.Dialogues.Board";

        private readonly DropdownField _dialogueField;
        private readonly DropdownField _tableField;
        private readonly DropdownField _localeField;
        private readonly DropdownField _viewField;
        private readonly Button _checkButton;
        private readonly ScrollView _list;
        private readonly VisualElement _boardHost;
        private readonly ScrollView _inspectorHost;
        private readonly List<DialogueIssue> _issues = new List<DialogueIssue>();
        private readonly VisualElement _dockContent;
        private readonly Button _playPage;
        private readonly Button _problemsPage;
        private readonly Button _collapse;
        private readonly DialoguePlaybackPanel _playback;
        private readonly DialogueIssuesPanel _problems;
        private readonly ColumnsLayout _columns;
        private readonly SplitPane _split;

        private IDialogueBoardView _board;
        private DialogueAsset _asset;
        private string _locale;
        private string _nodeId;
        private SpeakerProfile _speaker;
        private DialogueVariableCatalog _flagCatalog;
        private string _flagName;
        private VisualElement _inspector;
        private List<DialogueAsset> _dialogues = new List<DialogueAsset>();
        private bool _stale = true;
        private bool _inspectorQueued;
        private DialogueAsset _currentAsset;
        private string _currentNode;
        private DockPage _page = DockPage.Playback;
        private bool _dockOpen;

        public DialoguesTab()
        {
            AddToClassList("dlg-tab");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (styleSheet != null)
                styleSheets.Add(styleSheet);

            var toolbar = new VisualElement();
            toolbar.AddToClassList("tc-toolbar");
            Add(toolbar);

            _dialogueField = new DropdownField { tooltip = "Диалог" };
            _dialogueField.AddToClassList("dlg-toolbar__dialogue");
            _dialogueField.RegisterValueChangedCallback(_ =>
            {
                int index = _dialogueField.index;
                if (index >= 0 && index < _dialogues.Count)
                    Select(_dialogues[index], null);
            });
            toolbar.Add(_dialogueField);

            _tableField = new DropdownField { tooltip = "Таблица строк диалога по умолчанию" };
            _tableField.AddToClassList("dlg-toolbar__table");
            _tableField.RegisterValueChangedCallback(evt =>
            {
                if (_asset != null && evt.newValue != _asset.DefaultTable && !string.IsNullOrEmpty(evt.newValue))
                    Edit(_asset, "таблица диалога", () => _asset.DefaultTable = evt.newValue, true);
            });
            toolbar.Add(_tableField);

            toolbar.Add(new Button(ShowAddNodeMenu) { text = "+ Нода", tooltip = "Новая нода. На графе — ещё пробел или правый клик" });
            _checkButton = new Button(ShowProblems) { text = "Проверить", tooltip = "Проблемы диалога в нижней панели: клик по строке — к ноде" };
            toolbar.Add(_checkButton);
            toolbar.Add(new Button(PlayFromStart) { text = "▶ Проиграть", tooltip = "Проиграть диалог в окне, без запуска игры" });

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            _viewField = new DropdownField(DialogueBoards.Types.Select(DialogueBoards.Title).ToList(), 0) { tooltip = "Вид доски" };
            _viewField.AddToClassList("dlg-toolbar__view");
            _viewField.RegisterValueChangedCallback(_ => SwitchBoard(DialogueBoards.Types[_viewField.index]));
            toolbar.Add(_viewField);

            _localeField = new DropdownField { tooltip = "Язык текстов на доске и в инспекторе" };
            _localeField.AddToClassList("tc-toolbar__locale");
            _localeField.RegisterValueChangedCallback(evt =>
            {
                _locale = evt.newValue;
                _board?.Refresh();
                RebuildInspector();
                _playback?.OnLocaleChanged();
            });
            toolbar.Add(_localeField);

            _list = TypecraftLayout.VerticalScroll("tc-lib__list");

            _boardHost = new VisualElement();
            _boardHost.AddToClassList("dlg-board");

            _inspectorHost = TypecraftLayout.VerticalScroll("dlg-side");
            _inspectorHost.AddToClassList(TypecraftLayout.FitClass);

            _columns = new ColumnsLayout("Dialogues", _list, _boardHost, _inspectorHost, 220f, 380f, 260f);
            toolbar.Insert(0, _columns.MenuButton);

            var dock = new VisualElement();
            dock.AddToClassList("dlg-dock");
            var dockBar = new VisualElement();
            dockBar.AddToClassList("dlg-row");
            dockBar.AddToClassList("dlg-dock__bar");
            _playPage = new Button(() => OpenDock(DockPage.Playback)) { text = "▶ Проигрывание" };
            _problemsPage = new Button(() => OpenDock(DockPage.Problems)) { text = "Проблемы" };
            dockBar.Add(_playPage);
            dockBar.Add(_problemsPage);
            var dockSpacer = new VisualElement();
            dockSpacer.style.flexGrow = 1;
            dockBar.Add(dockSpacer);
            _collapse = new Button(() => SetDockOpen(!_dockOpen)) { tooltip = "Свернуть / развернуть нижнюю панель" };
            _collapse.AddToClassList("dlg-small");
            dockBar.Add(_collapse);
            dock.Add(dockBar);
            _dockContent = new VisualElement();
            _dockContent.AddToClassList("dlg-dock__content");
            dock.Add(_dockContent);
            _split = new SplitPane("Dialogues.Dock", _columns, dock, false, 300f, 300f, 120f, 160f) { Vertical = true };
            Add(_split);

            _playback = new DialoguePlaybackPanel(this);
            _problems = new DialogueIssuesPanel(Reveal);
            string dockState = SessionState.GetString(DockSession, string.Empty);
            _page = dockState.EndsWith("problems", StringComparison.Ordinal) ? DockPage.Problems : DockPage.Playback;
            SetDockOpen(dockState.StartsWith("open", StringComparison.Ordinal));

            string boardType = SessionState.GetString(BoardSession, null);
            SwitchBoard(DialogueBoards.Types.FirstOrDefault(type => type.FullName == boardType));

            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.projectChanged += OnProjectChanged;
        }

        public string Title => "☰ Диалоги";
        public int Order => 100;
        public VisualElement Element => this;
        public DialogueAsset Asset => _asset;
        public string Locale => _locale ?? DialogueTexts.Locale(null);
        public VisualElement Root => this;
        public IDialogueBoardView Board => _board;
        public string SelectedNodeId => _nodeId;
        public IReadOnlyList<DialogueIssue> Issues => _issues;
        public VisualElement InspectorElement => _inspector;
        public DialoguePlaybackPanel Playback => _playback;
        public DialogueIssuesPanel Problems => _problems;
        public bool DockOpen => _dockOpen;
        public DockPage Page => _page;
        public string CurrentNodeId => _currentNode;

        public static void Open(DialogueAsset asset, string nodeId = null) =>
            TypecraftWindow.ShowTab<DialoguesTab>(tab => tab.Select(asset, nodeId));

        public static void OpenSpeaker(SpeakerProfile speaker) =>
            TypecraftWindow.ShowTab<DialoguesTab>(tab => tab.SelectSpeaker(speaker));

        public void OnShow()
        {
            if (!_stale && _asset != null)
                return;

            _stale = false;
            DialogueIdGuard.FixDuplicates();
            ReloadToolbar();
            if (_asset == null)
            {
                string path = AssetDatabase.GUIDToAssetPath(SessionState.GetString(AssetSession, string.Empty));
                DialogueAsset remembered = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<DialogueAsset>(path);
                SetAsset(remembered != null ? remembered : _dialogues.FirstOrDefault());
            }
            else
            {
                SetAsset(_asset);
            }
        }

        public void Tick(float deltaTime)
        {
            _playback.Observe(EditorApplication.isPlaying && Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<DialoguePlayer>() : null);
            _playback.Tick(deltaTime);
            switch (_inspector)
            {
                case NodeInspector node:
                    node.Tick(deltaTime);
                    break;
                case SpeakerInspector speaker:
                    speaker.Tick(deltaTime);
                    break;
            }
        }

        public void OnTypecraftChanged()
        {
            if (!IsShown())
            {
                _stale = true;
                return;
            }

            ReloadToolbar();
            _board?.Refresh();
            Revalidate();
            RebuildInspector();
            _playback.RefreshFlags();
            _playback.OnLocaleChanged();
        }

        public void Dispose()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.projectChanged -= OnProjectChanged;
            _playback.Dispose();
            DisposeInspector();
            _board?.Dispose();
            _board = null;
        }

        public void Select(DialogueAsset asset, string nodeId)
        {
            if (asset == null)
                return;

            if (asset != _asset || _stale)
            {
                _stale = false;
                DialogueIdGuard.FixDuplicates();
                SetAsset(asset);
            }

            if (!string.IsNullOrEmpty(nodeId))
                SelectNode(nodeId, true);
        }

        public void SelectNode(string nodeId) => SelectNode(nodeId, true);

        public void SelectNode(string nodeId, bool frame)
        {
            ClearSelection();
            _nodeId = nodeId;
            _board?.SelectNode(nodeId, frame);
            RebuildInspector();
            RebuildList();
        }

        public void SelectSpeaker(SpeakerProfile speaker)
        {
            if (_asset == null || _stale)
                OnShow();
            ClearSelection();
            _speaker = speaker;
            _board?.SelectNode(null, false);
            RebuildInspector();
            RebuildList();
        }

        public void SelectFlag(DialogueVariableCatalog catalog, string name)
        {
            ClearSelection();
            _flagCatalog = catalog;
            _flagName = name;
            _board?.SelectNode(null, false);
            RebuildInspector();
            RebuildList();
        }

        public void Edit(Object target, string undoName, Action change, bool rebuildInspector = false)
        {
            if (target == null)
                return;

            DialogueEdits.Change(target, undoName, change);
            if (target is DialogueVariableCatalog || target is SpeakerProfile)
                RebuildList();
            if (target == _asset && target is DialogueAsset)
                ReloadToolbar();
            _board?.Refresh();
            Revalidate();
            if (rebuildInspector)
                QueueInspector();
        }

        public void RemoveNode(string nodeId)
        {
            if (_asset == null || string.IsNullOrEmpty(nodeId))
                return;

            DialogueEdits.RemoveNodes(_asset, new[] { nodeId });
            if (_nodeId == nodeId)
                _nodeId = null;
            _board?.Refresh();
            Revalidate();
            QueueInspector();
            RebuildList();
        }

        public DialogueNode AddNode(Type type, Vector2? position = null)
        {
            if (_asset == null || type == null)
                return null;

            DialogueNode node = DialogueEdits.AddNode(_asset, type, position ?? (_board?.SuggestPosition() ?? Vector2.zero));
            _board?.Refresh();
            Revalidate();
            SelectNode(node.Id, true);
            return node;
        }

        public void OnNodeSelected(string nodeId)
        {
            if (nodeId == _nodeId && _speaker == null && _flagCatalog == null)
                return;

            ClearSelection();
            _nodeId = nodeId;
            QueueInspector();
            RebuildList();
        }

        public void OnBoardEdited(IReadOnlyList<string> selectNodes = null)
        {
            if (_nodeId != null && (_asset == null || !_asset.TryGetNode(_nodeId, out _)))
                _nodeId = null;
            if (selectNodes != null && selectNodes.Count > 0)
            {
                ClearSelection();
                _nodeId = selectNodes[0];
            }

            Revalidate();
            QueueInspector();
            RebuildList();
        }

        public DialogueAsset CreateDialogue(string path)
        {
            ITableBackend backend = DialogueTexts.Backend;
            string table = _asset != null && !string.IsNullOrEmpty(_asset.DefaultTable) ? _asset.DefaultTable : backend?.Tables.FirstOrDefault();
            DialogueAsset asset = DialogueProject.CreateDialogue(path, table);
            ReloadToolbar();
            Select(asset, null);
            return asset;
        }

        public SpeakerProfile CreateSpeaker(string name)
        {
            SpeakerProfile speaker = DialogueProject.CreateSpeaker(BaseFolder() + "/Speakers", name);
            RebuildList();
            SelectSpeaker(speaker);
            return speaker;
        }

        public DialogueVariableDefinition CreateFlag()
        {
            DialogueVariableCatalog catalog = DialogueProject.CatalogForNewFlag(BaseFolder());
            var definition = new DialogueVariableDefinition(DialogueProject.UniqueVariableName("flag"), DialogueValue.Bool(false));
            Edit(catalog, "новый флаг", () => catalog.Add(definition));
            SelectFlag(catalog, definition.Name);
            return definition;
        }

        public void ShowCurrent(DialogueAsset asset, string nodeId)
        {
            _currentAsset = asset;
            _currentNode = nodeId;
            if (asset != null && asset != _asset)
                Select(asset, null);
            _board?.SetCurrent(asset != null && asset == _asset ? nodeId : null);
        }

        public void Reveal(DialogueAsset asset, string nodeId)
        {
            if (asset == null)
                return;

            Select(asset, null);
            if (!string.IsNullOrEmpty(nodeId) && asset.TryGetNode(nodeId, out _))
                SelectNode(nodeId, true);
        }

        public void OpenDock(DockPage page)
        {
            _page = page;
            SetDockOpen(true);
        }

        public void PlayFromStart()
        {
            OpenDock(DockPage.Playback);
            _playback.Play(false);
        }

        public void ShowProblems()
        {
            Revalidate();
            OpenDock(DockPage.Problems);
            _problems.ShowCurrent(_asset, _issues);
        }

        public void RenameFlag(DialogueVariableCatalog catalog, string from, string to, bool rewriteDialogues)
        {
            if (catalog == null || !catalog.TryGet(from, out DialogueVariableDefinition definition))
                return;

            DialogueFlagRename.Prepare(from, to).Apply(catalog, definition, rewriteDialogues);
            if (_flagCatalog == catalog && _flagName == from)
                _flagName = to;
            AfterExternalEdit();
        }

        private bool RenameFlagInteractive(DialogueVariableCatalog catalog, string from, string to)
        {
            if (catalog.Contains(to))
            {
                EditorUtility.DisplayDialog("Typecraft", $"Флаг «{to}» уже есть в каталоге.", "OK");
                return false;
            }

            DialogueFlagRename plan = DialogueFlagRename.Prepare(from, to);
            if (plan.Places == 0)
            {
                RenameFlag(catalog, from, to, false);
                return true;
            }

            int answer = EditorUtility.DisplayDialogComplex("Typecraft", $"Переименовать флаг «{from}» → «{to}»?\n\n{plan.Describe()}\n\nУсловия и присваивания будут переписаны. Откатить — Ctrl+Z.", "Переименовать везде", "Отмена", "Только в каталоге");
            if (answer == 1)
                return false;

            RenameFlag(catalog, from, to, answer == 0);
            return true;
        }

        private void AfterExternalEdit()
        {
            foreach (DialogueVariableCatalog catalog in DialogueProject.Catalogs())
                catalog.InvalidateLookup();
            _asset?.InvalidateLookup();
            _board?.Refresh();
            Revalidate();
            QueueInspector();
            RebuildList();
            _playback.RefreshFlags();
        }

        private void SetDockOpen(bool open)
        {
            _dockOpen = open;
            SessionState.SetString(DockSession, (open ? "open" : "closed") + "/" + (_page == DockPage.Problems ? "problems" : "playback"));
            _collapse.text = open ? "▾" : "▴";
            _dockContent.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            _split?.SetFixedAuto(!open);
            _playPage.EnableInClassList("dlg-dock__page--active", open && _page == DockPage.Playback);
            _problemsPage.EnableInClassList("dlg-dock__page--active", open && _page == DockPage.Problems);

            VisualElement shown = _page == DockPage.Problems ? (VisualElement)_problems : _playback;
            if (shown.parent != _dockContent)
            {
                _dockContent.Clear();
                _dockContent.Add(shown);
            }

            if (open && _page == DockPage.Problems && !_problems.ShowsAll)
                _problems.ShowCurrent(_asset, _issues);
        }

        private bool IsShown() => panel != null;

        private void SetAsset(DialogueAsset asset)
        {
            _asset = asset;
            _nodeId = null;
            ClearSelection();
            if (asset != null)
                SessionState.SetString(AssetSession, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)));

            ReloadToolbar();
            _board?.Show(asset);
            _board?.SetCurrent(asset != null && asset == _currentAsset ? _currentNode : null);
            Revalidate();
            RebuildInspector();
            RebuildList();
        }

        private void SwitchBoard(Type type)
        {
            _board?.Dispose();
            _boardHost.Clear();
            _board = DialogueBoards.Create(type);
            _board.Bind(this);
            _board.Element.style.flexGrow = 1;
            _boardHost.Add(_board.Element);

            int index = DialogueBoards.Types.ToList().IndexOf(_board.GetType());
            if (index >= 0)
            {
                _viewField.SetValueWithoutNotify(_viewField.choices[index]);
                SessionState.SetString(BoardSession, _board.GetType().FullName);
            }

            _board.Show(_asset);
            _board.SetIssues(_issues);
            _board.SetCurrent(_asset != null && _asset == _currentAsset ? _currentNode : null);
            if (_nodeId != null)
                _board.SelectNode(_nodeId, true);
        }

        private void ReloadToolbar()
        {
            _dialogues = DialogueProject.Dialogues();
            List<string> names = _dialogues.Select(DialogueTitle).ToList();
            _dialogueField.choices = names.Count > 0 ? names : new List<string> { "(нет диалогов)" };
            int index = _asset != null ? _dialogues.IndexOf(_asset) : -1;
            _dialogueField.SetValueWithoutNotify(index >= 0 ? names[index] : _asset == null ? "(не выбран)" : _asset.name);

            ITableBackend backend = DialogueTexts.Backend;
            List<string> tables = backend != null ? backend.Tables.Distinct().ToList() : new List<string>();
            if (_asset != null && !string.IsNullOrEmpty(_asset.DefaultTable) && !tables.Contains(_asset.DefaultTable))
                tables.Add(_asset.DefaultTable);
            _tableField.choices = tables;
            _tableField.SetValueWithoutNotify(_asset != null ? _asset.DefaultTable ?? string.Empty : string.Empty);
            _tableField.SetEnabled(_asset != null);

            List<string> locales = backend != null ? backend.Locales.ToList() : new List<string> { Typecraft.Locale };
            _locale = DialogueTexts.Locale(_locale);
            _localeField.choices = locales;
            _localeField.SetValueWithoutNotify(_locale);
        }

        private string DialogueTitle(DialogueAsset asset)
        {
            string duplicate = _dialogues.Count(other => other.name == asset.name) > 1 ? $" ({DialogueProject.FolderOf(asset)})".Replace('/', '∕') : string.Empty;
            return asset.name + duplicate;
        }

        private void Revalidate()
        {
            _issues.Clear();
            if (_asset != null)
                DialogueChecks.Run(_asset, DialogueProject.MergedCatalog(), _issues);
            _board?.SetIssues(_issues);
            _problems?.Refresh(_asset, _issues);

            int errors = _issues.Count(issue => issue.Severity == DialogueIssueSeverity.Error);
            int warnings = _issues.Count(issue => issue.Severity == DialogueIssueSeverity.Warning);
            _checkButton.text = errors + warnings == 0 ? "Проверить ✓" : $"Проверить · {errors + warnings}";
            _checkButton.EnableInClassList("dlg-check--error", errors > 0);
            _checkButton.EnableInClassList("dlg-check--warning", errors == 0 && warnings > 0);
        }

        private void ShowAddNodeMenu()
        {
            if (_asset == null)
                return;

            var menu = new GenericMenu();
            foreach (Type type in DialogueNodeInfo.Types)
            {
                Type captured = type;
                menu.AddItem(new GUIContent($"{DialogueNodeInfo.Glyph(type)} {DialogueNodeInfo.Title(type)}"), false, () => AddNode(captured));
            }

            menu.ShowAsContext();
        }

        private void ClearSelection()
        {
            _nodeId = null;
            _speaker = null;
            _flagCatalog = null;
            _flagName = null;
        }

        private void QueueInspector()
        {
            if (_inspectorQueued)
                return;

            _inspectorQueued = true;
            schedule.Execute(() =>
            {
                _inspectorQueued = false;
                RebuildInspector();
            });
        }

        private void DisposeInspector()
        {
            (_inspector as IDisposable)?.Dispose();
            _inspector = null;
        }

        private void RebuildInspector()
        {
            float scroll = _inspectorHost.scrollOffset.y;
            DisposeInspector();
            _inspectorHost.Clear();

            if (_speaker != null)
                _inspector = new SpeakerInspector(this, _speaker, () => DeleteAsset(_speaker, "говорящего"));
            else if (_flagCatalog != null && _flagCatalog.TryGet(_flagName, out DialogueVariableDefinition definition))
                _inspector = new FlagInspector(this, _flagCatalog, definition, DeleteFlag, value => RenameFlagInteractive(_flagCatalog, definition.Name, value));
            else if (_asset != null && _nodeId != null && _asset.TryGetNode(_nodeId, out DialogueNode node))
                _inspector = new NodeInspector(this, node, _issues.Where(issue => issue.NodeId == _nodeId).ToList());
            else
                _inspector = DialogueSummary();

            _inspectorHost.Add(_inspector);
            foreach (KeyField key in _inspector.Query<KeyField>().ToList())
                key.Refresh();
            _inspectorHost.schedule.Execute(() => _inspectorHost.scrollOffset = new Vector2(0f, scroll));
        }

        private VisualElement DialogueSummary()
        {
            var panel = new VisualElement();
            panel.AddToClassList("dlg-inspector");
            if (_asset == null)
            {
                var hint = new Label("Выбери диалог слева или создай новый «+».");
                hint.AddToClassList("tc-placeholder");
                panel.Add(hint);
                return panel;
            }

            var title = new Label(_asset.name);
            title.AddToClassList("tc-lib__title");
            panel.Add(title);
            panel.Add(NodeInspector.Hint(AssetDatabase.GetAssetPath(_asset)));
            panel.Add(NodeInspector.Hint($"Нод: {_asset.Nodes.Count} · проблем: {_issues.Count}"));
            panel.Add(NodeInspector.Hint("Выбери ноду на доске. Связи — перетаскиванием с выхода на вход, удаление — Delete, копирование — Ctrl+C / Ctrl+V / Ctrl+D, новая нода — пробел или правый клик."));
            var id = new Label("Id " + _asset.Id);
            id.AddToClassList("dlg-id");
            panel.Add(id);
            return panel;
        }

        private void RebuildList()
        {
            _list.Clear();
            AddSection("Диалоги", "Новый диалог", CreateDialogueInteractive);
            foreach (DialogueAsset asset in _dialogues.Where(asset => asset != null))
            {
                DialogueAsset captured = asset;
                AddItem(asset.name, asset.Nodes.Count.ToString(), asset == _asset && _speaker == null && _flagCatalog == null, () => Select(captured, null), menu =>
                {
                    menu.AddItem(new GUIContent("Переименовать"), false, () => RenameAsset(captured));
                    menu.AddItem(new GUIContent("Дублировать"), false, () => DuplicateDialogue(captured));
                    menu.AddItem(new GUIContent("Показать в Project"), false, () => EditorGUIUtility.PingObject(captured));
                    menu.AddSeparator(string.Empty);
                    menu.AddItem(new GUIContent("Удалить"), false, () => DeleteAsset(captured, "диалог"));
                });
            }

            AddSection("Говорящие", "Новый говорящий", CreateSpeakerInteractive);
            foreach (SpeakerProfile speaker in DialogueProject.Speakers())
            {
                SpeakerProfile captured = speaker;
                AddItem("☺ " + speaker.name, DialogueTexts.SpeakerName(speaker, _asset != null ? _asset.DefaultTable : null, Locale), speaker == _speaker, () => SelectSpeaker(captured), menu =>
                {
                    menu.AddItem(new GUIContent("Переименовать"), false, () => RenameAsset(captured));
                    menu.AddItem(new GUIContent("Показать в Project"), false, () => EditorGUIUtility.PingObject(captured));
                    menu.AddSeparator(string.Empty);
                    menu.AddItem(new GUIContent("Удалить"), false, () => DeleteAsset(captured, "говорящего"));
                });
            }

            AddSection("Флаги", "Новый флаг (каталог создастся, если его нет)", () => CreateFlag());
            List<DialogueVariableCatalog> catalogs = DialogueProject.Catalogs();
            foreach (DialogueVariableCatalog catalog in catalogs)
            foreach (DialogueVariableDefinition definition in catalog.Variables.Where(definition => definition != null))
            {
                DialogueVariableCatalog owner = catalog;
                string name = definition.Name;
                string note = definition.Type.ToString().ToLowerInvariant() + (catalogs.Count > 1 ? " · " + catalog.name : string.Empty);
                AddItem("⚑ " + name, note, owner == _flagCatalog && name == _flagName, () => SelectFlag(owner, name), null);
            }
        }

        private void AddSection(string title, string tooltip, Action create)
        {
            var row = new VisualElement();
            row.AddToClassList("dlg-row");
            row.AddToClassList("dlg-list-section");
            var label = new Label(title);
            label.AddToClassList("tc-lib__section");
            row.Add(label);
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            row.Add(spacer);
            var button = new Button(create) { text = "+", tooltip = tooltip };
            button.AddToClassList("dlg-small");
            row.Add(button);
            _list.Add(row);
        }

        private void AddItem(string title, string note, bool selected, Action click, Action<GenericMenu> menu)
        {
            var item = new VisualElement();
            item.AddToClassList("tc-lib__item");
            item.EnableInClassList("tc-lib__item--selected", selected);
            item.Add(new Label(title) { name = "title" });
            item.Add(new Label(DialogueTexts.Shorten(note, 24)) { name = "note" });
            item.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;
                click();
                _columns.HideList();
            });
            if (menu != null)
            {
                item.RegisterCallback<ContextClickEvent>(_ =>
                {
                    var context = new GenericMenu();
                    menu(context);
                    context.ShowAsContext();
                });
            }

            _list.Add(item);
        }

        private string BaseFolder()
        {
            string folder = _asset != null ? DialogueProject.FolderOf(_asset) : null;
            return !string.IsNullOrEmpty(folder) && folder.StartsWith("Assets", StringComparison.Ordinal) ? folder : DialogueProject.Folder;
        }

        private void CreateDialogueInteractive()
        {
            string folder = DialogueProject.Folder;
            string path = EditorUtility.SaveFilePanelInProject("Новый диалог", "Dialogue", "asset", "Куда сохранить диалог", AssetDatabase.IsValidFolder(folder) ? folder : "Assets");
            if (string.IsNullOrEmpty(path))
                return;

            DialogueProject.Folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            CreateDialogue(path);
        }

        private void CreateSpeakerInteractive()
        {
            string name = TextPrompt.Show("Новый говорящий", "Имя ассета говорящего (латиницей):", "Speaker");
            if (string.IsNullOrWhiteSpace(name))
                return;

            name = new string(name.Trim().Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == ' ').ToArray());
            if (name.Length > 0)
                CreateSpeaker(name);
        }

        private void RenameAsset(Object asset)
        {
            string name = TextPrompt.Show("Переименовать", "Новое имя ассета:", asset.name);
            if (string.IsNullOrWhiteSpace(name) || name == asset.name)
                return;

            string error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(asset), name.Trim());
            if (!string.IsNullOrEmpty(error))
                EditorUtility.DisplayDialog("Typecraft", error, "OK");
            ReloadToolbar();
            RebuildList();
            RebuildInspector();
        }

        private void DuplicateDialogue(DialogueAsset asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string copy = AssetDatabase.GenerateUniqueAssetPath(path);
            if (!AssetDatabase.CopyAsset(path, copy))
                return;

            DialogueIdGuard.FixDuplicates();
            ReloadToolbar();
            Select(AssetDatabase.LoadAssetAtPath<DialogueAsset>(copy), null);
        }

        private void DeleteAsset(Object asset, string what)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path) || !EditorUtility.DisplayDialog("Typecraft", $"Удалить {what} «{asset.name}»? Ассет уйдёт в корзину системы.", "Удалить", "Отмена"))
                return;

            if (!AssetDatabase.MoveAssetToTrash(path))
            {
                EditorUtility.DisplayDialog("Typecraft", $"Не удалось переместить {path} в корзину.", "OK");
                return;
            }

            if (asset == _asset)
                _asset = null;
            if (asset == _speaker)
                _speaker = null;
            ReloadToolbar();
            SetAsset(_asset != null ? _asset : _dialogues.FirstOrDefault());
        }

        private void DeleteFlag()
        {
            DialogueVariableCatalog catalog = _flagCatalog;
            string name = _flagName;
            if (catalog == null || !EditorUtility.DisplayDialog("Typecraft", $"Удалить флаг «{name}» из каталога {catalog.name}? Условия в диалогах останутся и будут подсвечены.", "Удалить", "Отмена"))
                return;

            _flagCatalog = null;
            _flagName = null;
            Edit(catalog, "удаление флага", () => catalog.Remove(name), true);
        }

        private void OnUndoRedo()
        {
            if (!IsShown())
            {
                _stale = true;
                return;
            }

            foreach (DialogueVariableCatalog catalog in DialogueProject.Catalogs())
                catalog.InvalidateLookup();
            _asset?.InvalidateLookup();
            ReloadToolbar();
            _board?.Refresh();
            Revalidate();
            if (_nodeId != null && (_asset == null || !_asset.TryGetNode(_nodeId, out _)))
                _nodeId = null;
            RebuildInspector();
            RebuildList();
            _playback.RefreshFlags();
        }

        private void OnProjectChanged()
        {
            if (!IsShown())
            {
                _stale = true;
                return;
            }

            if (_asset == null && _dialogues.Count == 0)
            {
                ReloadToolbar();
                RebuildList();
                return;
            }

            if (!ReferenceEquals(_asset, null) && _asset == null)
            {
                ReloadToolbar();
                SetAsset(_dialogues.FirstOrDefault());
                return;
            }

            ReloadToolbar();
            RebuildList();
        }
    }

    public enum DockPage
    {
        Playback,
        Problems
    }
}
