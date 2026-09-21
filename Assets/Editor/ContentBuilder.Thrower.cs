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
        SpriteSheetImporter.ImportSingle($"{ThrowerArtDir}/Cup.png", 880f, new Vector2(0.5f, 0.707f));
        var slingshot = LoadOrThrow<Sprite>($"{ThrowerArtDir}/Slingshot.png");
        var cup = LoadOrThrow<Sprite>($"{ThrowerArtDir}/Cup.png");

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
        tsr.sortingOrder = 2;
        //the band anchor sits between the fork tips
        thrower.anchor.position = tt.position + (Vector3)((TipLeft + TipRight) * 0.5f);
        thrower.anchor.localScale = Vector3.one;
        var tipL = Child(tt, "TipLeft", TipLeft);
        var tipR = Child(tt, "TipRight", TipRight);

        //the grabber carries the cup, drawn in front of the loaded hog. unit scale so hogs stay unit scale
        var gt = grabber.transform;
        gt.localScale = Vector3.one;
        var gsr = gt.GetComponent<SpriteRenderer>();
        if (gsr == null) gsr = gt.gameObject.AddComponent<SpriteRenderer>();
        gsr.sprite = cup;
        gsr.drawMode = SpriteDrawMode.Simple;
        gsr.color = Color.white;
        gsr.sortingOrder = 5;
        var sphere = gt.GetComponent<SphereCollider>();
        if (sphere != null) sphere.radius = 0.6f;
        var loopL = Child(gt, "LoopLeft", LoopLeft);
        var loopR = Child(gt, "LoopRight", LoopRight);
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
                lr.sortingOrder = 3;
            }
        }

        EditorSceneManager.MarkSceneDirty(tt.gameObject.scene);
        EditorSceneManager.SaveScene(tt.gameObject.scene);
        Debug.Log("ContentBuilder: thrower dressed");
    }
}
