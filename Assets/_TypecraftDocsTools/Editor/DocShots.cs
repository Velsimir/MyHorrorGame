using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class DocShots
{
    public const string OutDir = "C:/Users/ninte/AppData/Local/Temp/claude/C--Projects-HomeUnity-MyHorrorGame-Assets/3600855b-c6bf-477d-9181-7519cca9c6e8/scratchpad/shots/";
    private const BindingFlags All = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    public static EditorWindow Win(string typeName = "TypecraftWindow") =>
        Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(x => x.GetType().Name == typeName);

    public static void Resize(EditorWindow w, float width, float height)
    {
        w.position = new Rect(0f, 26f, width, height);
    }

    public static string Shot(string name, EditorWindow w = null, int cropTop = 0)
    {
        w = w ?? Win();
        Directory.CreateDirectory(OutDir);
        w.Repaint();
        object view = typeof(EditorWindow).GetField("m_Parent", All).GetValue(w);
        Type guiView = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GUIView");
        MethodInfo immediate = guiView.GetMethod("RepaintImmediately", All);
        if (immediate != null)
            immediate.Invoke(view, null);
        Rect pos = (Rect)guiView.GetProperty("position", All).GetValue(view, null);
        float ppp = EditorGUIUtility.pixelsPerPoint;
        int width = Mathf.RoundToInt(pos.width * ppp);
        int height = Mathf.RoundToInt(pos.height * ppp);
        MethodInfo grab = guiView.GetMethod("GrabPixels", All, null, new[] { typeof(RenderTexture), typeof(Rect) }, null);
        var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        grab.Invoke(view, new object[] { rt, new Rect(0f, 0f, pos.width * ppp, pos.height * ppp) });
        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(rt);

        Color32[] src = tex.GetPixels32();
        int cropped = height - cropTop;
        var dst = new Color32[width * cropped];
        for (int y = 0; y < cropped; y++)
            Array.Copy(src, (height - 1 - y) * width, dst, y * width, width);
        var outTex = new Texture2D(width, cropped, TextureFormat.RGB24, false);
        outTex.SetPixels32(dst);
        outTex.Apply();
        string path = OutDir + name + ".png";
        File.WriteAllBytes(path, outTex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(outTex);
        return path + " " + width + "x" + cropped;
    }

    public static string Dump(VisualElement root, int maxDepth = 8, bool onlyNamedOrText = true)
    {
        var sb = new StringBuilder();
        Walk(root, 0, maxDepth, sb, onlyNamedOrText);
        return sb.ToString();
    }

    private static void Walk(VisualElement e, int depth, int maxDepth, StringBuilder sb, bool onlyNamedOrText)
    {
        if (depth > maxDepth)
            return;
        string text = (e as TextElement)?.text;
        bool interesting = !onlyNamedOrText || !string.IsNullOrEmpty(e.name) || !string.IsNullOrEmpty(text) || e is Button || e is Toggle || e is TextField || e is DropdownField;
        if (interesting)
        {
            sb.Append(' ', depth * 2).Append(e.GetType().Name);
            if (!string.IsNullOrEmpty(e.name)) sb.Append(" #").Append(e.name);
            if (!string.IsNullOrEmpty(text)) sb.Append(" '").Append(text.Replace("\n", " ")).Append('\'');
            if (e.tooltip != null && e.tooltip.Length > 0) sb.Append(" tip='").Append(e.tooltip).Append('\'');
            sb.Append(" [").Append(string.Join(".", e.GetClasses().ToArray())).Append(']');
            if (e.resolvedStyle.display == DisplayStyle.None) sb.Append(" (hidden)");
            sb.AppendLine();
        }
        foreach (VisualElement child in e.hierarchy.Children())
            Walk(child, depth + 1, maxDepth, sb, onlyNamedOrText);
    }

    public static List<VisualElement> Find(VisualElement root, Func<VisualElement, bool> predicate)
    {
        var list = new List<VisualElement>();
        Collect(root, predicate, list);
        return list;
    }

    private static void Collect(VisualElement e, Func<VisualElement, bool> predicate, List<VisualElement> list)
    {
        if (predicate(e))
            list.Add(e);
        foreach (VisualElement child in e.hierarchy.Children())
            Collect(child, predicate, list);
    }

    public static VisualElement ByText(VisualElement root, string text, bool exact = true) =>
        Find(root, e => e is TextElement t && e.resolvedStyle.display != DisplayStyle.None && (exact ? t.text == text : t.text != null && t.text.Contains(text))).FirstOrDefault();

    public static bool Click(VisualElement element)
    {
        if (element == null)
            return false;
        VisualElement target = element;
        while (target != null && !(target is Button) && !target.ClassListContains("tab"))
            target = target.parent;
        target = target ?? element;
        var clickable = (target as Button)?.clickable;
        if (clickable != null)
        {
            MethodInfo sim = clickable.GetType().GetMethod("SimulateSingleClick", All);
            if (sim != null)
            {
                sim.Invoke(clickable, new object[] { null, 0 });
                return true;
            }
        }
        using (var ev = NavigationSubmitEvent.GetPooled())
        {
            ev.target = target;
            target.SendEvent(ev);
        }
        return true;
    }

    public static string ClickText(string text, bool exact = true, EditorWindow w = null)
    {
        w = w ?? Win();
        VisualElement e = ByText(w.rootVisualElement, text, exact);
        if (e == null)
            return "NOT FOUND: " + text;
        Click(e);
        return "clicked " + text;
    }

    public static void Pointer(VisualElement target, int button = 0, int clicks = 1)
    {
        Rect r = target.worldBound;
        var legacy = new Event { type = EventType.MouseDown, button = button, mousePosition = r.center, clickCount = clicks };
        using (var down = PointerDownEvent.GetPooled(legacy))
        {
            down.target = target;
            target.SendEvent(down);
        }
        var up = new Event { type = EventType.MouseUp, button = button, mousePosition = r.center, clickCount = clicks };
        using (var upEvent = PointerUpEvent.GetPooled(up))
        {
            upEvent.target = target;
            target.SendEvent(upEvent);
        }
    }

    public static string PointerText(string text, bool exact = true, int button = 0, int clicks = 1, EditorWindow w = null)
    {
        w = w ?? Win();
        VisualElement e = ByText(w.rootVisualElement, text, exact);
        if (e == null)
            return "NOT FOUND: " + text;
        Pointer(e, button, clicks);
        return "pointer " + text;
    }

    public static EditorWindow Modeless(string typeName, string buildName, object[] args, string title, float width, float height)
    {
        Type type = typeof(UnityEditor.Editor).Assembly.GetType(typeName)
            ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(typeName)).FirstOrDefault(t => t != null);
        var window = (EditorWindow)ScriptableObject.CreateInstance(type);
        window.titleContent = new GUIContent(title);
        if (buildName != null)
            type.GetMethod(buildName, All).Invoke(window, args);
        Rect main = EditorGUIUtility.GetMainWindowPosition();
        window.position = new Rect(main.center.x - width / 2f, main.center.y - height / 2f, width, height);
        window.Show();
        return window;
    }

    public static string DumpWin(int depth = 8, bool onlyNamedOrText = true, EditorWindow w = null)
    {
        w = w ?? Win();
        return Dump(w.rootVisualElement, depth, onlyNamedOrText);
    }

    public static void ClickEvent(VisualElement target)
    {
        using (var ev = UnityEngine.UIElements.ClickEvent.GetPooled())
        {
            ev.target = target;
            target.SendEvent(ev);
        }
    }
}
