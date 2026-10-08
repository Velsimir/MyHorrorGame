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

namespace Velsi.Typecraft.Samples.BranchingDialogue
{
    public sealed class BranchingDialogueSample : MonoBehaviour
    {
        [SerializeField] private DialogueVariableCatalog _catalog;
        [SerializeField] private string _table = "BranchingDialogue";

        [Header("Dialogues")]
        [SerializeField] private DialoguePlayer _keeper;
        [SerializeField] private DialoguePlayer _note;
        [SerializeField] private DialoguePlayer _thought;
        [SerializeField] private DialogueTrigger _zone;
        [SerializeField] private SampleDoor _door;

        [Header("UI")]
        [SerializeField] private Button _talk;
        [SerializeField] private Button _readNote;
        [SerializeField] private Button _enterZone;
        [SerializeField] private Button _next;
        [SerializeField] private Button _resetFlags;
        [SerializeField] private Button _locale;
        [SerializeField] private TMP_Text _localeLabel;
        [SerializeField] private TMP_Text _flags;
        [SerializeField] private GameObject _setupHint;

        private IDialogueVariables _watched;

        public DialoguePlayer Keeper => _keeper;
        public DialoguePlayer Note => _note;
        public DialoguePlayer Thought => _thought;
        public SampleDoor Door => _door;

        private void Awake()
        {
            DialogueGlobals.Catalog = _catalog;
            EnsureEventSystem();
        }

        private void Start()
        {
            _talk.onClick.AddListener(Talk);
            _readNote.onClick.AddListener(ReadNote);
            _enterZone.onClick.AddListener(EnterZone);
            _next.onClick.AddListener(() => Next());
            _resetFlags.onClick.AddListener(ResetFlags);
            _locale.onClick.AddListener(NextLocale);

            DialogueGlobals.VariablesReplaced += Watch;
            Typecraft.Changed += UpdateLocaleLabel;
            Watch();
            UpdateLocaleLabel();

            if (_setupHint != null)
                _setupHint.SetActive(!TableRegistered());
        }

        private void OnDestroy()
        {
            DialogueGlobals.VariablesReplaced -= Watch;
            Typecraft.Changed -= UpdateLocaleLabel;
            if (_watched != null)
                _watched.Changed -= OnVariableChanged;
        }

        private void Update()
        {
            if (NextPressed())
                Next();
        }

        public void Talk() => _keeper.Play();

        public void ReadNote() => _note.Play();

        public void EnterZone() => _zone.Fire();

        public bool Next()
        {
            foreach (DialoguePlayer player in Players())
            {
                if (player.IsPlaying && player.Current.Kind == DialogueStepKind.Line)
                    return player.Advance();
            }

            return false;
        }

        public void ResetFlags()
        {
            foreach (DialoguePlayer player in Players())
                player.Stop();

            IDialogueVariables variables = DialogueGlobals.Variables;
            if (variables is DialogueVariables own)
                own.Clear();
            if (_catalog != null)
                _catalog.ApplyDefaults(variables, overwrite: true);

            _zone.ResetFired();
            _door.Close();
            UpdateFlags();
        }

        private IEnumerable<DialoguePlayer> Players()
        {
            if (_keeper != null)
                yield return _keeper;
            if (_note != null)
                yield return _note;
            if (_thought != null)
                yield return _thought;
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

            text.Append("thought played = ").Append(_thought != null && _thought.HasPlayed ? "true" : "false");
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
