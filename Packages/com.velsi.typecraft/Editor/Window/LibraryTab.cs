using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Art;
using Velsi.Typecraft.Editor.Fonts;
using Velsi.Typecraft.Editor.Library;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Editor.Window.Dialogs;
using Velsi.Typecraft.Editor.Window.Effects;
using Velsi.Typecraft.Editor.Window.Markup;
using Velsi.Typecraft.Fonts;
using Velsi.Typecraft.Library;
using Velsi.Typecraft.Styles;

namespace Velsi.Typecraft.Editor.Window
{
    public sealed class LibraryTab : VisualElement, IDisposable
    {
        private readonly ScrollView _list;
        private readonly VisualElement _detail;
        private readonly Button _fromText;
        private readonly FormOverlay _overlay = new FormOverlay();

        private IconSet _set;
        private string _styleId;
        private StylePanel _stylePanel;
        private IconSetPanel _iconPanel;
        private string _presetKey;
        private string _effectKey;

        public LibraryTab()
        {
            AddToClassList("tc-lib");

            var toolbar = new VisualElement();
            toolbar.AddToClassList("tc-toolbar");
            Add(toolbar);

            var title = new Label("Библиотека");
            title.AddToClassList("tc-toolbar__target");
            toolbar.Add(title);
            toolbar.Add(Spacer());
            toolbar.Add(new Button(CreateIconSet) { text = "+ набор иконок" });
            toolbar.Add(new Button(CreateStyle) { text = "+ стиль" });
            toolbar.Add(new Button(CreateMaterial) { text = "+ материал", tooltip = "Пресет SDF-материала для роли шрифта" });
            toolbar.Add(new Button(CreateEffect) { text = "+ эффект", tooltip = "Своя анимация TMPEffects в базе анимаций проекта" });
            _fromText = new Button(CreateStyleFromText) { text = "Стиль из TMP_Text", tooltip = "Создать стиль по выделенному в сцене TMP_Text: шрифт, цвет, начертание, регистр, интервал" };
            toolbar.Add(_fromText);

            var body = new VisualElement();
            body.AddToClassList("tc-lib__body");
            Add(body);

            _list = new ScrollView();
            _list.AddToClassList("tc-lib__list");
            body.Add(_list);

            _detail = new VisualElement();
            _detail.AddToClassList("tc-lib__detail");
            body.Add(_detail);
            Add(_overlay);

            Reload();
        }

        public void Reload()
        {
            _list.Clear();
            TypecraftSettings settings = Typecraft.Settings;
            if (settings == null)
            {
                _list.Add(new Label("Нет TypecraftSettings — Tools/Typecraft/Create Project Settings."));
                ShowDetail();
                return;
            }

            AddSection("Наборы иконок");
            foreach (IconSet set in MarkupCandidates.AllIconSets())
            {
                IconSet captured = set;
                AddItem($"{{ }} {set.SetId}", $"{set.Entries.Count}", set == _set, () => SelectSet(captured));
            }

            AddSection("Стили");
            if (settings.Styles == null)
                _list.Add(new Label("Нет библиотеки стилей") { name = "tc-lib__empty" });
            else
            {
                foreach (TextStyle style in settings.Styles.Styles.Where(style => style != null && !string.IsNullOrEmpty(style.Id)))
                {
                    string id = style.Id;
                    AddItem($"[ ] {id}", style.FontRole, _set == null && id == _styleId, () => SelectStyle(id));
                }
            }

            AddSection("Пресеты материалов");
            if (settings.Fonts != null)
            {
                foreach (FontRole role in settings.Fonts.Roles.Where(role => role != null && !string.IsNullOrEmpty(role.Id)))
                {
                    foreach (string preset in MaterialPresets.PresetIds(settings.Fonts, role.Id))
                    {
                        string key = PresetKey(role.Id, preset);
                        AddItem($"◐ {preset}", role.Id, key == _presetKey, () => SelectPreset(key));
                    }
                }
            }

#if TYPECRAFT_TMPEFFECTS
            AddSection("Свои эффекты");
            foreach (EffectEntry effect in EffectCatalog.Entries.Where(entry => entry.Kind != EffectKind.Command && EffectLibrary.IsCustom(entry.Kind, entry.Name)))
            {
                string key = EffectKey(effect.Kind, effect.Name);
                AddItem($"✦ <{effect.TagName}>", effect.Kind.ToString(), key == _effectKey, () => SelectEffect(key));
            }
#endif

            ShowDetail();
        }

        public void OnSelectionChanged() => _fromText.SetEnabled(SelectedText() != null);

        public void Tick(float deltaTime)
        {
            _stylePanel?.Tick(deltaTime);
            _overlay.Tick(deltaTime);
        }

        public FormOverlay Overlay => _overlay;

        public void Dispose()
        {
            _overlay.Close();
            _stylePanel?.Dispose();
            _iconPanel?.Dispose();
        }

        private void SelectSet(IconSet set)
        {
            Deselect();
            _set = set;
            Reload();
        }

        private void SelectStyle(string id)
        {
            Deselect();
            _styleId = id;
            Reload();
        }

        private void SelectPreset(string key)
        {
            Deselect();
            _presetKey = key;
            Reload();
        }

        private void SelectEffect(string key)
        {
            Deselect();
            _effectKey = key;
            Reload();
        }

        private void Deselect()
        {
            _set = null;
            _styleId = null;
            _presetKey = null;
            _effectKey = null;
        }

        private static string PresetKey(string role, string preset) => role + "\n" + preset;

        private static string EffectKey(EffectKind kind, string name) => (int)kind + "\n" + name;

        private void ShowDetail()
        {
            _stylePanel?.Dispose();
            _stylePanel = null;
            _iconPanel?.Dispose();
            _iconPanel = null;
            _detail.Clear();
            OnSelectionChanged();

            if (_set != null)
            {
                _iconPanel = new IconSetPanel(_set);
                _detail.Add(_iconPanel);
                return;
            }

            if (_presetKey != null && ShowPreset())
                return;

            if (_effectKey != null && ShowEffect())
                return;

            StyleLibrary library = Typecraft.Settings != null ? Typecraft.Settings.Styles : null;
            int index = library != null && _styleId != null ? library.Styles.ToList().FindIndex(style => style != null && style.Id == _styleId) : -1;
            if (index < 0)
            {
                var hint = new Label("Выбери набор иконок или стиль слева.");
                hint.AddToClassList("tc-placeholder");
                _detail.Add(hint);
                return;
            }

            _stylePanel = new StylePanel(library, index);
            _stylePanel.Selected += id => SelectStyle(id);
            _detail.Add(_stylePanel);
        }

        private void AddSection(string title)
        {
            var label = new Label(title);
            label.AddToClassList("tc-lib__section");
            _list.Add(label);
        }

        private void AddItem(string title, string note, bool selected, Action onClick)
        {
            var item = new VisualElement();
            item.AddToClassList("tc-lib__item");
            item.EnableInClassList("tc-lib__item--selected", selected);
            item.Add(new Label(title) { name = "title" });
            item.Add(new Label(note ?? string.Empty) { name = "note" });
            item.RegisterCallback<PointerDownEvent>(_ => onClick());
            _list.Add(item);
        }

        private void CreateIconSet()
        {
            TypecraftSettings settings = Typecraft.Settings;
            if (settings == null)
                return;

            string name = TextPrompt.Show("Новый набор иконок", "Имя набора (латиницей):", "Icons");
            if (string.IsNullOrWhiteSpace(name))
                return;

            name = new string(name.Trim().Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            string parent = EditorUtility.SaveFolderPanel("Где создать папку набора", "Assets", string.Empty);
            if (string.IsNullOrEmpty(parent))
                return;

            string projectRoot = Path.GetFullPath(".").Replace('\\', '/');
            parent = parent.Replace('\\', '/');
            if (!parent.StartsWith(projectRoot + "/Assets"))
            {
                EditorUtility.DisplayDialog("Typecraft", "Папка должна быть внутри Assets.", "OK");
                return;
            }

            string folder = $"{parent.Substring(projectRoot.Length + 1)}/{name}";
            if (folder.Split('/').Contains("Resources"))
            {
                EditorUtility.DisplayDialog("Typecraft", "Не клади набор в Resources: исходные PNG попадут в билд.", "OK");
                return;
            }

            if (AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog("Typecraft", $"Папка {folder} уже существует.", "OK");
                return;
            }

            Directory.CreateDirectory(Path.GetFullPath($"{folder}/Source"));
            AssetDatabase.Refresh();

            var set = ScriptableObject.CreateInstance<IconSet>();
            set.Configure(IconKeyNaming.ToKey(name), null);
            set.BuildSettings.SourceFolder = $"{folder}/Source";
            AssetDatabase.CreateAsset(set, $"{folder}/{name}.asset");

            var serialized = new SerializedObject(settings);
            SerializedProperty sets = serialized.FindProperty("_iconSets");
            sets.arraySize++;
            sets.GetArrayElementAtIndex(sets.arraySize - 1).objectReferenceValue = set;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Typecraft.Refresh();
            SelectSet(set);
        }

        private void CreateStyle()
        {
            StyleLibrary library = Typecraft.Settings != null ? Typecraft.Settings.Styles : null;
            if (library == null)
                return;

            var form = new StyleForm(library, null, default, false, Typecraft.Locale, Typecraft.Device);
            form.Saved += saved => SelectStyle(saved.SavedId);
            _overlay.Show(form);
        }

        private void CreateMaterial()
        {
            FontProfile fonts = Typecraft.Settings != null ? Typecraft.Settings.Fonts : null;
            if (fonts == null)
                return;

            string role = _presetKey != null ? _presetKey.Split('\n')[0] : Typecraft.Settings.DefaultFontRole;
            var form = new MaterialPresetForm(fonts, role, Typecraft.Locale);
            form.Saved += (savedRole, preset) => SelectPreset(PresetKey(savedRole, preset));
            _overlay.Show(form);
        }

        private void CreateEffect()
        {
#if TYPECRAFT_TMPEFFECTS
            var form = new EffectForm(null, default, Typecraft.Locale, Typecraft.Device);
            form.Saved += tag =>
            {
                EffectEntry entry = EffectCatalog.Entries.FirstOrDefault(e => e.TagName == tag);
                SelectEffect(EffectKey(entry.Kind, entry.Name));
            };
            _overlay.Show(form);
#else
            EditorUtility.DisplayDialog("Typecraft", "Нужен пакет TMPEffects.", "OK");
#endif
        }

        private bool ShowPreset()
        {
            FontProfile fonts = Typecraft.Settings != null ? Typecraft.Settings.Fonts : null;
            string[] parts = _presetKey.Split('\n');
            if (fonts == null || parts.Length != 2 || !fonts.TryGetRole(parts[0], out FontRole role))
                return false;

            string presetId = parts[1];
            var panel = new VisualElement();
            panel.AddToClassList("tc-lib__panel");
            var header = new VisualElement();
            header.AddToClassList("tc-lib__header");
            var title = new Label($"Пресет «{presetId}» · роль {role.Id}");
            title.AddToClassList("tc-lib__title");
            header.Add(title);
            header.Add(new Button(() =>
            {
                if (!EditorUtility.DisplayDialog("Typecraft", $"Удалить пресет «{presetId}» роли {role.Id}? Материалы уйдут в корзину.", "Удалить", "Отмена"))
                    return;
                MaterialPresets.Remove(fonts, role.Id, presetId);
                _presetKey = null;
                Reload();
            }) { text = "Удалить" });
            panel.Add(header);

            var hint = new Label("По материалу на каждый шрифт роли: у каждого языка свой атлас. Параметры правятся в инспекторе материала.");
            hint.AddToClassList("tc-lib__hint");
            panel.Add(hint);

            foreach ((string locale, FontVariant variant) in MaterialPresets.Variants(role))
            {
                Material material = variant.FindMaterial(presetId);
                var field = new ObjectField($"{locale ?? "основной"} · {variant.Font.name}") { objectType = typeof(Material), value = material };
                field.SetEnabled(false);
                panel.Add(field);
            }

            _detail.Add(panel);
            return true;
        }

        private bool ShowEffect()
        {
#if TYPECRAFT_TMPEFFECTS
            string[] parts = _effectKey.Split('\n');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int kindIndex))
                return false;

            var kind = (EffectKind)kindIndex;
            string name = parts[1];
            UnityEngine.Object asset = EffectLibrary.AssetOf(kind, name);
            if (asset == null)
                return false;

            var panel = new VisualElement();
            panel.AddToClassList("tc-lib__panel");
            var header = new VisualElement();
            header.AddToClassList("tc-lib__header");
            var title = new Label("<" + EffectLibrary.TagOf(kind, name) + ">") { enableRichText = false };
            title.AddToClassList("tc-lib__title");
            header.Add(title);
            header.Add(new Button(() =>
            {
                if (!EditorUtility.DisplayDialog("Typecraft", $"Удалить эффект {EffectLibrary.TagOf(kind, name)}? Ассет уйдёт в корзину, теги в тексте останутся.", "Удалить", "Отмена"))
                    return;
                EffectLibrary.Remove(kind, name);
                _effectKey = null;
                Reload();
            }) { text = "Удалить" });
            panel.Add(header);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            scroll.Add(new InspectorElement(asset));
            panel.Add(scroll);
            _detail.Add(panel);
            return true;
#else
            return false;
#endif
        }

        private void CreateStyleFromText()
        {
            TypecraftSettings settings = Typecraft.Settings;
            TMP_Text text = SelectedText();
            if (settings == null || settings.Styles == null || text == null)
                return;

            string id = StyleFromText.Create(settings.Styles, settings.Fonts, text);
            if (id != null)
                SelectStyle(id);
        }

        private static TMP_Text SelectedText() => Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<TMP_Text>() : null;

        private static VisualElement Spacer()
        {
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            return spacer;
        }
    }
}
