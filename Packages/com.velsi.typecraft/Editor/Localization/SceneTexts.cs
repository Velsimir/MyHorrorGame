using System;
using System.Collections.Generic;
using UnityEngine;
using Velsi.Typecraft.Text;
using Velsi.Typecraft.Text.Tables;
using Object = UnityEngine.Object;

namespace Velsi.Typecraft.Editor.Localization
{
    public static class SceneTexts
    {
        private static int _batchDepth;
        private static bool _refreshPending;

        public static IEnumerable<TypecraftText> Localized(string table, string key)
        {
            foreach (TypecraftText text in Object.FindObjectsByType<TypecraftText>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (Uses(text, table, key))
                    yield return text;
            }
        }

        public static bool Uses(TypecraftText text, string table, string key)
        {
            if (text == null || text.Mode != TextMode.Localized || text.Key != key)
                return false;
            if (!string.IsNullOrEmpty(text.Table))
                return text.Table == table;
            return ResolvesTo(table, key);
        }

        public static bool ResolvesTo(string table, string key)
        {
            ITextSource source = Typecraft.Settings != null ? Typecraft.Settings.TextSource : null;
            return source is not TableTextSource tables || tables.FindTable(null, key)?.TableId == table;
        }

        public static void RefreshAll()
        {
            if (_batchDepth > 0)
            {
                _refreshPending = true;
                return;
            }

            foreach (TypecraftText text in Object.FindObjectsByType<TypecraftText>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (text.Mode == TextMode.Localized)
                    text.Refresh();
            }
        }

        public static IDisposable Batch()
        {
            _batchDepth++;
            return new BatchScope();
        }

        private sealed class BatchScope : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _batchDepth--;
                if (_batchDepth > 0 || !_refreshPending)
                    return;

                _refreshPending = false;
                RefreshAll();
            }
        }
    }
}
