using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

//hog, hen and VFX prefabs
public static partial class ContentBuilder
{
    const string PrefabDir = "Assets/Prefabs";
    const string VfxDir = "Assets/Prefabs/VFX";

    static T LoadOrThrow<T>(string path) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new System.Exception("Missing asset " + path);
        return asset;
    }

    //clones Hedgehog_01, swaps the Hog component for `hogType`, and saves it under a new name.
    //keeps the collider / rigidbody / physics material setup the neutral hog was tuned with
    static GameObject CloneHogPrefab(string newName, System.Type hogType, AnimatorController controller, Sprite idleSprite, float colliderRadius, float mass, float gfxScale = 1f)
    {
        var source = LoadOrThrow<GameObject>($"{PrefabDir}/Hedgehog_01.prefab");
        string path = $"{PrefabDir}/{newName}.prefab";

        var root = (GameObject)PrefabUtility.InstantiatePrefab(source);
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        root.name = newName;
        root.transform.position = Vector3.zero;

        var oldHog = root.GetComponent<Hog>();
        var so = new SerializedObject(oldHog);
        float walkSpeed = so.FindProperty("walkSpeed").floatValue;
        Object.DestroyImmediate(oldHog);

        var hog = (Hog)root.AddComponent(hogType);
        var rb = root.GetComponent<Rigidbody2D>();
        var col = root.GetComponent<CircleCollider2D>();
        col.radius = colliderRadius;
        rb.mass = mass;

        var gfx = root.transform.Find("GFX");
        var sr = gfx.GetComponent<SpriteRenderer>();
        sr.sprite = idleSprite;
        sr.color = Color.white;
        gfx.localScale = Vector3.one * gfxScale;
        var anim = gfx.GetComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        var hso = new SerializedObject(hog);
        hso.FindProperty("rb").objectReferenceValue = rb;
        hso.FindProperty("anim").objectReferenceValue = anim;
        hso.FindProperty("col").objectReferenceValue = col;
        hso.FindProperty("walkSpeed").floatValue = walkSpeed;
        hso.ApplyModifiedPropertiesWithoutUndo();

        var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved;
    }

    static void SetRef(GameObject prefab, System.Type componentType, string field, Object value)
    {
        var c = prefab.GetComponent(componentType);
        var so = new SerializedObject(c);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void SetFloat(GameObject prefab, System.Type componentType, string field, float value)
    {
        var c = prefab.GetComponent(componentType);
        var so = new SerializedObject(c);
        so.FindProperty(field).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void SetInt(GameObject prefab, System.Type componentType, string field, int value)
    {
        var c = prefab.GetComponent(componentType);
        var so = new SerializedObject(c);
        so.FindProperty(field).intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void BuildHogPrefabs()
    {
        var controllers = BuildAnimations();
        var dust = LoadOrThrow<GameObject>($"{VfxDir}/VFX_DustPoof.prefab");
        var explosion = LoadOrThrow<GameObject>($"{VfxDir}/VFX_Explosion.prefab");

        //exploding: the existing prefab gets the real art (it was a red-tinted neutral hog)
        {
            string path = $"{PrefabDir}/ExplodingHedgehog_01.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var gfx = root.transform.Find("GFX");
            var sr = gfx.GetComponent<SpriteRenderer>();
            sr.sprite = SpriteSheetImporter.LoadSprites("Assets/Art/Hogs/Exploding/ExplodingHog_Idle.png")[0];
            sr.color = Color.white;
            gfx.GetComponent<Animator>().runtimeAnimatorController = controllers["ExplodingHog"];
            var hog = root.GetComponent<ExplodingHog>();
            var so = new SerializedObject(hog);
            so.FindProperty("impactVfx").objectReferenceValue = dust;
            so.FindProperty("explosionVfx").objectReferenceValue = explosion;
            so.FindProperty("explosionRadius").floatValue = 4f;
            so.FindProperty("explosionForce").floatValue = 12f;
            so.FindProperty("lethalRadius").floatValue = 2f;
            so.FindProperty("fuseDelay").floatValue = 1.0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        //neutral: give it the dust poof too
        {
            var neutral = LoadOrThrow<GameObject>($"{PrefabDir}/Hedgehog_01.prefab");
            SetRef(neutral, typeof(Hog), "impactVfx", dust);
            EditorUtility.SetDirty(neutral);
        }

        //mini first, the cluster references it
        var mini = CloneHogPrefab("MiniHedgehog_01", typeof(Hog), controllers["MiniHog"],
            SpriteSheetImporter.LoadSprites("Assets/Art/Hogs/Cluster/MiniHog.png")[0], 0.28f, 0.35f);
        SetRef(mini, typeof(Hog), "impactVfx", dust);
        //smaller pop so a shower of minis doesn't fill the screen
        var mso = new SerializedObject(mini.GetComponent<Hog>());
        mso.FindProperty("popVelocity").vector2Value = new Vector2(-2f, 5f);
        mso.FindProperty("popDepthSpeed").floatValue = 3f;
        mso.FindProperty("popDuration").floatValue = 1.1f;
        mso.FindProperty("impactSound").stringValue = "mini_impact";
        mso.FindProperty("popSound").stringValue = "mini_squeak";
        mso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(mini);

        var cluster = CloneHogPrefab("ClusterHedgehog_01", typeof(ClusterHog), controllers["ClusterHog"],
            SpriteSheetImporter.LoadSprites("Assets/Art/Hogs/Cluster/ClusterHog_Idle.png")[0], 0.55f, 1.2f);
        SetRef(cluster, typeof(ClusterHog), "impactVfx", dust);
        SetRef(cluster, typeof(ClusterHog), "miniPrefab", mini.GetComponent<Hog>());
        SetRef(cluster, typeof(ClusterHog), "splitVfx", dust);
        SetInt(cluster, typeof(ClusterHog), "pieces", 5);
        SetFloat(cluster, typeof(ClusterHog), "spreadAngle", 70f);
        SetFloat(cluster, typeof(ClusterHog), "burstSpeed", 4f);
        EditorUtility.SetDirty(cluster);

        AssetDatabase.SaveAssets();
        Debug.Log("ContentBuilder: hog prefabs built");
    }

    //hen prefabs: the root keeps physics, a GFX child carries the sprite + animator + breathing
    static GameObject BuildEnemyPrefab(string name, Sprite idle, AnimatorController controller, float radius, float mass, int hitPoints, int scoreOnPop, float gfxScale, GameObject feathers, GameObject hitVfx, string hitSound = "hen_hit", string popSound = "hen_pop", string cluckSound = "hen_cluck")
    {
        string path = $"{PrefabDir}/{name}.prefab";

        var root = new GameObject(name);
        var rb = root.AddComponent<Rigidbody2D>();
        rb.mass = mass;
        rb.angularDamping = 0.5f;
        var col = root.AddComponent<CircleCollider2D>();
        col.radius = radius;
        //the hens share the hogs' bouncy-ish physics material if there is one
        var hogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Hedgehog_01.prefab");
        if (hogPrefab != null) col.sharedMaterial = hogPrefab.GetComponent<CircleCollider2D>().sharedMaterial;

        var gfx = new GameObject("GFX");
        gfx.transform.SetParent(root.transform, false);
        gfx.transform.localScale = Vector3.one * gfxScale;
        var sr = gfx.AddComponent<SpriteRenderer>();
        sr.sprite = idle;
        sr.sortingOrder = 3;
        var anim = gfx.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        gfx.AddComponent<Breathing>();

        var enemy = root.AddComponent<Enemy>();
        var so = new SerializedObject(enemy);
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        so.FindProperty("anim").objectReferenceValue = anim;
        so.FindProperty("col").objectReferenceValue = col;
        so.FindProperty("rb").objectReferenceValue = rb;
        so.FindProperty("hitPoints").intValue = hitPoints;
        so.FindProperty("scoreOnPop").intValue = scoreOnPop;
        so.FindProperty("feathersVfx").objectReferenceValue = feathers;
        so.FindProperty("hitVfx").objectReferenceValue = hitVfx;
        so.FindProperty("popDuration").floatValue = 1.15f;
        so.FindProperty("hitSound").stringValue = hitSound;
        so.FindProperty("popSound").stringValue = popSound;
        so.FindProperty("cluckSound").stringValue = cluckSound;
        so.ApplyModifiedPropertiesWithoutUndo();

        var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved;
    }

    public static void BuildEnemyPrefabs()
    {
        var controllers = BuildAnimations();
        var feathers = LoadOrThrow<GameObject>($"{VfxDir}/VFX_Feathers.prefab");
        var dust = LoadOrThrow<GameObject>($"{VfxDir}/VFX_DustPoof.prefab");

        var hen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Enemies/Chicken_Single.png");
        var boss = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Enemies/BossChicken_Single.png");

        //Enemy_01 is rebuilt in place under the same path (the old one was a green circle)
        BuildEnemyPrefab("Enemy_01", hen, controllers["Hen"], 0.38f, 1f, 1, 1000, 1f, feathers, dust);
        //the boss is above idle chatter
        BuildEnemyPrefab("Enemy_Boss", boss, controllers["BossHen"], 1.0f, 4f, 3, 5000, 3f, feathers, dust, "boss_hit", "boss_pop", "");

        AssetDatabase.SaveAssets();
        Debug.Log("ContentBuilder: enemy prefabs built");
    }
}
