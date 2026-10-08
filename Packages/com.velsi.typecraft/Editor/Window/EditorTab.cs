using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Fonts;
using Velsi.Typecraft.Editor.Library;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Editor.Window.Dialogs;
using Velsi.Typecraft.Editor.Window.Drawer;
using Velsi.Typecraft.Editor.Window.Effects;
using Velsi.Typecraft.Editor.Window.Markup;
using Velsi.Typecraft.Editor.Window.Preview;
using Velsi.Typecraft.Editor.Window.Targets;
using Velsi.Typecraft.Fonts;
using Velsi.Typecraft.Library;
using Velsi.Typecraft.Markup;
using Velsi.Typecraft.Styles;
using Velsi.Typecraft.Text;
#if TYPECRAFT_INPUTSYSTEM
using Velsi.Typecraft.InputSystem;
#endif

namespace Velsi.Typecraft.Editor.Window
{
    public sealed class EditorTab : VisualElement, IDisposable
    {
        private static readonly (DeviceFamily Family, string Label)[] Devices =
        {
            (DeviceFamily.KeyboardMouse, "KBM"),
            (DeviceFamily.Xbox, "Xbox"),
            (DeviceFamily.PlayStation, "PS"),
            (DeviceFamily.Switch, "Switch"),
            (DeviceFamily.SteamDeck, "Deck"),
            (DeviceFamily.Touch, "Touch")
        };

        private readonly TextPreview _preview = new TextPreview();
        private readonly MarkupField _markupField;
        private readonly GalleryDrawer _drawer = new GalleryDrawer();
        private readonly List<MarkupDiagnostic> _diagnostics = new List<MarkupDiagnostic>();
        private readonly Label _targetLabel;
        private readonly Toggle _lockToggle;
        private readonly DropdownField _localeField;
        private readonly VisualElement _deviceBar;
        private readonly Button _applyButton;
        private readonly Button _toTableButton;
        private readonly Button _animateButton;
        private readonly VisualElement _floatingBar;
        private readonly Label _selectionLabel;
        private readonly Label _diagnosticsLabel;
        private readonly Label _modeLabel;
        private readonly Label _readOnlyBanner;
        private readonly EffectComponentsWarning _effectWarning = new EffectComponentsWarning();
        private readonly VisualElement _chips;
        private readonly TagInspector _tagInspector = new TagInspector();
        private readonly Button _markersButton;
        private readonly FormOverlay _overlay = new FormOverlay();
        private ColorField _colorField;

        private ITextEditTarget _target = new ScratchTarget();
        private string _working = string.Empty;
        private bool _dirty;
        private string _locale;
        private DeviceFamily _device = DeviceFamily.KeyboardMouse;
        private TextRange _selection;
        private bool _prompting;

        public EditorTab()
        {
            AddToClassList("tc-editor");

            var toolbar = new VisualElement();
            toolbar.AddToClassList("tc-toolbar");
            Add(toolbar);

            _targetLabel = new Label();
            _targetLabel.AddToClassList("tc-toolbar__target");
            toolbar.Add(_targetLabel);

            _lockToggle = new Toggle("🔒") { tooltip = "Не переключаться на выделенный объект" };
            _lockToggle.AddToClassList("tc-toolbar__lock");
            toolbar.Add(_lockToggle);

            toolbar.Add(new Button(() =>
            {
                if (ResolvePendingWordChange())
                    SetTarget(new ScratchTarget());
            }) { text = "Черновик", tooltip = "Свободный текст без объекта" });
            toolbar.Add(Spacer());

            _localeField = new DropdownField { tooltip = "Язык превью" };
            _localeField.AddToClassList("tc-toolbar__locale");
            _localeField.RegisterValueChangedCallback(evt =>
            {
                _locale = evt.newValue;
                RefreshAll();
            });
            toolbar.Add(_localeField);

            _deviceBar = new VisualElement();
            _deviceBar.AddToClassList("tc-seg");
            foreach ((DeviceFamily family, string label) in Devices)
            {
                var button = new Button(() => SetDevice(family)) { text = label, userData = family };
                _deviceBar.Add(button);
            }
            toolbar.Add(_deviceBar);

            var playback = new VisualElement();
            playback.AddToClassList("tc-seg");
            _animateButton = new Button(ToggleAnimation) { text = "❚❚", tooltip = "Анимация TMPEffects" };
            playback.Add(_animateButton);
            playback.Add(new Button(_preview.RestartTypewriter) { text = "⟲ машинка", tooltip = "Показать текст по символу" });
            playback.Add(new Button(_preview.ShowAll) { text = "Весь", tooltip = "Показать весь текст" });
            _markersButton = new Button(ToggleMarkers) { text = "‖ метки", tooltip = "Показывать паузы и задержки над текстом" };
            _markersButton.AddToClassList("tc-seg--on");
            playback.Add(_markersButton);
            toolbar.Add(playback);

            _toTableButton = new Button(MoveToTable) { text = "→ в таблицу", tooltip = "Вынести текст в таблицу строк и перевести объект на ключ" };
            toolbar.Add(_toTableButton);

            _applyButton = new Button(Apply) { text = "Применить", tooltip = "Ctrl+Enter" };
            _applyButton.AddToClassList("tc-toolbar__apply");
            toolbar.Add(_applyButton);

            _readOnlyBanner = new Label();
            _readOnlyBanner.AddToClassList("tc-banner");
            Add(_readOnlyBanner);
            _effectWarning.Added += RefreshChrome;
            Add(_effectWarning);

            var stage = new VisualElement();
            stage.AddToClassList("tc-stage");
            stage.Add(_preview.Element);
            _floatingBar = BuildFloatingBar();
            stage.Add(_floatingBar);
            _chips = new VisualElement();
            _chips.AddToClassList("tc-chips");
            stage.Add(_chips);
            _tagInspector.Changed += ReplaceTag;
            stage.Add(_tagInspector);
            Add(stage);

            _markupField = new MarkupField(MarkupCandidates.For);
            _markupField.MarkupChanged += OnMarkupChanged;
            _markupField.SelectionChanged += OnFieldSelectionChanged;
            _markupField.ApplyRequested += Apply;
            Add(_markupField);

            _drawer.WrapRequested += WrapSelection;
            _drawer.InsertRequested += InsertAtSelection;
            _drawer.FontRoleRequested += role =>
            {
                _target.SetFontRole(role);
                RefreshPreview();
            };
            _drawer.CreateStyleRequested += () => OpenStyleForm(false);
            _drawer.CreateMaterialRequested += OpenMaterialForm;
            _drawer.CreateEffectRequested += OpenEffectForm;
            Add(_drawer);

            var status = new VisualElement();
            status.AddToClassList("tc-status");
            _selectionLabel = new Label();
            _diagnosticsLabel = new Label();
            _diagnosticsLabel.AddToClassList("tc-status__warn");
            _modeLabel = new Label();
            status.Add(_selectionLabel);
            status.Add(_diagnosticsLabel);
            status.Add(Spacer());
            status.Add(_modeLabel);
            Add(status);

            _preview.SelectionChanged += OnPreviewSelectionChanged;
            _preview.SelectionCleared += OnPreviewSelectionCleared;
            _preview.CommandClicked += OpenTagInspector;

            Add(_overlay);

            schedule.Execute(UpdateFloatingBar).Every(60);
            SetTarget(_target);
        }

        public FormOverlay Overlay => _overlay;

        public void OpenStyleForm(bool fromSelection)
        {
            StyleLibrary library = Typecraft.Settings != null ? Typecraft.Settings.Styles : null;
            if (library == null)
            {
                EditorUtility.DisplayDialog("Typecraft", "Нет библиотеки стилей — Tools → Typecraft → Create Project Settings.", "OK");
                return;
            }

            TextRange selection = _target.CanWrite ? _selection : default;
            var form = new StyleForm(library, _working, selection, fromSelection && !selection.IsEmpty, _locale, _device);
            form.Saved += saved =>
            {
                if (saved.ResultMarkup != null && _target.CanWrite)
                    Commit(saved.ResultMarkup, saved.ResultRange);
                TypecraftLog.Info($"Стиль [{saved.SavedId}] добавлен в библиотеку");
            };
            _overlay.Show(form);
        }

        public void OpenMaterialForm(string roleId)
        {
            FontProfile fonts = Typecraft.Settings != null ? Typecraft.Settings.Fonts : null;
            if (fonts == null)
            {
                EditorUtility.DisplayDialog("Typecraft", "Нет профиля шрифтов — Tools → Typecraft → Create Project Settings.", "OK");
                return;
            }

            string role = roleId ?? _target.FontRole ?? Typecraft.Settings.DefaultFontRole;
            var form = new MaterialPresetForm(fonts, role, _locale);
            form.Saved += (savedRole, preset) => TypecraftLog.Info($"Пресет материала «{preset}» роли {savedRole} создан");
            _overlay.Show(form);
        }

        public void OpenEffectForm()
        {
#if TYPECRAFT_TMPEFFECTS
            var form = new EffectForm(_working, _target.CanWrite ? _selection : default, _locale, _device);
            form.Saved += tag => TypecraftLog.Info($"Эффект <{tag}> добавлен в базу анимаций проекта");
            _overlay.Show(form);
#else
            EditorUtility.DisplayDialog("Typecraft", "Нужен пакет TMPEffects.", "OK");
#endif
        }

        public ITextEditTarget Target => _target;

        public void SetTarget(ITextEditTarget target)
        {
            _target = target ?? new ScratchTarget();
            _working = _target.Markup;
            _dirty = false;
            _markupField.SetValue(_working, notify: false);
            _markupField.SetReadOnly(!_target.CanWrite);
            RefreshAll();
        }

        public bool HasPendingWordChange => _dirty && _target is LocalizedTextTarget localized && localized.ChangesWords(_working);

        public void Pin(ITextEditTarget target)
        {
            if (!ResolvePendingWordChange())
                return;

            _lockToggle.value = true;
            SetTarget(target);
        }

        public void FollowSelection()
        {
            if (_lockToggle.value || _prompting)
                return;

            GameObject selected = Selection.activeGameObject;
            if (selected == null)
                return;

            ITextEditTarget wanted = null;
            var typecraftText = selected.GetComponent<TypecraftText>();
            if (typecraftText != null)
            {
                ITextEditTarget candidate = TargetFor(typecraftText);
                if (_target.GetType() != candidate.GetType() || _target.Reference != candidate.Reference)
                    wanted = candidate;
            }
            else
            {
                var tmp = selected.GetComponent<TMP_Text>();
                if (tmp != null && (_target is not TmpTextTarget || _target.Reference != tmp))
                    wanted = new TmpTextTarget(tmp);
            }

            if (wanted == null || (_dirty && !HasPendingWordChange) || !ResolvePendingWordChange())
                return;

            SetTarget(wanted);
        }

        public bool ResolvePendingWordChange()
        {
            if (!HasPendingWordChange)
                return true;
            if (_prompting)
                return false;

            var target = (LocalizedTextTarget)_target;
            TransferResult restored = target.RestoreWords(_working);
            string refusal = restored.Exact ? null : "часть тегов стояла на изменённых словах.\n" + restored.Describe();

            _prompting = true;
            TextChangeChoice choice;
            try
            {
                choice = TextChangeDialog.Show(target.Subject, target.TableText, Decoration.Strip(_working), refusal);
            }
            finally
            {
                _prompting = false;
            }

            if (choice == TextChangeChoice.Cancel)
                return false;

            string markup = choice == TextChangeChoice.RestoreWords ? restored.Markup : _working;
            target.Write(markup);
            _working = markup;
            _dirty = false;
            _markupField.SetValue(_working, notify: false);
            RefreshPreview();
            return true;
        }

        private ITextEditTarget TargetFor(TypecraftText text) =>
            text.Mode == TextMode.Localized ? new LocalizedTextTarget(text, () => _locale) : new TypecraftTextTarget(text);

        public void Tick(float deltaTime)
        {
            if (!_target.IsValid)
                SetTarget(_target.Reference != null && _target.Reference.GetComponent<TypecraftText>() is TypecraftText text ? TargetFor(text) : new ScratchTarget());
            else if (!_dirty && _target.Markup != _working)
                SetTarget(_target);

            _preview.Tick(deltaTime);
            _overlay.Tick(deltaTime);
        }

        public void RefreshAll()
        {
            TypecraftSettings settings = Typecraft.Settings;
            RefreshLocales(settings);
            UpdateDeviceButtons();
            RefreshPreview();
            _drawer.Refresh(_locale, _device);
            RefreshChrome();
        }

        public void Dispose()
        {
            _overlay.Close();
            _preview.Dispose();
            _drawer.Dispose();
        }

        private void RefreshLocales(TypecraftSettings settings)
        {
            var locales = new List<string>();
            if (settings?.TextSource != null)
                locales.AddRange(settings.TextSource.Locales);
            if (locales.Count == 0)
                locales.Add(settings != null ? settings.DefaultLocale : "en");

            _locale ??= Typecraft.Locale;
            if (!locales.Contains(_locale))
                locales.Insert(0, _locale);

            _localeField.choices = locales;
            _localeField.SetValueWithoutNotify(_locale);
        }

        private void RefreshPreview()
        {
            TypecraftSettings settings = Typecraft.Settings;
            FontVariant variant = null;
            if (settings != null && settings.Fonts != null)
                variant = settings.Fonts.Resolve(_target.FontRole ?? settings.DefaultFontRole, _locale);

            _preview.Configure(_target.Reference, variant?.Font, null);
            _diagnostics.Clear();
            _preview.SetMarkup(_working, _locale, _device, _diagnostics);
            _markupField.SetDiagnostics(_diagnostics);
            RefreshChrome();
        }

        private void RefreshChrome()
        {
            _targetLabel.text = (_dirty ? "● " : string.Empty) + _target.Title;
            _applyButton.SetEnabled(_dirty && _target.CanWrite && _target is not ScratchTarget);
            _toTableButton.style.display = (_target is TypecraftTextTarget || _target is TmpTextTarget) && TableBackends.Current != null ? DisplayStyle.Flex : DisplayStyle.None;
            _readOnlyBanner.text = _target.ReadOnlyReason ?? string.Empty;
            _readOnlyBanner.style.display = string.IsNullOrEmpty(_target.ReadOnlyReason) ? DisplayStyle.None : DisplayStyle.Flex;
            _effectWarning.Refresh(_target.Reference);

            _diagnosticsLabel.text = _diagnostics.Count == 0 ? string.Empty : $"⚠ {_diagnostics.Count}";
            _diagnosticsLabel.tooltip = string.Join("\n", _diagnostics.Select(Describe));
            _modeLabel.text = Application.isPlaying ? "Play Mode · правки живые" + (PlayModeEdits.KeepAfterPlay ? ", сохранятся после Play" : string.Empty) : "Edit Mode";

            string word = _selection.IsEmpty ? null : MarkupEdits.WordAt(_working, _selection);
            _selectionLabel.text = string.IsNullOrEmpty(word) ? string.Empty : $"Выделено: «{Truncate(word, 40)}» · {word.Length} симв.";
            RefreshModules();
        }

        private void RefreshModules()
        {
            _chips.Clear();
            MarkupDocument document = _selection.IsEmpty ? null : MarkupDocument.Parse(_working);

            if (document != null)
            {
                foreach ((MarkupModule module, ModuleCoverage coverage) in document.ModulesIn(_selection))
                {
                    string slot = module.Slot;
                    var chip = new Button(() => RemoveModule(slot))
                    {
                        text = (coverage == ModuleCoverage.Partial ? "◐ " : "✓ ") + module.Label + "  ✕",
                        tooltip = coverage == ModuleCoverage.Partial ? "Применено к части выделенного · снять" : "Снять с выделенного"
                    };
                    chip.AddToClassList("tc-chip");
                    _chips.Add(chip);

                    if (slot == "color" && coverage == ModuleCoverage.Full && TryParseColor(module.Open, out Color color))
                        _colorField.SetValueWithoutNotify(color);
                }
            }

            AddAutoTagChips();
            _chips.style.display = _chips.childCount > 0 && _target.CanWrite ? DisplayStyle.Flex : DisplayStyle.None;
            _drawer.ShowApplied(open => document != null && MarkupModule.TryParse(open, out MarkupModule module) ? CoverageOf(document, module) : ModuleCoverage.None);
        }

        private void AddAutoTagChips()
        {
            StyleLibrary styles = Typecraft.Settings != null ? Typecraft.Settings.Styles : null;
            string word = _selection.IsEmpty ? null : MarkupEdits.WordAt(_working, _selection);
            if (styles == null || string.IsNullOrWhiteSpace(word))
                return;

            for (int i = 0; i < styles.Styles.Count; i++)
            {
                TextStyle style = styles.Styles[i];
                if (style == null || !style.AutoTagWords.Any(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase)))
                    continue;

                int index = i;
                var chip = new Button(() => ToggleAutoTag(styles, index, word))
                {
                    text = $"авто [{style.Id}]  ✕",
                    tooltip = $"«{word}» размечается стилем везде · снять автотег"
                };
                chip.AddToClassList("tc-chip");
                chip.AddToClassList("tc-chip--auto");
                _chips.Add(chip);
            }
        }

        private ModuleCoverage CoverageOf(MarkupDocument document, MarkupModule module)
        {
            bool bySlot = module.Open == $"<{module.Slot}>";
            return document.Coverage(_selection, m => bySlot ? m.Slot == module.Slot : m.Equals(module));
        }

        private ModuleCoverage CoverageOf(string open)
        {
            if (_selection.IsEmpty || !MarkupModule.TryParse(open, out MarkupModule module))
                return ModuleCoverage.None;
            return CoverageOf(MarkupDocument.Parse(_working), module);
        }

        private static string Checked(ModuleCoverage coverage) => coverage == ModuleCoverage.Partial ? "◐ " : string.Empty;

        private static bool TryParseColor(string open, out Color color)
        {
            color = default;
            int equals = open.IndexOf('=');
            return equals > 0 && ColorUtility.TryParseHtmlString(open.Substring(equals + 1, open.Length - equals - 2).Trim('"'), out color);
        }

        private void RemoveModule(string slot)
        {
            if (!_target.CanWrite || _selection.IsEmpty)
                return;

            string updated = MarkupDocument.Parse(_working).RemoveSlot(_selection, slot, out TextRange result);
            if (!result.IsEmpty)
                Commit(updated, result);
        }

        private void OnMarkupChanged(string markup)
        {
            _working = markup;
            _dirty = _working != _target.Markup;
            if (_target is ScratchTarget scratch)
            {
                scratch.Write(markup);
                _dirty = false;
            }

            RefreshPreview();
        }

        private void OpenTagInspector(int markupIndex)
        {
            if (_target.CanWrite && markupIndex >= 0)
                _tagInspector.Edit(_working, markupIndex);
        }

        private void ReplaceTag(int start, int length, string text)
        {
            if (!_target.CanWrite || start < 0 || start + length > _working.Length)
                return;

            _markupField.SetValue(_working.Substring(0, start) + text + _working.Substring(start + length), notify: true);
        }

        private void ToggleMarkers()
        {
            _preview.ShowMarkers = !_preview.ShowMarkers;
            _markersButton.EnableInClassList("tc-seg--on", _preview.ShowMarkers);
        }

        private void OnFieldSelectionChanged(TextRange range)
        {
            _selection = range;
            if (range.IsEmpty && _target.CanWrite && TagInspector.TryFindTag(_working, range.Start, out int tagStart))
                _tagInspector.Edit(_working, tagStart);
            else
                _tagInspector.Hide();

            if (range.IsEmpty)
            {
                ShowFloatingBar(false);
                _preview.HighlightMarkupRange(range);
            }
            else
            {
                _preview.HighlightMarkupRange(range);
                ShowFloatingBar(true);
            }

            RefreshChrome();
        }

        private void OnPreviewSelectionCleared()
        {
            _selection = default;
            ShowFloatingBar(false);
            RefreshChrome();
        }

        private void OnPreviewSelectionChanged(TextRange range)
        {
            _selection = range;
            _markupField.Select(range);
            ShowFloatingBar(true);
            RefreshChrome();
        }

        private void Apply()
        {
            if (!_target.CanWrite || _target is ScratchTarget)
                return;

            if (HasPendingWordChange)
            {
                ResolvePendingWordChange();
                return;
            }

            _target.Write(_working);
            if (_target is TmpTextTarget && Selection.activeGameObject != null)
                SetTarget(new TypecraftTextTarget(Selection.activeGameObject.GetComponent<TypecraftText>()));

            _dirty = false;
            RefreshChrome();
        }

        private void MoveToTable()
        {
            ITableBackend backend = TableBackends.Current;
            TMP_Text reference = _target.Reference;
            if (backend == null || reference == null || string.IsNullOrWhiteSpace(_working))
                return;

            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Typecraft", "Вынести текст в таблицу можно только вне Play Mode: компонент поменяет режим, а изменения Play Mode не сохраняются.", "OK");
                return;
            }

            string table = backend.Tables.FirstOrDefault();
            if (table == null)
            {
                EditorUtility.DisplayDialog("Typecraft", "Нет ни одной таблицы — создай её во вкладке «Локализация».", "OK");
                return;
            }

            string key = TextPrompt.Show("Вынести в таблицу", $"Ключ строки в таблице «{table}»:", SuggestKey(reference));
            if (key == null)
                return;

            if (!backend.AddKey(table, key) && !string.IsNullOrEmpty(backend.Get(table, key, backend.BaseLocale)) &&
                !EditorUtility.DisplayDialog("Typecraft", $"Ключ «{key}» уже есть. Перезаписать текст основного языка?", "Перезаписать", "Отмена"))
                return;

            backend.Set(table, key, backend.BaseLocale, _working);

            TypecraftText component = reference.GetComponent<TypecraftText>();
            if (component == null)
                component = Undo.AddComponent<TypecraftText>(reference.gameObject);

            Undo.RecordObject(component, "Typecraft: текст в таблицу");
            component.SetKey(table, key);
            EditorUtility.SetDirty(component);

            _dirty = false;
            SetTarget(TargetFor(component));
        }

        private static string SuggestKey(TMP_Text reference)
        {
            string scene = reference.gameObject.scene.name;
            string name = new string(reference.name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');
            return string.IsNullOrEmpty(scene) ? name : $"{scene.ToLowerInvariant()}.{name}";
        }

        private void WrapSelection(string open, string close)
        {
            if (!_target.CanWrite)
                return;

            if (!_selection.IsEmpty && MarkupModule.TryParse(open, out MarkupModule module))
            {
                string toggled = MarkupDocument.Parse(_working).Toggle(_selection, module, out TextRange toggledRange);
                if (!toggledRange.IsEmpty)
                    Commit(toggled, toggledRange);
                return;
            }

            TextRange range = _selection.IsEmpty ? new TextRange(_markupField.Caret, _markupField.Caret) : _selection;
            string updated = MarkupEdits.Wrap(_working, range, open, close, out TextRange result);
            Commit(updated, result);
        }

        private void InsertAtSelection(string text)
        {
            if (!_target.CanWrite)
                return;

            int index = _selection.IsEmpty ? _markupField.Caret : _selection.End;
            string updated = MarkupEdits.Insert(_working, index, text, out int caret);
            Commit(updated, new TextRange(caret, caret));
        }

        private void Commit(string updated, TextRange select)
        {
            _markupField.SetValue(updated, notify: true);
            _selection = select;
            _markupField.Select(select);
            _preview.HighlightMarkupRange(select);
            RefreshChrome();
        }

        private void SetDevice(DeviceFamily device)
        {
            _device = device;
            UpdateDeviceButtons();
            RefreshPreview();
        }

        private void UpdateDeviceButtons()
        {
            foreach (VisualElement child in _deviceBar.Children())
                child.EnableInClassList("tc-seg--on", child.userData is DeviceFamily family && family == _device);
        }

        private void ToggleAnimation()
        {
            _preview.Animate = !_preview.Animate;
            _animateButton.text = _preview.Animate ? "❚❚" : "▶";
        }

        private VisualElement BuildFloatingBar()
        {
            var bar = new VisualElement();
            bar.AddToClassList("tc-float");
            bar.style.display = DisplayStyle.None;

            bar.Add(new Button(() => ShowMenu(StyleMenu)) { text = "Стиль ▾" });
            bar.Add(new Button(() => OpenStyleForm(true)) { text = "Стиль из выделения", tooltip = "Создать стиль из выделенного: теги вокруг (<color>, <b>, эффекты…) станут стилем и заменятся на [id]…[/id]" });
            bar.Add(new Button(() => ShowMenu(EffectMenu)) { text = "Эффект ▾" });

            _colorField = new ColorField { showAlpha = false, value = new Color(0.89f, 0.36f, 0.29f), tooltip = "Цвет выделенного" };
            _colorField.AddToClassList("tc-float__color");
            _colorField.RegisterValueChangedCallback(evt => WrapSelection($"<color=#{ColorUtility.ToHtmlStringRGB(evt.newValue)}>", "</color>"));
            bar.Add(_colorField);

            bar.Add(new Button(() => ShowMenu(IconMenu)) { text = "Иконка ▾" });
            bar.Add(new Button(() => ShowMenu(AutoTagMenu)) { text = "Автотег ▾", tooltip = "Автоматически размечать это слово везде" });
            bar.Add(new Button(Unwrap) { text = "✕", tooltip = "Снять всю разметку с выделенного" });
            return bar;
        }

        private void ShowFloatingBar(bool show) =>
            _floatingBar.style.display = show && _target.CanWrite ? DisplayStyle.Flex : DisplayStyle.None;

        private void UpdateFloatingBar()
        {
            if (_floatingBar.resolvedStyle.display == DisplayStyle.None)
                return;

            Rect? bounds = _preview.SelectionBounds;
            Rect area = _preview.Element.layout;
            float width = _floatingBar.layout.width > 0 ? _floatingBar.layout.width : 380f;

            float x = bounds.HasValue ? bounds.Value.center.x - width / 2f : 8f;
            float y = bounds.HasValue ? bounds.Value.yMin - 34f : 8f;
            if (bounds.HasValue && y < 4f)
                y = bounds.Value.yMax + 6f;

            _floatingBar.style.left = Mathf.Clamp(x, 4f, Mathf.Max(4f, area.width - width - 4f));
            _floatingBar.style.top = Mathf.Clamp(y, 4f, Mathf.Max(4f, area.height - 32f));
        }

        private static void ShowMenu(Action<GenericMenu> fill)
        {
            var menu = new GenericMenu();
            fill(menu);
            menu.ShowAsContext();
        }

        private void StyleMenu(GenericMenu menu)
        {
            StyleLibrary styles = Typecraft.Settings != null ? Typecraft.Settings.Styles : null;
            if (styles == null)
                return;

            foreach (TextStyle style in styles.Styles.Where(s => s != null && !string.IsNullOrEmpty(s.Id)))
            {
                string id = style.Id;
                ModuleCoverage coverage = CoverageOf($"[{id}]");
                menu.AddItem(new GUIContent($"{Checked(coverage)}{style.DisplayName}  [{id}]"), coverage == ModuleCoverage.Full, () => WrapSelection($"[{id}]", $"[/{id}]"));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Новый стиль…"), false, () => OpenStyleForm(false));
            menu.AddItem(new GUIContent("Стиль из выделения…"), false, () => OpenStyleForm(true));
        }

        private void EffectMenu(GenericMenu menu)
        {
            foreach (EffectEntry effect in EffectCatalog.Entries)
            {
                EffectEntry captured = effect;
                string group = effect.Kind switch
                {
                    EffectKind.Show => "Появление/",
                    EffectKind.Hide => "Исчезание/",
                    EffectKind.Command => "Команды/",
                    _ => string.Empty
                };

                ModuleCoverage coverage = effect.Wraps ? CoverageOf(effect.Open) : ModuleCoverage.None;
                menu.AddItem(new GUIContent(group + Checked(coverage) + effect.TagName), coverage == ModuleCoverage.Full, () =>
                {
                    if (captured.Wraps)
                        WrapSelection(captured.Open, captured.Close);
                    else
                        InsertAtSelection(captured.Open);
                });
            }
        }

        private void IconMenu(GenericMenu menu)
        {
            foreach (IconSet set in MarkupCandidates.AllIconSets())
            {
                foreach (IconEntry entry in set.Entries)
                {
                    string reference = $"{set.SetId}/{entry.Key}";
                    menu.AddItem(new GUIContent(reference), false, () => InsertAtSelection($"{{{reference}}}"));
                }
            }
        }

        private void AutoTagMenu(GenericMenu menu)
        {
            StyleLibrary styles = Typecraft.Settings != null ? Typecraft.Settings.Styles : null;
            string word = MarkupEdits.WordAt(_working, _selection);
            if (styles == null || string.IsNullOrWhiteSpace(word))
                return;

            for (int i = 0; i < styles.Styles.Count; i++)
            {
                TextStyle style = styles.Styles[i];
                if (style == null || string.IsNullOrEmpty(style.Id))
                    continue;

                int index = i;
                bool already = style.AutoTagWords.Any(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase));
                menu.AddItem(new GUIContent($"«{word}» → [{style.Id}]"), already, () => ToggleAutoTag(styles, index, word));
            }
        }

        private void ToggleAutoTag(StyleLibrary styles, int styleIndex, string word)
        {
            var serialized = new SerializedObject(styles);
            SerializedProperty list = serialized.FindProperty("_styles");
            bool had = false;

            for (int s = 0; s < list.arraySize; s++)
            {
                SerializedProperty words = list.GetArrayElementAtIndex(s).FindPropertyRelative("_autoTagWords");
                for (int i = words.arraySize - 1; i >= 0; i--)
                {
                    if (!string.Equals(words.GetArrayElementAtIndex(i).stringValue, word, StringComparison.OrdinalIgnoreCase))
                        continue;

                    had |= s == styleIndex;
                    words.DeleteArrayElementAtIndex(i);
                }
            }

            if (!had)
            {
                SerializedProperty target = list.GetArrayElementAtIndex(styleIndex).FindPropertyRelative("_autoTagWords");
                target.InsertArrayElementAtIndex(target.arraySize);
                target.GetArrayElementAtIndex(target.arraySize - 1).stringValue = word;
            }

            serialized.ApplyModifiedProperties();
            Typecraft.Refresh();
            RefreshPreview();
            TypecraftLog.Info(had ? $"Автотег снят: «{word}»" : $"Автотег: «{word}» теперь всегда размечается стилем");
        }

        private void Unwrap()
        {
            if (!_target.CanWrite || _selection.IsEmpty)
                return;

            string updated = MarkupDocument.Parse(_working).Clear(_selection, out TextRange result);
            if (!result.IsEmpty)
                Commit(updated, result);
        }

        private static string Describe(MarkupDiagnostic diagnostic) => diagnostic.Issue switch
        {
            MarkupIssue.UnknownIcon => $"Нет иконки «{diagnostic.Value}»",
            MarkupIssue.PlaceholderIcon => $"У иконки «{diagnostic.Value}» нет арта",
            MarkupIssue.UnknownStyle => $"Нет стиля «{diagnostic.Value}» (оставлено текстом)",
            MarkupIssue.UnknownTag => $"Неизвестный тег «{diagnostic.Value}»",
            MarkupIssue.UnresolvedTag => $"Нет глифа для «{diagnostic.Value}» на этом устройстве",
            MarkupIssue.StrayClosingStyle => $"Лишний закрывающий [/{diagnostic.Value}]",
            MarkupIssue.UnclosedStyle => $"Стиль [{diagnostic.Value}] не закрыт",
            _ => diagnostic.ToString()
        };

        private static string Truncate(string text, int length) => text.Length <= length ? text : text.Substring(0, length) + "…";

        private static VisualElement Spacer()
        {
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            return spacer;
        }
    }
}
