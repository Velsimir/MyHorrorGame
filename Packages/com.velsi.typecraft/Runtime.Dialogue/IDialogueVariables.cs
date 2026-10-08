using System;

namespace Velsi.Typecraft.Dialogue
{
    public interface IDialogueVariables
    {
        event Action<string> Changed;

        bool TryGet(string name, out DialogueValue value);

        void Set(string name, DialogueValue value);
    }
}
