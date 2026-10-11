using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Velsi.Typecraft.Editor.Welcome
{
    internal sealed class TypecraftWelcomeWindow : EditorWindow
    {
        private const string Prefix = "Velsi.Typecraft.Welcome.";
        private const string LanguageKey = Prefix + "Language";
        private const string ShowOnStartKey = Prefix + "ShowOnStart";
        private const string SessionShownKey = Prefix + "ShownThisSession";
        private const string StylePath = "Packages/com.velsi.typecraft/Editor.Welcome/TypecraftWelcome.uss";

        private static string SeenVersionKey => Prefix + "SeenVersion." + PlayerSettings.productGUID;

        private static bool Russian
        {
            get => EditorPrefs.GetString(LanguageKey, DefaultLanguage) == "ru";
            set => EditorPrefs.SetString(LanguageKey, value ? "ru" : "en");
        }

        private static string DefaultLanguage =>
            Application.systemLanguage == SystemLanguage.Russian || Application.systemLanguage == SystemLanguage.Ukrainian ||
            Application.systemLanguage == SystemLanguage.Belarusian ? "ru" : "en";

        [MenuItem("Tools/Typecraft/Welcome")]
        [MenuItem("Help/Typecraft Welcome")]
        public static void Open()
        {
            var window = GetWindow<TypecraftWelcomeWindow>(true, "Typecraft", true);
            window.minSize = new Vector2(380, 420);
            if (window.position.width < 560)
            {
                Rect main = EditorGUIUtility.GetMainWindowPosition();
                var size = new Vector2(640, Mathf.Min(720, main.height - 80));
                window.position = new Rect(main.center - size / 2, size);
            }
        }

        public static void ShowIfNeeded(bool installedSomething)
        {
            string version = TypecraftDependencies.Version;
            bool newVersion = EditorPrefs.GetString(SeenVersionKey, "") != version;
            bool onStart = EditorPrefs.GetBool(ShowOnStartKey, false) && !SessionState.GetBool(SessionShownKey, false);

            if (installedSomething || newVersion || onStart)
            {
                SessionState.SetBool(SessionShownKey, true);
                EditorPrefs.SetString(SeenVersionKey, version);
                Open();
            }
        }

        private static string T(string ru, string en) => Russian ? ru : en;

        private void OnEnable()
        {
            TypecraftDependencies.Changed += Rebuild;
            Events.registeredPackages += OnPackagesChanged;
        }

        private void OnDisable()
        {
            TypecraftDependencies.Changed -= Rebuild;
            Events.registeredPackages -= OnPackagesChanged;
        }

        private void OnFocus() => Rebuild();

        private void OnPackagesChanged(PackageRegistrationEventArgs _) => Rebuild();

        private void OnProjectChange() => Rebuild();

        private void CreateGUI() => Rebuild();

        private void Rebuild()
        {
            VisualElement root = rootVisualElement;
            if (root == null)
                return;

            root.Clear();
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (style != null && !root.styleSheets.Contains(style))
                root.styleSheets.Add(style);
            root.AddToClassList("tcw-root");

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("tcw-scroll");
            root.Add(scroll);

            scroll.Add(Header());
            AddReport(scroll);

            Section(scroll, T("Обязательно для работы", "Required"));
            scroll.Add(Row(TypecraftDependencies.HasTmpEssentials, "TMP Essential Resources",
                T("Шейдеры, шрифт по умолчанию и настройки TextMeshPro.", "TextMeshPro shaders, default font and settings."),
                T("Импортировать", "Import"), TypecraftDependencies.ImportTmpEssentials));
            scroll.Add(Row(TypecraftDependencies.HasTmpEffects, $"TMPEffects {TypecraftDependencies.TmpEffectsVersion}",
                T("Анимации текста и команды печати. Ставится из копии внутри пакета, интернет не нужен. Лицензия MIT.",
                    "Text animations and typewriter commands. Installed from the copy bundled with the package, no internet needed. MIT license."),
                T("Установить", "Install"), TypecraftDependencies.InstallTmpEffects, Busy("TMPEffects")));
            scroll.Add(Row(TypecraftDependencies.HasTmpEffectsResources, "TMPEffects Required Resources",
                T("Базы встроенных анимаций и команд TMPEffects.", "Built-in TMPEffects animation and command databases."),
                T("Импортировать", "Import"), TypecraftDependencies.ImportTmpEffectsResources, !TypecraftDependencies.HasTmpEffects));

            Section(scroll, T("Дополнительно", "Optional"));
            scroll.Add(Row(TypecraftDependencies.HasInputSystem, "Input System",
                T("Подсказки ввода {key:Action} по текущим биндингам и смена устройства на лету. Без него — только иконки.",
                    "Input prompts {key:Action} from current bindings and live device switching. Without it — icons only."),
                T("Установить", "Install"), () => TypecraftDependencies.Install(TypecraftDependencies.InputSystemPackage, "Input System"),
                Busy("Input System")));
            scroll.Add(Row(TypecraftDependencies.HasLocalization, "Localization",
                T("Unity Localization как источник строк вместо встроенных таблиц Typecraft.",
                    "Unity Localization as the string source instead of the built-in Typecraft tables."),
                T("Установить", "Install"), () => TypecraftDependencies.Install(TypecraftDependencies.LocalizationPackage, "Localization"),
                Busy("Localization")));

            Section(scroll, T("Проект", "Project"));
            scroll.Add(Row(TypecraftDependencies.HasSettings, T("Настройки Typecraft", "Typecraft settings"),
                T("Библиотека стилей, шрифты по ролям, подсказки ввода и таблица строк в Assets/Typecraft.",
                    "Style library, font roles, input prompts and a string table in Assets/Typecraft."),
                T("Создать", "Create"), () => Menu("Tools/Typecraft/Create Project Settings"), !TypecraftDependencies.HasTmpEssentials));

            Section(scroll, T("Быстрый старт", "Quick start"));
            scroll.Add(Step("1", T("Окно Typecraft", "Typecraft window"),
                T("Window → Typecraft (Ctrl+Alt+T): редактор текста, локализация, библиотека, шрифты, диалоги.",
                    "Window → Typecraft (Ctrl+Alt+T): text editor, localization, library, fonts, dialogues."),
                T("Открыть", "Open"), () => Menu("Window/Typecraft")));
            scroll.Add(Step("2", T("Текст в сцене", "Text in a scene"),
                T("Выдели объект с TextMeshPro и добавь Typecraft Text: разметка [style], {icon}, {key:Action} и ключи локализации.",
                    "Select a TextMeshPro object and add Typecraft Text: [style], {icon}, {key:Action} markup and localization keys."),
                T("Добавить к выделенному", "Add to selection"), AddTypecraftText));
            scroll.Add(Step("3", T("Диалог", "Dialogue"),
                T("Assets → Create → Typecraft → Dialogue, затем GameObject → Typecraft → Dialogue Box. Ноды правятся во вкладке «Диалоги».",
                    "Assets → Create → Typecraft → Dialogue, then GameObject → Typecraft → Dialogue Box. Edit nodes in the Dialogues tab."),
                T("Окно диалога в сцену", "Dialogue box to scene"), () => Menu("GameObject/Typecraft/Dialogue Box")));
            scroll.Add(Step("4", T("Демо", "Demo"),
                T("Сэмпл Showcase: стили, иконки, эффекты, таблица на трёх языках и ветвящийся диалог.",
                    "Showcase sample: styles, icons, effects, a three-language table and a branching dialogue."),
                T("Импортировать Showcase", "Import Showcase"), () => ImportSample("Showcase")));

            Section(scroll, T("Документация", "Documentation"));
            var docs = new VisualElement();
            docs.AddToClassList("tcw-buttons");
            docs.Add(DocButton(T("Руководство", "Manual"), Russian ? "Manual.ru.md" : "Manual.en.md"));
            docs.Add(DocButton("API", Russian ? "API.ru.md" : "API.en.md"));
            docs.Add(DocButton(T("Правила арта", "Art rules"), Russian ? "ART_RULES.ru.md" : "ART_RULES.en.md"));
            docs.Add(new Button(() => EditorUtility.RevealInFinder(Path.Combine(TypecraftDependencies.PackageRoot ?? "", "Documentation~", "Images")))
                { text = T("Папка документации", "Docs folder") });
            scroll.Add(docs);

            var footer = new VisualElement();
            footer.AddToClassList("tcw-footer");
            var showOnStart = new Toggle(T("Показывать при каждом запуске Unity", "Show every time Unity starts"))
                { value = EditorPrefs.GetBool(ShowOnStartKey, false) };
            showOnStart.RegisterValueChangedCallback(e => EditorPrefs.SetBool(ShowOnStartKey, e.newValue));
            footer.Add(showOnStart);
            footer.Add(new Label(T("Открыть снова: Help → Typecraft Welcome", "Open again: Help → Typecraft Welcome")) { name = "hint" });
            root.Add(footer);
        }

        private VisualElement Header()
        {
            var header = new VisualElement();
            header.AddToClassList("tcw-header");

            var title = new VisualElement();
            title.AddToClassList("tcw-header__title");
            title.Add(new Label("Typecraft") { name = "name" });
            title.Add(new Label(TypecraftDependencies.Version) { name = "version" });
            header.Add(title);

            var language = new VisualElement();
            language.AddToClassList("tcw-lang");
            language.Add(LanguageButton("RU", true));
            language.Add(LanguageButton("EN", false));
            header.Add(language);

            var about = new Label(T(
                "Инструмент для текста на TextMeshPro: короткая разметка, стили и иконки, подсказки ввода, шрифты по языкам, локализация и диалоги.",
                "A text toolkit for TextMeshPro: short markup, styles and icons, input prompts, per-language fonts, localization and dialogues."));
            about.AddToClassList("tcw-about");
            header.Add(about);
            return header;
        }

        private Button LanguageButton(string text, bool russian)
        {
            var button = new Button(() =>
            {
                Russian = russian;
                Rebuild();
            }) { text = text };
            if (Russian == russian)
                button.AddToClassList("tcw-lang--on");
            return button;
        }

        private void AddReport(VisualElement parent)
        {
            var installed = TypecraftBootstrap.Report;
            if (installed.Count > 0)
            {
                var box = new Label(T("Установлено автоматически: ", "Installed automatically: ") + string.Join(", ", installed));
                box.AddToClassList("tcw-note");
                parent.Add(box);
            }

            if (TypecraftDependencies.Busy)
            {
                var box = new Label(T("Устанавливается: ", "Installing: ") + TypecraftDependencies.Installing + "…");
                box.AddToClassList("tcw-note");
                parent.Add(box);
            }

            if (!string.IsNullOrEmpty(TypecraftDependencies.LastError))
            {
                var box = new Label(T("Ошибка установки: ", "Install failed: ") + TypecraftDependencies.LastError);
                box.AddToClassList("tcw-note");
                box.AddToClassList("tcw-note--error");
                parent.Add(box);
            }
        }

        private static void Section(VisualElement parent, string text)
        {
            var label = new Label(text);
            label.AddToClassList("tcw-section");
            parent.Add(label);
        }

        private static VisualElement Row(bool done, string title, string description, string action, Action onClick, bool disabled = false)
        {
            var row = Item(done ? "✓" : "✗", title, description);
            row.AddToClassList(done ? "tcw-row--done" : "tcw-row--missing");
            if (!done)
            {
                var button = new Button(onClick) { text = action };
                button.SetEnabled(!disabled && !TypecraftDependencies.Busy);
                row.Add(button);
            }

            return row;
        }

        private static VisualElement Step(string number, string title, string description, string action, Action onClick)
        {
            var row = Item(number, title, description);
            row.AddToClassList("tcw-row--step");
            row.Add(new Button(onClick) { text = action });
            return row;
        }

        private static VisualElement Item(string mark, string title, string description)
        {
            var row = new VisualElement();
            row.AddToClassList("tcw-row");
            var badge = new Label(mark);
            badge.AddToClassList("tcw-row__mark");
            row.Add(badge);

            var text = new VisualElement();
            text.AddToClassList("tcw-row__text");
            var name = new Label(title);
            name.AddToClassList("tcw-row__title");
            text.Add(name);
            var info = new Label(description);
            info.AddToClassList("tcw-row__info");
            text.Add(info);
            row.Add(text);
            return row;
        }

        private static bool Busy(string name) => TypecraftDependencies.Busy && TypecraftDependencies.Installing == name;

        private static Button DocButton(string text, string file) =>
            new Button(() => OpenDocument(file)) { text = text };

        private static void OpenDocument(string file)
        {
            string path = Path.Combine(TypecraftDependencies.PackageRoot ?? "", "Documentation~", file);
            if (File.Exists(path))
                EditorUtility.OpenWithDefaultApp(path);
            else
                Debug.LogWarning($"[Typecraft] Documentation not found: {path}");
        }

        private static void Menu(string path)
        {
            if (!EditorApplication.ExecuteMenuItem(path))
                Debug.LogWarning($"[Typecraft] Menu item not available yet: {path}");
        }

        private static void AddTypecraftText()
        {
            Type type = Type.GetType("Velsi.Typecraft.Text.TypecraftText, Velsi.Typecraft.Runtime");
            GameObject target = Selection.activeGameObject;
            if (type == null || target == null || target.GetComponent<TMP_Text>() == null)
            {
                EditorUtility.DisplayDialog("Typecraft",
                    T("Выдели в сцене объект с компонентом TextMeshPro.", "Select a scene object with a TextMeshPro component."), "OK");
                return;
            }

            if (target.GetComponent(type) == null)
                Undo.AddComponent(target, type);
            EditorGUIUtility.PingObject(target);
        }

        private static void ImportSample(string displayName)
        {
            Sample sample = Sample.FindByPackage(TypecraftDependencies.PackageName, TypecraftDependencies.Version)
                .FirstOrDefault(s => s.displayName == displayName);
            if (string.IsNullOrEmpty(sample.displayName))
            {
                Debug.LogWarning($"[Typecraft] Sample not found: {displayName}");
                return;
            }

            if (!sample.isImported)
                sample.Import();

            string assetPath = "Assets" + sample.importPath.Substring(Application.dataPath.Length).Replace('\\', '/');
            AssetDatabase.Refresh();
            var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (folder != null)
            {
                Selection.activeObject = folder;
                EditorGUIUtility.PingObject(folder);
            }
        }
    }
}
