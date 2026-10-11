using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Art;
using Velsi.Typecraft.Editor.Window.Drawer;
using Velsi.Typecraft.Editor.Window.Layout;
using Velsi.Typecraft.Editor.Window.Preview;
using Velsi.Typecraft.Library;

namespace Velsi.Typecraft.Editor.Library
{
    public sealed class IconSetPanel : VisualElement, IDisposable
    {
        private const float OffsetLimit = 0.5f;

        private readonly IconSet _set;
        private readonly bool _editable;
        private readonly Label _title;
        private readonly ScrollView _grid;
        private readonly VisualElement _details;
        private IconLinePreview _linePreview;
        private int _selected = -1;

        public IconSetPanel(IconSet set)
        {
            _set = set;
            _editable = AssetDatabase.IsValidFolder(set.BuildSettings.SourceFolder ?? string.Empty);
            AddToClassList("tc-lib__panel");

            var header = new VisualElement();
            header.AddToClassList("tc-lib__header");
            _title = new Label();
            _title.AddToClassList("tc-lib__title");
            header.Add(_title);
            header.Add(new Button(Rebuild) { text = "Пересобрать", tooltip = "Пересобрать атлас из папки исходников" });
            header.Add(new Button(() => EditorGUIUtility.PingObject(_set)) { text = "▸", tooltip = "Показать ассет набора" });
            header.Add(new Button(RevealSource) { text = "Папка", tooltip = set.BuildSettings.SourceFolder });
            Add(header);

            var hint = new Label(_editable
                ? "Перетащи PNG из проводника или окна Project — файл скопируется в папку исходников, атлас пересоберётся."
                : "Встроенный набор пакета: исходники лежат в Art~ и пересобираются через Tools/Typecraft/Rebuild Base Prompt Sets.");
            hint.AddToClassList("tc-lib__hint");
            Add(hint);

            _grid = TypecraftLayout.VerticalScroll("tc-lib__icons");
            _grid.contentContainer.AddToClassList("tc-lib__icons-content");
            Add(_grid);

            _details = new VisualElement();
            _details.AddToClassList("tc-lib__icon-details");
            Add(_details);

            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<DragLeaveEvent>(_ => RemoveFromClassList("tc-lib__panel--drop"));
            Undo.undoRedoPerformed += OnUndoRedo;

            Refresh();
        }

        public void Dispose()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            _linePreview?.Dispose();
            _linePreview = null;
        }

        private void OnUndoRedo()
        {
            if (_set == null)
                return;

            TmpSpriteAssetSync.ApplyPlacement(_set);
            ShowDetails();
        }

        private void Refresh()
        {
            _title.text = $"{_set.SetId} · иконок: {_set.Entries.Count}";
            _grid.Clear();

            for (int i = 0; i < _set.Entries.Count; i++)
            {
                IconEntry entry = _set.Entries[i];
                int index = i;
                var tile = new VisualElement { tooltip = $"{{{_set.SetId}/{entry.Key}}}" };
                tile.AddToClassList("tc-icon");
                tile.EnableInClassList("tc-icon--selected", i == _selected);

                Sprite sprite = GalleryDrawer.FindSprite(_set, entry);
                if (sprite != null)
                    tile.style.backgroundImage = new StyleBackground(sprite);
                else
                    tile.Add(new Label("?") { name = "missing" });

                var caption = new Label(entry.Key);
                caption.AddToClassList("tc-icon__caption");
                tile.Add(caption);
                tile.RegisterCallback<PointerDownEvent>(_ => Select(index));
                _grid.Add(tile);
            }

            if (_set.Entries.Count == 0)
                _grid.Add(new Label(_editable ? "Набор пуст — перетащи сюда PNG." : "Набор пуст.") { name = "tc-lib__empty" });

            ShowDetails();
        }

        private void Select(int index)
        {
            _selected = index;
            for (int i = 0; i < _grid.childCount; i++)
                _grid[i].EnableInClassList("tc-icon--selected", i == index);
            ShowDetails();
        }

        private void ShowDetails()
        {
            _details.Clear();
            _linePreview?.Dispose();
            _linePreview = null;
            if (_selected < 0 || _selected >= _set.Entries.Count)
                return;

            IconEntry entry = _set.Entries[_selected];
            string reference = $"{{{_set.SetId}/{entry.Key}}}";

            var key = new TextField("Ключ") { isDelayed = true, value = entry.Key };
            key.AddToClassList("tc-lib__icon-key");
            key.SetEnabled(_editable);
            key.RegisterValueChangedCallback(evt => Rename(entry, evt.newValue, key));
            _details.Add(key);

            var tint = new Toggle("Красится цветом текста") { value = entry.Tintable, tooltip = "Иконка берёт цвет окружающего текста (<sprite … tint=1>)" };
            tint.SetEnabled(_editable);
            tint.RegisterValueChangedCallback(evt => SetEntryField(_selected, "_tintable", property => property.boolValue = evt.newValue, "Typecraft: окраска иконки"));
            _details.Add(tint);

            _details.Add(new Button(() => EditorGUIUtility.systemCopyBuffer = reference) { text = $"Копировать {reference}" });

            var remove = new Button(() => Remove(_selected)) { text = "Удалить" };
            remove.SetEnabled(_editable);
            _details.Add(remove);

            if (entry.IsPlaceholder)
            {
                _details.Add(new Label("Картинки нет — ключ ждёт PNG с тем же именем.") { name = "tc-lib__placeholder" });
                return;
            }

            _details.Add(BuildFitting(_selected, entry));
        }

        private VisualElement BuildFitting(int index, IconEntry entry)
        {
            var fitting = new VisualElement();
            fitting.AddToClassList("tc-lib__fit");

            var sliders = new VisualElement();
            sliders.AddToClassList("tc-lib__fit-sliders");
            fitting.Add(sliders);

            Slider offsetX = FitSlider(sliders, "Смещение X", -OffsetLimit, OffsetLimit, entry.Offset.x, "Сдвиг вправо, доля высоты иконки");
            Slider offsetY = FitSlider(sliders, "Смещение Y", -OffsetLimit, OffsetLimit, entry.Offset.y, "Сдвиг вверх, доля высоты иконки");
            Slider scale = FitSlider(sliders, "Масштаб", IconEntry.MinScale, IconEntry.MaxScale, entry.Scale, "Масштаб от нижнего левого угла");
            Slider setOffset = FitSlider(sliders, "Сдвиг набора по Y", -OffsetLimit, OffsetLimit, _set.BuildSettings.VerticalOffset, "Общий сдвиг всех иконок набора вверх, доля высоты");

            offsetX.RegisterValueChangedCallback(evt => Place(index, "_offset", property => property.vector2Value = new Vector2(evt.newValue, property.vector2Value.y)));
            offsetY.RegisterValueChangedCallback(evt => Place(index, "_offset", property => property.vector2Value = new Vector2(property.vector2Value.x, evt.newValue)));
            scale.RegisterValueChangedCallback(evt => Place(index, "_scale", property => property.floatValue = evt.newValue));
            setOffset.RegisterValueChangedCallback(evt => PlaceSet(evt.newValue));

            sliders.Add(new Button(() =>
            {
                offsetX.value = 0f;
                offsetY.value = 0f;
                scale.value = 1f;
            }) { text = "Сбросить иконку" });

            _linePreview = new IconLinePreview();
            _linePreview.Show(_set.SpriteAsset, entry.GlyphName, entry.Tintable);
            fitting.Add(_linePreview.Element);
            return fitting;
        }

        private static Slider FitSlider(VisualElement parent, string label, float min, float max, float value, string tooltip)
        {
            var slider = new Slider(label, min, max) { showInputField = true, tooltip = tooltip };
            slider.SetValueWithoutNotify(value);
            parent.Add(slider);
            return slider;
        }

        private void Place(int index, string field, Action<SerializedProperty> set)
        {
            var serialized = new SerializedObject(_set);
            set(serialized.FindProperty("_entries").GetArrayElementAtIndex(index).FindPropertyRelative(field));
            serialized.ApplyModifiedProperties();
            Undo.SetCurrentGroupName("Typecraft: подгонка иконки");
            ApplyPlacement();
        }

        private void PlaceSet(float offset)
        {
            var serialized = new SerializedObject(_set);
            serialized.FindProperty("_buildSettings._verticalOffset").floatValue = offset;
            serialized.ApplyModifiedProperties();
            Undo.SetCurrentGroupName("Typecraft: сдвиг набора");
            ApplyPlacement();
        }

        private void ApplyPlacement()
        {
            TmpSpriteAssetSync.ApplyPlacement(_set);
            _linePreview?.Refresh();
        }

        private void Rename(IconEntry entry, string value, TextField field)
        {
            string oldKey = entry.Key;
            string newKey = IconKeyNaming.ToKey(value ?? string.Empty);
            if (newKey == oldKey)
            {
                field.SetValueWithoutNotify(oldKey);
                return;
            }

            if (newKey.Length == 0 || newKey.IndexOfAny(new[] { '{', '}', '/', ':' }) >= 0 || _set.Entries.Any(other => other.Key == newKey))
            {
                EditorUtility.DisplayDialog("Typecraft", $"Ключ «{newKey}» пустой, содержит недопустимые символы или уже занят.", "OK");
                field.SetValueWithoutNotify(oldKey);
                return;
            }

            bool plain = Typecraft.Icons.TryResolve(oldKey, out IconSet owner, out _) && owner == _set;
            MarkupRewriter rewriter = MarkupRewriter.Prepare(MarkupRewriter.IconPattern(_set.SetId, oldKey, plain), $"{{{_set.SetId}/{newKey}}}");
            if (!rewriter.IsEmpty && !EditorUtility.DisplayDialog("Typecraft", $"Ссылки на иконку будут переписаны:\n\n{rewriter.Describe()}{rewriter.UndoNote}", "Переименовать", "Отмена"))
            {
                field.SetValueWithoutNotify(oldKey);
                return;
            }

            Undo.SetCurrentGroupName("Typecraft: переименование иконки");
            int group = Undo.GetCurrentGroup();
            SetEntryField(_selected, "_key", property => property.stringValue = newKey, "Typecraft: переименование иконки");
            rewriter.Apply("Typecraft: переименование иконки");
            Undo.CollapseUndoOperations(group);

            Typecraft.Refresh();
            Refresh();
        }

        private void SetEntryField(int index, string field, Action<SerializedProperty> set, string undoName)
        {
            var serialized = new SerializedObject(_set);
            SerializedProperty property = serialized.FindProperty("_entries").GetArrayElementAtIndex(index).FindPropertyRelative(field);
            set(property);
            serialized.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(undoName);
            Typecraft.Refresh();
        }

        private void Remove(int index)
        {
            if (index < 0 || index >= _set.Entries.Count)
                return;

            IconEntry entry = _set.Entries[index];
            bool plain = Typecraft.Icons.TryResolve(entry.Key, out IconSet owner, out _) && owner == _set;
            MarkupRewriter uses = MarkupRewriter.Prepare(MarkupRewriter.IconPattern(_set.SetId, entry.Key, plain), "$0");
            string message = uses.IsEmpty
                ? $"Удалить иконку «{entry.Key}»? PNG уйдёт в корзину Unity."
                : $"Иконка «{entry.Key}» используется:\n\n{uses.Describe()}\n\nСсылки останутся в тексте и будут подсвечены как ошибка. PNG уйдёт в корзину Unity.";
            if (!EditorUtility.DisplayDialog("Typecraft", message, "Удалить", "Отмена"))
                return;

            string file = SourceFile(entry.GlyphName);
            if (file != null && !AssetDatabase.MoveAssetToTrash(file))
            {
                EditorUtility.DisplayDialog("Typecraft", $"Не удалось переместить в корзину: {file}", "OK");
                return;
            }

            var serialized = new SerializedObject(_set);
            serialized.FindProperty("_entries").DeleteArrayElementAtIndex(index);
            serialized.ApplyModifiedProperties();

            _selected = -1;
            if (HasSourceImages())
                IconSetBuilder.Build(_set);
            Typecraft.Refresh();
            Refresh();
        }

        private string SourceFile(string glyphName)
        {
            if (string.IsNullOrEmpty(glyphName))
                return null;

            string folder = _set.BuildSettings.SourceFolder;
            string root = Path.GetFullPath(folder);
            SearchOption option = _set.BuildSettings.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string full = Directory.GetFiles(root, glyphName + ".png", option).FirstOrDefault();
            return full == null ? null : $"{folder}/{full.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/')}";
        }

        private bool HasSourceImages() => SourceImages().Length > 0;

        private string[] SourceImages()
        {
            SearchOption option = _set.BuildSettings.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.GetFiles(Path.GetFullPath(_set.BuildSettings.SourceFolder), "*.png", option);
        }

        private void Rebuild()
        {
            if (!_editable && !EditorUtility.DisplayDialog("Typecraft", "Это встроенный набор пакета. Пересобрать его атлас из Art~?", "Пересобрать", "Отмена"))
                return;

            IconSetBuilder.Build(_set);
            Typecraft.Refresh();
            Refresh();
        }

        private void RevealSource()
        {
            string folder = _set.BuildSettings.SourceFolder;
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(Path.GetFullPath(folder)))
                EditorUtility.RevealInFinder(Path.GetFullPath(folder));
        }

        private static List<string> DraggedImages() =>
            DragAndDrop.paths.Where(path => string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase)).ToList();

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (!_editable || DraggedImages().Count == 0)
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            AddToClassList("tc-lib__panel--drop");
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            RemoveFromClassList("tc-lib__panel--drop");
            List<string> images = DraggedImages();
            if (!_editable || images.Count == 0)
                return;

            DragAndDrop.AcceptDrag();
            EditorApplication.delayCall += () => AddImages(images);
        }

        private void AddImages(List<string> images)
        {
            string folder = _set.BuildSettings.SourceFolder;
            Vector2Int? prevailing = IconImageCheck.PrevailingSize(SourceImages());
            string fitGlyph = null;
            int added = 0;

            foreach (string path in images)
            {
                string source = Path.GetFullPath(path);
                string destination = Path.GetFullPath($"{folder}/{Path.GetFileName(path)}");
                if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (File.Exists(destination) && !EditorUtility.DisplayDialog("Typecraft", $"«{Path.GetFileName(path)}» уже есть в наборе. Заменить?", "Заменить", "Пропустить"))
                    continue;

                IconCheckChoice choice = Check(source, prevailing, out Vector2Int size);
                if (choice == IconCheckChoice.Cancel)
                    continue;

                if (choice == IconCheckChoice.Fit)
                    fitGlyph = Path.GetFileNameWithoutExtension(path);
                prevailing ??= size;

                File.Copy(source, destination, true);
                added++;
            }

            if (added == 0)
                return;

            AssetDatabase.Refresh();
            IconSetBuilder.Build(_set);
            Typecraft.Refresh();

            int fitIndex = fitGlyph == null ? -1 : _set.Entries.ToList().FindIndex(entry => entry.GlyphName == fitGlyph);
            if (fitIndex >= 0)
                _selected = fitIndex;
            Refresh();
        }

        private static IconCheckChoice Check(string file, Vector2Int? prevailing, out Vector2Int size)
        {
            Texture2D texture = IconImageCheck.Load(file);
            if (texture == null)
            {
                size = default;
                EditorUtility.DisplayDialog("Typecraft", $"Не удалось прочитать «{Path.GetFileName(file)}» как PNG.", "OK");
                return IconCheckChoice.Cancel;
            }

            try
            {
                size = new Vector2Int(texture.width, texture.height);
                List<string> problems = IconImageCheck.Inspect(texture, prevailing);
                return problems.Count == 0 ? IconCheckChoice.Add : IconCheckDialog.Show(texture, texture.name, problems);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
