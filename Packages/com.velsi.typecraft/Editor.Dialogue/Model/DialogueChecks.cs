using System;
using System.Collections.Generic;
using System.Linq;
using Velsi.Typecraft.Editor.Localization;

namespace Velsi.Typecraft.Dialogue.Editor.Model
{
    public static class DialogueChecks
    {
        public static List<DialogueIssue> Run(DialogueAsset asset)
        {
            var issues = new List<DialogueIssue>();
            Run(asset, DialogueProject.MergedCatalog(), issues);
            return issues;
        }

        public static void Run(DialogueAsset asset, DialogueVariableCatalog catalog, List<DialogueIssue> issues)
        {
            DialogueValidator.Validate(asset, catalog, issues);
            if (asset == null)
                return;

            ITableBackend backend = DialogueTexts.Backend;
            var speakers = new HashSet<SpeakerProfile>();
            foreach (DialogueNode node in asset.Nodes)
            {
                switch (node)
                {
                    case LineNode line:
                        string table = asset.ResolveTable(line.Table);
                        CheckKey(backend, table, line.Key, node.Id, issues);
                        CheckEmotion(line.Speaker, line.Emotion, node.Id, issues);
                        foreach (LineVariant variant in line.Variants.Where(variant => variant != null))
                        {
                            CheckKey(backend, table, variant.Key, node.Id, issues);
                            CheckEmotion(line.Speaker, variant.Emotion, node.Id, issues);
                        }

                        if (line.Speaker != null && speakers.Add(line.Speaker))
                            CheckSpeaker(backend, line.Speaker, table, node.Id, issues);
                        break;
                    case ChoiceNode choice:
                        foreach (ChoiceOption option in choice.Options.Where(option => option != null))
                            CheckKey(backend, asset.ResolveTable(option.Table), option.Key, node.Id, issues);
                        break;
                }
            }
        }

        public static int Errors(IEnumerable<DialogueIssue> issues) => issues.Count(issue => issue.Severity == DialogueIssueSeverity.Error);

        public static int Warnings(IEnumerable<DialogueIssue> issues) => issues.Count(issue => issue.Severity == DialogueIssueSeverity.Warning);

        private static void CheckSpeaker(ITableBackend backend, SpeakerProfile speaker, string table, string nodeId, List<DialogueIssue> issues)
        {
            if (string.IsNullOrEmpty(speaker.NameKey))
            {
                issues.Add(new DialogueIssue(DialogueIssueSeverity.Warning, DialogueIssueKind.SpeakerWithoutName, nodeId, $"Speaker '{speaker.name}' has no name key", speaker.name));
                return;
            }

            CheckKey(backend, speaker.ResolveNameTable(table), speaker.NameKey, nodeId, issues);
        }

        private static void CheckEmotion(SpeakerProfile speaker, string emotion, string nodeId, List<DialogueIssue> issues)
        {
            if (speaker == null || string.IsNullOrEmpty(emotion))
                return;

            bool known = speaker.Portraits.Any(portrait => portrait != null && string.Equals(portrait.Emotion, emotion, StringComparison.OrdinalIgnoreCase));
            if (!known)
                issues.Add(new DialogueIssue(DialogueIssueSeverity.Warning, DialogueIssueKind.UnknownEmotion, nodeId, $"Emotion '{emotion}' has no portrait in '{speaker.name}'", emotion, speaker.name));
        }

        private static void CheckKey(ITableBackend backend, string table, string key, string nodeId, List<DialogueIssue> issues)
        {
            if (backend == null || string.IsNullOrEmpty(key))
                return;

            if (!DialogueTexts.HasKey(table, key))
            {
                issues.Add(new DialogueIssue(DialogueIssueSeverity.Error, DialogueIssueKind.MissingKey, nodeId, $"Key '{key}' is not in table '{table}'", key, table));
                return;
            }

            foreach (string locale in backend.Locales)
            {
                string markup = MarkupLayers.Display(backend, table, key, locale);
                if (string.IsNullOrEmpty(markup))
                {
                    issues.Add(new DialogueIssue(DialogueIssueSeverity.Warning, DialogueIssueKind.MissingTranslation, nodeId, $"Key '{key}' has no text in {locale}", key, locale));
                    continue;
                }

                if (MarkupLayers.State(backend, table, key, locale) == LayerState.Stale)
                    issues.Add(new DialogueIssue(DialogueIssueSeverity.Warning, DialogueIssueKind.StaleMarkup, nodeId, $"Markup of '{key}' ({locale}) is stale", key, locale));

                string missing = LocalizationChecks.MissingCharacters(locale, markup);
                if (missing.Length > 0)
                    issues.Add(new DialogueIssue(DialogueIssueSeverity.Warning, DialogueIssueKind.MissingGlyphs, nodeId, $"Font for {locale} lacks '{missing}' used by '{key}'", key, $"{locale}: «{missing}»"));
            }
        }
    }
}
