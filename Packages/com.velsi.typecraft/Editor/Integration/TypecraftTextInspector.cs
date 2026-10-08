using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Window.Effects;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Editor
{
    [CustomEditor(typeof(TypecraftText)), CanEditMultipleObjects]
    public sealed class TypecraftTextInspector : UnityEditor.Editor
    {
        private const long RefreshInterval = 500;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            if (targets.Length != 1)
                return root;

            var warning = new EffectComponentsWarning();
            root.Add(warning);
            warning.schedule.Execute(() =>
            {
                var text = target as TypecraftText;
                warning.Refresh(text != null ? text.Target : null);
            }).Every(RefreshInterval);

            return root;
        }
    }
}
