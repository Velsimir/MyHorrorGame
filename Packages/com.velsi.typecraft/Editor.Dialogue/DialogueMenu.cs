using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Velsi.Typecraft.Text;
using Object = UnityEngine.Object;
#if TYPECRAFT_TMPEFFECTS
using TMPEffects.Components;
using Velsi.Typecraft.Editor.Window.Effects;
#endif

namespace Velsi.Typecraft.Dialogue.Editor
{
    public static class DialogueMenu
    {
        private const int Priority = 30;
        private const int UiLayer = 5;

        [MenuItem("GameObject/Typecraft/Dialogue Box", false, Priority)]
        private static void DialogueBoxMenu(MenuCommand command) => Select(CreateDialogueBox(command.context as GameObject), "Create Dialogue Box");

        [MenuItem("GameObject/Typecraft/Subtitles", false, Priority + 1)]
        private static void SubtitlesMenu(MenuCommand command) => Select(CreateSubtitles(command.context as GameObject), "Create Subtitles");

        [MenuItem("GameObject/Typecraft/World Text", false, Priority + 2)]
        private static void WorldTextMenu(MenuCommand command) => Select(CreateWorldText(command.context as GameObject), "Create World Text");

        public static GameObject CreateDialogueBox(GameObject context)
        {
            Transform parent = CanvasParent(context);

            GameObject box = Ui("Dialogue Box", parent, typeof(Image), typeof(CanvasGroup), typeof(DialogueBoxPresenter));
            Place(box, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(-160f, 260f));
            box.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

            Image portrait = CreatePortrait("Portrait", box.transform, new Vector2(0f, 0.5f), new Vector2(24f, 0f));
            Image portraitRight = CreatePortrait("Portrait Right", box.transform, new Vector2(1f, 0.5f), new Vector2(-24f, 0f));

            TypecraftText nameText = CreateUiText("Name", box.transform, 32f, TextAlignmentOptions.BottomLeft, writer: false);
            Stretch(nameText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(250f, -60f), new Vector2(-250f, -12f));
            nameText.SetMarkup("Name");

            TypecraftText lineText = CreateUiText("Text", box.transform, 36f, TextAlignmentOptions.TopLeft, writer: true);
            Stretch(lineText.gameObject, Vector2.zero, Vector2.one, new Vector2(250f, 24f), new Vector2(-250f, -68f));
            lineText.SetMarkup("Dialogue text");

            GameObject next = Ui("Next", box.transform, typeof(TextMeshProUGUI));
            Place(next, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 16f), new Vector2(48f, 48f));
            var nextText = next.GetComponent<TextMeshProUGUI>();
            nextText.text = ">";
            nextText.fontSize = 36f;
            nextText.alignment = TextAlignmentOptions.Center;
            next.SetActive(false);

            GameObject choices = Ui("Choices", box.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            Place(choices, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(800f, 0f));
            var layout = choices.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            choices.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Button template = CreateChoiceTemplate(choices.transform);
            choices.SetActive(false);

            var presenter = new SerializedObject(box.GetComponent<DialogueBoxPresenter>());
            presenter.FindProperty("_root").objectReferenceValue = box.GetComponent<CanvasGroup>();
            presenter.FindProperty("_text").objectReferenceValue = lineText;
            presenter.FindProperty("_nameText").objectReferenceValue = nameText;
            presenter.FindProperty("_portrait").objectReferenceValue = portrait;
            presenter.FindProperty("_portraitRight").objectReferenceValue = portraitRight;
            presenter.FindProperty("_nextIndicator").objectReferenceValue = next;
            presenter.FindProperty("_choiceContainer").objectReferenceValue = choices.GetComponent<RectTransform>();
            presenter.FindProperty("_choicePrefab").objectReferenceValue = template;
            presenter.FindProperty("_hideAfter").floatValue = 0f;
            presenter.ApplyModifiedPropertiesWithoutUndo();

            EnsureEventSystem(box.scene);
            return box;
        }

        public static GameObject CreateSubtitles(GameObject context)
        {
            Transform parent = CanvasParent(context);

            GameObject root = Ui("Subtitles", parent, typeof(CanvasGroup), typeof(SubtitlePresenter));
            Place(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(1400f, 160f));

            TypecraftText text = CreateUiText("Text", root.transform, 36f, TextAlignmentOptions.Bottom, writer: true);
            Stretch(text.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.SetMarkup("Subtitle");

            var presenter = new SerializedObject(root.GetComponent<SubtitlePresenter>());
            presenter.FindProperty("_root").objectReferenceValue = root.GetComponent<CanvasGroup>();
            presenter.FindProperty("_text").objectReferenceValue = text;
            presenter.FindProperty("_hideAfter").floatValue = 2f;
            presenter.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        public static GameObject CreateWorldText(GameObject context)
        {
            var root = new GameObject("World Text");
            if (context != null)
                GameObjectUtility.SetParentAndAlign(root, context);
            else
                MoveToScene(root, SceneManager.GetActiveScene());

            var tmp = root.AddComponent<TextMeshPro>();
            tmp.fontSize = 3f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(4f, 1.5f);

            TypecraftText text = root.AddComponent<TypecraftText>();
            AddWriter(tmp);
            text.SetMarkup("World text");

            var presenter = new SerializedObject(root.AddComponent<WorldTextPresenter>());
            presenter.FindProperty("_root").objectReferenceValue = null;
            presenter.FindProperty("_text").objectReferenceValue = text;
            presenter.FindProperty("_hideAfter").floatValue = 3f;
            presenter.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static void Select(GameObject created, string undoName)
        {
            if (created == null)
                return;

            Undo.RegisterCreatedObjectUndo(created, undoName);
            Selection.activeGameObject = created;
        }

        private static Image CreatePortrait(string name, Transform parent, Vector2 anchor, Vector2 position)
        {
            GameObject portrait = Ui(name, parent, typeof(Image));
            Place(portrait, anchor, anchor, anchor, position, new Vector2(200f, 200f));
            var image = portrait.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            portrait.SetActive(false);
            return image;
        }

        private static Button CreateChoiceTemplate(Transform parent)
        {
            GameObject choice = Ui("Choice", parent, typeof(Image), typeof(Button), typeof(LayoutElement));
            choice.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            choice.GetComponent<LayoutElement>().minHeight = 56f;

            var button = choice.GetComponent<Button>();
            button.targetGraphic = choice.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.selectedColor = new Color(1f, 0.85f, 0.5f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            TypecraftText label = CreateUiText("Label", choice.transform, 30f, TextAlignmentOptions.MidlineLeft, writer: false);
            Stretch(label.gameObject, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, 0f));
            label.SetMarkup("Choice");
            label.Target.raycastTarget = false;

            choice.SetActive(false);
            return button;
        }

        private static TypecraftText CreateUiText(string name, Transform parent, float size, TextAlignmentOptions alignment, bool writer)
        {
            GameObject host = Ui(name, parent, typeof(TextMeshProUGUI));
            var tmp = host.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;

            TypecraftText text = host.AddComponent<TypecraftText>();
            if (writer)
                AddWriter(tmp);
            return text;
        }

        private static void AddWriter(TMP_Text text)
        {
#if TYPECRAFT_TMPEFFECTS
            var writer = text.gameObject.AddComponent<TMPWriter>();
            if (Application.isPlaying)
            {
                writer.WriteOnStart = true;
                writer.WriteOnNewText = true;
                writer.SetDatabase(EffectCatalog.DefaultCommandDatabase);
                return;
            }

            var serialized = new SerializedObject(writer);
            serialized.FindProperty("database").objectReferenceValue = EffectCatalog.DefaultCommandDatabase;
            serialized.FindProperty("writeOnStart").boolValue = true;
            serialized.FindProperty("writeOnNewText").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // TMPWriter.Reset в Edit Mode выключает компонент и включает его через delayCall, который может не дойти.
            writer.enabled = true;
#endif
        }

        private static GameObject Ui(string name, Transform parent, params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : UiLayer;
            go.transform.SetParent(parent, false);
            foreach (Type component in components)
                go.AddComponent(component);
            return go;
        }

        private static void Place(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Transform CanvasParent(GameObject context)
        {
            if (context != null)
            {
                Canvas owner = context.GetComponentInParent<Canvas>(true);
                if (owner != null && owner.rootCanvas.renderMode != RenderMode.WorldSpace)
                    return context.transform;
            }

            Scene scene = context != null ? context.scene : SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Canvas canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas != null && canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
                    return canvas.transform;
            }

            return CreateCanvas(scene).transform;
        }

        private static Canvas CreateCanvas(Scene scene)
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = UiLayer;
            MoveToScene(go, scene);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
            return canvas;
        }

        private static void EnsureEventSystem(Scene scene)
        {
            if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
                return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
#if TYPECRAFT_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            MoveToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        }

        private static void MoveToScene(GameObject go, Scene scene)
        {
            if (scene.IsValid() && go.scene != scene)
                SceneManager.MoveGameObjectToScene(go, scene);
        }
    }
}
