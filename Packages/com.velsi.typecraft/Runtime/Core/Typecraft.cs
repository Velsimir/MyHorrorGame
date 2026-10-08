using System;
using System.Collections.Generic;
using UnityEngine;
using Velsi.Typecraft.AutoTags;
using Velsi.Typecraft.Library;
using Velsi.Typecraft.Markup;

namespace Velsi.Typecraft
{
    public static class Typecraft
    {
        private const int CacheLimit = 1024;

        private static readonly Dictionary<string, IMarkupTagHandler> Handlers = new Dictionary<string, IMarkupTagHandler>();
        private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>();

        private static TypecraftSettings _settings;
        private static bool _settingsLoaded;
        private static IconLibrary _icons;
        private static AutoTagger _autoTagger;
        private static string _locale;
        private static DeviceFamily? _device;
        private static bool _activated;

        public static event Action Changed;

        public static TypecraftSettings Settings
        {
            get
            {
                if (!_settingsLoaded)
                    Initialize(Resources.Load<TypecraftSettings>(TypecraftSettings.ResourcesPath));
                return _settings;
            }
        }

        public static string Locale => _locale ?? (Settings != null ? Settings.DefaultLocale : "en");

        public static DeviceFamily Device => _device ?? (Settings != null ? Settings.DefaultDevice : DeviceFamily.KeyboardMouse);

        public static IconLibrary Icons
        {
            get
            {
                _ = Settings;
                return _icons ??= BuildIcons();
            }
        }

        public static IReadOnlyDictionary<string, IMarkupTagHandler> TagHandlers
        {
            get
            {
                EnsureBuiltInHandlers();
                return Handlers;
            }
        }

        public static void Initialize(TypecraftSettings settings)
        {
            _settings = settings;
            _settingsLoaded = true;
            _activated = false;
            Rebuild();
        }

        public static void Register(IMarkupTagHandler handler)
        {
            if (handler == null || string.IsNullOrEmpty(handler.Prefix))
                return;

            EnsureBuiltInHandlers();
            Handlers[handler.Prefix] = handler;
            NotifyChanged();
        }

        public static void SetLocale(string locale)
        {
            if (string.IsNullOrEmpty(locale) || locale == _locale)
                return;

            _locale = locale;
            NotifyChanged();
        }

        public static void SetDevice(DeviceFamily device)
        {
            if (_device == device)
                return;

            _device = device;
            NotifyChanged();
        }

        public static void Refresh()
        {
            Rebuild();
            NotifyChanged();
        }

        public static string Render(string markup) => Render(markup, null);

        public static string Render(string markup, List<MarkupDiagnostic> diagnostics)
        {
            if (string.IsNullOrEmpty(markup))
                return markup ?? string.Empty;

            EnsureActivated();

            if (diagnostics == null && Cache.TryGetValue(markup, out string cached))
                return cached;

            string source = _autoTagger != null ? _autoTagger.Apply(markup) : markup;
            var context = new MarkupContext(Settings, Icons, Locale, Device, diagnostics);
            string rendered = MarkupRenderer.Render(source, context, TagHandlers);

            if (Cache.Count >= CacheLimit)
                Cache.Clear();
            Cache[markup] = rendered;

            return rendered;
        }

        public static string RenderPreview(string markup, string locale, DeviceFamily device, List<MarkupDiagnostic> diagnostics, List<int> sourceMap)
        {
            if (diagnostics != null && sourceMap == null && _autoTagger != null)
                sourceMap = new List<int>();

            sourceMap?.Clear();
            if (string.IsNullOrEmpty(markup))
                return markup ?? string.Empty;

            _ = Settings;
            string source = markup;
            List<int> autoMap = null;

            if (_autoTagger != null)
            {
                autoMap = sourceMap != null ? new List<int>(markup.Length + 32) : null;
                source = autoMap != null ? _autoTagger.Apply(markup, autoMap) : _autoTagger.Apply(markup);
            }

            var context = new MarkupContext(_settings, Icons, locale ?? Locale, device, diagnostics);
            int firstDiagnostic = diagnostics?.Count ?? 0;
            string rendered = MarkupRenderer.Render(source, context, TagHandlers, sourceMap);

            if (diagnostics != null && autoMap != null)
            {
                for (int i = firstDiagnostic; i < diagnostics.Count; i++)
                {
                    MarkupDiagnostic diagnostic = diagnostics[i];
                    int index = diagnostic.Index < autoMap.Count ? autoMap[diagnostic.Index] : markup.Length;
                    diagnostics[i] = new MarkupDiagnostic(diagnostic.Issue, diagnostic.Value, index);
                }
            }

            if (sourceMap != null && autoMap != null)
            {
                for (int i = 0; i < sourceMap.Count; i++)
                {
                    int index = sourceMap[i];
                    sourceMap[i] = index < autoMap.Count ? autoMap[index] : markup.Length;
                }
            }

            return rendered;
        }

        private static IconLibrary BuildIcons()
        {
            var icons = new IconLibrary(_settings != null ? _settings.IconSets : null);
            if (_settings != null && _settings.InputGlyphs != null)
            {
                foreach (IconSet set in _settings.InputGlyphs.IconSets)
                    icons.Add(set);
            }

            return icons;
        }

        private static void EnsureActivated()
        {
            if (_activated || Settings == null)
                return;

            _activated = true;
            _settings.TextSource?.Activate();
            _settings.InputGlyphs?.Activate();
        }

        private static void Rebuild()
        {
            Cache.Clear();
            _icons = null;
            _autoTagger = _settings != null ? AutoTagger.Build(_settings.AutoTags, _settings.Styles) : null;
            TmpAssetRegistry.Rebuild(_settings);
        }

        private static void NotifyChanged()
        {
            Cache.Clear();
            Changed?.Invoke();
        }

        private static void EnsureBuiltInHandlers()
        {
            if (!Handlers.ContainsKey("key"))
                Handlers["key"] = new KeyTagHandler();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Changed = null;
            Handlers.Clear();
            Cache.Clear();
            _settings = null;
            _settingsLoaded = false;
            _icons = null;
            _autoTagger = null;
            _locale = null;
            _device = null;
            _activated = false;
            TmpAssetRegistry.Reset();
        }
    }
}
