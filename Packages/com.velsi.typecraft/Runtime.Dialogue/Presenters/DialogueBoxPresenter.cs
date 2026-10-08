using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Dialogue
{
    [AddComponentMenu("Typecraft/Dialogue/Dialogue Box Presenter")]
    public sealed class DialogueBoxPresenter : TextDialoguePresenter
    {
        [Header("Speaker")]
        [SerializeField] private TypecraftText _nameText;
        [Tooltip("Hidden together with the name. Empty: the name object")]
        [SerializeField] private GameObject _nameRoot;
        [Tooltip("Style when the speaker has none")]
        [SerializeField] private string _nameStyle;
        [SerializeField] private Image _portrait;
        [Tooltip("Used for speakers with Side = Right. Empty: the left portrait")]
        [SerializeField] private Image _portraitRight;

        [Header("Flow")]
        [SerializeField] private GameObject _nextIndicator;

        [Header("Choices")]
        [SerializeField] private RectTransform _choiceContainer;
        [Tooltip("Button with a TypecraftText in its children. May be an inactive child of the container")]
        [SerializeField] private Button _choicePrefab;

        [Header("Voice")]
        [SerializeField] private AudioSource _voice;
        [SerializeField, Min(0f)] private float _blipInterval = 0.06f;

        private readonly List<Button> _buttons = new List<Button>();
        private Action<int> _choose;
        private int _nameRequest;
        private float _lastBlip = float.NegativeInfinity;

        public override bool SupportsChoices => _choiceContainer != null && _choicePrefab != null;

        public IReadOnlyList<Button> ChoiceButtons => _buttons;

        public override void ShowLine(DialogueLine line)
        {
            base.ShowLine(line);
            if (CurrentLine == line && IsLineComplete)
                PlayBlip(line, force: true);
        }

        public override void ShowChoices(IReadOnlyList<DialogueChoice> choices, Action<int> choose)
        {
            if (!SupportsChoices)
            {
                base.ShowChoices(choices, choose);
                return;
            }

            SetVisible(true);
            SetActive(_nextIndicator, false);
            _choose = choose;
            _choiceContainer.gameObject.SetActive(true);

            Button first = null;
            int count = choices != null ? choices.Count : 0;
            for (int i = 0; i < count; i++)
            {
                DialogueChoice choice = choices[i];
                Button button = ButtonAt(i);
                button.gameObject.SetActive(true);
                button.interactable = choice.Available;

                TypecraftText label = button.GetComponentInChildren<TypecraftText>(true);
                if (label != null)
                    label.SetKey(choice.ResolveTable(), choice.Key);

                int index = choice.Index;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Pick(index));

                if (first == null && choice.Available)
                    first = button;
            }

            for (int i = count; i < _buttons.Count; i++)
                _buttons[i].gameObject.SetActive(false);

            if (first != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        protected override void OnLineShowing(DialogueLine line)
        {
            ClearChoices();
            SetActive(_nextIndicator, false);
            ShowPortrait(line);
            ShowName(line);
        }

        protected override void OnLineShown()
        {
            if (CurrentLine != null)
                SetActive(_nextIndicator, CurrentLine.Advance == LineAdvance.Input);
        }

        protected override void OnCharacterShown(char character)
        {
            if (!char.IsWhiteSpace(character) && CurrentLine != null)
                PlayBlip(CurrentLine, force: false);
        }

        protected override void OnHidden()
        {
            _nameRequest++;
            ClearChoices();
            SetActive(_nextIndicator, false);
        }

        protected override void OnTypecraftChanged() => ShowName(CurrentLine);

        private void ShowName(DialogueLine line)
        {
            if (_nameText == null)
                return;

            int request = ++_nameRequest;
            DialogueSpeakerText.RequestName(line?.Speaker, line?.ResolveTable(), _nameStyle, markup =>
            {
                if (request != _nameRequest || this == null)
                    return;

                bool visible = !string.IsNullOrEmpty(markup);
                SetActive(_nameRoot != null ? _nameRoot : _nameText.gameObject, visible);
                if (visible)
                    _nameText.SetMarkup(markup);
            });
        }

        private void ShowPortrait(DialogueLine line)
        {
            Sprite sprite = line?.Portrait;
            bool right = line?.Speaker != null && line.Speaker.Side == SpeakerSide.Right && _portraitRight != null;
            Image shown = right ? _portraitRight : _portrait;

            SetPortrait(_portrait, shown == _portrait ? sprite : null);
            if (_portraitRight != _portrait)
                SetPortrait(_portraitRight, shown == _portraitRight ? sprite : null);
        }

        private static void SetPortrait(Image image, Sprite sprite)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.gameObject.SetActive(sprite != null);
        }

        private Button ButtonAt(int index)
        {
            while (_buttons.Count <= index)
            {
                Button button = Instantiate(_choicePrefab, _choiceContainer);
                button.name = $"{_choicePrefab.name} {_buttons.Count}";
                _buttons.Add(button);
            }

            return _buttons[index];
        }

        private void Pick(int index)
        {
            Action<int> choose = _choose;
            if (choose == null)
                return;

            ClearChoices();
            choose(index);
        }

        private void ClearChoices()
        {
            _choose = null;
            EventSystem eventSystem = EventSystem.current;
            foreach (Button button in _buttons)
            {
                if (button == null)
                    continue;

                if (eventSystem != null && eventSystem.currentSelectedGameObject == button.gameObject)
                    eventSystem.SetSelectedGameObject(null);
                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
            }

            if (_choiceContainer != null)
                _choiceContainer.gameObject.SetActive(false);
        }

        private void PlayBlip(DialogueLine line, bool force)
        {
            AudioClip clip = line.Speaker != null ? line.Speaker.VoiceBlip : null;
            if (_voice == null || clip == null || !Application.isPlaying)
                return;

            float now = Time.unscaledTime;
            if (!force && now - _lastBlip < _blipInterval)
                return;

            _lastBlip = now;
            _voice.PlayOneShot(clip);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _buttons.Clear();
        }
    }
}
