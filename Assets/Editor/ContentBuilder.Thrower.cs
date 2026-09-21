using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

//dresses the slingshot in GameScene: forked-branch sprite on the Thrower, pouch on the Grabber,
//and a rope band that runs tip -> cup -> cup -> tip
public static partial class ContentBuilder
{
    const string ThrowerArtDir = "Assets/Art/Thrower";

    //fork tips and cup loops, in units from each sprite's pivot (eyeballed off the paintings)
    static readonly Vector2 TipLeft = new Vector2(-0.62f, 2.48f);
    static readonly Vector2 TipRight = new Vector2(0.72f, 2.48f);
    static readonly Vector2 LoopLeft = new Vector2(-0.5f, -0.17f);
    static readonly Vector2 LoopRight = new Vector2(0.5f, -0.17f);
    //the pouch's own stretch (the player's tuning): wide and deep enough to hold a hog. lives on a Cup
    //child so the hog parented to the grabber stays unit scale
    static readonly Vector3 CupScale = new Vector3(1.82f, 1.24f, 1f);
    //the loaded hog rests this far below the fork tips
    const float RestDrop = 0.37f;
    static readonly Color Jute = new Color32(0x9C, 0x6B, 0x3A, 0xFF);

    static Transform Child(Transform parent, string name, Vector3 localPos)
    {
        var t = parent.Find(name);
        if (t == null)
        {
            t = new GameObject(name).transform;
            t.SetParent(parent, false);
        }
        t.localPosition = localPos;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
        return t;
    }

    public static void DressThrower()
    {
        //slingshot: base pivot, ~2.65 units tall. cup: pivot just under the rim so the hog peeks out
        SpriteSheetImporter.ImportSingle($"{ThrowerArtDir}/Slingshot.png", 580f, new Vector2(0.5f, 0f));
        //the pouch is two halves on one canvas (Tools/ArtDirector/splitcup.mjs): back behind the hog, front over it
        SpriteSheetImporter.ImportSingle($"{ThrowerArtDir}/Cup_Back.png", 880f, new Vector2(0.5f, 0.707f));
        SpriteSheetImporter.ImportSingle($"{ThrowerArtDir}/Cup_Front.png", 880f, new Vector2(0.5f, 0.707f));
        var slingshot = LoadOrThrow<Sprite>($"{ThrowerArtDir}/Slingshot.png");
        var cupFront = LoadOrThrow<Sprite>($"{ThrowerArtDir}/Cup_Front.png");
        var cupBack = LoadOrThrow<Sprite>($"{ThrowerArtDir}/Cup_Back.png");

        var thrower = Object.FindFirstObjectByType<Thrower>();
        var grabber = Object.FindFirstObjectByType<Grabber>();
        var band = Object.FindFirstObjectByType<RubberBand>();
        if (thrower == null || grabber == null) { Debug.LogError("DressThrower: no Thrower/Grabber in the open scene"); return; }

        //the thrower object was a scaled white post; it becomes the slingshot, planted on the ground
        var tt = thrower.transform;
        Vector3 anchorWorld = thrower.anchor.position;
        tt.localScale = Vector3.one;
        tt.position = new Vector3(anchorWorld.x - (TipLeft.x + TipRight.x) * 0.5f, -3.72f, 0f);
        var tsr = tt.GetComponent<SpriteRenderer>();
        if (tsr == null) tsr = tt.gameObject.AddComponent<SpriteRenderer>();
        tsr.sprite = slingshot;
        tsr.drawMode = SpriteDrawMode.Simple;
        tsr.color = Color.white;
        tsr.sortingOrder = 1;
        //the band anchor sits between the fork tips, a little down: a loaded pouch sags
        thrower.anchor.position = tt.position + (Vector3)((TipLeft + TipRight) * 0.5f) + Vector3.down * RestDrop;
        thrower.anchor.localScale = Vector3.one;
        var tipL = Child(tt, "TipLeft", TipLeft);
        var tipR = Child(tt, "TipRight", TipRight);

        //the grabber carries the cup, drawn in front of the loaded hog. unit scale so hogs stay unit scale
        var gt = grabber.transform;
        gt.localScale = Vector3.one;
        //the grabber root used to draw the cup itself; the art now lives on the Cup child
        var rootSr = gt.GetComponent<SpriteRenderer>();
        var material = rootSr != null ? rootSr.sharedMaterial : null;
        if (rootSr != null) Object.DestroyImmediate(rootSr);
        var cupT = Child(gt, "Cup", Vector3.zero);
        cupT.localScale = CupScale;
        var gsr = cupT.GetComponent<SpriteRenderer>();
        if (gsr == null) gsr = cupT.gameObject.AddComponent<SpriteRenderer>();
        gsr.sprite = cupFront;
        gsr.drawMode = SpriteDrawMode.Simple;
        gsr.color = Color.white;
        if (material != null) gsr.sharedMaterial = material;
        gsr.sortingOrder = 5; //hogs draw at 4: front half over, back half under
        var oldBack = gt.Find("CupBack"); if (oldBack != null) Object.DestroyImmediate(oldBack.gameObject);
        var backT = Child(cupT, "CupBack", Vector3.zero);
        var bsr = backT.GetComponent<SpriteRenderer>();
        if (bsr == null) bsr = backT.gameObject.AddComponent<SpriteRenderer>();
        bsr.sprite = cupBack;
        bsr.sharedMaterial = gsr.sharedMaterial;
        bsr.sortingOrder = 3;
        var sphere = gt.GetComponent<SphereCollider>();
        if (sphere != null) sphere.radius = 0.7f;
        //loops ride on the cup so they stretch with it
        foreach (var n in new[] { "LoopLeft", "LoopRight" }) { var old = gt.Find(n); if (old != null) Object.DestroyImmediate(old.gameObject); }
        var loopL = Child(cupT, "LoopLeft", LoopLeft);
        var loopR = Child(cupT, "LoopRight", LoopRight);
        gt.position = thrower.anchor.position;

        //the band: rope coloured, behind the hog and cup, in front of the fork
        if (band != null)
        {
            var so = new SerializedObject(band);
            so.FindProperty("tipLeft").objectReferenceValue = tipL;
            so.FindProperty("tipRight").objectReferenceValue = tipR;
            so.FindProperty("cupLeft").objectReferenceValue = loopL;
            so.FindProperty("cupRight").objectReferenceValue = loopR;
            var lrProp = so.FindProperty("lineRenderer");
            so.ApplyModifiedPropertiesWithoutUndo();
            var lr = lrProp.objectReferenceValue as LineRenderer;
            if (lr != null)
            {
                lr.positionCount = 4;
                lr.startWidth = lr.endWidth = 0.09f;
                lr.startColor = lr.endColor = Jute;
                lr.numCornerVertices = 4;
                lr.numCapVertices = 4;
                lr.sortingOrder = 2;
            }
        }

        EditorSceneManager.MarkSceneDirty(tt.gameObject.scene);
        EditorSceneManager.SaveScene(tt.gameObject.scene);
        Debug.Log("ContentBuilder: thrower dressed");
    }
}
