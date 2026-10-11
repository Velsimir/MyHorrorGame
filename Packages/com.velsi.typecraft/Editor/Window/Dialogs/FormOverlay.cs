using System;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Window.Layout;

namespace Velsi.Typecraft.Editor.Window.Dialogs
{
    public sealed class FormOverlay : VisualElement
    {
        private readonly VisualElement _card;
        private TypecraftForm _form;

        public FormOverlay()
        {
            AddToClassList("tc-overlay");
            style.display = DisplayStyle.None;
            focusable = true;

            _card = new VisualElement();
            _card.AddToClassList("tc-overlay__card");
            Add(_card);

            RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Escape || _form == null)
                    return;

                evt.StopPropagation();
                Close();
            });
        }

        public bool IsOpen => _form != null;
        public TypecraftForm Form => _form;

        public void Show(TypecraftForm form)
        {
            Close();
            _form = form;
            _form.Closed += Close;
            _card.Add(_form);
            style.display = DisplayStyle.Flex;
            BringToFront();
            Focus();
        }

        public void Close()
        {
            if (_form == null)
                return;

            TypecraftForm form = _form;
            _form = null;
            form.Closed -= Close;
            form.RemoveFromHierarchy();
            form.Dispose();
            style.display = DisplayStyle.None;
        }

        public void Tick(float deltaTime) => _form?.Tick(deltaTime);
    }

    public abstract class TypecraftForm : VisualElement, IDisposable
    {
        private readonly Label _error;

        protected TypecraftForm(string title)
        {
            AddToClassList("tc-form");

            var header = new Label(title);
            header.AddToClassList("tc-form__title");
            Add(header);

            var body = new VisualElement();
            body.AddToClassList("tc-form__body");
            TypecraftLayout.Responsive(body, 640f, "tc-form__body--narrow");
            Add(body);

            Fields = TypecraftLayout.VerticalScroll("tc-form__fields");
            Fields.AddToClassList(TypecraftLayout.FitClass);
            body.Add(Fields);

            Side = new VisualElement();
            Side.AddToClassList("tc-form__side");
            Side.AddToClassList(TypecraftLayout.FitClass);
            body.Add(Side);

            var footer = new VisualElement();
            footer.AddToClassList("tc-form__footer");
            _error = new Label();
            _error.AddToClassList("tc-form__error");
            footer.Add(_error);
            footer.Add(new Button(Close) { text = "Отмена" });
            SaveButton = new Button(TrySave) { text = "Сохранить" };
            SaveButton.AddToClassList("tc-toolbar__apply");
            footer.Add(SaveButton);
            Add(footer);
        }

        public event Action Closed;

        protected ScrollView Fields { get; }
        protected VisualElement Side { get; }
        protected Button SaveButton { get; }

        public virtual void Tick(float deltaTime)
        {
        }

        public virtual void Dispose()
        {
        }

        public bool Submit()
        {
            if (!SaveButton.enabledSelf)
                return false;

            bool saved = Save();
            if (saved)
                Close();
            return saved;
        }

        protected abstract bool Save();

        protected void SetError(string error)
        {
            _error.text = error ?? string.Empty;
            SaveButton.SetEnabled(string.IsNullOrEmpty(error));
        }

        protected void Close() => Closed?.Invoke();

        protected static VisualElement Section(string title)
        {
            var label = new Label(title);
            label.AddToClassList("tc-lib__section");
            return label;
        }

        private void TrySave() => Submit();
    }
}
