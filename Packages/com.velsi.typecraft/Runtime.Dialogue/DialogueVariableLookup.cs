namespace Velsi.Typecraft.Dialogue
{
    public static class DialogueVariableLookup
    {
        public static DialogueValue Get(IDialogueVariables variables, DialogueVariableCatalog catalog, string name, DialogueValueType fallback)
        {
            if (string.IsNullOrEmpty(name))
                return DialogueValue.DefaultOf(fallback);

            if (variables != null && variables.TryGet(name, out DialogueValue value))
                return value;

            if (catalog != null && catalog.TryGet(name, out DialogueVariableDefinition definition))
                return definition.Default;

            return DialogueValue.DefaultOf(fallback);
        }
    }
}
