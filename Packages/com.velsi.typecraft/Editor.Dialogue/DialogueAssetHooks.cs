using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
using Velsi.Typecraft.Dialogue.Editor.Model;
using Velsi.Typecraft.Editor.Localization;

namespace Velsi.Typecraft.Dialogue.Editor
{
    public static class DialogueIdGuard
    {
        public static int FixDuplicates()
        {
            int fixedCount = 0;
            foreach (IGrouping<string, DialogueAsset> group in DialogueProject.Dialogues().GroupBy(asset => asset.Id).Where(group => group.Count() > 1))
            {
                List<DialogueAsset> copies = group.OrderBy(Created).ThenBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal).ToList();
                string original = AssetDatabase.GetAssetPath(copies[0]);
                foreach (DialogueAsset copy in copies.Skip(1))
                {
                    string before = copy.Id;
                    copy.RegenerateId();
                    EditorUtility.SetDirty(copy);
                    AssetDatabase.SaveAssetIfDirty(copy);
                    fixedCount++;
                    Debug.Log($"[Typecraft] Диалог {AssetDatabase.GetAssetPath(copy)} — копия {original} с тем же Id {before}; выдан новый Id {copy.Id}.");
                }
            }

            return fixedCount;
        }

        private static DateTime Created(DialogueAsset asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? DateTime.MaxValue : File.GetCreationTimeUtc(Path.GetFullPath(path));
        }
    }

    internal sealed class DialogueIdPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!imported.Any(path => path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) && AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(DialogueAsset)))
                return;

            EditorApplication.delayCall -= Fix;
            EditorApplication.delayCall += Fix;
        }

        private static void Fix() => DialogueIdGuard.FixDuplicates();
    }

    internal static class DialogueOpenAsset
    {
        [OnOpenAsset]
        private static bool Open(EntityId entityId, int line)
        {
            if (!(EditorUtility.EntityIdToObject(entityId) is DialogueAsset asset))
                return false;

            DialoguesTab.Open(asset);
            return true;
        }
    }

    [CustomEditor(typeof(DialogueAsset))]
    internal sealed class DialogueAssetInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var asset = (DialogueAsset)target;
            var root = new VisualElement();
            root.style.paddingTop = 4;

            var open = new Button(() => DialoguesTab.Open(asset)) { text = "Открыть в Typecraft" };
            open.style.height = 28;
            root.Add(open);

            List<DialogueIssue> issues = DialogueValidator.Validate(asset, DialogueProject.MergedCatalog());
            root.Add(new Label($"Нод: {asset.Nodes.Count} · таблица: {(string.IsNullOrEmpty(asset.DefaultTable) ? "не задана" : asset.DefaultTable)}"));
            root.Add(new Label($"Проблем: {issues.Count(issue => issue.Severity != DialogueIssueSeverity.Info)}"));
            root.Add(new Label("Id " + asset.Id) { style = { color = new Color(0.6f, 0.6f, 0.6f) } });
            return root;
        }
    }

    [InitializeOnLoad]
    internal static class DialogueKeyUsages
    {
        static DialogueKeyUsages()
        {
            KeyUsages.RegisterSource(Find);
        }

        public static IEnumerable<KeyUsage> Find(string table, string key)
        {
            var found = new List<KeyUsage>();
            var speakers = new HashSet<SpeakerProfile>();
            foreach (DialogueAsset asset in DialogueProject.Dialogues())
            {
                string path = AssetDatabase.GetAssetPath(asset);
                foreach (DialogueNode node in asset.Nodes)
                {
                    switch (node)
                    {
                        case LineNode line:
                            string lineTable = asset.ResolveTable(line.Table);
                            if (lineTable == table && line.Key == key)
                                found.Add(Usage(asset, path, node, "реплика"));
                            for (int i = 0; i < line.Variants.Count; i++)
                            {
                                if (line.Variants[i] != null && lineTable == table && line.Variants[i].Key == key)
                                    found.Add(Usage(asset, path, node, $"вариант реплики {i + 1}"));
                            }

                            if (line.Speaker != null && line.Speaker.NameKey == key && line.Speaker.ResolveNameTable(lineTable) == table)
                                speakers.Add(line.Speaker);
                            break;
                        case ChoiceNode choice:
                            for (int i = 0; i < choice.Options.Count; i++)
                            {
                                ChoiceOption option = choice.Options[i];
                                if (option != null && option.Key == key && asset.ResolveTable(option.Table) == table)
                                    found.Add(Usage(asset, path, node, $"вариант выбора {i + 1}"));
                            }

                            break;
                    }
                }
            }

            foreach (SpeakerProfile speaker in DialogueProject.Speakers())
            {
                if (speaker.NameKey == key && (speakers.Contains(speaker) || speaker.NameTable == table))
                {
                    SpeakerProfile captured = speaker;
                    found.Add(new KeyUsage(AssetDatabase.GetAssetPath(speaker), "Говорящий", $"имя · {speaker.name}", () => DialoguesTab.OpenSpeaker(captured)));
                }
            }

            return found;
        }

        private static KeyUsage Usage(DialogueAsset asset, string path, DialogueNode node, string what)
        {
            string id = node.Id;
            return new KeyUsage(path, "Диалог", $"{DialogueNodeInfo.Title(node)} #{DialogueTexts.ShortId(id)} · {what}", () => DialoguesTab.Open(asset, id));
        }
    }
}
