using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    public abstract class DialogueVariablesSource : MonoBehaviour
    {
        public abstract IDialogueVariables Variables { get; }

        public virtual DialogueVariableCatalog Catalog => null;
    }
}
