using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Editor.Localization
{
    public sealed class MarkupLayerMigration
    {
        public readonly struct Cell
        {
            public Cell(string table, string key, string locale, string markup, string text)
            {
                Table = table;
                Key = key;
                Locale = locale;
                Markup = markup;
                Text = text;
            }

            public string Table { get; }
            public string Key { get; }
            public string Locale { get; }
            public string Markup { get; }
            public string Text { get; }
        }

        private readonly ITableBackend _backend;
        private readonly List<Cell> _cells = new List<Cell>();

        private MarkupLayerMigration(ITableBackend backend)
        {
            _backend = backend;
        }

        public IReadOnlyList<Cell> Cells => _cells;

        public static MarkupLayerMigration Plan(ITableBackend backend)
        {
            var plan = new MarkupLayerMigration(backend);
            foreach (string table in backend.Tables)
            foreach (string key in backend.Keys(table))
            foreach (string locale in backend.Locales)
            {
                string value = backend.Get(table, key, locale);
                if (Decoration.Has(value))
                    plan._cells.Add(new Cell(table, key, locale, value, Decoration.Strip(value)));
            }

            return plan;
        }

        public string Describe()
        {
            var lines = new List<string>();
            foreach (IGrouping<string, Cell> table in _cells.GroupBy(cell => cell.Table))
                lines.Add($"Таблица «{table.Key}»: ячеек с оформлением — {table.Count()}, ключей — {table.Select(cell => cell.Key).Distinct().Count()}");

            lines.Add(string.Empty);
            foreach (Cell cell in _cells.Take(6))
                lines.Add($"{cell.Key} [{cell.Locale}]\n  в таблицу: {Truncate(cell.Text)}\n  в слой: {Truncate(cell.Markup)}");
            if (_cells.Count > 6)
                lines.Add($"… и ещё {_cells.Count - 6}");

            return string.Join("\n", lines);
        }

        public void Apply(Func<string, MarkupLayer> layerFor)
        {
            foreach (IGrouping<string, Cell> group in _cells.GroupBy(cell => cell.Table))
            {
                MarkupLayer layer = layerFor(group.Key);
                if (layer == null)
                    continue;

                Undo.RecordObject(layer, "Typecraft: разметка в слой");
                foreach (Cell cell in group)
                {
                    _backend.Set(cell.Table, cell.Key, cell.Locale, cell.Text);
                    layer.Set(cell.Key, cell.Locale, cell.Markup, cell.Text);
                }

                EditorUtility.SetDirty(layer);
            }

            AssetDatabase.SaveAssets();
            SceneTexts.RefreshAll();
            Typecraft.Refresh();
        }

        [MenuItem("Tools/Typecraft/Move Markup To Layers")]
        private static void MoveMarkupToLayers()
        {
            ITableBackend backend = TableBackends.Current;
            if (backend == null)
            {
                EditorUtility.DisplayDialog("Typecraft", "В TypecraftSettings не задан источник строк, который умеет править окно Typecraft.", "OK");
                return;
            }

            MarkupLayerMigration plan = Plan(backend);
            if (plan.Cells.Count == 0)
            {
                EditorUtility.DisplayDialog("Typecraft", "В таблицах нет оформления — переносить нечего.", "OK");
                return;
            }

            string message = "В таблицу запишется текст без стилей и тегов, оформление уйдёт в слой разметки рядом с таблицей.\n\n" + plan.Describe();
            if (!EditorUtility.DisplayDialog("Move Markup To Layers", message, "Перенести", "Отмена"))
                return;

            plan.Apply(table => MarkupLayers.GetOrCreate(backend, table));
            TypecraftLog.Info($"Оформление перенесено в слои разметки: ячеек {plan.Cells.Count}.");
        }

        private static string Truncate(string text)
        {
            string line = text.Replace("\r", string.Empty).Replace('\n', '⏎');
            return line.Length <= 70 ? line : line.Substring(0, 70) + "…";
        }
    }
}
