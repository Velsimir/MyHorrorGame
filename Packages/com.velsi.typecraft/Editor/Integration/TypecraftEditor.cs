using System;
using TMPro;
using UnityEditor;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Editor.Window;
using Velsi.Typecraft.Editor.Window.Targets;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Editor
{
    public static class TypecraftEditor
    {
        public static void Edit(string markup, Action<string> onApply, string title = null, TMP_Text reference = null) =>
            TypecraftWindow.Edit(new CallbackTarget(markup, onApply, title, reference));

        public static void Edit(ITextEditTarget target) => TypecraftWindow.Edit(target);

        public static void Edit(TMP_Text text)
        {
            if (text == null)
                return;

            var component = text.GetComponent<TypecraftText>();
            if (component == null)
                TypecraftWindow.Edit(new TmpTextTarget(text));
            else if (component.Mode == TextMode.Localized)
                TypecraftWindow.Edit(new LocalizedTextTarget(component, () => Typecraft.Locale));
            else
                TypecraftWindow.Edit(new TypecraftTextTarget(component));
        }

        public static void Edit(SerializedProperty property)
        {
            if (property == null || property.propertyType != SerializedPropertyType.String)
                return;

            UnityEngine.Object owner = property.serializedObject.targetObject;
            if (owner is TMP_Text text && property.propertyPath == "m_text")
                Edit(text);
            else if (owner is TypecraftText component && property.propertyPath == "_markup")
                Edit(component.Target);
            else
                TypecraftWindow.Edit(new PropertyTarget(property));
        }

        public static void RegisterTableBackend(Func<ITextSource, ITableBackend> factory) => TableBackends.Register(factory);
    }
}
