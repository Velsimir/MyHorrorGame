using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Localization
{
    [CreateAssetMenu(fileName = "UnityLocalizationTextSource", menuName = "Typecraft/Text Sources/Unity Localization")]
    public sealed class UnityLocalizationTextSource : ScriptableObject, ITextSource
    {
        [SerializeField] private string _defaultTable;

        private readonly List<string> _locales = new List<string>();
        private bool _subscribed;

        public IReadOnlyList<string> Locales
        {
            get
            {
                _locales.Clear();
                IList<Locale> available = LocalizationSettings.HasSettings ? LocalizationSettings.AvailableLocales?.Locales : null;
                if (available != null)
                {
                    foreach (Locale locale in available)
                        _locales.Add(locale.Identifier.Code);
                }

                if (_locales.Count > 0)
                    _locales.Add(Pseudolocalizer.Locale);

                return _locales;
            }
        }

        public string BaseLocale =>
            LocalizationSettings.HasSettings && LocalizationSettings.ProjectLocale != null ? LocalizationSettings.ProjectLocale.Identifier.Code : "en";

        public void Activate()
        {
            if (!LocalizationSettings.HasSettings)
                return;

            if (Application.isPlaying)
            {
                if (!_subscribed)
                {
                    LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
                    _subscribed = true;
                }

                AsyncOperationHandle<LocalizationSettings> init = LocalizationSettings.InitializationOperation;
                if (init.IsDone)
                    SyncSelectedLocale();
                else
                    init.Completed += _ => SyncSelectedLocale();
            }
            else if (LocalizationSettings.ProjectLocale != null)
            {
                Typecraft.SetLocale(LocalizationSettings.ProjectLocale.Identifier.Code);
            }
        }

        public void Request(string table, string key, string locale, Action<TextEntry> onReady)
        {
            if (!LocalizationSettings.HasSettings)
            {
                onReady(TextEntry.Missing(table, key, locale));
                return;
            }

            string tableName = string.IsNullOrEmpty(table) ? _defaultTable : table;
            TableReference reference = string.IsNullOrEmpty(tableName) ? LocalizationSettings.StringDatabase.DefaultTable : tableName;
            if (reference.ReferenceType == TableReference.Type.Empty)
            {
                onReady(TextEntry.Missing(table, key, locale));
                return;
            }

            AsyncOperationHandle<LocalizedStringDatabase.TableEntryResult> operation =
                LocalizationSettings.StringDatabase.GetTableEntryAsync(reference, key, FindLocale(locale));

            if (operation.IsDone)
                onReady(Entry(operation, table, key, locale));
            else
                operation.Completed += handle => onReady(Entry(handle, table, key, locale));
        }

        public void SelectLocale(string localeCode)
        {
            if (Pseudolocalizer.IsPseudo(localeCode))
            {
                Typecraft.SetLocale(localeCode);
                return;
            }

            Locale locale = FindLocale(localeCode);
            if (locale == null)
                return;

            if (Application.isPlaying)
                LocalizationSettings.SelectedLocale = locale;
            else
                Typecraft.SetLocale(locale.Identifier.Code);
        }

        private static TextEntry Entry(AsyncOperationHandle<LocalizedStringDatabase.TableEntryResult> handle, string table, string key, string locale)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
                return TextEntry.Missing(table, key, locale);

            StringTableEntry entry = handle.Result.Entry;
            StringTable source = handle.Result.Table;
            if (entry == null || source == null || string.IsNullOrEmpty(entry.Value))
                return TextEntry.Missing(table, key, locale);

            string code = source.LocaleIdentifier.Code;
            bool smart = entry.IsSmart;
            Locale provider = FindLocale(code);
            return new TextEntry(source.TableCollectionName, key, code, entry.Value, (text, arguments) => Format(smart, provider, text, arguments));
        }

        private static string Format(bool smart, Locale provider, string text, IList<object> arguments)
        {
            if (!smart && (arguments == null || arguments.Count == 0))
                return text;

            try
            {
                return smart
                    ? LocalizationSettings.StringDatabase.SmartFormatter.Format(provider, arguments, text)
                    : string.Format(provider, text, arguments as object[] ?? arguments.ToArray());
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Typecraft] {exception.Message}");
                return text;
            }
        }

        private static Locale FindLocale(string code)
        {
            if (string.IsNullOrEmpty(code) || !LocalizationSettings.HasSettings || LocalizationSettings.AvailableLocales == null)
                return null;

            return LocalizationSettings.AvailableLocales.GetLocale(code);
        }

        private static void SyncSelectedLocale()
        {
            Locale selected = LocalizationSettings.SelectedLocale;
            if (selected != null)
                Typecraft.SetLocale(selected.Identifier.Code);
        }

        private static void OnSelectedLocaleChanged(Locale locale)
        {
            if (locale != null)
                Typecraft.SetLocale(locale.Identifier.Code);
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
                _subscribed = false;
            }
        }
    }
}
