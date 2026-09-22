using System.Linq;
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
        public Sprite[] shards;
        public GameObject dust;
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
            shards = SpriteSheetImporter.LoadSprites("Assets/Art/VFX/WoodShards.png"),
            dust = AssetDatabase.LoadAssetAtPath<GameObject>($"{VfxDir}/VFX_DustPoof.prefab"),
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
        var gfxT = block.transform.Find("GFX");
        var gfxSr = gfxT != null ? gfxT.GetComponent<SpriteRenderer>() : null;
        int order = oldSr != null ? oldSr.sortingOrder : gfxSr != null ? gfxSr.sortingOrder : 0;
        if (oldSr != null) Object.DestroyImmediate(oldSr);

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

        //planks splinter in an explosion, crates only get shoved
        bool plank = aspect > 1.35f || aspect < 1f / 1.35f;
        var splinter = block.GetComponent<Splinterable>();
        if (plank && art.shards != null)
        {
            if (splinter == null) splinter = block.AddComponent<Splinterable>();
            var so = new SerializedObject(splinter);
            var shards = so.FindProperty("shardSprites");
            shards.arraySize = art.shards.Length;
            for (int i = 0; i < art.shards.Length; i++) shards.GetArrayElementAtIndex(i).objectReferenceValue = art.shards[i];
            so.FindProperty("dustVfx").objectReferenceValue = art.dust;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else if (!plank && splinter != null) Object.DestroyImmediate(splinter);
    }

    public static void SkinBlockSets()
    {
        var arts = new BlockArt[BlockArtSets.Length];
        for (int i = 0; i < arts.Length; i++) arts[i] = ImportBlockArt(BlockArtSets[i]);
        foreach (string name in BlockSetNames)
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

    public static readonly string[] BlockSetNames = { "BlockSet_01", "BlockSet_02", "BlockSet_03", "BlockSet_04", "BlockSet_Boss", "BlockSet_Tutorial" };

    //hand edits in the prefabs sometimes move or stretch the GFX child instead of the block root, which
    //leaves the collider somewhere else than the wood. this makes each GFX the truth: the root is moved,
    //rotated and scaled onto the GFX's visual rect (collider reset to a unit box), a second GFX under one
    //root becomes a block of its own, and a root with no GFX at all is removed. then everything is re-skinned
    public static string ReseatBlocks()
    {
        var log = new System.Text.StringBuilder();
        foreach (string name in BlockSetNames)
        {
            string path = $"{PrefabDir}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            var blocks = new System.Collections.Generic.List<Transform>();
            foreach (Transform c in root.transform)
                if (c.GetComponent<Enemy>() == null && c.GetComponent<BoxCollider2D>() != null) blocks.Add(c);

            foreach (var block in blocks)
            {
                var gfxs = new System.Collections.Generic.List<SpriteRenderer>();
                foreach (Transform c in block) { var sr = c.GetComponent<SpriteRenderer>(); if (sr != null && sr.sprite != null) gfxs.Add(sr); }

                if (gfxs.Count == 0)
                {
                    log.AppendLine($"{name}/{block.name}: no GFX, removed (collider was at {Fmt(block.localPosition)} size {Fmt(block.localScale)})");
                    Object.DestroyImmediate(block.gameObject);
                    continue;
                }

                for (int i = 0; i < gfxs.Count; i++)
                {
                    var target = block;
                    if (i > 0)
                    {
                        //an extra GFX is a block the level designer meant to add: clone the root for it
                        var clone = Object.Instantiate(block.gameObject, root.transform);
                        clone.name = block.name + "_" + i;
                        target = clone.transform;
                        foreach (Transform c in clone.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(c.gameObject);
                        log.AppendLine($"{name}/{block.name}: extra GFX \"{gfxs[i].name}\" became block {clone.name}");
                    }
                    Seat(name, target, gfxs[i], log);
                }
                //the GFX children are rebuilt by the skinner from the seated root
                foreach (Transform c in block.Cast<Transform>().ToArray()) Object.DestroyImmediate(c.gameObject);
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        SkinBlockSets();
        Debug.Log("ContentBuilder: reseat\n" + log);
        return log.ToString();
    }

    //move a block root onto its GFX's visual rectangle. the visual is the sprite quad (sliced size or the
    //sprite's own size) under the GFX's full transform, so stretches done on either level are folded in
    static void Seat(string set, Transform block, SpriteRenderer gfx, System.Text.StringBuilder log)
    {
        Vector2 quad = gfx.drawMode == SpriteDrawMode.Simple ? (Vector2)gfx.sprite.bounds.size : gfx.size;
        Vector3 ls = gfx.transform.lossyScale;
        var size = new Vector2(Mathf.Abs(quad.x * ls.x), Mathf.Abs(quad.y * ls.y));
        Vector3 centre = gfx.transform.position;
        Quaternion rot = gfx.transform.rotation;
        //sliced posts that were drawn rotated (an old beam trick) count as lying down
        float z = rot.eulerAngles.z;
        if (Mathf.Abs(Mathf.DeltaAngle(z, 90f)) < 1f || Mathf.Abs(Mathf.DeltaAngle(z, -90f)) < 1f) { size = new Vector2(size.y, size.x); rot = Quaternion.identity; }

        var box = block.GetComponent<BoxCollider2D>();
        Vector3 oldPos = block.localPosition, oldScale = block.localScale;
        bool moved = (oldPos - centre).sqrMagnitude > 1e-4f || ((Vector2)oldScale - size).sqrMagnitude > 1e-4f || box.size != Vector2.one || box.offset != Vector2.zero;
        block.position = centre;
        block.rotation = rot;
        block.localScale = new Vector3(size.x, size.y, 1f);
        box.size = Vector2.one;
        box.offset = Vector2.zero;
        var rb = block.GetComponent<Rigidbody2D>();
        if (rb != null) rb.mass = Mathf.Max(0.5f, size.x * size.y);
        if (moved) log.AppendLine($"{set}/{block.name}: {Fmt(oldPos)} {Fmt(oldScale)} -> {Fmt(block.localPosition)} {Fmt(block.localScale)}");
    }

    static string Fmt(Vector3 v) => $"({v.x:0.00}, {v.y:0.00})";

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
