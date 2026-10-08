using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Editor.Fonts;
using Velsi.Typecraft.Editor.Localization;
using Velsi.Typecraft.Fonts;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Editor.Window
{
    public sealed class FontsTab : VisualElement
    {
        private readonly Label _title;
        private readonly ScrollView _grid;
        private readonly VisualElement _roleDetail;
        private readonly ObjectField _source;
        private readonly ObjectField _extra;
        private readonly DropdownField _buildLocale;
        private readonly Label _buildResult;
        private readonly Dictionary<string, string> _charsets = new Dictionary<string, string>();
        private readonly List<string> _locales = new List<string>();

        private FontProfile _profile;
        private string _selectedRole;
        private bool _reloadScheduled;

        public FontsTab()
        {
            AddToClassList("tc-fonts");

            var toolbar = new VisualElement();
            toolbar.AddToClassList("tc-toolbar");
            Add(toolbar);

            _title = new Label();
            _title.AddToClassList("tc-toolbar__target");
            toolbar.Add(_title);
            toolbar.Add(Spacer());
            toolbar.Add(new Button(AddRole) { text = "+ роль" });
            toolbar.Add(new Button(() => EditorGUIUtility.PingObject(_profile)) { text = "▸", tooltip = "Показать профиль шрифтов" });
            toolbar.Add(new Button(Reload) { text = "⟳", tooltip = "Перепроверить покрытие символов" });

            _grid = new ScrollView();
            _grid.AddToClassList("tc-fonts__grid");
            Add(_grid);

            var bottom = new VisualElement();
            bottom.AddToClassList("tc-fonts__bottom");
            Add(bottom);

            var build = new VisualElement();
            build.AddToClassList("tc-fonts__build");
            bottom.Add(build);

            var buildTitle = new Label("Статический шрифт по набору символов");
            buildTitle.AddToClassList("tc-lib__title");
            build.Add(buildTitle);
            build.Add(new Label("Берёт из исходного TTF/OTF только символы, которые встречаются в строках языка (плюс ASCII и пунктуация), и назначает шрифт всем ролям для этого языка. После новых строк нажми «Пересобрать» в ячейке.") { name = "tc-fonts__hint" });

            _source = new ObjectField("Исходный шрифт") { objectType = typeof(Font), allowSceneObjects = false };
            build.Add(_source);
            _buildLocale = new DropdownField("Язык");
            build.Add(_buildLocale);
            _extra = new ObjectField("Доп. символы") { objectType = typeof(TextAsset), allowSceneObjects = false, tooltip = "Текстовый файл с символами, которых нет в таблицах (имена игроков, ввод с клавиатуры)" };
            build.Add(_extra);
            build.Add(new Button(BuildStatic) { text = "Собрать и назначить" });
            _buildResult = new Label();
            _buildResult.AddToClassList("tc-fonts__result");
            build.Add(_buildResult);

            _roleDetail = new ScrollView();
            _roleDetail.AddToClassList("tc-fonts__role");
            bottom.Add(_roleDetail);

            Reload();
        }

        public void ScheduleReload()
        {
            if (_reloadScheduled)
                return;

            _reloadScheduled = true;
            schedule.Execute(() =>
            {
                _reloadScheduled = false;
                Reload();
            });
        }

        public void Reload()
        {
            _grid.Clear();
            _roleDetail.Clear();
            TypecraftSettings settings = Typecraft.Settings;
            _profile = settings != null ? settings.Fonts : null;

            if (_profile == null)
            {
                _title.text = "Нет профиля шрифтов";
                _grid.Add(new Label("В TypecraftSettings не назначен FontProfile.") { name = "tc-lib__empty" });
                return;
            }

            _locales.Clear();
            ITextSource source = settings.TextSource;
            _locales.AddRange(source != null ? source.Locales.Where(locale => !Pseudolocalizer.IsPseudo(locale)) : new[] { settings.DefaultLocale });

            _charsets.Clear();
            foreach (string locale in _locales)
                _charsets[locale] = FontCharset.Used(locale);

            string previous = _buildLocale.value;
            _buildLocale.choices = _locales.ToList();
            _buildLocale.SetValueWithoutNotify(_locales.Contains(previous) ? previous : _locales.FirstOrDefault(NeedsFont) ?? _locales.FirstOrDefault());

            int problems = 0;
            var header = new VisualElement();
            header.AddToClassList("tc-fonts__row");
            header.AddToClassList("tc-fonts__row--header");
            header.Add(HeaderCell("Роль", "tc-fonts__role-cell"));
            header.Add(HeaderCell("По умолчанию", "tc-fonts__cell"));
            string baseLocale = TableBackends.Current?.BaseLocale ?? settings.DefaultLocale;
            foreach (string locale in _locales)
                header.Add(HeaderCell(locale == baseLocale ? $"{locale} ★" : locale, "tc-fonts__cell"));
            _grid.Add(header);

            var serialized = new SerializedObject(_profile);
            for (int r = 0; r < _profile.Roles.Count; r++)
            {
                FontRole role = _profile.Roles[r];
                if (role == null)
                    continue;

                var row = new VisualElement();
                row.AddToClassList("tc-fonts__row");
                row.EnableInClassList("tc-fonts__row--selected", role.Id == _selectedRole);

                string id = role.Id;
                var name = new Label(id + (id == settings.DefaultFontRole ? " ★" : string.Empty)) { tooltip = "Клик — материалы и переопределения; правый клик — переименовать или удалить" };
                name.AddToClassList("tc-fonts__role-cell");
                name.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 1)
                        ShowRoleMenu(id);
                    else
                        SelectRole(id);
                });
                row.Add(name);

                var defaultCell = new VisualElement();
                defaultCell.AddToClassList("tc-fonts__cell");
                var defaultField = new ObjectField { objectType = typeof(TMP_FontAsset), allowSceneObjects = false, value = role.Default.Font };
                defaultField.RegisterValueChangedCallback(evt => SetDefaultFont(id, evt.newValue as TMP_FontAsset));
                defaultCell.Add(defaultField);
                row.Add(defaultCell);

                foreach (string locale in _locales)
                {
                    row.Add(LocaleCell(role, locale, out bool missing));
                    if (missing)
                        problems++;
                }

                _grid.Add(row);
            }

            _title.text = $"{_profile.name} · ролей: {_profile.Roles.Count}" + (problems > 0 ? $" · ячеек без символов: {problems}" : string.Empty);
            ShowRoleDetail(serialized);
        }

        private VisualElement LocaleCell(FontRole role, string locale, out bool hasMissing)
        {
            var cell = new VisualElement();
            cell.AddToClassList("tc-fonts__cell");

            LocaleFontOverride exact = role.LocaleOverrides.FirstOrDefault(o => o != null && o.Locale == locale);
            var field = new ObjectField { objectType = typeof(TMP_FontAsset), allowSceneObjects = false, value = exact?.Variant?.Font };
            string roleId = role.Id;
            field.RegisterValueChangedCallback(evt => SetOverride(roleId, locale, evt.newValue as TMP_FontAsset));
            cell.Add(field);

            TMP_FontAsset effective = role.Resolve(locale)?.Font;
            string missing = FontCharset.Missing(effective, _charsets[locale]);
            hasMissing = missing.Length > 0;

            var status = new VisualElement();
            status.AddToClassList("tc-fonts__status");
            cell.Add(status);

            if (exact?.Variant?.Font == null)
                status.Add(new Label($"↳ {(effective != null ? effective.name : "—")}") { name = "inherited", tooltip = "Шрифт не переопределён для языка и берётся из «по умолчанию»" });

            var coverage = new Label(hasMissing ? $"□ нет {missing.Length}" : "✓");
            coverage.AddToClassList(hasMissing ? "tc-fonts__missing" : "tc-fonts__ok");
            coverage.tooltip = hasMissing ? $"Нет в шрифте: {Shorten(missing, 80)}" : $"Все {_charsets[locale].Length} символов строк есть в шрифте";
            if (hasMissing)
                coverage.RegisterCallback<PointerDownEvent>(_ => EditorUtility.DisplayDialog("Typecraft", $"В «{effective?.name}» для {locale} нет {missing.Length} символов:\n\n{Shorten(missing, 600)}", "OK"));
            status.Add(coverage);

            if (hasMissing && StaticFontBuilder.IsBuilt(effective))
            {
                TMP_FontAsset target = effective;
                status.Add(new Button(() => Rebuild(target)) { text = "Пересобрать", tooltip = "Строки изменились — пересобрать статический шрифт" });
            }

            return cell;
        }

        private void SetDefaultFont(string roleId, TMP_FontAsset font)
        {
            var serialized = new SerializedObject(_profile);
            SerializedProperty role = FindRole(serialized, roleId);
            if (role == null)
                return;

            role.FindPropertyRelative("_default._font").objectReferenceValue = font;
            serialized.ApplyModifiedProperties();
            Typecraft.Refresh();
            ScheduleReload();
        }

        private void SetOverride(string roleId, string locale, TMP_FontAsset font)
        {
            var serialized = new SerializedObject(_profile);
            SerializedProperty role = FindRole(serialized, roleId);
            if (role == null)
                return;

            SerializedProperty overrides = role.FindPropertyRelative("_localeOverrides");
            int index = -1;
            for (int i = 0; i < overrides.arraySize; i++)
            {
                if (overrides.GetArrayElementAtIndex(i).FindPropertyRelative("_locale").stringValue == locale)
                    index = i;
            }

            if (font == null)
            {
                if (index >= 0)
                    overrides.DeleteArrayElementAtIndex(index);
            }
            else
            {
                if (index < 0)
                {
                    overrides.arraySize++;
                    index = overrides.arraySize - 1;
                    SerializedProperty added = overrides.GetArrayElementAtIndex(index);
                    added.FindPropertyRelative("_locale").stringValue = locale;
                    added.FindPropertyRelative("_variant._materials").arraySize = 0;
                }

                overrides.GetArrayElementAtIndex(index).FindPropertyRelative("_variant._font").objectReferenceValue = font;
            }

            serialized.ApplyModifiedProperties();
            Typecraft.Refresh();
            ScheduleReload();
        }

        private void BuildStatic()
        {
            var source = _source.value as Font;
            string locale = _buildLocale.value;
            if (source == null || string.IsNullOrEmpty(locale))
            {
                EditorUtility.DisplayDialog("Typecraft", "Выбери исходный шрифт (TTF/OTF) и язык.", "OK");
                return;
            }

            string path = StaticFontBuilder.DefaultPath(source, locale);
            StaticFontBuilder.Result result;
            try
            {
                EditorUtility.DisplayProgressBar("Typecraft", $"Сборка {path}", 0.5f);
                result = StaticFontBuilder.Create(source, locale, _extra.value as TextAsset, path);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            foreach (FontRole role in _profile.Roles.Where(role => role != null))
                SetOverride(role.Id, locale, result.Font);

            ShowResult(result, path);
        }

        private void Rebuild(TMP_FontAsset font)
        {
            StaticFontBuilder.Result result;
            try
            {
                EditorUtility.DisplayProgressBar("Typecraft", $"Пересборка {font.name}", 0.5f);
                result = StaticFontBuilder.Rebuild(font);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ShowResult(result, AssetDatabase.GetAssetPath(font));
            ScheduleReload();
        }

        private void ShowResult(StaticFontBuilder.Result result, string path)
        {
            _buildResult.text = $"Готово: {result.Characters} символов → {path}" +
                (result.NotInSource.Length > 0 ? $"\nНет в исходном шрифте: {Shorten(result.NotInSource, 80)}" : string.Empty);
        }

        private void SelectRole(string id)
        {
            _selectedRole = _selectedRole == id ? null : id;
            Reload();
        }

        private void ShowRoleDetail(SerializedObject serialized)
        {
            SerializedProperty role = _selectedRole != null ? FindRole(serialized, _selectedRole) : null;
            if (role == null)
            {
                _roleDetail.Add(new Label("Клик по роли слева покажет её материалы и переопределения по языкам.") { name = "tc-fonts__hint" });
                return;
            }

            var content = new VisualElement();
            _roleDetail.Add(content);

            var title = new Label($"Роль «{_selectedRole}»");
            title.AddToClassList("tc-lib__title");
            content.Add(title);
            content.Add(new PropertyField(role.FindPropertyRelative("_default._materials"), "Пресеты материалов (по умолчанию)"));
            content.Add(new PropertyField(role.FindPropertyRelative("_localeOverrides"), "Переопределения по языкам"));
            content.Bind(serialized);
            content.TrackSerializedObjectValue(serialized, _ => Typecraft.Refresh());
        }

        private void ShowRoleMenu(string id)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Переименовать"), false, () => RenameRole(id));
            menu.AddItem(new GUIContent("Сделать ролью по умолчанию"), id == Typecraft.Settings?.DefaultFontRole, () => SetDefaultRole(id));
            menu.AddItem(new GUIContent("Удалить"), false, () => RemoveRole(id));
            menu.ShowAsContext();
        }

        private void AddRole()
        {
            if (_profile == null)
                return;

            string id = TextPrompt.Show("Новая роль шрифта", "Id роли (например, Body, Header, Handwriting):", "Role");
            if (string.IsNullOrWhiteSpace(id) || _profile.TryGetRole(id.Trim(), out _))
                return;

            Undo.RecordObject(_profile, "Typecraft: роль шрифта");
            _profile.Add(new FontRole(id.Trim(), TMP_Settings.defaultFontAsset));
            EditorUtility.SetDirty(_profile);
            Typecraft.Refresh();
            ScheduleReload();
        }

        private void RenameRole(string id)
        {
            string newId = TextPrompt.Show("Переименовать роль", $"Новый id роли «{id}»:", id)?.Trim();
            if (string.IsNullOrEmpty(newId) || newId == id)
                return;

            if (_profile.TryGetRole(newId, out _))
            {
                EditorUtility.DisplayDialog("Typecraft", $"Роль «{newId}» уже есть.", "OK");
                return;
            }

            Undo.SetCurrentGroupName("Typecraft: переименование роли");
            int group = Undo.GetCurrentGroup();

            var profile = new SerializedObject(_profile);
            FindRole(profile, id).FindPropertyRelative("_id").stringValue = newId;
            profile.ApplyModifiedProperties();

            TypecraftSettings settings = Typecraft.Settings;
            if (settings.Styles != null)
            {
                var styles = new SerializedObject(settings.Styles);
                SerializedProperty list = styles.FindProperty("_styles");
                for (int i = 0; i < list.arraySize; i++)
                {
                    SerializedProperty role = list.GetArrayElementAtIndex(i).FindPropertyRelative("_fontRole");
                    if (role.stringValue == id)
                        role.stringValue = newId;
                }
                styles.ApplyModifiedProperties();
            }

            if (settings.DefaultFontRole == id)
            {
                var serializedSettings = new SerializedObject(settings);
                serializedSettings.FindProperty("_defaultFontRole").stringValue = newId;
                serializedSettings.ApplyModifiedProperties();
            }

            foreach (TypecraftText text in Object.FindObjectsByType<TypecraftText>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var serializedText = new SerializedObject(text);
                SerializedProperty role = serializedText.FindProperty("_fontRole");
                if (role.stringValue != id)
                    continue;

                role.stringValue = newId;
                serializedText.ApplyModifiedProperties();
                if (text.gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(text.gameObject.scene);
            }

            Undo.CollapseUndoOperations(group);
            if (_selectedRole == id)
                _selectedRole = newId;
            Typecraft.Refresh();
            ScheduleReload();
        }

        private void SetDefaultRole(string id)
        {
            var serialized = new SerializedObject(Typecraft.Settings);
            serialized.FindProperty("_defaultFontRole").stringValue = id;
            serialized.ApplyModifiedProperties();
            Typecraft.Refresh();
            ScheduleReload();
        }

        private void RemoveRole(string id)
        {
            if (!EditorUtility.DisplayDialog("Typecraft", $"Удалить роль «{id}»? Стили и тексты с этой ролью перейдут на шрифт самого TMP_Text.", "Удалить", "Отмена"))
                return;

            var serialized = new SerializedObject(_profile);
            SerializedProperty roles = serialized.FindProperty("_roles");
            for (int i = 0; i < roles.arraySize; i++)
            {
                if (roles.GetArrayElementAtIndex(i).FindPropertyRelative("_id").stringValue == id)
                {
                    roles.DeleteArrayElementAtIndex(i);
                    break;
                }
            }

            serialized.ApplyModifiedProperties();
            Typecraft.Refresh();
            ScheduleReload();
        }

        private bool NeedsFont(string locale) =>
            _profile.Roles.Any(role => role != null && FontCharset.Missing(role.Resolve(locale)?.Font, _charsets[locale]).Length > 0);

        private static SerializedProperty FindRole(SerializedObject profile, string id)
        {
            SerializedProperty roles = profile.FindProperty("_roles");
            for (int i = 0; i < roles.arraySize; i++)
            {
                SerializedProperty role = roles.GetArrayElementAtIndex(i);
                if (role.FindPropertyRelative("_id").stringValue == id)
                    return role;
            }

            return null;
        }

        private static string Shorten(string text, int length) => text.Length <= length ? text : text.Substring(0, length) + "…";

        private static Label HeaderCell(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static VisualElement Spacer()
        {
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            return spacer;
        }
    }
}
