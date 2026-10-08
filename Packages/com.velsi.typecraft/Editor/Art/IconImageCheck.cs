using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Velsi.Typecraft.Editor.Art
{
    public static class IconImageCheck
    {
        public const int StandardSize = 128;
        public const int StandardMargin = 12;
        private const float AlphaThreshold = 0.05f;

        public static Texture2D Load(string file)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            if (texture.LoadImage(File.ReadAllBytes(file)))
            {
                texture.name = Path.GetFileNameWithoutExtension(file);
                return texture;
            }

            Object.DestroyImmediate(texture);
            return null;
        }

        public static Vector2Int? PrevailingSize(IEnumerable<string> files)
        {
            var counts = new Dictionary<Vector2Int, int>();
            foreach (string file in files)
            {
                Vector2Int? size = SizeOf(file);
                if (size.HasValue)
                    counts[size.Value] = counts.TryGetValue(size.Value, out int count) ? count + 1 : 1;
            }

            return counts.Count == 0 ? (Vector2Int?)null : counts.OrderByDescending(pair => pair.Value).First().Key;
        }

        public static List<string> Inspect(Texture2D texture, Vector2Int? prevailing)
        {
            var problems = new List<string>();
            int width = texture.width;
            int height = texture.height;

            if (width != height)
                problems.Add($"не квадрат: {width}×{height}");

            if (prevailing.HasValue && prevailing.Value != new Vector2Int(width, height))
                problems.Add($"размер {width}×{height} отличается от остальных иконок набора ({prevailing.Value.x}×{prevailing.Value.y})");

            int marginX = MarginFor(width);
            int marginY = MarginFor(height);
            if (HasOpaqueInMargin(texture, marginX, marginY))
                problems.Add($"рисунок заходит в поля: нужно {marginX} px пустоты с каждой стороны (12 px на холсте 128)");

            return problems;
        }

        public static void Report(IEnumerable<string> files, string setName)
        {
            List<string> list = files.ToList();
            Vector2Int? prevailing = PrevailingSize(list);
            var lines = new List<string>();

            foreach (string file in list)
            {
                Texture2D texture = Load(file);
                if (texture == null)
                    continue;

                List<string> problems = Inspect(texture, prevailing);
                Object.DestroyImmediate(texture);

                if (problems.Count > 0)
                    lines.Add($"{Path.GetFileName(file)}: {string.Join("; ", problems)}");
            }

            if (lines.Count == 0)
                return;

            const int shown = 10;
            string more = lines.Count > shown ? $"\n… и ещё {lines.Count - shown}" : string.Empty;
            TypecraftLog.Warning($"{setName}: {lines.Count} картинок могут некорректно отображаться в тексте\n{string.Join("\n", lines.Take(shown))}{more}");
        }

        private static int MarginFor(int size) => Mathf.RoundToInt(size * (float)StandardMargin / StandardSize);

        private static bool HasOpaqueInMargin(Texture2D texture, int marginX, int marginY)
        {
            Color32[] pixels = texture.GetPixels32();
            int width = texture.width;
            int height = texture.height;
            byte threshold = (byte)Mathf.CeilToInt(AlphaThreshold * 255f);

            for (int y = 0; y < height; y++)
            {
                bool rowInMargin = y < marginY || y >= height - marginY;
                for (int x = 0; x < width; x++)
                {
                    if (!rowInMargin && x >= marginX && x < width - marginX)
                    {
                        x = width - marginX - 1;
                        continue;
                    }

                    if (pixels[y * width + x].a > threshold)
                        return true;
                }
            }

            return false;
        }

        private static Vector2Int? SizeOf(string file)
        {
            using (FileStream stream = File.OpenRead(file))
            {
                var header = new byte[24];
                if (stream.Read(header, 0, header.Length) < header.Length || header[12] != 'I' || header[13] != 'H' || header[14] != 'D' || header[15] != 'R')
                    return null;

                return new Vector2Int(BigEndian(header, 16), BigEndian(header, 20));
            }
        }

        private static int BigEndian(byte[] bytes, int offset) =>
            bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];
    }
}
