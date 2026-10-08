using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Velsi.Typecraft.Text;
using Velsi.Typecraft.Text.Tables;
#if TYPECRAFT_SAMPLE_INPUTSYSTEM
using UnityEngine.InputSystem;
#endif
#if TYPECRAFT_SAMPLE_TMPEFFECTS
using TMPEffects.Components;
#endif

namespace Velsi.Typecraft.Samples.SimpleDialogue
{
    public sealed class SimpleDialogue : MonoBehaviour
    {
        [Serializable]
        private sealed class DialogueLine
        {
            [Tooltip("Ключ строки в таблице. Пусто — показывается текст ниже как есть, без перевода")]
            [SerializeField] private string _key;
            [SerializeField, TypecraftMarkup] private string _text;

            public string Key => _key;
            public string Text => _text;
        }

        [SerializeField] private TypecraftText _text;
        [SerializeField] private Button _next;
        [SerializeField] private GameObject _setupHint;
        [SerializeField] private string _table = "SimpleDialogue";
        [SerializeField] private string _playerName = "Alex";
        [SerializeField] private List<DialogueLine> _lines = new List<DialogueLine>();

        private int _index;

        private void Start()
        {
            PlayerTag.Name = _playerName;
            _next.onClick.AddListener(Next);
            if (_setupHint != null)
                _setupHint.SetActive(!TableRegistered());
            Show();
        }

        private void Update()
        {
            if (NextPressed())
                Next();
        }

        public void Next()
        {
#if TYPECRAFT_SAMPLE_TMPEFFECTS
            var writer = _text.GetComponent<TMPWriter>();
            if (writer != null && writer.IsWriting)
            {
                writer.SkipWriter();
                return;
            }
#endif
            _index = (_index + 1) % _lines.Count;
            Show();
        }

        private void Show()
        {
            if (_lines.Count == 0)
                return;

            DialogueLine line = _lines[_index];
            if (string.IsNullOrEmpty(line.Key))
                _text.SetMarkup(line.Text);
            else
                _text.SetKey(_table, line.Key);
        }

        private bool TableRegistered()
        {
            TypecraftSettings settings = Typecraft.Settings;
            return settings != null && settings.TextSource is TableTextSource tables && tables.FindTable(_table) != null;
        }

        private static bool NextPressed()
        {
#if TYPECRAFT_SAMPLE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                || gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
#else
            return false;
#endif
        }
    }
}
