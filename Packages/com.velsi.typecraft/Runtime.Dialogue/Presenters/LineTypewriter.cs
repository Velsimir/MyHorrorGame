using System;
using UnityEngine;
using Velsi.Typecraft.Text;
#if TYPECRAFT_TMPEFFECTS
using TMPEffects.CharacterData;
using TMPEffects.Components;
using UnityEngine.Events;
#endif

namespace Velsi.Typecraft.Dialogue
{
    public sealed class LineTypewriter : IDisposable
    {
        private readonly TypecraftText _text;
        private bool _active;
        private bool _complete = true;
#if TYPECRAFT_TMPEFFECTS
        private TMPWriter _writer;
#endif

        public LineTypewriter(TypecraftText text) => _text = text;

        public event Action Completed;
        public event Action<char> CharacterShown;

        public TypecraftText Text => _text;
        public bool IsActive => _active;
        public bool IsComplete => _complete;

        public bool IsTyping
        {
            get
            {
#if TYPECRAFT_TMPEFFECTS
                return _active && !_complete && _writer != null && _writer.IsWriting;
#else
                return false;
#endif
            }
        }

        public void Begin(Action apply)
        {
            _active = true;
            _complete = false;
            apply?.Invoke();
            if (!_active || _complete)
                return;

#if TYPECRAFT_TMPEFFECTS
            TMPWriter writer = Bind();
            if (Application.isPlaying && writer != null && writer.isActiveAndEnabled && _text.isActiveAndEnabled)
            {
                _text.Target.ForceMeshUpdate();
                if (_active && !_complete && !writer.IsWriting)
                    writer.RestartWriter();
                return;
            }
#endif
            Complete();
        }

        public bool TryComplete()
        {
            if (!_active || _complete)
                return false;

#if TYPECRAFT_TMPEFFECTS
            if (_writer != null && _writer.isActiveAndEnabled && _writer.IsWriting)
            {
                if (_writer.MaySkip)
                    _writer.SkipWriter();
                return true;
            }
#endif
            Complete();
            return true;
        }

        public void Cancel() => _active = false;

        public void Dispose()
        {
            Cancel();
#if TYPECRAFT_TMPEFFECTS
            Unbind();
#endif
        }

        private void Complete()
        {
            if (_complete)
                return;

            _complete = true;
            Completed?.Invoke();
        }

#if TYPECRAFT_TMPEFFECTS
        private TMPWriter Bind()
        {
            TMPWriter writer = _text != null ? _text.GetComponent<TMPWriter>() : null;
            if (writer == _writer)
                return writer;

            Unbind();
            _writer = writer;
            if (_writer == null)
                return null;

            _writer.OnFinishWriter ??= new UnityEvent<TMPWriter>();
            _writer.OnCharacterShown ??= new UnityEvent<TMPWriter, CharData>();
            _writer.OnFinishWriter.AddListener(OnWriterFinished);
            _writer.OnCharacterShown.AddListener(OnWriterCharacterShown);
            return _writer;
        }

        private void Unbind()
        {
            if (_writer != null)
            {
                _writer.OnFinishWriter?.RemoveListener(OnWriterFinished);
                _writer.OnCharacterShown?.RemoveListener(OnWriterCharacterShown);
            }

            _writer = null;
        }

        private void OnWriterFinished(TMPWriter writer)
        {
            if (_active)
                Complete();
        }

        private void OnWriterCharacterShown(TMPWriter writer, CharData character)
        {
            if (_active && !_complete)
                CharacterShown?.Invoke(character.info.character);
        }
#endif
    }
}
