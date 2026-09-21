using UnityEditor;
using UnityEngine;

//wooden skins for the block sets. a block is a root object scaled to its size with a 1x1 box collider,
//so the sprite goes on a counter-scaled GFX child sized to exactly what collides. beams and posts are
//separate sprites (so the upper-left light stays upper-left and the grain runs the right way), each
//trimmed to its painted pixels and 9-sliced along its long axis only: the GFX child is scaled uniformly
//so the sprite's thickness matches the block's, then the plain middle stretches to the block's length
//while the end caps (worn ends, nails, the daisy) keep their proportions
public static partial class ContentBuilder
{
    const string BlockArtDir = "Assets/Art/Blocks";
    //which art sets the block sets use, cycled per block so a stacked structure alternates and doesn't read
    //as one slab. sets live in Assets/Art/Blocks (or Assets/Art/Generated/Blocks while still an option) as
    //<Set>_Beam_H.png + <Set>_Post_V.png; OakLight is Oak run through Tools/ArtDirector/variant.mjs
    public static string[] BlockArtSets = { "Oak", "OakLight" };
    //share of the long axis that is a fixed end cap: all the character lives there, the rest is stretch-safe
    const float BlockCapFraction = 0.25f;

    public struct BlockArt
    {
        public Sprite beam, post, crate;
        public Material material;
    }

    static string BlockArtPath(string set, string kind)
    {
        string promoted = $"{BlockArtDir}/{set}_{kind}.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(promoted) != null) return promoted;
        return $"Assets/Art/Generated/Blocks/{set}_{kind}.png";
    }

    //import a trimmed beam/post so its sliced borders sit on the long axis
    static Sprite ImportBlockSprite(string path, bool horizontal)
    {
        var tex = LoadOrThrow<Texture2D>(path);
        float cap = (horizontal ? tex.width : tex.height) * BlockCapFraction;
        var border = horizontal ? new Vector4(cap, 0f, cap, 0f) : new Vector4(0f, cap, 0f, cap);
        SpriteSheetImporter.ImportSingle(path, 100f, null, true, border);
        return LoadOrThrow<Sprite>(path);
    }

    public static BlockArt ImportBlockArt(string set)
    {
        return new BlockArt
        {
            beam = ImportBlockSprite(BlockArtPath(set, "Beam_H"), true),
            post = ImportBlockSprite(BlockArtPath(set, "Post_V"), false),
            crate = LoadOrThrow<Sprite>("Assets/Art/Environment/Static/Box_Texture.png"),
            //same lit sprite material the rest of the playfield uses
            material = LoadOrThrow<GameObject>($"{PrefabDir}/Hedgehog_01.prefab").GetComponentInChildren<SpriteRenderer>().sharedMaterial,
        };
    }

    static void SkinBlock(GameObject block, BlockArt art)
    {
        var box = block.GetComponent<BoxCollider2D>();
        if (box == null) return;

        //what the collider actually covers, in the block's own space
        Vector3 scale = block.transform.localScale;
        float sx = Mathf.Max(0.0001f, Mathf.Abs(scale.x)), sy = Mathf.Max(0.0001f, Mathf.Abs(scale.y));
        float w = sx * box.size.x;
        float h = sy * box.size.y;

        //the old look was the renderer on the root, sized by hand. it goes
        var oldSr = block.GetComponent<SpriteRenderer>();
        int order = oldSr != null ? oldSr.sortingOrder : 0;
        if (oldSr != null) Object.DestroyImmediate(oldSr);

        var gfxT = block.transform.Find("GFX");
        var gfx = gfxT != null ? gfxT.gameObject : new GameObject("GFX");
        gfx.transform.SetParent(block.transform, false);
        gfx.transform.localPosition = (Vector3)box.offset;
        gfx.transform.localRotation = Quaternion.identity;

        var sr = gfx.GetComponent<SpriteRenderer>();
        if (sr == null) sr = gfx.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = art.material;
        sr.sortingOrder = order;
        sr.color = Color.white;

        float aspect = w / Mathf.Max(0.0001f, h);
        if (aspect > 1.35f)
        {
            //beam: uniform scale so the sprite is as thick as the block, then stretch the middle to its length
            float k = h / art.beam.bounds.size.y;
            sr.sprite = art.beam;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(w / k, art.beam.bounds.size.y);
            gfx.transform.localScale = new Vector3(k / sx, k / sy, 1f);
        }
        else if (aspect < 1f / 1.35f)
        {
            float k = w / art.post.bounds.size.x;
            sr.sprite = art.post;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(art.post.bounds.size.x, h / k);
            gfx.transform.localScale = new Vector3(k / sx, k / sy, 1f);
        }
        else
        {
            //crate texture is square and opaque: just fit it
            sr.sprite = art.crate;
            sr.drawMode = SpriteDrawMode.Simple;
            Vector3 s = art.crate.bounds.size;
            gfx.transform.localScale = new Vector3(w / s.x / sx, h / s.y / sy, 1f);
        }
    }

    public static void SkinBlockSets()
    {
        var arts = new BlockArt[BlockArtSets.Length];
        for (int i = 0; i < arts.Length; i++) arts[i] = ImportBlockArt(BlockArtSets[i]);
        foreach (string name in new[] { "BlockSet_01", "BlockSet_02", "BlockSet_03", "BlockSet_04", "BlockSet_Boss", "BlockSet_Tutorial" })
        {
            string path = $"{PrefabDir}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            int skinned = 0;
            foreach (Transform c in root.transform)
            {
                if (c.GetComponent<Enemy>() != null || c.GetComponent<BoxCollider2D>() == null) continue;
                //pick the variant from the block's position so the mix is stable across rebuilds but not
                //storey-by-storey like child order would give
                var p = c.localPosition;
                int hash = (Mathf.RoundToInt(p.x * 10f) * 73856093) ^ (Mathf.RoundToInt(p.y * 10f) * 19349663);
                SkinBlock(c.gameObject, arts[Mathf.Abs(hash) % arts.Length]);
                skinned++;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"ContentBuilder: {name}: skinned {skinned} blocks with {string.Join("/", BlockArtSets)}");
        }
        AssetDatabase.SaveAssets();
    }

    //a row per art set of the real block sizes, each with its collider drawn as a thin frame, dropped into
    //the active scene as "BlockArtPreview" so the options can be compared side by side. destroy it after
    public static GameObject BuildBlockArtPreview(string[] sets, Vector3 origin)
    {
        var root = new GameObject("BlockArtPreview");
        root.transform.position = origin;
        //the sizes the level builder actually uses: thin post, tall post, short beam, long beam, boss shelf, crate
        var sizes = new[] { new Vector2(0.37f, 1.9f), new Vector2(0.62f, 3.2f), new Vector2(1.62f, 0.31f), new Vector2(2.73f, 0.53f), new Vector2(4.6f, 0.4f), new Vector2(0.9f, 0.9f) };
        //URP 2D unlit: the built-in Sprites/Default shader draws every sprite with one texture under URP
        var lineMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        for (int r = 0; r < sets.Length; r++)
        {
            var art = ImportBlockArt(sets[r]);
            //unlit so the comparison doesn't depend on whichever scene's 2D lights happen to be open
            art.material = lineMat;
            var row = new GameObject(sets[r]).transform;
            row.SetParent(root.transform, false);
            row.localPosition = new Vector3(0f, -r * 4.2f, 0f);
            float x = 0f;
            foreach (var s in sizes)
            {
                x += s.x * 0.5f + 0.4f;
                var block = new GameObject($"{s.x}x{s.y}");
                block.transform.SetParent(row, false);
                block.transform.localPosition = new Vector3(x, s.y * 0.5f, 0f);
                block.transform.localScale = new Vector3(s.x, s.y, 1f);
                var box = block.AddComponent<BoxCollider2D>();
                box.size = Vector2.one;
                SkinBlock(block, art);
                //collider outline: a magenta loop around the box, drawn in front of the wood
                var outline = new GameObject("ColliderFrame");
                outline.transform.SetParent(block.transform, false);
                var lr = outline.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.loop = true;
                lr.widthMultiplier = 0.03f;
                lr.sharedMaterial = lineMat;
                lr.startColor = lr.endColor = new Color(1f, 0f, 0.6f, 1f);
                lr.sortingOrder = 50;
                var c = block.transform.position + new Vector3(0f, 0f, -0.1f);
                lr.positionCount = 4;
                lr.SetPositions(new[]
                {
                    c + new Vector3(-s.x * 0.5f, -s.y * 0.5f, 0f), c + new Vector3(s.x * 0.5f, -s.y * 0.5f, 0f),
                    c + new Vector3(s.x * 0.5f, s.y * 0.5f, 0f), c + new Vector3(-s.x * 0.5f, s.y * 0.5f, 0f),
                });
                x += s.x * 0.5f + 0.4f;
            }
        }
        return root;
    }
}
