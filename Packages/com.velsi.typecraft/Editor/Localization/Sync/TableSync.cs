using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Editor.Localization
{
    public sealed class TableSyncOptions
    {
        public bool AddLocales = true;
        public bool ClearEmpty;
        public bool RemoveMissing;
    }

    public readonly struct CellChange
    {
        public CellChange(string key, string locale, string before, string after, bool newLocale, bool losesMarkup)
        {
            Key = key;
            Locale = locale;
            Before = before;
            After = after;
            NewLocale = newLocale;
            LosesMarkup = losesMarkup;
        }

        public string Key { get; }
        public string Locale { get; }
        public string Before { get; }
        public string After { get; }
        public bool NewLocale { get; }
        public bool LosesMarkup { get; }
        public bool Clears => string.IsNullOrEmpty(After);
    }

    public readonly struct CommentChange
    {
        public CommentChange(string key, string before, string after)
        {
            Key = key;
            Before = before;
            After = after;
        }

        public string Key { get; }
        public string Before { get; }
        public string After { get; }
        public bool Clears => string.IsNullOrEmpty(After);
    }

    public sealed class TableSyncPlan
    {
        public string Table { get; internal set; }
        public string Origin { get; internal set; }
        public List<string> NewLocales { get; } = new List<string>();
        public List<SheetImport.Row> AddedKeys { get; } = new List<SheetImport.Row>();
        public List<CellChange> Changes { get; } = new List<CellChange>();
        public List<CommentChange> Comments { get; } = new List<CommentChange>();
        public List<string> MissingKeys { get; } = new List<string>();
        public List<string> MissingKeysWithMarkup { get; } = new List<string>();
        public List<string> DuplicateKeys { get; } = new List<string>();
        public List<string> IgnoredColumns { get; } = new List<string>();

        public bool IsEmpty => NewLocales.Count == 0 && AddedKeys.Count == 0 && Changes.Count == 0 && Comments.Count == 0 && MissingKeys.Count == 0;

        public IEnumerable<CellChange> EffectiveChanges(TableSyncOptions options) =>
            Changes.Where(change => (options.AddLocales || !change.NewLocale) && (options.ClearEmpty || !change.Clears));

        public IEnumerable<CommentChange> EffectiveComments(TableSyncOptions options) =>
            Comments.Where(change => options.ClearEmpty || !change.Clears);

        public IEnumerable<KeyValuePair<string, string>> AddedValues(SheetImport.Row row, TableSyncOptions options) =>
            row.Values.Where(pair => !string.IsNullOrEmpty(pair.Value) && (options.AddLocales || !NewLocales.Contains(pair.Key)));

        public int MarkupLost(TableSyncOptions options) => EffectiveChanges(options).Count(change => change.LosesMarkup);
    }

    public static class TableSync
    {
        public static TableSyncPlan Plan(ITableBackend backend, string table, SheetImport data, string origin = null)
        {
            var plan = new TableSyncPlan { Table = table, Origin = origin };
            plan.DuplicateKeys.AddRange(data.DuplicateKeys);
            plan.IgnoredColumns.AddRange(data.IgnoredColumns);

            List<string> known = backend.Locales.ToList();
            var canonical = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string locale in data.Locales)
            {
                string match = known.FirstOrDefault(existing => string.Equals(existing, locale, StringComparison.OrdinalIgnoreCase));
                canonical[locale] = match ?? locale;
                if (match == null)
                    plan.NewLocales.Add(locale);
            }

            var existingKeys = new HashSet<string>(backend.Keys(table), StringComparer.Ordinal);
            var incoming = new HashSet<string>(StringComparer.Ordinal);

            foreach (SheetImport.Row source in data.Rows)
            {
                incoming.Add(source.Key);
                var row = new SheetImport.Row { Key = source.Key, Line = source.Line, Comment = source.Comment };
                foreach (KeyValuePair<string, string> pair in source.Values)
                    row.Values[canonical[pair.Key]] = pair.Value;

                if (!existingKeys.Contains(row.Key))
                {
                    plan.AddedKeys.Add(row);
                    continue;
                }

                foreach (KeyValuePair<string, string> pair in row.Values)
                {
                    bool newLocale = plan.NewLocales.Contains(pair.Key);
                    string before = newLocale ? string.Empty : backend.Get(table, row.Key, pair.Key) ?? string.Empty;
                    string after = pair.Value ?? string.Empty;
                    if (before == after)
                        continue;

                    bool losesMarkup = !newLocale && MarkupLayers.State(backend, table, row.Key, pair.Key) == LayerState.Applied;
                    plan.Changes.Add(new CellChange(row.Key, pair.Key, before, after, newLocale, losesMarkup));
                }

                if (row.Comment != null)
                {
                    string before = backend.Comment(table, row.Key) ?? string.Empty;
                    if (before != row.Comment)
                        plan.Comments.Add(new CommentChange(row.Key, before, row.Comment));
                }
            }

            MarkupLayer layer = MarkupLayers.Find(table);
            foreach (string key in backend.Keys(table))
            {
                if (incoming.Contains(key))
                    continue;

                plan.MissingKeys.Add(key);
                if (layer != null && layer.Entries.Any(entry => entry != null && entry.Key == key && !string.IsNullOrEmpty(entry.Markup)))
                    plan.MissingKeysWithMarkup.Add(key);
            }

            return plan;
        }

        public static void Apply(ITableBackend backend, TableSyncPlan plan, TableSyncOptions options)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Typecraft: синхронизация таблицы");
            string table = plan.Table;

            using (SceneTexts.Batch())
            {
                if (options.AddLocales)
                {
                    foreach (string locale in plan.NewLocales)
                        backend.AddLocale(locale);
                }

                foreach (SheetImport.Row row in plan.AddedKeys)
                {
                    backend.AddKey(table, row.Key);
                    if (!string.IsNullOrEmpty(row.Comment))
                        backend.SetComment(table, row.Key, row.Comment);
                    foreach (KeyValuePair<string, string> pair in plan.AddedValues(row, options))
                        backend.Set(table, row.Key, pair.Key, pair.Value);
                }

                foreach (CellChange change in plan.EffectiveChanges(options))
                    backend.Set(table, change.Key, change.Locale, change.After);

                foreach (CommentChange change in plan.EffectiveComments(options))
                    backend.SetComment(table, change.Key, change.After);

                if (options.RemoveMissing)
                {
                    foreach (string key in plan.MissingKeys)
                    {
                        backend.RemoveKey(table, key);
                        MarkupLayers.RemoveKey(table, key);
                    }
                }
            }

            Undo.CollapseUndoOperations(group);
            Typecraft.Refresh();
        }

        public static string Summary(TableSyncPlan plan, TableSyncOptions options)
        {
            int changes = plan.EffectiveChanges(options).Count();
            int comments = plan.EffectiveComments(options).Count();
            var parts = new List<string>
            {
                $"новых ключей: {plan.AddedKeys.Count}",
                $"изменённых строк: {changes}",
                $"комментариев: {comments}"
            };
            if (options.AddLocales && plan.NewLocales.Count > 0)
                parts.Add($"новых языков: {plan.NewLocales.Count}");
            if (options.RemoveMissing && plan.MissingKeys.Count > 0)
                parts.Add($"удалено ключей: {plan.MissingKeys.Count}");
            return string.Join(", ", parts);
        }
    }
}
