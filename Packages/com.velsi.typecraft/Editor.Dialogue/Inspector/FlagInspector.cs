using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Velsi.Typecraft.Dialogue.Editor.Inspector
{
    public sealed class FlagInspector : VisualElement
    {
        private static readonly List<string> Types = new List<string> { "bool", "int", "float", "string" };

        public FlagInspector(IDialogueEditorContext context, DialogueVariableCatalog catalog, DialogueVariableDefinition definition, Action delete, Func<string, bool> rename)
        {
            AddToClassList("dlg-inspector");
            void Edit(string undoName, Action change, bool rebuild = false) => context.Edit(catalog, undoName, change, rebuild);

            var header = new VisualElement();
            header.AddToClassList("tc-lib__header");
            var title = new Label("⚑ " + definition.Name);
            title.AddToClassList("tc-lib__title");
            header.Add(title);
            header.Add(new Button(delete) { text = "Удалить" });
            Add(header);

            var owner = new ObjectField("Каталог") { objectType = typeof(DialogueVariableCatalog), value = catalog };
            owner.SetEnabled(false);
            Add(owner);

            var name = new TextField("Имя") { value = definition.Name, isDelayed = true, tooltip = "Переименование перепишет условия и присваивания во всех диалогах проекта (с подтверждением)" };
            name.RegisterValueChangedCallback(evt =>
            {
                string value = evt.newValue?.Trim();
                if (string.IsNullOrEmpty(value) || value == definition.Name)
                    return;
                if (catalog.Contains(value))
                {
                    EditorUtility.DisplayDialog("Typecraft", $"Флаг «{value}» уже есть в каталоге.", "OK");
                    name.SetValueWithoutNotify(definition.Name);
                    return;
                }

                if (!rename(value))
                    name.SetValueWithoutNotify(definition.Name);
            });
            Add(name);

            var type = new DropdownField("Тип", Types, (int)definition.Type);
            type.RegisterValueChangedCallback(_ => Edit("тип флага", () =>
            {
                definition.Type = (DialogueValueType)type.index;
                definition.Default = definition.Default.ConvertTo(definition.Type);
            }, true));
            Add(type);

            var value = new DialogueValueField("По умолчанию");
            value.SetValue(definition.Default);
            value.Changed += changed => Edit("значение флага", () => definition.Default = changed);
            Add(value);

            var description = new TextField("Описание") { value = definition.Description, multiline = true, isDelayed = true };
            description.AddToClassList("dlg-multiline");
            description.RegisterValueChangedCallback(evt => Edit("описание флага", () => definition.Description = evt.newValue));
            Add(description);
        }
    }
}
