using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

//import helper for the generated art. AI sprite sheets come out on a roughly-even grid, so instead of
//trusting a fixed cell size this finds the rows by projecting alpha onto y, then the frames in each row
//by projecting onto x. every frame becomes a sprite named <sheet>_<n> in reading order.
//call from code or the menu (Over The Hedgehog > Art > Reslice selected sheets)
public static class SpriteSheetImporter
{
    public struct Frame
    {
        public RectInt rect; //in texture pixels, origin bottom-left (Unity convention)
    }

    //border = 9-slice margins in pixels (left, bottom, right, top), for UI panels and buttons
    public static void ImportSingle(string assetPath, float pixelsPerUnit, Vector2? pivot = null, bool fullRect = false, Vector4? border = null)
    {
        var importer = Prepare(assetPath, pixelsPerUnit, fullRect);
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePivot = pivot ?? new Vector2(0.5f, 0.5f);
        importer.spriteBorder = border ?? Vector4.zero;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot ?? new Vector2(0.5f, 0.5f);
        settings.spriteBorder = border ?? Vector4.zero;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    //fixed grid, for sheets where the frames are scattered (particle bursts) and band detection can't help
    public static int ImportGrid(string assetPath, float pixelsPerUnit, int columns, int rows, bool skipEmpty = true, Vector2? pivot = null)
    {
        var tex = LoadReadable(assetPath);
        int cw = tex.width / columns, ch = tex.height / rows;
        var frames = new List<Frame>();
        var pixels = tex.GetPixels32();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                //reading order: top row first, Unity's y is bottom-up
                var rect = new RectInt(c * cw, tex.height - (r + 1) * ch, cw, ch);
                if (skipEmpty && !HasAlpha(pixels, tex.width, rect)) continue;
                frames.Add(new Frame { rect = rect });
            }
        }
        Object.DestroyImmediate(tex);
        return Apply(assetPath, pixelsPerUnit, frames, pivot);
    }

    //band detection. rows/cols are hints: 0 = detect. mergeWiderThan splits a band that's obviously two
    //touching frames (wider than this multiple of the median band width) down the middle
    public static int ImportBands(string assetPath, float pixelsPerUnit, int alphaThreshold = 24, int minGap = 4, float splitWiderThan = 1.6f, Vector2? pivot = null)
    {
        var tex = LoadReadable(assetPath);
        var pixels = tex.GetPixels32();
        int w = tex.width, h = tex.height;

        //row bands (in Unity y, bottom-up)
        var rowOcc = new int[h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (pixels[y * w + x].a > alphaThreshold) rowOcc[y]++;
        var rowBands = Bands(rowOcc, minGap, 8);

        var frames = new List<Frame>();
        //top row first for reading order
        for (int i = rowBands.Count - 1; i >= 0; i--)
        {
            var (y0, y1) = rowBands[i];
            var colOcc = new int[w];
            for (int x = 0; x < w; x++)
                for (int y = y0; y <= y1; y++)
                    if (pixels[y * w + x].a > alphaThreshold) { colOcc[x]++; }
            var colBands = Bands(colOcc, minGap, 8);
            colBands = SplitWide(colBands, splitWiderThan);

            foreach (var (x0, x1) in colBands)
                frames.Add(new Frame { rect = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1) });
        }

        Object.DestroyImmediate(tex);
        return Apply(assetPath, pixelsPerUnit, frames, pivot);
    }

    static List<(int, int)> Bands(int[] occupancy, int minGap, int minSize)
    {
        var bands = new List<(int, int)>();
        int start = -1, gap = 0;
        for (int i = 0; i < occupancy.Length; i++)
        {
            if (occupancy[i] > 0)
            {
                if (start < 0) start = i;
                gap = 0;
            }
            else if (start >= 0)
            {
                gap++;
                if (gap >= minGap)
                {
                    int end = i - gap;
                    if (end - start + 1 >= minSize) bands.Add((start, end));
                    start = -1; gap = 0;
                }
            }
        }
        if (start >= 0)
        {
            int end = occupancy.Length - 1 - gap;
            if (end - start + 1 >= minSize) bands.Add((start, end));
        }
        return bands;
    }

    static List<(int, int)> SplitWide(List<(int, int)> bands, float factor)
    {
        if (bands.Count < 2) return bands;
        var widths = new List<int>();
        foreach (var (a, b) in bands) widths.Add(b - a + 1);
        widths.Sort();
        int median = widths[widths.Count / 2];

        var result = new List<(int, int)>();
        foreach (var (a, b) in bands)
        {
            int width = b - a + 1;
            if (width > median * factor)
            {
                int parts = Mathf.RoundToInt(width / (float)median);
                for (int p = 0; p < parts; p++)
                    result.Add((a + width * p / parts, a + width * (p + 1) / parts - 1));
            }
            else result.Add((a, b));
        }
        return result;
    }

    static bool HasAlpha(Color32[] pixels, int width, RectInt rect)
    {
        for (int y = rect.yMin; y < rect.yMax; y++)
            for (int x = rect.xMin; x < rect.xMax; x++)
                if (pixels[y * width + x].a > 24) return true;
        return false;
    }

    static Texture2D LoadReadable(string assetPath)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(assetPath));
        return tex;
    }

    static TextureImporter Prepare(string assetPath, float pixelsPerUnit, bool fullRect)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) throw new System.Exception("Not a texture: " + assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = fullRect ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
        importer.SetTextureSettings(settings);
        return importer;
    }

    static int Apply(string assetPath, float pixelsPerUnit, List<Frame> frames, Vector2? pivot)
    {
        var importer = Prepare(assetPath, pixelsPerUnit, false);
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        string baseName = Path.GetFileNameWithoutExtension(assetPath);
        var rects = new List<SpriteRect>();
        for (int i = 0; i < frames.Count; i++)
        {
            var r = frames[i].rect;
            rects.Add(new SpriteRect
            {
                name = $"{baseName}_{i}",
                spriteID = GUID.Generate(),
                rect = new Rect(r.x, r.y, r.width, r.height),
                alignment = SpriteAlignment.Custom,
                pivot = pivot ?? new Vector2(0.5f, 0.5f),
            });
        }
        provider.SetSpriteRects(rects.ToArray());

        //keep the sprite names stable across reimports
        var nameIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameIds != null)
        {
            var pairs = new List<SpriteNameFileIdPair>();
            foreach (var rect in rects) pairs.Add(new SpriteNameFileIdPair(rect.name, rect.spriteID));
            nameIds.SetNameFileIdPairs(pairs);
        }

        provider.Apply();
        importer.SaveAndReimport();
        Debug.Log($"SpriteSheetImporter: {assetPath} -> {frames.Count} frames");
        return frames.Count;
    }

    public static Sprite[] LoadSprites(string assetPath)
    {
        var list = new List<Sprite>();
        foreach (var obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
            if (obj is Sprite s) list.Add(s);
        //sort by the _n suffix
        list.Sort((a, b) => Index(a.name).CompareTo(Index(b.name)));
        return list.ToArray();
    }

    static int Index(string name)
    {
        int us = name.LastIndexOf('_');
        return us >= 0 && int.TryParse(name.Substring(us + 1), out int n) ? n : 0;
    }

    [MenuItem("Over The Hedgehog/Art/Reslice selected sheets (bands)")]
    static void ResliceSelected()
    {
        foreach (var obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (path.EndsWith(".png"))
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                ImportBands(path, importer != null ? importer.spritePixelsToUnits : 100f);
            }
        }
    }
}
