using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using Velsi.Typecraft.Editor.Window.Effects;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Editor.Window.Targets
{
    [InitializeOnLoad]
    public static class PlayModeEdits
    {
        private const string StorageKey = "Typecraft.PlayModeEdits";
        private const string KeepKey = "Typecraft.KeepPlayModeEdits";

        static PlayModeEdits()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public static bool KeepAfterPlay
        {
            get => EditorPrefs.GetBool(KeepKey, true);
            set => EditorPrefs.SetBool(KeepKey, value);
        }

        public static void Record(TMP_Text text, string markup)
        {
            if (text == null)
                return;

            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(text);
            if (id.identifierType == 0)
            {
                TypecraftLog.Warning($"{text.name} создан в рантайме — правка не переживёт выход из Play Mode");
                return;
            }

            Storage storage = Load();
            storage.Set(id.ToString(), markup);
            Save(storage);
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode)
                return;

            Storage storage = Load();
            SessionState.EraseString(StorageKey);

            if (!KeepAfterPlay || storage.Items.Count == 0)
                return;

            int applied = 0;
            foreach (Storage.Item item in storage.Items)
            {
                if (!GlobalObjectId.TryParse(item.Id, out GlobalObjectId id))
                    continue;

                if (GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) is not TMP_Text target)
                    continue;

                TypecraftText text = target.GetComponent<TypecraftText>();
                if (text == null)
                    text = Undo.AddComponent<TypecraftText>(target.gameObject);

                Undo.RecordObject(text, "Typecraft Play Mode Edit");
                var serialized = new SerializedObject(text);
                serialized.FindProperty("_markup").stringValue = item.Markup;
                serialized.ApplyModifiedProperties();
                EffectComponents.Ensure(target, item.Markup);
                text.Refresh();
                EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
                applied++;
            }

            if (applied > 0)
                TypecraftLog.Info($"Правки из Play Mode сохранены: {applied}");
        }

        private static Storage Load()
        {
            string json = SessionState.GetString(StorageKey, null);
            return string.IsNullOrEmpty(json) ? new Storage() : JsonUtility.FromJson<Storage>(json);
        }

        private static void Save(Storage storage) => SessionState.SetString(StorageKey, JsonUtility.ToJson(storage));

        [Serializable]
        private sealed class Storage
        {
            public List<Item> Items = new List<Item>();

            public void Set(string id, string markup)
            {
                Item existing = Items.Find(item => item.Id == id);
                if (existing != null)
                    existing.Markup = markup;
                else
                    Items.Add(new Item { Id = id, Markup = markup });
            }

            [Serializable]
            public sealed class Item
            {
                public string Id;
                public string Markup;
            }
        }
    }
}
