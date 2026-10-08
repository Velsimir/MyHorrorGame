using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    public abstract class DialoguePresenter : MonoBehaviour
    {
        public event Action LineCompleted;

        public virtual bool SupportsChoices => false;

        public virtual bool IsLineComplete => true;

        public abstract void ShowLine(DialogueLine line);

        public virtual void ShowChoices(IReadOnlyList<DialogueChoice> choices, Action<int> choose) =>
            Debug.LogWarning($"[Typecraft] Dialogue: {GetType().Name} on '{name}' does not support choices", this);

        public abstract void Hide();

        public virtual bool TryCompleteLine() => false;

        protected void RaiseLineCompleted() => LineCompleted?.Invoke();
    }
}
