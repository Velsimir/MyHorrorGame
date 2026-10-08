using UnityEditor;
using UnityEngine;

namespace Velsi.Typecraft.Editor.Localization
{
    public sealed class TextPrompt : EditorWindow
    {
        private string _message;
        private string _value;
        private string _result;
        private bool _focused;

        public static string Show(string title, string message, string initial)
        {
            var window = CreateInstance<TextPrompt>();
            window.titleContent = new GUIContent(title);
            window._message = message;
            window._value = initial ?? string.Empty;
            window.minSize = window.maxSize = new Vector2(380f, 96f);
            window.ShowModalUtility();
            return window._result;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(_message, EditorStyles.wordWrappedLabel);
            GUI.SetNextControlName("value");
            _value = EditorGUILayout.TextField(_value);
            if (!_focused)
            {
                EditorGUI.FocusTextInControl("value");
                _focused = true;
            }

            Event current = Event.current;
            bool submit = current.type == EventType.KeyDown && (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter);
            bool cancel = current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape;

            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                cancel |= GUILayout.Button("Отмена", GUILayout.Width(90f));
                submit |= GUILayout.Button("OK", GUILayout.Width(90f));
            }

            if (submit)
            {
                _result = string.IsNullOrWhiteSpace(_value) ? null : _value.Trim();
                Close();
            }
            else if (cancel)
            {
                Close();
            }
        }
    }
}
