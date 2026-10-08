using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Velsi.Typecraft.Editor.Art
{
    public static class SpriteAtlasBuilder
    {
        public static Dictionary<string, Sprite> Build(
            IReadOnlyList<string> sourceFiles,
            string atlasAssetPath,
            int padding,
            int maxAtlasSize,
            FilterMode filterMode)
        {
            var textures = new List<Texture2D>();
            var names = new List<string>();
            var sizes = new List<Vector2Int>();

            foreach (string file in sourceFiles)
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (texture.LoadImage(File.ReadAllBytes(file)))
                {
                    texture.name = Path.GetFileNameWithoutExtension(file);
                    textures.Add(texture);
                    names.Add(texture.name);
                    sizes.Add(new Vector2Int(texture.width, texture.height));
                }
                else
                {
                    Object.DestroyImmediate(texture);
                    TypecraftLog.Warning($"Skipped unreadable image: {file}");
                }
            }

            if (textures.Count == 0)
            {
                TypecraftLog.Warning($"No images to pack for {atlasAssetPath}");
                return null;
            }

            SortByDescendingArea(textures, names, sizes);

            var atlas = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            int packPadding = Mathf.Max(0, padding);
            Rect[] rects = atlas.PackTextures(textures.ToArray(), packPadding, maxAtlasSize, false);

            if (rects == null || rects.Length != textures.Count || !FitsWithoutDownscale(atlas, rects, sizes))
            {
                TypecraftLog.Error($"Atlas does not fit into {maxAtlasSize}px without downscaling: {atlasAssetPath}");
                Cleanup(textures, atlas);
                return null;
            }

            ExtrudeEdges(atlas, rects, sizes, packPadding);
            atlas.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(atlasAssetPath)));
            File.WriteAllBytes(Path.GetFullPath(atlasAssetPath), atlas.EncodeToPNG());

            int atlasWidth = atlas.width;
            int atlasHeight = atlas.height;
            Cleanup(textures, atlas);

            AssetDatabase.ImportAsset(atlasAssetPath, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(atlasAssetPath) is not TextureImporter importer)
            {
                TypecraftLog.Error($"No TextureImporter for {atlasAssetPath}");
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = filterMode;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            var spriteRects = new SpriteRect[rects.Length];
            for (int i = 0; i < rects.Length; i++)
            {
                spriteRects[i] = new SpriteRect
                {
                    name = names[i],
                    rect = new Rect(rects[i].x * atlasWidth, rects[i].y * atlasHeight, sizes[i].x, sizes[i].y),
                    alignment = SpriteAlignment.BottomLeft,
                    pivot = Vector2.zero
                };
            }

            dataProvider.SetSpriteRects(spriteRects);
            dataProvider.Apply();
            importer.SaveAndReimport();

            var result = new Dictionary<string, Sprite>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(atlasAssetPath))
            {
                if (asset is Sprite sprite)
                    result[sprite.name] = sprite;
            }

            return result;
        }

        private static bool FitsWithoutDownscale(Texture2D atlas, Rect[] rects, List<Vector2Int> sizes)
        {
            for (int i = 0; i < rects.Length; i++)
            {
                if (Mathf.RoundToInt(rects[i].width * atlas.width) != sizes[i].x)
                    return false;
            }

            return true;
        }

        private static void ExtrudeEdges(Texture2D atlas, Rect[] rects, List<Vector2Int> sizes, int padding)
        {
            int extrude = Mathf.Max(1, padding / 2);
            for (int i = 0; i < rects.Length; i++)
            {
                int x = Mathf.RoundToInt(rects[i].x * atlas.width);
                int y = Mathf.RoundToInt(rects[i].y * atlas.height);
                int w = sizes[i].x;
                int h = sizes[i].y;

                for (int row = 0; row < h; row++)
                {
                    int py = y + row;
                    Color left = atlas.GetPixel(x, py);
                    Color right = atlas.GetPixel(x + w - 1, py);
                    for (int e = 1; e <= extrude; e++)
                    {
                        if (x - e >= 0) atlas.SetPixel(x - e, py, left);
                        if (x + w - 1 + e < atlas.width) atlas.SetPixel(x + w - 1 + e, py, right);
                    }
                }

                for (int col = 0; col < w; col++)
                {
                    int px = x + col;
                    Color bottom = atlas.GetPixel(px, y);
                    Color top = atlas.GetPixel(px, y + h - 1);
                    for (int e = 1; e <= extrude; e++)
                    {
                        if (y - e >= 0) atlas.SetPixel(px, y - e, bottom);
                        if (y + h - 1 + e < atlas.height) atlas.SetPixel(px, y + h - 1 + e, top);
                    }
                }
            }
        }

        private static void SortByDescendingArea(List<Texture2D> textures, List<string> names, List<Vector2Int> sizes)
        {
            int[] order = Enumerable.Range(0, textures.Count)
                .OrderByDescending(i => (long)sizes[i].x * sizes[i].y)
                .ToArray();

            List<Texture2D> sortedTextures = order.Select(i => textures[i]).ToList();
            List<string> sortedNames = order.Select(i => names[i]).ToList();
            List<Vector2Int> sortedSizes = order.Select(i => sizes[i]).ToList();

            textures.Clear();
            textures.AddRange(sortedTextures);
            names.Clear();
            names.AddRange(sortedNames);
            sizes.Clear();
            sizes.AddRange(sortedSizes);
        }

        private static void Cleanup(List<Texture2D> textures, Texture2D atlas)
        {
            foreach (Texture2D texture in textures)
            {
                if (texture != null)
                    Object.DestroyImmediate(texture);
            }

            if (atlas != null)
                Object.DestroyImmediate(atlas);
        }
    }
}
