using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Velsi.Typecraft.Dialogue;
using Velsi.Typecraft.Text;
using Velsi.Typecraft.Text.Tables;
#if TYPECRAFT_SAMPLE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Velsi.Typecraft.Samples.Showcase
{
    public sealed class ShowcaseSample : MonoBehaviour
    {
        [SerializeField] private DialogueVariableCatalog _catalog;
        [SerializeField] private string _table = "Showcase";
        [SerializeField] private DialoguePlayer _player;
        [SerializeField] private Image _lamp;
        [SerializeField] private Color _lampOff = new Color(0.25f, 0.22f, 0.2f);
        [SerializeField] private Color _lampOn = new Color(1f, 0.82f, 0.35f);
        [SerializeField] private string _eventName = "lamp";
        [SerializeField] private Button _talk;
        [SerializeField] private Button _next;
        [SerializeField] private Button _resetFlags;
        [SerializeField] private Button _locale;
        [SerializeField] private TMP_Text _localeLabel;
        [SerializeField] private TMP_Text _flags;
        [SerializeField] private GameObject _setupHint;

        private IDialogueVariables _watched;

        private void Awake()
        {
            DialogueGlobals.Catalog = _catalog;
            EnsureEventSystem();
        }

        private void Start()
        {
            _talk.onClick.AddListener(Talk);
            _next.onClick.AddListener(() => Next());
            _resetFlags.onClick.AddListener(ResetFlags);
            _locale.onClick.AddListener(NextLocale);
            _player.EventRaised += OnDialogueEvent;

            DialogueGlobals.VariablesReplaced += Watch;
            Typecraft.Changed += UpdateLocaleLabel;
            Watch();
            UpdateLocaleLabel();
            ApplyLamp(false);

            if (_setupHint != null)
                _setupHint.SetActive(!TableRegistered());
        }

        private void OnDestroy()
        {
            DialogueGlobals.VariablesReplaced -= Watch;
            Typecraft.Changed -= UpdateLocaleLabel;
            if (_player != null)
                _player.EventRaised -= OnDialogueEvent;
            if (_watched != null)
                _watched.Changed -= OnVariableChanged;
        }

        private void Update()
        {
            if (NextPressed())
                Next();
        }

        public void Talk() => _player.Play();

        public bool Next() => _player.IsPlaying && _player.Current.Kind == DialogueStepKind.Line && _player.Advance();

        public void ResetFlags()
        {
            _player.Stop();

            IDialogueVariables variables = DialogueGlobals.Variables;
            if (variables is DialogueVariables own)
                own.Clear();
            if (_catalog != null)
                _catalog.ApplyDefaults(variables, overwrite: true);

            ApplyLamp(false);
            UpdateFlags();
        }

        private void OnDialogueEvent(string eventName, string argument)
        {
            if (eventName == _eventName)
                ApplyLamp(argument == "on");
        }

        private void ApplyLamp(bool lit)
        {
            if (_lamp != null)
                _lamp.color = lit ? _lampOn : _lampOff;
        }

        private void Watch()
        {
            if (_watched != null)
                _watched.Changed -= OnVariableChanged;

            _watched = DialogueGlobals.Variables;
            _watched.Changed += OnVariableChanged;
            UpdateFlags();
        }

        private void OnVariableChanged(string name) => UpdateFlags();

        private void UpdateFlags()
        {
            if (_flags == null || _catalog == null)
                return;

            var text = new StringBuilder();
            foreach (DialogueVariableDefinition definition in _catalog.Variables)
            {
                if (definition == null || string.IsNullOrEmpty(definition.Name))
                    continue;

                DialogueValue value = DialogueVariableLookup.Get(DialogueGlobals.Variables, _catalog, definition.Name, definition.Type);
                text.Append(definition.Name).Append(" = ").Append(value).Append('\n');
            }

            _flags.text = text.ToString();
        }

        private void NextLocale()
        {
            ITextSource source = Typecraft.Settings != null ? Typecraft.Settings.TextSource : null;
            List<string> locales = source != null ? source.Locales.ToList() : new List<string>();
            if (locales.Count == 0)
                return;

            int index = locales.IndexOf(Typecraft.Locale);
            source.SelectLocale(locales[(index + 1) % locales.Count]);
        }

        private void UpdateLocaleLabel()
        {
            if (_localeLabel != null)
                _localeLabel.text = "Language: " + Typecraft.Locale;
        }

        private bool TableRegistered()
        {
            TypecraftSettings settings = Typecraft.Settings;
            return settings != null && settings.TextSource is TableTextSource tables && tables.FindTable(_table) != null;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null)
                return;

            var host = new GameObject("EventSystem", typeof(EventSystem));
#if TYPECRAFT_SAMPLE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            host.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            host.AddComponent<StandaloneInputModule>();
#endif
        }

        private static bool NextPressed()
        {
#if TYPECRAFT_SAMPLE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame
                || gamepad != null && gamepad.buttonEast.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }
    }
}
