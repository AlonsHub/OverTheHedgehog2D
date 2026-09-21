using UnityEditor;
using UnityEngine;

//wooden skins for the block sets. a block is a root object scaled to its size with a 1x1 box collider,
//so the sprite goes on a counter-scaled GFX child drawn in sliced mode at the block's size: the beam
//ends stay square while the middle stretches, and what you see is exactly what collides
public static partial class ContentBuilder
{
    const string BlockArtDir = "Assets/Art/Blocks";

    static Sprite BlockSprite(string name) => LoadOrThrow<Sprite>($"{BlockArtDir}/{name}.png");

    //post: 9-slice caps at 18% of its height (beams are the same post lying down). crate: the existing
    //opaque texture, stretched
    public static void ImportBlockArt()
    {
        var postTex = LoadOrThrow<Texture2D>($"{BlockArtDir}/Post_V.png");
        float ph = postTex.height * 0.18f;
        SpriteSheetImporter.ImportSingle($"{BlockArtDir}/Post_V.png", 100f, null, true, new Vector4(0f, ph, 0f, ph));
    }

    static void SkinBlock(GameObject block, Sprite post, Sprite crate, Material material)
    {
        var box = block.GetComponent<BoxCollider2D>();
        if (box == null) return;

        //what the collider actually covers, in the block's own space
        Vector3 scale = block.transform.localScale;
        float w = Mathf.Abs(scale.x) * box.size.x;
        float h = Mathf.Abs(scale.y) * box.size.y;

        //the old look was the renderer on the root, sized by hand. it goes
        var oldSr = block.GetComponent<SpriteRenderer>();
        int order = oldSr != null ? oldSr.sortingOrder : 0;
        if (oldSr != null) Object.DestroyImmediate(oldSr);

        var gfxT = block.transform.Find("GFX");
        var gfx = gfxT != null ? gfxT.gameObject : new GameObject("GFX");
        gfx.transform.SetParent(block.transform, false);
        gfx.transform.localPosition = (Vector3)box.offset;
        gfx.transform.localRotation = Quaternion.identity;
        //undo the root's stretch so sliced caps aren't squashed
        gfx.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, Mathf.Abs(scale.x)), 1f / Mathf.Max(0.0001f, Mathf.Abs(scale.y)), 1f);

        var sr = gfx.GetComponent<SpriteRenderer>();
        if (sr == null) sr = gfx.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = material;
        sr.sortingOrder = order;
        sr.color = Color.white;

        float aspect = w / Mathf.Max(0.0001f, h);
        if (aspect > 1.35f)
        {
            //a beam is a post lying on its side
            sr.sprite = post;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(h, w);
            gfx.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            gfx.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, Mathf.Abs(scale.y)), 1f / Mathf.Max(0.0001f, Mathf.Abs(scale.x)), 1f);
        }
        else if (aspect < 1f / 1.35f)
        {
            sr.sprite = post;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(w, h);
        }
        else
        {
            //crate texture is square and opaque: just fit it
            sr.sprite = crate;
            sr.drawMode = SpriteDrawMode.Simple;
            Vector3 s = crate.bounds.size;
            gfx.transform.localScale = new Vector3(gfx.transform.localScale.x * w / s.x, gfx.transform.localScale.y * h / s.y, 1f);
        }
    }

    public static void SkinBlockSets()
    {
        ImportBlockArt();
        var post = BlockSprite("Post_V");
        var crate = LoadOrThrow<Sprite>("Assets/Art/Environment/Static/Box_Texture.png");
        //same lit sprite material the rest of the playfield uses
        var material = LoadOrThrow<GameObject>($"{PrefabDir}/Hedgehog_01.prefab").GetComponentInChildren<SpriteRenderer>().sharedMaterial;

        foreach (string name in new[] { "BlockSet_01", "BlockSet_02", "BlockSet_03", "BlockSet_04", "BlockSet_Boss", "BlockSet_Tutorial" })
        {
            string path = $"{PrefabDir}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            int skinned = 0;
            foreach (Transform c in root.transform)
            {
                if (c.GetComponent<Enemy>() != null || c.GetComponent<BoxCollider2D>() == null) continue;
                SkinBlock(c.gameObject, post, crate, material);
                skinned++;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"ContentBuilder: {name}: skinned {skinned} blocks");
        }
        AssetDatabase.SaveAssets();
    }
}
