using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using Velsi.Typecraft.Editor.Window.Targets;
using Velsi.Typecraft.Markup;
using Velsi.Typecraft.Text;
#if TYPECRAFT_TMPEFFECTS
using TMPEffects.Components;
#endif

namespace Velsi.Typecraft.Editor.Window.Effects
{
    public static class EffectComponents
    {
        public static void Ensure(TMP_Text text, string markup) => Ensure(text, new[] { markup });

        public static void Ensure(TMP_Text text, IEnumerable<string> markups)
        {
#if TYPECRAFT_TMPEFFECTS
            if (text == null || markups == null)
                return;

            Scan(markups, out bool animations, out bool commands);

            var animator = text.GetComponent<TMPAnimator>();
            if (animations && animator == null)
            {
                animator = Undo.AddComponent<TMPAnimator>(text.gameObject);
                EffectLibrary.Configure(animator);
                // TMPAnimator.Reset в Edit Mode выключает компонент так же, как TMPWriter.
                animator.enabled = true;
            }
            else if (animator != null && EffectLibrary.Project != null && EffectLibrary.UsesDefault(animator))
                EffectLibrary.Configure(animator);

            if (commands && text.GetComponent<TMPWriter>() == null)
                AddWriter(text);
#endif
        }

        public static void Missing(TMP_Text text, IEnumerable<string> markups, out bool writer, out bool animator)
        {
            writer = false;
            animator = false;
#if TYPECRAFT_TMPEFFECTS
            if (text == null || markups == null)
                return;

            Scan(markups, out bool animations, out bool commands);
            writer = commands && text.GetComponent<TMPWriter>() == null;
            animator = animations && text.GetComponent<TMPAnimator>() == null;
#endif
        }

        public static IEnumerable<string> Markups(TypecraftText text)
        {
            if (text == null)
                return Array.Empty<string>();

            return text.Mode == TextMode.Localized
                ? new LocalizedTextTarget(text, () => null).AllMarkups()
                : new[] { text.Markup };
        }

        private static void Scan(IEnumerable<string> markups, out bool animations, out bool commands)
        {
            animations = false;
            commands = false;

            foreach (string markup in markups)
            {
                if (string.IsNullOrEmpty(markup))
                    continue;

                EffectTags.Scan(Typecraft.Render(markup), EffectCatalog.IsEffectTag, out bool hasAnimations, out bool hasCommands);
                animations |= hasAnimations;
                commands |= hasCommands;
            }
        }

#if TYPECRAFT_TMPEFFECTS
        private static void AddWriter(TMP_Text text)
        {
            var writer = Undo.AddComponent<TMPWriter>(text.gameObject);
            if (Application.isPlaying)
            {
                writer.WriteOnStart = true;
                writer.WriteOnNewText = true;
                writer.SetDatabase(EffectCatalog.DefaultCommandDatabase);
                return;
            }

            var serialized = new SerializedObject(writer);
            serialized.FindProperty("database").objectReferenceValue = EffectCatalog.DefaultCommandDatabase;
            serialized.FindProperty("writeOnStart").boolValue = true;
            serialized.FindProperty("writeOnNewText").boolValue = true;
            serialized.ApplyModifiedProperties();

            // TMPWriter.Reset в Edit Mode выключает компонент и включает его через delayCall, который может не дойти; включаем сами, OnEnable соберёт обработчики уже с базой.
            writer.enabled = true;
        }
#endif
    }
}
