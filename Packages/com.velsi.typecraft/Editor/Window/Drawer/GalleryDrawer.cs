using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Fonts;
using Velsi.Typecraft.Editor.Window.Effects;
using Velsi.Typecraft.Editor.Window.Markup;
using Velsi.Typecraft.Editor.Window.Preview;
using Velsi.Typecraft.Fonts;
using Velsi.Typecraft.Library;
using Velsi.Typecraft.Styles;

namespace Velsi.Typecraft.Editor.Window.Drawer
{
    public sealed class GalleryDrawer : VisualElement
    {
        private const string TabPrefKey = "Typecraft.Drawer.Tab";
        private static readonly Vector2Int ThumbnailSize = new Vector2Int(240, 96);
        private static readonly string[] TabNames = { "Стили", "Материалы", "Эффекты", "Иконки", "Шрифты" };

        private readonly ThumbnailRenderer _thumbnails = new ThumbnailRenderer();
        private readonly VisualElement _tabs;
        private readonly ScrollView _content;
        private readonly TextField _iconFilter;
        private Func<string, ModuleCoverage> _coverage;
        private int _tab;
        private string _locale;
        private DeviceFamily _device;

        public GalleryDrawer()
        {
            AddToClassList("tc-drawer");

            _tabs = new VisualElement();
            _tabs.AddToClassList("tc-drawer__tabs");
            Add(_tabs);

            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i;
                var tab = new Label(TabNames[i]);
                tab.AddToClassList("tc-drawer__tab");
                tab.RegisterCallback<PointerDownEvent>(_ => SelectTab(index));
                _tabs.Add(tab);
            }

            _iconFilter = new TextField { value = string.Empty };
            _iconFilter.AddToClassList("tc-drawer__filter");
            _iconFilter.RegisterValueChangedCallback(_ => Rebuild());
            _tabs.Add(_iconFilter);

            _content = new ScrollView(ScrollViewMode.Horizontal);
            _content.AddToClassList("tc-drawer__content");
            Add(_content);

            _tab = Mathf.Clamp(EditorPrefs.GetInt(TabPrefKey, 0), 0, TabNames.Length - 1);
        }

        public event Action<string, string> WrapRequested;
        public event Action<string> InsertRequested;
        public event Action<string> FontRoleRequested;
        public event Action CreateStyleRequested;
        public event Action<string> CreateMaterialRequested;
        public event Action CreateEffectRequested;

        public void Refresh(string locale, DeviceFamily device)
        {
            _locale = locale;
            _device = device;
            _thumbnails.Clear();
            Rebuild();
        }

        public void Dispose() => _thumbnails.Dispose();

        public void ShowApplied(Func<string, ModuleCoverage> coverage)
        {
            _coverage = coverage;
            foreach (VisualElement card in _content.Children())
            {
                ModuleCoverage state = card.userData is string open && _coverage != null ? _coverage(open) : ModuleCoverage.None;
                card.EnableInClassList("tc-card--applied", state == ModuleCoverage.Full);
                card.EnableInClassList("tc-card--partial", state == ModuleCoverage.Partial);
            }
        }

        private void SelectTab(int index)
        {
            _tab = index;
            EditorPrefs.SetInt(TabPrefKey, index);
            Rebuild();
        }

        private void Rebuild()
        {
            for (int i = 0; i < TabNames.Length; i++)
                _tabs[i].EnableInClassList("tc-drawer__tab--on", i == _tab);

            _iconFilter.style.display = _tab == 3 ? DisplayStyle.Flex : DisplayStyle.None;
            _content.Clear();
            _content.mode = _tab == 3 ? ScrollViewMode.Vertical : ScrollViewMode.Horizontal;
            _content.contentContainer.EnableInClassList("tc-drawer__grid", _tab == 3);

            TypecraftSettings settings = Typecraft.Settings;
            if (settings == null)
            {
                _content.Add(new Label("Нет TypecraftSettings — Tools → Typecraft → Create Project Settings"));
                return;
            }

            switch (_tab)
            {
                case 0: BuildStyles(settings); break;
                case 1: BuildMaterials(settings); break;
                case 2: BuildEffects(); break;
                case 3: BuildIcons(settings); break;
                case 4: BuildFonts(settings); break;
            }

            ShowApplied(_coverage);
        }

        private void BuildStyles(TypecraftSettings settings)
        {
            if (settings.Styles == null)
                return;

            TMP_FontAsset body = BodyFont(settings);
            foreach (TextStyle style in settings.Styles.Styles)
            {
                if (style == null || string.IsNullOrEmpty(style.Id))
                    continue;

                string sample = Typecraft.RenderPreview($"[{style.Id}]{style.DisplayName}[/{style.Id}]", _locale, _device, null, null);
                Texture2D thumbnail = _thumbnails.Render($"style:{style.Id}:{_locale}", sample, body, null, ThumbnailSize);
                string id = style.Id;
                AddCard(thumbnail, id, style.FontRole, () => WrapRequested?.Invoke($"[{id}]", $"[/{id}]"), settings.Styles).userData = $"[{id}]";
            }

            AddActionCard("+ стиль", "создать здесь", () => CreateStyleRequested?.Invoke());
        }

        private void BuildMaterials(TypecraftSettings settings)
        {
            if (settings.Fonts == null)
                return;

            foreach (FontRole role in settings.Fonts.Roles)
            {
                FontVariant variant = role?.Resolve(_locale);
                if (variant?.Font == null)
                    continue;

                foreach (FontMaterialPreset preset in variant.Materials)
                {
                    if (preset?.Material == null)
                        continue;

                    string open = $"<font=\"{variant.Font.name}\" material=\"{preset.Material.name}\">";
                    Texture2D thumbnail = _thumbnails.Render($"mat:{preset.Material.GetInstanceID()}", SampleFor(variant.Font), variant.Font, preset.Material, ThumbnailSize);
                    VisualElement card = AddCard(thumbnail, preset.Id, role.Id, () => WrapRequested?.Invoke(open, "</font>"), preset.Material);
                    card.userData = open;
                    string roleId = role.Id;
                    string presetId = preset.Id;
                    FontProfile fonts = settings.Fonts;
                    card.AddManipulator(new ContextualMenuManipulator(evt =>
                    {
                        evt.menu.AppendAction($"Удалить пресет «{presetId}» роли {roleId}", _ => RemovePreset(fonts, roleId, presetId));
                        evt.menu.AppendAction("+ материал для этой роли", _ => CreateMaterialRequested?.Invoke(roleId));
                    }));
                }
            }

            AddActionCard("+ материал", "пресет SDF-материала", () => CreateMaterialRequested?.Invoke(null));
        }

        private void BuildEffects()
        {
            foreach (EffectEntry effect in EffectCatalog.Entries)
            {
                string kind = effect.Kind switch
                {
                    EffectKind.Show => "появление",
                    EffectKind.Hide => "исчезание",
                    EffectKind.Command => "команда",
                    _ => "постоянный"
                };

                EffectEntry captured = effect;
                bool custom = IsCustomEffect(effect);
                var card = CreateCard(null, effect.TagName, custom ? "свой · " + kind : kind);
                card.AddToClassList($"tc-card--{effect.Kind.ToString().ToLowerInvariant()}");
                if (effect.Wraps)
                    card.userData = effect.Open;
                if (custom)
                    card.AddManipulator(new ContextualMenuManipulator(evt => evt.menu.AppendAction($"Удалить эффект {captured.TagName}", _ => RemoveEffect(captured))));
                card.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0)
                        return;

                    if (captured.Wraps)
                        WrapRequested?.Invoke(captured.Open, captured.Close);
                    else
                        InsertRequested?.Invoke(captured.Open);
                });
                _content.Add(card);
            }

            if (EffectCatalog.Entries.Count == 0)
                _content.Add(new Label("TMPEffects не найден или не импортированы его ресурсы"));
            else if (EffectLibrary.Available)
                AddActionCard("+ эффект", "своя анимация TMPEffects", () => CreateEffectRequested?.Invoke());
        }

        private void BuildIcons(TypecraftSettings settings)
        {
            string filter = _iconFilter.value?.Trim() ?? string.Empty;
            var sets = new List<IconSet>(settings.IconSets.Where(s => s != null));
            if (settings.InputGlyphs != null)
                sets.AddRange(settings.InputGlyphs.IconSets.Where(s => s != null && !sets.Contains(s)));

            int shown = 0;
            foreach (IconSet set in sets)
            {
                foreach (IconEntry entry in set.Entries)
                {
                    string reference = $"{set.SetId}/{entry.Key}";
                    if (filter.Length > 0 && reference.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    if (++shown > 400)
                        return;

                    var tile = new VisualElement { tooltip = reference };
                    tile.AddToClassList("tc-icon");
                    Sprite sprite = FindSprite(set, entry);
                    if (sprite != null)
                        tile.style.backgroundImage = new StyleBackground(sprite);
                    else
                        tile.Add(new Label("?") { name = "missing" });

                    var caption = new Label(entry.Key);
                    caption.AddToClassList("tc-icon__caption");
                    tile.Add(caption);
                    tile.RegisterCallback<PointerDownEvent>(_ => InsertRequested?.Invoke($"{{{reference}}}"));
                    _content.Add(tile);
                }
            }
        }

        private void BuildFonts(TypecraftSettings settings)
        {
            if (settings.Fonts == null)
                return;

            foreach (FontRole role in settings.Fonts.Roles)
            {
                FontVariant variant = role?.Resolve(_locale);
                if (variant == null)
                    continue;

                Texture2D thumbnail = _thumbnails.Render($"font:{role.Id}:{_locale}", "Аа Bb 字", variant.Font, null, ThumbnailSize);
                string roleId = role.Id;
                AddCard(thumbnail, roleId, variant.Font != null ? variant.Font.name : "нет шрифта", () => FontRoleRequested?.Invoke(roleId), settings.Fonts);
            }

            AddPingCard("+ роль", settings.Fonts);
        }

        private VisualElement AddCard(Texture2D thumbnail, string title, string subtitle, Action onClick, UnityEngine.Object source)
        {
            VisualElement card = CreateCard(thumbnail, title, subtitle);
            card.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;

                if (evt.clickCount >= 2 && source != null)
                {
                    Selection.activeObject = source;
                    EditorGUIUtility.PingObject(source);
                    return;
                }

                onClick();
            });
            _content.Add(card);
            return card;
        }

        private void AddPingCard(string title, UnityEngine.Object source)
        {
            VisualElement card = CreateCard(null, title, "открыть в инспекторе");
            card.AddToClassList("tc-card--add");
            card.RegisterCallback<PointerDownEvent>(_ =>
            {
                Selection.activeObject = source;
                EditorGUIUtility.PingObject(source);
            });
            _content.Add(card);
        }

        private void AddActionCard(string title, string subtitle, Action action)
        {
            VisualElement card = CreateCard(null, title, subtitle);
            card.AddToClassList("tc-card--add");
            card.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0)
                    action();
            });
            _content.Add(card);
        }

        private static bool IsCustomEffect(EffectEntry effect)
        {
#if TYPECRAFT_TMPEFFECTS
            return effect.Kind != EffectKind.Command && EffectLibrary.IsCustom(effect.Kind, effect.Name);
#else
            return false;
#endif
        }

        private static void RemoveEffect(EffectEntry effect)
        {
#if TYPECRAFT_TMPEFFECTS
            if (EditorUtility.DisplayDialog("Typecraft", $"Удалить эффект {effect.TagName}? Ассет уйдёт в корзину, теги в тексте останутся и перестанут работать.", "Удалить", "Отмена"))
                EffectLibrary.Remove(effect.Kind, effect.Name);
#endif
        }

        private static void RemovePreset(FontProfile fonts, string roleId, string presetId)
        {
            StyleLibrary styles = Typecraft.Settings != null ? Typecraft.Settings.Styles : null;
            int users = styles != null ? styles.Styles.Count(style => style != null && style.FontRole == roleId && style.MaterialPreset == presetId) : 0;
            string note = users > 0 ? $"\n\nЕго используют стили: {users} — они вернутся к основному материалу шрифта." : string.Empty;
            if (EditorUtility.DisplayDialog("Typecraft", $"Удалить пресет «{presetId}» роли {roleId}? Материалы уйдут в корзину.{note}", "Удалить", "Отмена"))
                MaterialPresets.Remove(fonts, roleId, presetId);
        }

        internal static string SampleFor(TMP_FontAsset font)
        {
            const string sample = "Аа Подвал";
            if (font == null || sample.Where(c => c != ' ').All(c => font.HasCharacter(c)))
                return sample;

            string own = new string(font.characterTable.Select(c => c.unicode).Where(code => code < 0x10000 && char.IsLetter((char)code)).OrderByDescending(code => code >= 0x2E80).Take(4).Select(code => (char)code).ToArray());
            return string.IsNullOrEmpty(own) ? sample : own;
        }

        private static VisualElement CreateCard(Texture2D thumbnail, string title, string subtitle)
        {
            var card = new VisualElement();
            card.AddToClassList("tc-card");

            var image = new VisualElement();
            image.AddToClassList("tc-card__image");
            if (thumbnail != null)
                image.style.backgroundImage = new StyleBackground(thumbnail);
            else
                image.Add(new Label(title) { name = "tc-card__big" });
            card.Add(image);

            var footer = new VisualElement();
            footer.AddToClassList("tc-card__footer");
            footer.Add(new Label(title) { name = "tc-card__title" });
            footer.Add(new Label(subtitle ?? string.Empty) { name = "tc-card__subtitle" });
            card.Add(footer);
            return card;
        }

        private static TMP_FontAsset BodyFont(TypecraftSettings settings) =>
            settings.Fonts != null ? settings.Fonts.Resolve(settings.DefaultFontRole, Typecraft.Locale)?.Font : null;

        internal static Sprite FindSprite(IconSet set, IconEntry entry)
        {
            if (entry.IsPlaceholder || set.SpriteAsset == null)
                return null;

            foreach (TMP_SpriteCharacter character in set.SpriteAsset.spriteCharacterTable)
            {
                if (character.name == entry.GlyphName)
                    return (character.glyph as TMP_SpriteGlyph)?.sprite;
            }

            return null;
        }
    }
}
