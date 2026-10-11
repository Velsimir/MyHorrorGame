using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Window.Layout;
using Velsi.Typecraft.Editor.Window.Targets;

namespace Velsi.Typecraft.Editor.Window
{
    public sealed class TypecraftWindow : EditorWindow
    {
        internal const string StylePath = "Packages/com.velsi.typecraft/Editor/Window/TypecraftWindow.uss";
        private const float FrameTime = 1f / 30f;

        private static readonly string[] Tabs = { "✎ Редактор", "⇄ Локализация", "◆ Библиотека", "F Шрифты" };

        private EditorTab _editor;
        private LocalizationTab _localization;
        private bool _localizationStale;
        private LibraryTab _library;
        private FontsTab _fonts;
        private bool _fontsStale;
        private readonly List<ITypecraftTab> _extraTabs = new List<ITypecraftTab>();
        private Action _pending;
        private VisualElement _tabBar;
        private VisualElement _body;
        private double _lastTick;
        private int _activeTab;

        [MenuItem("Window/Typecraft %&t")]
        [MenuItem("Tools/Typecraft/Open Window")]
        public static TypecraftWindow Open()
        {
            var window = GetWindow<TypecraftWindow>();
            window.titleContent = new GUIContent("Typecraft");
            window.minSize = new Vector2(420f, 360f);
            return window;
        }

        public static void Edit(ITextEditTarget target)
        {
            TypecraftWindow window = Open();
            window.ShowTab(0);
            window._editor?.Pin(target);
        }

        public static void ShowTab<T>(Action<T> then = null) where T : class, ITypecraftTab
        {
            TypecraftWindow window = Open();
            if (window._body != null)
            {
                window.ShowExtraTab(then);
                return;
            }

            window._pending = () => window.ShowExtraTab(then);
        }

        private void ShowExtraTab<T>(Action<T> then) where T : class, ITypecraftTab
        {
            int index = _extraTabs.FindIndex(tab => tab is T);
            if (index < 0)
                return;

            ShowTab(Tabs.Length + index);
            then?.Invoke((T)_extraTabs[index]);
        }

        private void CreateGUI()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);

            rootVisualElement.AddToClassList("tc-root");
            TypecraftLayout.TruncatedTooltips(rootVisualElement);

            _tabBar = new VisualElement();
            _tabBar.AddToClassList("tc-tabs");
            rootVisualElement.Add(_tabBar);

            CreateExtraTabs();
            for (int i = 0; i < Tabs.Length + _extraTabs.Count; i++)
            {
                int index = i;
                var button = new Button(() => ShowTab(index)) { text = i < Tabs.Length ? Tabs[i] : _extraTabs[i - Tabs.Length].Title };
                button.AddToClassList("tc-tabs__tab");
                _tabBar.Add(button);
            }

            _body = new VisualElement();
            _body.AddToClassList("tc-body");
            rootVisualElement.Add(_body);

            _editor = new EditorTab();
            _editor.FollowSelection();
            _localization = new LocalizationTab();
            _library = new LibraryTab();
            _fonts = new FontsTab();
            ShowTab(0);

            Action pending = _pending;
            _pending = null;
            pending?.Invoke();
        }

        private void CreateExtraTabs()
        {
            DisposeExtraTabs();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<ITypecraftTab>())
            {
                if (type.IsAbstract || type.IsInterface || type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                try
                {
                    _extraTabs.Add((ITypecraftTab)Activator.CreateInstance(type));
                }
                catch (Exception exception)
                {
                    TypecraftLog.Error($"Вкладка {type.Name} не создана: {exception}");
                }
            }

            _extraTabs.Sort((left, right) => left.Order.CompareTo(right.Order));
        }

        private void DisposeExtraTabs()
        {
            foreach (ITypecraftTab tab in _extraTabs)
                tab.Dispose();
            _extraTabs.Clear();
        }

        private void ShowTab(int index)
        {
            if (_body == null)
                return;

            _activeTab = index;
            _body.Clear();

            for (int i = 0; i < _tabBar.childCount; i++)
                _tabBar[i].EnableInClassList("tc-tabs__tab--on", i == index);

            if (index == 0)
            {
                _editor.RefreshAll();
                _body.Add(_editor);
                return;
            }

            if (index == 1)
            {
                if (_localizationStale)
                    _localization.Reload();
                _localizationStale = false;
                _body.Add(_localization);
                return;
            }

            if (index == 2)
            {
                _library.Reload();
                _body.Add(_library);
                return;
            }

            if (index >= Tabs.Length)
            {
                ITypecraftTab tab = _extraTabs[index - Tabs.Length];
                tab.OnShow();
                _body.Add(tab.Element);
                return;
            }

            if (_fontsStale)
                _fonts.Reload();
            _fontsStale = false;
            _body.Add(_fonts);
        }

        private void OnEnable()
        {
            _lastTick = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            Selection.selectionChanged += OnSelectionChanged;
            Typecraft.Changed += OnTypecraftChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            Selection.selectionChanged -= OnSelectionChanged;
            Typecraft.Changed -= OnTypecraftChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            _editor?.Dispose();
            _editor = null;
            _localization?.Dispose();
            _localization = null;
            _library?.Dispose();
            _library = null;
            _fonts = null;
            DisposeExtraTabs();
        }

        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(now - _lastTick);
            if (deltaTime < FrameTime || _editor == null)
                return;

            _lastTick = now;
            bool pending = _editor.HasPendingWordChange;
            if (hasUnsavedChanges != pending)
            {
                saveChangesMessage = "Во вкладке «Редактор» изменены слова строки локализации, и они ещё не записаны.\n\n«Сохранить» откроет сравнение «было / стало» с выбором: записать в таблицу или вернуть слова.";
                hasUnsavedChanges = pending;
            }

            if (_activeTab == 0)
                _editor.Tick(Mathf.Min(deltaTime, 0.1f));
            else if (_activeTab == 1)
                _localization?.Tick(Mathf.Min(deltaTime, 0.1f));
            else if (_activeTab == 2)
                _library?.Tick(Mathf.Min(deltaTime, 0.1f));
            else if (_activeTab >= Tabs.Length && _activeTab - Tabs.Length < _extraTabs.Count)
                _extraTabs[_activeTab - Tabs.Length].Tick(Mathf.Min(deltaTime, 0.1f));
        }

        public override void SaveChanges()
        {
            if (_editor == null || _editor.ResolvePendingWordChange())
                base.SaveChanges();
        }

        private void OnSelectionChanged()
        {
            _editor?.FollowSelection();
            _library?.OnSelectionChanged();
        }

        private void OnTypecraftChanged()
        {
            if (_activeTab == 0)
                _editor?.RefreshAll();
            if (_activeTab == 1)
                _localization?.Reload();
            else
                _localizationStale = true;
            if (_activeTab != 3)
                _fontsStale = true;
            foreach (ITypecraftTab tab in _extraTabs)
                tab.OnTypecraftChanged();
        }

        private void OnPlayModeChanged(PlayModeStateChange change) => _editor?.RefreshAll();
    }
}
