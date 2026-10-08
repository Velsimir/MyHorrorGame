using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue.Editor.Board
{
    public static class DialogueBoards
    {
        private static List<Type> _types;

        public static IReadOnlyList<Type> Types => _types ??= TypeCache.GetTypesDerivedFrom<IDialogueBoardView>()
            .Where(type => !type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null)
            .OrderByDescending(Priority)
            .ThenBy(type => type.Name)
            .ToList();

        public static IDialogueBoardView Create(Type preferred = null)
        {
            IEnumerable<Type> candidates = preferred != null && Types.Contains(preferred) ? new[] { preferred }.Concat(Types) : Types;
            foreach (Type type in candidates)
            {
                try
                {
                    return (IDialogueBoardView)Activator.CreateInstance(type);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[Typecraft] Вид доски {type.Name} не создан, беру следующий: {exception.GetBaseException().Message}");
                }
            }

            return new DialogueListBoard();
        }

        public static string Title(Type type) => type.GetCustomAttribute<DialogueBoardPriorityAttribute>()?.Title ?? ObjectNames.NicifyVariableName(type.Name);

        private static int Priority(Type type) => type.GetCustomAttribute<DialogueBoardPriorityAttribute>()?.Priority ?? 0;
    }
}
