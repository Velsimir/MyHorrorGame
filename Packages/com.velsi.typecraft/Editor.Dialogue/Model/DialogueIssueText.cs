namespace Velsi.Typecraft.Dialogue.Editor.Model
{
    public static class DialogueIssueText
    {
        public static string Describe(DialogueIssue issue)
        {
            string subject = issue.Subject ?? "?";
            string detail = issue.Detail ?? "?";
            return issue.Kind switch
            {
                DialogueIssueKind.NoStartNode => issue.Subject == null ? "Нет стартовой ноды" : $"Стартовая нода {DialogueTexts.ShortId(subject)} не существует",
                DialogueIssueKind.MissingNodeType => "Нода без типа: класс удалён или переименован",
                DialogueIssueKind.EmptyNodeId => "У ноды нет id",
                DialogueIssueKind.DuplicateNodeId => $"Повтор id ноды {DialogueTexts.ShortId(subject)}",
                DialogueIssueKind.EmptyLink => $"Выход «{Port(subject)}» никуда не ведёт",
                DialogueIssueKind.BrokenLink => $"Выход «{Port(subject)}» ведёт на удалённую ноду",
                DialogueIssueKind.Unreachable => "Нода недостижима от старта",
                DialogueIssueKind.EmptyKey => subject switch
                {
                    "Variant" => $"У варианта {detail} нет ключа — он пропускается",
                    "Option" => $"У варианта выбора {detail} нет ключа",
                    _ => "У реплики нет ключа"
                },
                DialogueIssueKind.NoOptions => "У выбора нет вариантов",
                DialogueIssueKind.EmptyVariable => "Пустое имя флага",
                DialogueIssueKind.UndeclaredVariable => $"Флаг «{subject}» не объявлен в каталоге",
                DialogueIssueKind.VariableTypeMismatch => $"Флаг «{subject}»: тип в каталоге и в диалоге расходится ({detail})",
                DialogueIssueKind.EmptyEventName => "У события нет имени",
                DialogueIssueKind.BrokenJump => issue.Detail == null ? $"У диалога перехода «{subject}» нет стартовой ноды" : $"В диалоге «{subject}» нет ноды {DialogueTexts.ShortId(detail)}",
                DialogueIssueKind.MissingKey => $"Ключа «{subject}» нет в таблице «{detail}»",
                DialogueIssueKind.MissingTranslation => $"«{subject}»: нет перевода на {detail}",
                DialogueIssueKind.StaleMarkup => $"«{subject}» ({detail}): строка в таблице изменилась, разметка устарела",
                DialogueIssueKind.MissingGlyphs => $"«{subject}»: в шрифте нет символов ({detail})",
                DialogueIssueKind.SpeakerWithoutName => $"У говорящего «{subject}» не задан ключ имени",
                DialogueIssueKind.UnknownEmotion => $"Эмоции «{subject}» нет в портретах говорящего «{detail}»",
                _ => issue.Message
            };
        }

        public static string Port(string port)
        {
            if (port == DialogueNodeInfo.NextPort)
                return "Далее";
            if (port == DialogueNodeInfo.ElsePort)
                return "Иначе";
            if (DialogueNodeInfo.TryIndex(port, DialogueNodeInfo.OptionPort, out int option))
                return "вариант " + (option + 1);
            if (DialogueNodeInfo.TryIndex(port, DialogueNodeInfo.CasePort, out int index))
                return "условие " + (index + 1);
            return port;
        }

        public static string Mark(DialogueIssueSeverity severity) => severity == DialogueIssueSeverity.Error ? "✖" : severity == DialogueIssueSeverity.Warning ? "⚠" : "•";
    }
}
