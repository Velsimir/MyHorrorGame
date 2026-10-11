using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Dialogue.Editor.Inspector;
using Velsi.Typecraft.Dialogue.Editor.Model;
using Velsi.Typecraft.Editor.Window.Layout;
using Velsi.Typecraft.Editor.Window.Preview;
using Velsi.Typecraft.Fonts;
using Velsi.Typecraft.Markup;
using Object = UnityEngine.Object;

namespace Velsi.Typecraft.Dialogue.Editor.Playback
{
    public interface IDialoguePlaybackHost
    {
        DialogueAsset Asset { get; }
        string Locale { get; }
        string SelectedNodeId { get; }
        void ShowCurrent(DialogueAsset asset, string nodeId);
        void Reveal(DialogueAsset asset, string nodeId);
    }

    public readonly struct PlaybackLogEntry
    {
        public PlaybackLogEntry(string text, DialogueAsset asset, string nodeId, string kind)
        {
            Text = text;
            Asset = asset;
            NodeId = nodeId;
            Kind = kind;
        }

        public string Text { get; }
        public DialogueAsset Asset { get; }
        public string NodeId { get; }
        public string Kind { get; }

        public override string ToString() => Text;
    }

    public sealed class DialoguePlaybackPanel : VisualElement, IDisposable
    {
        private const int MaxLog = 400;
        private const string RunnerLogPrefix = "[Typecraft] Dialogue";
        private static readonly Color TextColor = new Color(0.91f, 0.88f, 0.83f);

        private readonly IDialoguePlaybackHost _host;
        private readonly DialogueVariables _variables = new DialogueVariables();
        private readonly DialogueRunner _runner;
        private readonly TextPreview _text = new TextPreview { Animate = true };
        private readonly TextPreview _name = new TextPreview { Animate = true };
        private readonly GameObject _referenceObject;
        private readonly TextMeshPro _reference;
        private readonly List<PlaybackLogEntry> _log = new List<PlaybackLogEntry>();
        private readonly Dictionary<string, DialogueValueField> _flagFields = new Dictionary<string, DialogueValueField>(StringComparer.Ordinal);
        private readonly List<(string Name, DialogueValue Before)> _pendingSet = new List<(string, DialogueValue)>();

        private readonly Label _flagsTitle;
        private readonly Button _resetFlags;
        private readonly ScrollView _flagsList;
        private readonly Button _fromSelected;
        private readonly Button _stop;
        private readonly Label _status;
        private readonly VisualElement _box;
        private readonly Image _portraitLeft;
        private readonly Image _portraitRight;
        private readonly VisualElement _nameHost;
        private readonly VisualElement _textHost;
        private readonly Label _placeholder;
        private readonly VisualElement _choices;
        private readonly Label _hidden;
        private readonly Label _waitLabel;
        private readonly Button _skipWait;
        private readonly Button _next;
        private readonly ScrollView _logView;

        private DialogueLine _line;
        private DialogueAsset _pendingSetAsset;
        private string _pendingSetNode;
        private float _timer = -1f;
        private bool _awaitingText;
        private bool _typewriter = true;
        private bool _flagsDirty = true;
        private bool _visitedOpen;
        private DialoguePlayer _observed;
        private IDialogueVariables _observedVariables;
        private string _observedKey;

        public DialoguePlaybackPanel(IDialoguePlaybackHost host)
        {
            _host = host;
            AddToClassList("dlg-play");

            _runner = new DialogueRunner(_variables, DialogueProject.MergedCatalog());
            _runner.NodeEntered += OnNodeEntered;
            _runner.LineStarted += OnLineStarted;
            _runner.ChoicesPresented += OnChoicesPresented;
            _runner.WaitRequested += OnWaitRequested;
            _runner.EventRaised += OnEventRaised;
            _runner.Ended += OnEnded;
            _variables.Changed += OnVariableChanged;
            DialogueProject.MergedCatalog().ApplyDefaults(_variables, true);

            _referenceObject = new GameObject("TypecraftDialoguePreviewReference") { hideFlags = HideFlags.HideAndDontSave };
            _referenceObject.SetActive(false);
            _reference = _referenceObject.AddComponent<TextMeshPro>();
            _reference.fontSize = 36f;
            _reference.alignment = TextAlignmentOptions.TopLeft;
            _reference.textWrappingMode = TextWrappingModes.Normal;
            _reference.color = TextColor;
            _reference.rectTransform.sizeDelta = new Vector2(60f, 20f);

            var flags = new VisualElement();
            flags.AddToClassList("dlg-play__flags");
            flags.AddToClassList(TypecraftLayout.FitClass);
            var flagsHeader = Row("dlg-play__header");
            _flagsTitle = new Label("Флаги проигрывания");
            _flagsTitle.AddToClassList("tc-lib__section");
            flagsHeader.Add(_flagsTitle);
            flagsHeader.Add(Grow());
            _resetFlags = new Button(ResetFlags) { text = "Сбросить", tooltip = "Вернуть значения из каталогов, обнулить посещения" };
            _resetFlags.AddToClassList("dlg-small");
            flagsHeader.Add(_resetFlags);
            flags.Add(flagsHeader);
            _flagsList = TypecraftLayout.VerticalScroll("dlg-play__flag-list");
            flags.Add(_flagsList);

            var center = new VisualElement();
            center.AddToClassList("dlg-play__center");
            var controls = Row("dlg-play__header");
            controls.Add(new Button(() => Play(false)) { text = "▶ С начала", tooltip = "Проиграть текущий диалог со стартовой ноды" });
            _fromSelected = new Button(() => Play(true)) { text = "▶ С выделенной", tooltip = "Начать с выделенной на доске ноды" };
            controls.Add(_fromSelected);
            _stop = new Button(Stop) { text = "■ Стоп" };
            controls.Add(_stop);
            var typewriter = new Toggle("Печать") { value = _typewriter, tooltip = "Выводить реплику по символу, как машинка в игре" };
            typewriter.AddToClassList("dlg-play__toggle");
            typewriter.RegisterValueChangedCallback(evt => _typewriter = evt.newValue);
            controls.Add(typewriter);
            controls.Add(new Button(Replay) { text = "⟲", tooltip = "Показать реплику заново: печать и анимации" });
            _status = new Label();
            _status.AddToClassList("dlg-play__status");
            controls.Add(_status);
            center.Add(controls);

            _box = new VisualElement();
            _box.AddToClassList("dlg-play__box");
            _portraitLeft = new Image { scaleMode = ScaleMode.ScaleToFit };
            _portraitLeft.AddToClassList("dlg-play__portrait");
            _box.Add(_portraitLeft);

            var content = new VisualElement();
            content.AddToClassList("dlg-play__content");
            _nameHost = new VisualElement();
            _nameHost.AddToClassList("dlg-play__name");
            _name.Element.style.flexGrow = 1;
            _nameHost.Add(_name.Element);
            content.Add(_nameHost);
            _textHost = new VisualElement();
            _textHost.AddToClassList("dlg-play__text");
            _text.Element.style.flexGrow = 1;
            _textHost.Add(_text.Element);
            content.Add(_textHost);
            _placeholder = new Label();
            _placeholder.AddToClassList("dlg-play__placeholder");
            content.Add(_placeholder);
            _choices = new VisualElement();
            _choices.AddToClassList("dlg-play__choices");
            content.Add(_choices);
            _hidden = new Label();
            _hidden.AddToClassList("dlg-play__hidden");
            content.Add(_hidden);
            var footer = Row("dlg-play__footer");
            _waitLabel = new Label();
            _waitLabel.AddToClassList("dlg-play__wait");
            footer.Add(_waitLabel);
            _skipWait = new Button(SkipWait) { text = "Пропустить ожидание" };
            footer.Add(_skipWait);
            footer.Add(Grow());
            _next = new Button(() => Next()) { text = "Далее ▸", tooltip = "Дописать текст или перейти дальше" };
            footer.Add(_next);
            content.Add(footer);
            _box.Add(content);

            _portraitRight = new Image { scaleMode = ScaleMode.ScaleToFit };
            _portraitRight.AddToClassList("dlg-play__portrait");
            _box.Add(_portraitRight);
            center.Add(_box);

            var log = new VisualElement();
            log.AddToClassList("dlg-play__log");
            var logHeader = Row("dlg-play__header");
            var logTitle = new Label("Лог");
            logTitle.AddToClassList("tc-lib__section");
            logHeader.Add(logTitle);
            logHeader.Add(Grow());
            var clear = new Button(ClearLog) { text = "Очистить" };
            clear.AddToClassList("dlg-small");
            logHeader.Add(clear);
            log.Add(logHeader);
            _logView = TypecraftLayout.VerticalScroll("dlg-play__log-list");
            log.Add(_logView);

            var columns = new ColumnsLayout("Dialogues.Playback", flags, center, log, 240f, 320f, 120f, 760f);
            controls.Insert(0, columns.MenuButton);
            Add(columns);

            ShowIdle("Нажми «▶ С начала» или выдели ноду и «▶ С выделенной». Игра не нужна, флаги — свои, игровые не трогаются.");
        }

        public DialogueRunner Runner => _runner;
        public DialogueVariables Variables => _variables;
        public bool IsPlaying => _runner.IsRunning;
        public DialogueStep Current => _runner.Current;
        public DialoguePlayer Observed => _observed;
        public IReadOnlyList<PlaybackLogEntry> Log => _log;
        public string StatusText => _status.text;
        public float WaitRemaining => _timer;

        public IEnumerable<(string Text, bool Enabled, string Tooltip)> ChoiceButtons =>
            _choices.Children().OfType<Button>().Select(button => (button.text, button.enabledSelf, button.tooltip));

        public string HiddenText => _hidden.style.display == DisplayStyle.None ? string.Empty : _hidden.text;

        public void Play(bool fromSelected)
        {
            DialogueAsset asset = _host.Asset;
            if (asset == null)
                return;

            string start = fromSelected ? _host.SelectedNodeId : null;
            if (fromSelected && (string.IsNullOrEmpty(start) || !asset.TryGetNode(start, out _)))
            {
                AddLog("⚠ Выдели ноду на доске, чтобы начать с неё", null, null, "warn");
                return;
            }

            Observe(null);
            _runner.Catalog = DialogueProject.MergedCatalog();
            ClearBox();
            AddLog(fromSelected ? $"▶ «{asset.name}» с ноды {Describe(asset, start)}" : $"▶ «{asset.name}» с начала", asset, start, "start");
            Run(() => _runner.Start(asset, start));
        }

        public void Stop()
        {
            if (!_runner.IsRunning)
                return;

            AddLog("■ Остановлено", null, null, "end");
            Run(_runner.Stop);
        }

        public bool Next()
        {
            if (_observed != null || !_runner.IsRunning)
                return false;

            switch (_runner.Current.Kind)
            {
                case DialogueStepKind.Line:
                    if (_text.IsTypewriting)
                    {
                        _text.ShowAll();
                        return true;
                    }

                    StopTimer();
                    Run(() => _runner.Continue());
                    return true;
                case DialogueStepKind.Wait:
                    SkipWait();
                    return true;
            }

            return false;
        }

        public bool Choose(int index)
        {
            if (_observed != null || !_runner.IsRunning || _runner.Current.Kind != DialogueStepKind.Choice)
                return false;

            DialogueChoice choice = _runner.Current.Choices?.FirstOrDefault(candidate => candidate.Index == index);
            if (choice == null || !choice.Available)
                return false;

            AddLog($"→ Выбран {index + 1}. {ChoiceText(choice.ResolveTable(), choice.Key)}", _runner.Current.Asset, _runner.Current.NodeId, "choice");
            bool chosen = false;
            Run(() => chosen = _runner.Choose(index));
            return chosen;
        }

        public void SkipWait()
        {
            if (_observed != null || !_runner.IsRunning)
                return;

            DialogueStepKind kind = _runner.Current.Kind;
            if (kind != DialogueStepKind.Wait && kind != DialogueStepKind.Line)
                return;

            StopTimer();
            _awaitingText = false;
            Run(() => _runner.Continue());
        }

        public void SetFlag(string name, DialogueValue value)
        {
            IDialogueVariables source = FlagSource;
            if (string.IsNullOrEmpty(name) || source == null)
                return;

            source.Set(name, value);
            AddLog($"✎ {name} = {DialogueTexts.Value(value)} (вручную{(_observed != null ? ", в игре" : string.Empty)})", null, null, "flag");
        }

        public void ResetFlags()
        {
            if (_observed != null)
                return;

            _variables.Clear();
            DialogueProject.MergedCatalog().ApplyDefaults(_variables, true);
            _flagsDirty = true;
            AddLog("↺ Флаги сброшены к значениям каталогов", null, null, "flag");
        }

        public void RefreshFlags() => _flagsDirty = true;

        public void OnLocaleChanged() => RenderStep(_observed != null ? _observed.Current : _runner.Current, false);

        public void Observe(DialoguePlayer player)
        {
            if (_runner.IsRunning)
                player = null;

            if (ReferenceEquals(player, _observed) && (player == null || ReferenceEquals(_observedVariables, player.Variables)))
            {
                if (player != null)
                    PollObserved();
                return;
            }

            bool was = !ReferenceEquals(_observed, null);
            if (_observedVariables != null)
                _observedVariables.Changed -= OnVariableChanged;

            _observed = player;
            _observedVariables = player != null ? player.Variables : null;
            if (_observedVariables != null)
                _observedVariables.Changed += OnVariableChanged;
            _observedKey = null;
            _flagsDirty = true;

            bool observing = !ReferenceEquals(player, null);
            EnableInClassList("dlg-play--game", observing);
            _flagsTitle.text = observing ? "Игровые флаги" : "Флаги проигрывания";
            _flagsTitle.tooltip = observing ? "Флаги DialoguePlayer в игре: правка меняет игру" : "Свои флаги проигрывания, игровые не трогаются";
            _resetFlags.SetEnabled(!observing);

            if (observing)
            {
                AddLog($"🎮 Слежу за «{player.name}» в игре", null, null, "game");
                PollObserved();
            }
            else if (was)
            {
                ShowIdle("Слежение за игрой выключено.");
                _host.ShowCurrent(null, null);
            }

            UpdateControls();
        }

        public void Tick(float deltaTime)
        {
            if (!ReferenceEquals(_observed, null))
            {
                if (_observed == null)
                    Observe(null);
                else
                    PollObserved();
            }

            _text.Tick(deltaTime);
            _name.Tick(deltaTime);

            if (_awaitingText && !_text.IsTypewriting && _runner.IsRunning && _runner.Current.Kind == DialogueStepKind.Line)
            {
                _awaitingText = false;
                StartTimer(_line != null ? _line.Seconds : 0f);
            }

            if (_timer >= 0f && _runner.IsRunning)
            {
                _timer -= deltaTime;
                if (_timer <= 0f)
                {
                    _timer = -1f;
                    Run(() => _runner.Continue());
                }
                else
                {
                    _waitLabel.text = $"◷ {_timer:0.0} с";
                }
            }

            if (_flagsDirty)
                RebuildFlags();
        }

        public void Dispose()
        {
            Observe(null);
            _runner.Stop();
            _variables.Changed -= OnVariableChanged;
            _text.Dispose();
            _name.Dispose();
            if (_referenceObject != null)
                Object.DestroyImmediate(_referenceObject);
        }

        private IDialogueVariables FlagSource => _observed != null ? _observedVariables : _variables;

        private DialogueVariableCatalog FlagCatalog => _observed != null && _observed.Catalog != null ? _observed.Catalog : DialogueProject.MergedCatalog();

        private void Run(Action action)
        {
            Application.logMessageReceived += OnRunnerLog;
            try
            {
                action();
            }
            finally
            {
                Application.logMessageReceived -= OnRunnerLog;
            }

            FlushSet();
            if (_runner.IsRunning)
                _host.ShowCurrent(_runner.Current.Asset ?? _runner.Asset, _runner.Current.NodeId);
            UpdateControls();
        }

        private void OnRunnerLog(string message, string stackTrace, LogType type)
        {
            if (message != null && message.StartsWith(RunnerLogPrefix, StringComparison.Ordinal))
                AddLog("⚠ " + message.Substring(RunnerLogPrefix.Length).TrimStart(':', ' '), _runner.Asset, _runner.Current.NodeId, "warn");
        }

        private void OnNodeEntered(DialogueAsset asset, DialogueNode node)
        {
            FlushSet();
            switch (node)
            {
                case BranchNode branch:
                    AddLog(BranchText(branch), asset, node.Id, "branch");
                    break;
                case SetVariablesNode set:
                    _pendingSetAsset = asset;
                    _pendingSetNode = node.Id;
                    foreach (VariableAssignment assignment in set.Assignments.Where(entry => entry != null && !string.IsNullOrEmpty(entry.Name)))
                    {
                        DialogueValueType type = assignment.Operation == VariableOperation.Toggle ? DialogueValueType.Bool : assignment.Value.Type;
                        _pendingSet.Add((assignment.Name, DialogueVariableLookup.Get(_variables, _runner.Catalog, assignment.Name, type)));
                    }

                    break;
                case JumpNode jump:
                    DialogueAsset target = jump.Target != null ? jump.Target : asset;
                    string targetNode = string.IsNullOrEmpty(jump.TargetNode) ? "старт" : Describe(target, jump.TargetNode);
                    AddLog($"↪ Переход → {(target != asset ? "«" + target.name + "» " : string.Empty)}{targetNode}", asset, node.Id, "jump");
                    break;
                case EndNode _:
                    AddLog("■ Конец диалога", asset, node.Id, "end");
                    break;
            }
        }

        private void FlushSet()
        {
            if (_pendingSet.Count == 0)
                return;

            foreach ((string name, DialogueValue before) in _pendingSet)
            {
                DialogueValue now = DialogueVariableLookup.Get(_variables, _runner.Catalog, name, before.Type);
                string text = now.Type == before.Type && now == before
                    ? $"⚑ {name} = {DialogueTexts.Value(now)} (не изменился)"
                    : $"⚑ {name}: {DialogueTexts.Value(before)} → {DialogueTexts.Value(now)}";
                AddLog(text, _pendingSetAsset, _pendingSetNode, "flag");
            }

            _pendingSet.Clear();
        }

        private void OnLineStarted(DialogueLine line)
        {
            _line = line;
            StopTimer();
            _awaitingText = false;
            string speaker = line.Speaker != null ? DialogueTexts.SpeakerName(line.Speaker, line.ResolveTable(), _host.Locale) + ": " : string.Empty;
            LineNode node = line.Asset != null ? line.Asset.FindNode<LineNode>(line.NodeId) : null;
            string variant = node != null && node.Key != line.Key ? " (вариант по условию)" : string.Empty;
            AddLog($"❝ {speaker}{LineText(line)}{variant}", line.Asset, line.NodeId, "line");

            RenderLine(line, true);
            if (line.Advance == LineAdvance.AfterSeconds)
                StartTimer(line.Seconds);
            else if (line.Advance == LineAdvance.AfterText)
                _awaitingText = true;
        }

        private void OnChoicesPresented(IReadOnlyList<DialogueChoice> choices)
        {
            StopTimer();
            _awaitingText = false;
            DialogueStep step = _runner.Current;
            int total = step.Asset != null && step.Asset.FindNode(step.NodeId) is ChoiceNode node ? node.Options.Count : choices.Count;
            AddLog($"☰ Выбор: доступно {choices.Count(choice => choice.Available)} из {total}", step.Asset, step.NodeId, "choice");
            RenderChoices(step.Asset, step.NodeId, choices, true);
        }

        private void OnWaitRequested(float seconds)
        {
            _awaitingText = false;
            DialogueStep step = _runner.Current;
            AddLog($"◷ Пауза {seconds:0.##} с", step.Asset, step.NodeId, "wait");
            ClearChoices();
            StartTimer(seconds);
        }

        private void OnEventRaised(string eventName, string argument)
        {
            DialogueAsset asset = _runner.Asset;
            AddLog($"✦ Событие {eventName}{(string.IsNullOrEmpty(argument) ? string.Empty : $" ({argument})")}", asset, null, "event");
        }

        private void OnEnded()
        {
            FlushSet();
            StopTimer();
            _awaitingText = false;
            ClearChoices();
            _host.ShowCurrent(null, null);
            _status.text = "Диалог завершён";
            UpdateControls();
        }

        private void OnVariableChanged(string name)
        {
            if (name != null && !name.StartsWith(DialogueRunner.VisitedPrefix, StringComparison.Ordinal) && _flagFields.TryGetValue(name, out DialogueValueField field))
            {
                DialogueValueType type = field.Value.Type;
                DialogueValue value = DialogueVariableLookup.Get(FlagSource, FlagCatalog, name, type);
                if (!(value.Type == field.Value.Type && value == field.Value))
                    field.SetValue(value);
                return;
            }

            _flagsDirty = true;
        }

        private void RenderStep(DialogueStep step, bool fresh)
        {
            switch (step.Kind)
            {
                case DialogueStepKind.Line:
                    RenderLine(step.Line, fresh);
                    break;
                case DialogueStepKind.Choice:
                    if (_line != null)
                        RenderLine(_line, false);
                    RenderChoices(step.Asset, step.NodeId, step.Choices, _observed == null);
                    break;
                case DialogueStepKind.Wait:
                    ClearChoices();
                    break;
            }
        }

        private void RenderLine(DialogueLine line, bool fresh)
        {
            if (line == null)
                return;

            _line = line;
            ClearChoices();
            _placeholder.style.display = DisplayStyle.None;
            _textHost.style.display = DisplayStyle.Flex;

            string locale = _host.Locale;
            string table = line.ResolveTable();
            FontVariant variant = Font(locale);
            string markup = DialogueTexts.Markup(table, line.Key, locale);
            if (string.IsNullOrEmpty(markup))
                markup = $"⚠ нет текста «{line.Key}» ({locale})";

            _text.Configure(_reference, variant?.Font, null);
            _text.SetMarkup(markup, locale, Typecraft.Device, new List<MarkupDiagnostic>());
            if (fresh && _typewriter)
                _text.RestartTypewriter();
            else
                _text.ShowAll();

            SpeakerProfile speaker = line.Speaker;
            _nameHost.style.display = speaker != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (speaker != null)
            {
                string name = DialogueTexts.Markup(speaker.ResolveNameTable(table), speaker.NameKey, locale);
                if (string.IsNullOrEmpty(name))
                    name = speaker.Id;
                _name.Configure(_reference, variant?.Font, null);
                _name.SetMarkup(DialogueSpeakerText.Styled(name, speaker.StyleId), locale, Typecraft.Device, new List<MarkupDiagnostic>());
                _name.ShowAll();
            }

            bool right = speaker != null && speaker.Side == SpeakerSide.Right;
            SetPortrait(_portraitLeft, right ? null : line.Portrait);
            SetPortrait(_portraitRight, right ? line.Portrait : null);

            bool timed = line.Advance != LineAdvance.Input;
            _status.text = timed ? (line.Advance == LineAdvance.AfterSeconds ? $"Реплика: сама через {line.Seconds:0.##} с" : $"Реплика: сама после текста + {line.Seconds:0.##} с") : "Реплика: ждёт «Далее»";
        }

        private void RenderChoices(DialogueAsset asset, string nodeId, IReadOnlyList<DialogueChoice> choices, bool interactive)
        {
            ClearChoices();
            _placeholder.style.display = _line == null ? DisplayStyle.Flex : DisplayStyle.None;
            if (_line == null)
                _placeholder.text = "Выбор без реплики перед ним";

            ChoiceNode node = asset != null ? asset.FindNode<ChoiceNode>(nodeId) : null;
            IDialogueVariables variables = FlagSource;
            DialogueVariableCatalog catalog = FlagCatalog;
            var shown = new HashSet<int>();
            foreach (DialogueChoice choice in choices ?? Array.Empty<DialogueChoice>())
            {
                shown.Add(choice.Index);
                int index = choice.Index;
                var button = new Button(() => Choose(index)) { text = $"{index + 1}. {ChoiceText(choice.ResolveTable(), choice.Key)}" };
                button.AddToClassList("dlg-play__choice");
                Condition condition = node != null && index < node.Options.Count ? node.Options[index]?.Condition : null;
                if (!choice.Available)
                {
                    button.AddToClassList("dlg-play__choice--off");
                    button.tooltip = $"Недоступен: нужно «{DialogueTexts.Condition(condition)}», сейчас {Values(condition, variables, catalog)}";
                }
                else if (condition != null && !condition.IsEmpty)
                {
                    button.tooltip = $"Условие «{DialogueTexts.Condition(condition)}» выполнено: {Values(condition, variables, catalog)}";
                }

                button.SetEnabled(choice.Available && interactive);
                _choices.Add(button);
            }

            var hidden = new List<string>();
            if (node != null)
            {
                for (int i = 0; i < node.Options.Count; i++)
                {
                    ChoiceOption option = node.Options[i];
                    if (option == null || shown.Contains(i))
                        continue;
                    hidden.Add($"{i + 1}. {ChoiceText(asset.ResolveTable(option.Table), option.Key)} — нужно «{DialogueTexts.Condition(option.Condition)}», сейчас {Values(option.Condition, variables, catalog)}");
                }
            }

            _hidden.text = hidden.Count > 0 ? "Скрыты: " + string.Join("; ", hidden) : string.Empty;
            _hidden.style.display = hidden.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _status.text = "Выбор";
            UpdateControls();
        }

        private void ClearChoices()
        {
            _choices.Clear();
            _hidden.text = string.Empty;
            _hidden.style.display = DisplayStyle.None;
        }

        private void ClearBox()
        {
            _line = null;
            ClearChoices();
            StopTimer();
            _awaitingText = false;
            _text.SetMarkup(string.Empty, _host.Locale, Typecraft.Device, null);
            _nameHost.style.display = DisplayStyle.None;
            SetPortrait(_portraitLeft, null);
            SetPortrait(_portraitRight, null);
            _placeholder.style.display = DisplayStyle.None;
            _status.text = string.Empty;
        }

        private void ShowIdle(string text)
        {
            ClearBox();
            _textHost.style.display = DisplayStyle.None;
            _placeholder.text = text;
            _placeholder.style.display = DisplayStyle.Flex;
            UpdateControls();
        }

        private void PollObserved()
        {
            if (!ReferenceEquals(_observedVariables, _observed.Variables))
            {
                DialoguePlayer player = _observed;
                Observe(null);
                Observe(player);
                return;
            }

            DialogueStep step = _observed.IsPlaying ? _observed.Current : default;
            string key = step.Kind == DialogueStepKind.None ? "none" : $"{step.Kind}/{(step.Asset != null ? step.Asset.Id : string.Empty)}/{step.NodeId}";
            if (key == _observedKey)
                return;

            _observedKey = key;
            if (step.Kind == DialogueStepKind.None)
            {
                ShowIdle($"«{_observed.name}» сейчас ничего не проигрывает.");
                _host.ShowCurrent(null, null);
                return;
            }

            if (step.Kind == DialogueStepKind.Line)
                _line = step.Line;
            _textHost.style.display = DisplayStyle.Flex;
            RenderStep(step, true);
            if (step.Kind == DialogueStepKind.Wait)
                _status.text = $"Пауза {step.Seconds:0.##} с";
            AddLog($"🎮 {Describe(step.Asset, step.NodeId)}", step.Asset, step.NodeId, "game");
            _host.ShowCurrent(step.Asset, step.NodeId);
            UpdateControls();
        }

        private void RebuildFlags()
        {
            _flagsDirty = false;
            float scroll = _flagsList.scrollOffset.y;
            _flagsList.Clear();
            _flagFields.Clear();

            IDialogueVariables source = FlagSource;
            DialogueVariableCatalog catalog = FlagCatalog;
            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (catalog != null)
            {
                foreach (DialogueVariableDefinition definition in catalog.Variables.Where(definition => definition != null && !string.IsNullOrEmpty(definition.Name)))
                {
                    if (seen.Add(definition.Name))
                        names.Add(definition.Name);
                }
            }

            var visited = new List<string>();
            if (source is DialogueVariables known)
            {
                foreach (string name in known.Names.OrderBy(name => name, StringComparer.Ordinal))
                {
                    if (name.StartsWith(DialogueRunner.VisitedPrefix, StringComparison.Ordinal))
                        visited.Add(name);
                    else if (seen.Add(name))
                        names.Add(name);
                }
            }

            if (names.Count == 0)
                _flagsList.Add(new Label("Флагов нет") { name = "empty" });

            foreach (string name in names)
            {
                DialogueVariableDefinition definition = null;
                bool declared = catalog != null && catalog.TryGet(name, out definition);
                DialogueValueType type = declared ? definition.Type : source != null && source.TryGet(name, out DialogueValue raw) ? raw.Type : DialogueValueType.String;
                var field = new DialogueValueField(name);
                field.AddToClassList("dlg-play__flag");
                field.EnableInClassList("dlg-play__flag--undeclared", !declared);
                field.tooltip = declared ? $"{type.ToString().ToLowerInvariant()}{(string.IsNullOrEmpty(definition.Description) ? string.Empty : " — " + definition.Description)}" : "Не объявлен в каталоге — встретился при проигрывании";
                field.SetValue(DialogueVariableLookup.Get(source, catalog, name, type));
                string captured = name;
                field.Changed += value => SetFlag(captured, value);
                _flagFields[name] = field;
                _flagsList.Add(field);
            }

            if (visited.Count > 0)
            {
                var foldout = new Foldout { text = $"Посещения нод ({visited.Count})", value = _visitedOpen };
                foldout.RegisterValueChangedCallback(evt => _visitedOpen = evt.newValue);
                foreach (string name in visited)
                {
                    source.TryGet(name, out DialogueValue count);
                    var label = new Label($"{VisitedTitle(name)}: {count.AsInt}");
                    label.AddToClassList("dlg-play__visited");
                    foldout.Add(label);
                }

                _flagsList.Add(foldout);
            }

            _flagsList.schedule.Execute(() => _flagsList.scrollOffset = new Vector2(0f, scroll));
        }

        private string VisitedTitle(string name)
        {
            string rest = name.Substring(DialogueRunner.VisitedPrefix.Length);
            int slash = rest.IndexOf('/');
            if (slash < 0)
                return rest;

            string assetId = rest.Substring(0, slash);
            string nodeId = rest.Substring(slash + 1);
            DialogueAsset asset = _host.Asset != null && _host.Asset.Id == assetId ? _host.Asset : DialogueProject.Dialogues().FirstOrDefault(candidate => candidate.Id == assetId);
            return asset != null ? $"{asset.name} · {Describe(asset, nodeId)}" : rest;
        }

        private void UpdateControls()
        {
            bool running = _runner.IsRunning && _observed == null;
            DialogueStepKind kind = running ? _runner.Current.Kind : DialogueStepKind.None;
            _stop.SetEnabled(running);
            _fromSelected.SetEnabled(_host.Asset != null && !string.IsNullOrEmpty(_host.SelectedNodeId));
            _next.style.display = kind == DialogueStepKind.Line ? DisplayStyle.Flex : DisplayStyle.None;
            bool waiting = kind == DialogueStepKind.Wait || (kind == DialogueStepKind.Line && _line != null && _line.Advance != LineAdvance.Input);
            _skipWait.style.display = waiting ? DisplayStyle.Flex : DisplayStyle.None;
            _waitLabel.style.display = waiting && _timer >= 0f ? DisplayStyle.Flex : DisplayStyle.None;
            if (kind == DialogueStepKind.Wait)
                _status.text = $"Пауза {_runner.Current.Seconds:0.##} с";
        }

        private void StartTimer(float seconds)
        {
            _timer = Mathf.Max(0f, seconds);
            _waitLabel.text = $"◷ {_timer:0.0} с";
            UpdateControls();
        }

        private void StopTimer()
        {
            _timer = -1f;
            _waitLabel.style.display = DisplayStyle.None;
        }

        private void Replay()
        {
            if (_line == null)
                return;

            _text.ShowAll();
            _text.RestartTypewriter();
#if TYPECRAFT_TMPEFFECTS
            _text.ReplayVisibility(true);
#endif
        }

        private void AddLog(string text, DialogueAsset asset, string nodeId, string kind)
        {
            var entry = new PlaybackLogEntry(text, asset, nodeId, kind);
            _log.Add(entry);
            if (_log.Count > MaxLog)
            {
                _log.RemoveAt(0);
                if (_logView.contentContainer.childCount > 0)
                    _logView.contentContainer.RemoveAt(0);
            }

            var label = new Label(text) { enableRichText = false, tooltip = nodeId != null ? "Клик — показать ноду" : null };
            label.AddToClassList("dlg-play__log-entry");
            label.AddToClassList("dlg-play__log--" + kind);
            if (asset != null && nodeId != null)
            {
                label.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0)
                        _host.Reveal(asset, nodeId);
                });
            }

            _logView.Add(label);
            _logView.schedule.Execute(() => _logView.ScrollTo(label));
        }

        private void ClearLog()
        {
            _log.Clear();
            _logView.Clear();
        }

        private string BranchText(BranchNode branch)
        {
            IDialogueVariables variables = _variables;
            DialogueVariableCatalog catalog = _runner.Catalog;
            var parts = new List<string>();
            string target = "Иначе";
            for (int i = 0; i < branch.Cases.Count; i++)
            {
                BranchCase branchCase = branch.Cases[i];
                if (branchCase == null)
                    continue;

                bool met = Condition.IsMet(branchCase.Condition, variables, catalog);
                string values = Values(branchCase.Condition, variables, catalog);
                parts.Add(branch.Cases.Count > 1 ? $"#{i + 1} {values} {(met ? "✓" : "✗")}" : values);
                if (met)
                {
                    target = branch.Cases.Count > 1 ? $"условие {i + 1}" : "Если";
                    break;
                }
            }

            return $"◇ Условие {string.Join("; ", parts)} → {target}";
        }

        private static string Values(Condition condition, IDialogueVariables variables, DialogueVariableCatalog catalog)
        {
            if (condition == null || condition.IsEmpty)
                return "всегда";

            var parts = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (ConditionClause clause in condition.Clauses)
            {
                if (clause == null || !seen.Add(clause.Variable ?? string.Empty))
                    continue;

                DialogueValueType type = clause.UsesValue ? clause.Value.Type : DialogueValueType.Bool;
                parts.Add($"{(string.IsNullOrEmpty(clause.Variable) ? "?" : clause.Variable)}={DialogueTexts.Value(DialogueVariableLookup.Get(variables, catalog, clause.Variable, type))}");
            }

            return string.Join(", ", parts);
        }

        private string LineText(DialogueLine line)
        {
            string text = DialogueTexts.Plain(line.ResolveTable(), line.Key, _host.Locale, 120);
            return string.IsNullOrEmpty(text) ? $"«{line.Key}» (нет текста)" : text;
        }

        private string ChoiceText(string table, string key)
        {
            if (string.IsNullOrEmpty(key))
                return "(нет ключа)";
            string text = DialogueTexts.Plain(table, key, _host.Locale, 80);
            return string.IsNullOrEmpty(text) ? key : text;
        }

        private string Describe(DialogueAsset asset, string nodeId) =>
            asset != null && asset.TryGetNode(nodeId, out DialogueNode node) ? $"{DialogueNodeInfo.Title(node)} #{DialogueTexts.ShortId(node.Id)}" : "#" + DialogueTexts.ShortId(nodeId);

        private static FontVariant Font(string locale)
        {
            TypecraftSettings settings = Typecraft.Settings;
            return settings != null && settings.Fonts != null ? settings.Fonts.Resolve(settings.DefaultFontRole, locale) : null;
        }

        private static void SetPortrait(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.style.display = sprite != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static VisualElement Row(string className)
        {
            var row = new VisualElement();
            row.AddToClassList("dlg-row");
            row.AddToClassList(className);
            return row;
        }

        private static VisualElement Grow()
        {
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            return spacer;
        }
    }
}
