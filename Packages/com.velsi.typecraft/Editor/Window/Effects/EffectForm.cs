#if TYPECRAFT_TMPEFFECTS
using System;
using System.Collections.Generic;
using System.Linq;
using TMPEffects.Databases.AnimationDatabase;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Window.Dialogs;
using Velsi.Typecraft.Editor.Window.Markup;
using Velsi.Typecraft.Editor.Window.Preview;
using Velsi.Typecraft.Fonts;

namespace Velsi.Typecraft.Editor.Window.Effects
{
    public sealed class EffectForm : TypecraftForm
    {
        private const string Sample = "Дверь заперта — The door is locked";
        private const string DraftName = "tc_draft";
        private const float ReplayInterval = 2.5f;

        private static readonly (EffectKind Kind, string Label)[] Kinds =
        {
            (EffectKind.Basic, "постоянный (basic)"),
            (EffectKind.Show, "появление (show, <+…>)"),
            (EffectKind.Hide, "исчезание (hide, <-…>)")
        };

        private readonly TextPreview _preview = new TextPreview();
        private readonly DropdownField _kindField;
        private readonly DropdownField _baseField;
        private readonly TextField _name;
        private readonly VisualElement _inspector;
        private readonly Button _replay;
        private readonly string _markup;
        private readonly TextRange _range;
        private readonly string _locale;
        private readonly DeviceFamily _device;

        private EffectKind _kind = EffectKind.Basic;
        private IReadOnlyList<EffectBase> _bases = Array.Empty<EffectBase>();
        private ScriptableObject _draft;
        private UnityEditor.Editor _draftEditor;
        private TMPAnimationDatabase _previewDatabase;
        private string _previewName;
        private float _replayTimer;

        public EffectForm(string markup, TextRange selection, string locale, DeviceFamily device) : base("Новый эффект")
        {
            _markup = markup ?? string.Empty;
            _range = selection.IsEmpty || _markup.Length == 0 ? default : MarkupEdits.Snap(_markup, selection);
            _locale = locale ?? Typecraft.Locale;
            _device = device;

            _kindField = new DropdownField("Тип", Kinds.Select(k => k.Label).ToList(), 0);
            _kindField.RegisterValueChangedCallback(_ => SetKind(Kinds[Mathf.Max(0, _kindField.index)].Kind));
            Fields.Add(_kindField);

            _baseField = new DropdownField("Основа") { tooltip = "Копия встроенной анимации с другими параметрами, Generic — своя анимация из дорожек, Stack — несколько анимаций разом" };
            _baseField.RegisterValueChangedCallback(_ => RebuildDraft());
            Fields.Add(_baseField);

            _name = new TextField("Имя тега") { value = "myeffect", tooltip = "Тег в тексте: <имя>, <+имя> или <-имя>" };
            _name.RegisterValueChangedCallback(_ => OnNameChanged());
            Fields.Add(_name);

            Fields.Add(Section("Параметры"));
            _inspector = new VisualElement();
            _inspector.AddToClassList("tc-form__inspector");
            Fields.Add(_inspector);

            _replay = new Button(Replay) { text = "▶ повторить", tooltip = "Проиграть появление / исчезание ещё раз" };
            Side.Add(_replay);
            _preview.Element.AddToClassList("tc-form__preview");
            Side.Add(_preview.Element);

            SetKind(EffectKind.Basic);
        }

        public event Action<string> Saved;

        public ScriptableObject Draft => _draft;
        public TextPreview Preview => _preview;
        public string PreviewMarkup { get; private set; }
        public ScriptableObject SavedAsset { get; private set; }
        public IReadOnlyList<string> BaseLabels => _bases.Select(b => b.Label).ToList();

        public void SetKind(EffectKind kind)
        {
            _kind = kind;
            _kindField.SetValueWithoutNotify(Kinds.First(k => k.Kind == kind).Label);
            _bases = EffectLibrary.Bases(kind);
            _baseField.choices = _bases.Select(b => b.Label).ToList();
            _baseField.SetValueWithoutNotify(_bases.Count > 0 ? _bases[0].Label : string.Empty);
            _replay.style.display = kind == EffectKind.Basic ? DisplayStyle.None : DisplayStyle.Flex;
            RebuildDraft();
        }

        public void SetBase(string label)
        {
            _baseField.SetValueWithoutNotify(label);
            RebuildDraft();
        }

        public void SetName(string name)
        {
            _name.SetValueWithoutNotify(name);
            OnNameChanged();
        }

        public override void Tick(float deltaTime)
        {
            if (_kind != EffectKind.Basic)
            {
                _replayTimer += deltaTime;
                if (_replayTimer >= ReplayInterval)
                    Replay();
            }

            _preview.Tick(deltaTime);
        }

        public override void Dispose()
        {
            _preview.Dispose();
            EffectLibrary.DestroyPreviewDatabase(_previewDatabase);
            _previewDatabase = null;
            DestroyDraft();
        }

        protected override bool Save()
        {
            string name = _name.value?.Trim();
            try
            {
                SavedAsset = EffectLibrary.Save(_kind, name, _draft);
            }
            catch (ArgumentException error)
            {
                SetError(error.Message);
                return false;
            }
            catch (InvalidOperationException error)
            {
                SetError(error.Message);
                return false;
            }

            Saved?.Invoke(EffectLibrary.TagOf(_kind, name));
            return true;
        }

        private void RebuildDraft()
        {
            DestroyDraft();

            int index = _baseField.index;
            if (index < 0 || index >= _bases.Count)
                return;

            _draft = EffectLibrary.CreateDraft(_bases[index]);
            _draftEditor = UnityEditor.Editor.CreateEditor(_draft);
            _inspector.Add(new InspectorElement(_draftEditor));
            RebuildPreviewDatabase();
            Validate();
        }

        private void OnNameChanged()
        {
            Validate();
            if (PreviewName() != _previewName)
                RebuildPreviewDatabase();
        }

        private void Validate() => SetError(_draft == null ? "Нет основы" : EffectLibrary.ValidateName(_kind, _name.value?.Trim()));

        private string PreviewName()
        {
            string name = _name.value?.Trim();
            return EffectLibrary.ValidateName(_kind, name) == null ? name : DraftName;
        }

        private void RebuildPreviewDatabase()
        {
            _previewName = PreviewName();
            TMPAnimationDatabase previous = _previewDatabase;
            _previewDatabase = EffectLibrary.CreatePreviewDatabase(_kind, _previewName, _draft);
            _preview.ExtraEffectTag = _kind == EffectKind.Basic ? _previewName : null;
            _preview.OverrideDatabase(_previewDatabase);
            EffectLibrary.DestroyPreviewDatabase(previous);
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            string tag = EffectLibrary.TagOf(_kind, _previewName);
            PreviewMarkup = _range.IsEmpty
                ? $"<{tag}>{Sample}</{tag}>"
                : MarkupEdits.Wrap(_markup, _range, $"<{tag}>", $"</{tag}>", out _);

            TypecraftSettings settings = Typecraft.Settings;
            FontVariant body = settings != null && settings.Fonts != null ? settings.Fonts.Resolve(settings.DefaultFontRole, _locale) : null;
            _preview.Configure(null, body?.Font, null);
            _preview.SetMarkup(PreviewMarkup, _locale, _device, null);
            Replay();
        }

        private void Replay()
        {
            _replayTimer = 0f;
            if (_kind != EffectKind.Basic)
                _preview.ReplayVisibility(_kind == EffectKind.Show);
        }

        private void DestroyDraft()
        {
            _inspector.Clear();
            if (_draftEditor != null)
                UnityEngine.Object.DestroyImmediate(_draftEditor);
            _draftEditor = null;

            if (_draft != null)
                UnityEngine.Object.DestroyImmediate(_draft);
            _draft = null;
        }
    }
}
#endif
