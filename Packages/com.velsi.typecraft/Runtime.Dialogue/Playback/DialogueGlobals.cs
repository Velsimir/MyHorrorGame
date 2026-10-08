using System;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    public static class DialogueGlobals
    {
        private static IDialogueVariables _variables;

        public static event Action VariablesReplaced;

        public static IDialogueVariables Variables => _variables ??= new DialogueVariables();

        public static DialogueVariableCatalog Catalog { get; set; }

        public static void SetVariables(IDialogueVariables variables)
        {
            if (ReferenceEquals(_variables, variables))
                return;

            _variables = variables;
            VariablesReplaced?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _variables = null;
            Catalog = null;
            VariablesReplaced = null;
        }
    }
}
