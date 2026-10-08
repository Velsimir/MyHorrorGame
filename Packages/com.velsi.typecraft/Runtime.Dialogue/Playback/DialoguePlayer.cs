using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Velsi.Typecraft.Dialogue
{
    [AddComponentMenu("Typecraft/Dialogue/Dialogue Player")]
    public sealed class DialoguePlayer : MonoBehaviour
    {
        [SerializeField] private DialogueAsset _asset;
        [SerializeField] private DialoguePresenter _presenter;
        [Tooltip("Used for choices when the main presenter does not support them")]
        [SerializeField] private DialoguePresenter _choicePresenter;
        [Tooltip("Empty: the start node of the asset")]
        [SerializeField] private string _startNodeId;
        [SerializeField] private bool _playOnStart;
        [Tooltip("Do not play again once the start node was visited (stored in the dialogue variables)")]
        [SerializeField] private bool _once;
        [SerializeField] private DialogueTimeMode _timeMode = DialogueTimeMode.Scaled;
        [Tooltip("Empty: DialogueGlobals.Variables and DialogueGlobals.Catalog")]
        [SerializeField] private DialogueVariablesSource _variablesSource;
        [SerializeField] private UnityEvent _onStarted = new UnityEvent();
        [SerializeField] private UnityEvent _onEnded = new UnityEvent();
        [SerializeField] private DialogueEventUnityEvent _onEvent = new DialogueEventUnityEvent();

        private DialogueRunner _runner;
        private DialoguePresenter _linePresenter;
        private DialoguePresenter _shownChoicePresenter;
        private DialogueLine _line;
        private bool _awaitingText;
        private bool _timerActive;
        private float _timerEnd;
        private int _choiceToken;

        public event Action Started;
        public event Action Ended;
        public event Action<string, string> EventRaised;
        public event Action<DialogueLine> LineStarted;
        public event Action<IReadOnlyList<DialogueChoice>> ChoicesPresented;

        public DialogueAsset Asset
        {
            get => _asset;
            set => _asset = value;
        }

        public DialoguePresenter Presenter
        {
            get => _presenter;
            set => _presenter = value;
        }

        public DialoguePresenter ChoicePresenter
        {
            get => _choicePresenter;
            set => _choicePresenter = value;
        }

        public string StartNodeId
        {
            get => _startNodeId;
            set => _startNodeId = value;
        }

        public bool PlayOnStart
        {
            get => _playOnStart;
            set => _playOnStart = value;
        }

        public bool Once
        {
            get => _once;
            set => _once = value;
        }

        public DialogueTimeMode TimeMode
        {
            get => _timeMode;
            set => _timeMode = value;
        }

        public DialogueVariablesSource VariablesSource
        {
            get => _variablesSource;
            set => _variablesSource = value;
        }

        public UnityEvent OnStarted => _onStarted;
        public UnityEvent OnEnded => _onEnded;
        public DialogueEventUnityEvent OnEvent => _onEvent;

        public DialogueRunner Runner => _runner;
        public bool IsPlaying => _runner != null && _runner.IsRunning;
        public DialogueStep Current => IsPlaying ? _runner.Current : default;

        public IDialogueVariables Variables =>
            _variablesSource != null && _variablesSource.Variables != null ? _variablesSource.Variables : DialogueGlobals.Variables;

        public DialogueVariableCatalog Catalog =>
            _variablesSource != null && _variablesSource.Catalog != null ? _variablesSource.Catalog : DialogueGlobals.Catalog;

        public bool HasPlayed => HasPlayedFrom(_asset, _startNodeId);

        public bool HasPlayedFrom(DialogueAsset asset, string startNodeId = null)
        {
            string startId = ResolveStart(asset, startNodeId);
            return startId != null && Variables.TryGet(DialogueRunner.VisitedVariable(asset.Id, startId), out DialogueValue value) && value.AsInt > 0;
        }

        public bool Play() => Play(_asset, _startNodeId);

        public bool Play(DialogueAsset asset, string startNodeId = null)
        {
            if (asset == null)
            {
                Debug.LogWarning($"[Typecraft] Dialogue: '{name}' has no dialogue asset to play", this);
                return false;
            }

            string startId = ResolveStart(asset, startNodeId);
            if (_once && HasPlayedFrom(asset, startId))
                return false;

            if (IsPlaying)
                Stop();

            IDialogueVariables variables = Variables;
            EnsureRunner(variables);
            _runner.Catalog = Catalog;

            int visits = startId != null ? _runner.VisitCount(asset, startId) : 0;
            BindPresenter();
            Started?.Invoke();
            _onStarted.Invoke();

            _runner.Start(asset, startId);

            if (startId != null && _runner.VisitCount(asset, startId) == visits)
                variables.Set(DialogueRunner.VisitedVariable(asset.Id, startId), DialogueValue.Int(visits + 1));

            return true;
        }

        public DialogueRunnerState State => IsPlaying ? _runner.State : null;

        public bool Restore(DialogueRunnerState state, Func<string, DialogueAsset> resolver = null)
        {
            if (state == null || !state.IsValid)
                return false;

            DialogueAsset asset = resolver != null ? resolver(state.AssetId) : _asset != null && _asset.Id == state.AssetId ? _asset : null;
            if (asset == null || !asset.TryGetNode(state.NodeId, out _))
            {
                Debug.LogWarning($"[Typecraft] Dialogue: '{name}' cannot restore {state.AssetId}/{state.NodeId}", this);
                return false;
            }

            if (IsPlaying)
                Stop();

            EnsureRunner(Variables);
            _runner.Catalog = Catalog;
            BindPresenter();
            Started?.Invoke();
            _onStarted.Invoke();
            return _runner.Restore(state, _ => asset);
        }

        public bool Advance()
        {
            if (!IsPlaying || _runner.Current.Kind != DialogueStepKind.Line)
                return false;

            if (_linePresenter != null && _linePresenter.TryCompleteLine())
                return true;

            _timerActive = false;
            _awaitingText = false;
            return _runner.Continue();
        }

        public bool Choose(int index)
        {
            if (!IsPlaying || _runner.Current.Kind != DialogueStepKind.Choice)
                return false;

            _choiceToken++;
            if (_runner.Choose(index))
                return true;

            if (IsPlaying && _runner.Current.Kind == DialogueStepKind.Choice)
                PresentChoices(_runner.Current.Choices);
            return false;
        }

        public void Stop()
        {
            if (IsPlaying)
                _runner.Stop();
        }

        private void Start()
        {
            if (_playOnStart)
                Play();
        }

        private void OnDisable() => Stop();

        private void OnDestroy()
        {
            if (_runner != null)
                Unsubscribe(_runner);
        }

        private void Update()
        {
            if (!_timerActive || Now < _timerEnd)
                return;

            _timerActive = false;
            if (IsPlaying && (_runner.Current.Kind == DialogueStepKind.Line || _runner.Current.Kind == DialogueStepKind.Wait))
                _runner.Continue();
        }

        private float Now => _timeMode == DialogueTimeMode.Unscaled ? Time.unscaledTime : Time.time;

        private static string ResolveStart(DialogueAsset asset, string startNodeId)
        {
            if (asset == null)
                return null;

            string startId = string.IsNullOrEmpty(startNodeId) ? asset.StartNodeId : startNodeId;
            return string.IsNullOrEmpty(startId) ? null : startId;
        }

        private void EnsureRunner(IDialogueVariables variables)
        {
            if (_runner != null && ReferenceEquals(_runner.Variables, variables))
                return;

            if (_runner != null)
                Unsubscribe(_runner);

            _runner = new DialogueRunner(variables);
            _runner.LineStarted += OnLineStarted;
            _runner.ChoicesPresented += OnChoicesPresented;
            _runner.WaitRequested += OnWaitRequested;
            _runner.EventRaised += OnEventRaised;
            _runner.Ended += OnRunnerEnded;
        }

        private void Unsubscribe(DialogueRunner runner)
        {
            runner.LineStarted -= OnLineStarted;
            runner.ChoicesPresented -= OnChoicesPresented;
            runner.WaitRequested -= OnWaitRequested;
            runner.EventRaised -= OnEventRaised;
            runner.Ended -= OnRunnerEnded;
        }

        private void BindPresenter()
        {
            UnbindPresenter();
            _linePresenter = _presenter;
            if (_linePresenter != null)
                _linePresenter.LineCompleted += OnPresenterLineCompleted;
        }

        private void UnbindPresenter()
        {
            if (!ReferenceEquals(_linePresenter, null))
                _linePresenter.LineCompleted -= OnPresenterLineCompleted;
            _linePresenter = null;
        }

        private void StartTimer(float seconds)
        {
            _timerActive = true;
            _timerEnd = Now + Mathf.Max(0f, seconds);
        }

        private void OnLineStarted(DialogueLine line)
        {
            _line = line;
            _timerActive = false;
            _awaitingText = line.Advance == LineAdvance.AfterText;
            _choiceToken++;
            HideSeparateChoices();

            if (line.Advance == LineAdvance.AfterSeconds)
                StartTimer(line.Seconds);

            LineStarted?.Invoke(line);

            if (_linePresenter != null)
                _linePresenter.ShowLine(line);
            else
                OnPresenterLineCompleted();
        }

        private void OnPresenterLineCompleted()
        {
            if (!_awaitingText || !IsPlaying || _runner.Current.Kind != DialogueStepKind.Line || _line == null)
                return;

            _awaitingText = false;
            StartTimer(_line.Seconds);
        }

        private void OnChoicesPresented(IReadOnlyList<DialogueChoice> choices)
        {
            _timerActive = false;
            _awaitingText = false;
            ChoicesPresented?.Invoke(choices);
            if (IsPlaying && _runner.Current.Kind == DialogueStepKind.Choice)
                PresentChoices(choices);
        }

        private void PresentChoices(IReadOnlyList<DialogueChoice> choices)
        {
            DialoguePresenter target = ChoiceTarget();
            if (target == null)
            {
                Debug.LogWarning($"[Typecraft] Dialogue: '{name}' has no presenter that supports choices, picking the first available option", this);
                foreach (DialogueChoice choice in choices)
                {
                    if (choice.Available)
                    {
                        _runner.Choose(choice.Index);
                        return;
                    }
                }

                return;
            }

            int token = ++_choiceToken;
            _shownChoicePresenter = target;
            target.ShowChoices(choices, index =>
            {
                if (token == _choiceToken)
                    Choose(index);
            });
        }

        private DialoguePresenter ChoiceTarget()
        {
            if (_presenter != null && _presenter.SupportsChoices)
                return _presenter;
            if (_choicePresenter != null && _choicePresenter.SupportsChoices)
                return _choicePresenter;
            return null;
        }

        private void HideSeparateChoices()
        {
            if (_shownChoicePresenter != null && _shownChoicePresenter != _linePresenter)
                _shownChoicePresenter.Hide();
            _shownChoicePresenter = null;
        }

        private void OnWaitRequested(float seconds)
        {
            _awaitingText = false;
            StartTimer(seconds);
        }

        private void OnEventRaised(string eventName, string argument)
        {
            EventRaised?.Invoke(eventName, argument);
            _onEvent.Invoke(eventName, argument);
        }

        private void OnRunnerEnded()
        {
            _timerActive = false;
            _awaitingText = false;
            _line = null;
            _choiceToken++;

            HideSeparateChoices();
            if (_linePresenter != null)
                _linePresenter.Hide();
            UnbindPresenter();

            Ended?.Invoke();
            _onEnded.Invoke();
        }
    }

    [Serializable]
    public sealed class DialogueEventUnityEvent : UnityEvent<string, string>
    {
    }

    public enum DialogueTimeMode
    {
        Scaled,
        Unscaled
    }
}
