using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UIElements;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Editor.Window.Effects
{
    public sealed class EffectComponentsWarning : VisualElement
    {
        private const string WriterMessage = "В тексте есть команды <!…>, но на объекте нет TMPWriter — они не выполнятся.";
        private const string AnimatorMessage = "В тексте есть анимации, но на объекте нет TMPAnimator.";

        private readonly HelpBox _box = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
        private TMP_Text _text;
        private string[] _markups = Array.Empty<string>();

        public EffectComponentsWarning()
        {
            Add(_box);
            _box.Add(new Button(AddMissing) { text = "Добавить" });
            style.display = DisplayStyle.None;
        }

        public event Action Added;

        public void Refresh(TMP_Text text)
        {
            IEnumerable<string> markups = text == null
                ? null
                : text.TryGetComponent(out TypecraftText component) ? EffectComponents.Markups(component) : new[] { text.text };
            Refresh(text, markups);
        }

        public void Refresh(TMP_Text text, IEnumerable<string> markups)
        {
            _text = text;
            _markups = markups?.ToArray() ?? Array.Empty<string>();
            EffectComponents.Missing(_text, _markups, out bool writer, out bool animator);

            var lines = new List<string>();
            if (writer)
                lines.Add(WriterMessage);
            if (animator)
                lines.Add(AnimatorMessage);

            _box.text = string.Join("\n", lines);
            style.display = lines.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void AddMissing()
        {
            if (_text == null)
                return;

            EffectComponents.Ensure(_text, _markups);
            if (_text.TryGetComponent(out TypecraftText component))
                component.Refresh();

            Refresh(_text, _markups);
            Added?.Invoke();
        }
    }
}
