using UnityEngine;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Dialogue
{
    public abstract class TextDialoguePresenter : DialoguePresenter
    {
        [Tooltip("Shown and hidden through alpha. Empty: the alpha of the text")]
        [SerializeField] private CanvasGroup _root;
        [SerializeField] private TypecraftText _text;
        [SerializeField] private bool _hideOnAwake = true;
        [Tooltip("Hide this many seconds after the line is fully shown. 0: keep it")]
        [SerializeField, Min(0f)] private float _hideAfter;
        [SerializeField] private bool _unscaledTime;

        private LineTypewriter _typewriter;
        private bool _hidePending;
        private float _hideAt;

        public CanvasGroup Root => _root;
        public TypecraftText Text => _text;

        public float HideAfter
        {
            get => _hideAfter;
            set => _hideAfter = Mathf.Max(0f, value);
        }

        public bool IsVisible { get; private set; }
        public DialogueLine CurrentLine { get; private set; }

        public override bool IsLineComplete => Typewriter == null || Typewriter.IsComplete;

        protected LineTypewriter Typewriter
        {
            get
            {
                if (_typewriter != null && _typewriter.Text == _text)
                    return _typewriter;

                _typewriter?.Dispose();
                _typewriter = null;
                if (_text == null)
                    return null;

                _typewriter = new LineTypewriter(_text);
                _typewriter.Completed += OnTypewriterCompleted;
                _typewriter.CharacterShown += OnCharacterShown;
                return _typewriter;
            }
        }

        protected float Now => _unscaledTime ? Time.unscaledTime : Time.time;

        public override void ShowLine(DialogueLine line)
        {
            CurrentLine = line;
            _hidePending = false;
            SetVisible(true);
            OnLineShowing(line);

            LineTypewriter typewriter = Typewriter;
            if (typewriter == null)
            {
                OnTypewriterCompleted();
                return;
            }

            typewriter.Begin(() => ApplyLine(line));
        }

        public override bool TryCompleteLine() => _typewriter != null && _typewriter.TryComplete();

        public override void Hide()
        {
            _hidePending = false;
            _typewriter?.Cancel();
            CurrentLine = null;
            SetVisible(false);
            OnHidden();
        }

        protected virtual void ApplyLine(DialogueLine line)
        {
            if (_text != null)
                _text.SetKey(line.ResolveTable(), line.Key);
        }

        protected virtual void OnLineShowing(DialogueLine line)
        {
        }

        protected virtual void OnLineShown()
        {
        }

        protected virtual void OnCharacterShown(char character)
        {
        }

        protected virtual void OnHidden()
        {
        }

        protected virtual void OnTypecraftChanged()
        {
        }

        protected virtual void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (_root != null)
            {
                _root.alpha = visible ? 1f : 0f;
                _root.interactable = visible;
                _root.blocksRaycasts = visible;
            }
            else if (_text != null && _text.Target != null)
            {
                _text.Target.alpha = visible ? 1f : 0f;
            }
        }

        protected virtual void Reset()
        {
            _root = GetComponent<CanvasGroup>();
            _text = GetComponentInChildren<TypecraftText>(true);
        }

        protected virtual void Awake()
        {
            if (_hideOnAwake && Application.isPlaying)
                Hide();
        }

        protected virtual void OnEnable() => Typecraft.Changed += HandleTypecraftChanged;

        protected virtual void OnDisable() => Typecraft.Changed -= HandleTypecraftChanged;

        protected virtual void OnDestroy()
        {
            _typewriter?.Dispose();
            _typewriter = null;
        }

        protected virtual void Update()
        {
            if (_hidePending && Now >= _hideAt)
                Hide();
        }

        private void HandleTypecraftChanged()
        {
            if (CurrentLine != null)
                OnTypecraftChanged();
        }

        private void OnTypewriterCompleted()
        {
            if (_hideAfter > 0f)
            {
                _hidePending = true;
                _hideAt = Now + _hideAfter;
            }

            OnLineShown();
            RaiseLineCompleted();
        }
    }
}
