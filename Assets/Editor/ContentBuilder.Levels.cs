using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//block sets, hog loadouts, level definitions and the catalogue
public static partial class ContentBuilder
{
    const string LevelDir = "Assets/Configs/Levels";
    //every block set is authored with its root here. the ground collider top is world y -3.65, so local 0.32
    static readonly Vector3 SetOrigin = new Vector3(2.63f, -3.968f, 0f);
    const float GroundY = 0.32f;

    static void AddWoodKnock(GameObject block)
    {
        if (block.GetComponent<Rigidbody2D>() != null && block.GetComponent<WoodKnock>() == null)
            block.AddComponent<WoodKnock>();
    }

    //the hand-placed sets have plain-copy hens (green circles). swap them for the real hen prefab,
    //and give every block its knock
    public static void ReplaceEnemiesInBlockSets()
    {
        var henPrefab = LoadOrThrow<GameObject>($"{PrefabDir}/Enemy_01.prefab");
        foreach (string name in new[] { "BlockSet_01", "BlockSet_02", "BlockSet_03" })
        {
            string path = $"{PrefabDir}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;

            var root = PrefabUtility.LoadPrefabContents(path);
            int swapped = 0;
            foreach (var enemy in root.GetComponentsInChildren<Enemy>(true))
            {
                var go = enemy.gameObject;
                //already the real thing (a nested Enemy_01 instance): just make sure it isn't scaled up
                if (PrefabUtility.GetCorrespondingObjectFromSource(go) != null)
                {
                    go.transform.localScale = Vector3.one;
                    continue;
                }
                Vector3 pos = go.transform.localPosition;
                string goName = go.name;
                int sibling = go.transform.GetSiblingIndex();
                Object.DestroyImmediate(go);

                var hen = (GameObject)PrefabUtility.InstantiatePrefab(henPrefab, root.transform);
                hen.name = goName;
                hen.transform.localPosition = pos;
                hen.transform.SetSiblingIndex(sibling);
                swapped++;
            }
            foreach (Transform c in root.transform)
                if (c.GetComponent<Enemy>() == null) AddWoodKnock(c.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"ContentBuilder: {name}: swapped {swapped} hens");
        }
    }

    //a wooden block cloned from the one the player already tuned (sprite, material, physics material)
    static GameObject BlockTemplate()
    {
        var set = LoadOrThrow<GameObject>($"{PrefabDir}/BlockSet_01.prefab");
        foreach (Transform c in set.transform)
        {
            var sr = c.GetComponent<SpriteRenderer>();
            if (c.GetComponent<Enemy>() == null && sr != null && sr.sharedMaterial != null && sr.sharedMaterial.name == "BrightWoodMat")
                return c.gameObject;
        }
        foreach (Transform c in set.transform)
            if (c.GetComponent<Enemy>() == null && c.GetComponent<BoxCollider2D>() != null)
                return c.gameObject;
        throw new System.Exception("No block to use as a template in BlockSet_01");
    }

    static GameObject Block(GameObject template, Transform parent, string name, float x, float bottomY, float width, float height, Material material = null)
    {
        var block = (GameObject)Object.Instantiate(template, parent);
        block.name = name;
        block.transform.localPosition = new Vector3(x, bottomY + height * 0.5f, 0f);
        block.transform.localRotation = Quaternion.identity;
        block.transform.localScale = new Vector3(width, height, 1f);
        if (material != null) block.GetComponent<SpriteRenderer>().sharedMaterial = material;
        var rb = block.GetComponent<Rigidbody2D>();
        if (rb != null) rb.mass = Mathf.Max(0.5f, width * height);
        AddWoodKnock(block);
        return block;
    }

    static GameObject Hen(GameObject henPrefab, Transform parent, string name, float x, float bottomY, float radius = 0.38f)
    {
        var hen = (GameObject)PrefabUtility.InstantiatePrefab(henPrefab, parent);
        hen.name = name;
        hen.transform.localPosition = new Vector3(x, bottomY + radius + 0.02f, 0f);
        return hen;
    }

    public static void BuildBlockSets()
    {
        var template = BlockTemplate();
        var henPrefab = LoadOrThrow<GameObject>($"{PrefabDir}/Enemy_01.prefab");
        var bossPrefab = LoadOrThrow<GameObject>($"{PrefabDir}/Enemy_Boss.prefab");
        var boxMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/Static/BoxesMat.mat");
        var woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/Static/BrightWoodMat.mat");

        //BlockSet_Tutorial: one hen up a post, one on the ground behind a crate. easy pickings
        {
            var root = new GameObject("BlockSet_Tutorial");
            var t = root.transform;
            float g = GroundY;
            Block(template, t, "Post", -1.2f, g, 0.5f, 1.6f, woodMat);
            Hen(henPrefab, t, "Hen_Post", -1.2f, g + 1.6f);
            Block(template, t, "Crate", 1.4f, g, 0.9f, 0.9f, boxMat);
            Hen(henPrefab, t, "Hen_Ground", 2.5f, g);
            SavePrefab(root, $"{PrefabDir}/BlockSet_Tutorial.prefab");
        }

        //BlockSet_04 "Potting shelves": a wide two-storey shelf with hens on every level and one balanced on top
        {
            var root = new GameObject("BlockSet_04");
            var t = root.transform;
            float g = GroundY;
            //ground floor: three posts, a long shelf
            Block(template, t, "Post_L", -2.6f, g, 0.5f, 2.6f, woodMat);
            Block(template, t, "Post_M", 0f, g, 0.5f, 2.6f, woodMat);
            Block(template, t, "Post_R", 2.6f, g, 0.5f, 2.6f, woodMat);
            Block(template, t, "Shelf_1", 0f, g + 2.6f, 6.0f, 0.35f, woodMat);
            Hen(henPrefab, t, "Hen_1", -1.3f, g);
            Hen(henPrefab, t, "Hen_2", 1.3f, g);
            //first floor: two posts set in, a shorter shelf
            float f1 = g + 2.95f;
            Block(template, t, "Post_L2", -1.5f, f1, 0.45f, 2.2f, woodMat);
            Block(template, t, "Post_R2", 1.5f, f1, 0.45f, 2.2f, woodMat);
            Block(template, t, "Shelf_2", 0f, f1 + 2.2f, 3.8f, 0.35f, woodMat);
            Hen(henPrefab, t, "Hen_3", 0f, f1);
            //crate + hen perched on top
            float f2 = f1 + 2.55f;
            Block(template, t, "Crate", 0f, f2, 0.9f, 0.9f, boxMat);
            Hen(henPrefab, t, "Hen_4", 0f, f2 + 0.9f);
            SavePrefab(root, $"{PrefabDir}/BlockSet_04.prefab");
        }

        //BlockSet_Boss "The Coop": the rooster sits in a sturdy coop, two hens lookout on the roof,
        //a crate wall out front takes the first hits
        {
            var root = new GameObject("BlockSet_Boss");
            var t = root.transform;
            float g = GroundY;
            //front crate wall
            Block(template, t, "Crate_1", -3.4f, g, 0.9f, 0.9f, boxMat);
            Block(template, t, "Crate_2", -3.4f, g + 0.9f, 0.9f, 0.9f, boxMat);
            Block(template, t, "Crate_3", -3.4f, g + 1.8f, 0.9f, 0.9f, boxMat);
            //coop: thick walls, a beam roof
            Block(template, t, "Wall_L", -1.9f, g, 0.6f, 3.6f, woodMat);
            Block(template, t, "Wall_R", 1.9f, g, 0.6f, 3.6f, woodMat);
            Block(template, t, "Roof", 0f, g + 3.6f, 4.6f, 0.4f, woodMat);
            //the boss lives inside
            var boss = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab, t);
            boss.name = "Boss";
            boss.transform.localPosition = new Vector3(0f, g + 1.05f, 0f);
            //roof guards on little perches
            Block(template, t, "Perch_L", -1.7f, g + 4.0f, 0.4f, 0.8f, woodMat);
            Block(template, t, "Perch_R", 1.7f, g + 4.0f, 0.4f, 0.8f, woodMat);
            Hen(henPrefab, t, "Hen_L", -1.7f, g + 4.8f);
            Hen(henPrefab, t, "Hen_R", 1.7f, g + 4.8f);
            Block(template, t, "Ridge", 0f, g + 4.8f, 2.2f, 0.3f, woodMat);
            Hen(henPrefab, t, "Hen_Top", 0f, g + 5.1f);
            //a crate behind so it doesn't just slide away
            Block(template, t, "Crate_Back", 3.2f, g, 0.9f, 0.9f, boxMat);
            SavePrefab(root, $"{PrefabDir}/BlockSet_Boss.prefab");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("ContentBuilder: block sets built");
    }

    static HogLoadout Loadout(string name, params (HogType type, int amount)[] entries)
    {
        EnsureFolder(LevelDir);
        string path = $"{LevelDir}/Loadout_{name}.asset";
        var loadout = AssetDatabase.LoadAssetAtPath<HogLoadout>(path);
        if (loadout == null)
        {
            loadout = ScriptableObject.CreateInstance<HogLoadout>();
            AssetDatabase.CreateAsset(loadout, path);
        }
        var so = new SerializedObject(loadout);
        var list = so.FindProperty("entries");
        list.ClearArray();
        for (int i = 0; i < entries.Length; i++)
        {
            list.InsertArrayElementAtIndex(i);
            var e = list.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("type").enumValueIndex = (int)entries[i].type;
            e.FindPropertyRelative("amount").intValue = entries[i].amount;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return loadout;
    }

    static LevelDefinition Level(string fileName, string title, string blurb, string blockSet, HogLoadout loadout, bool boss, bool tutorial = false)
    {
        string path = $"{LevelDir}/{fileName}.asset";
        var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
        if (level == null)
        {
            level = ScriptableObject.CreateInstance<LevelDefinition>();
            AssetDatabase.CreateAsset(level, path);
        }
        level.title = title;
        level.blurb = blurb;
        level.blockSet = LoadOrThrow<GameObject>($"{PrefabDir}/{blockSet}.prefab");
        level.blockSetPosition = SetOrigin;
        level.loadout = loadout;
        level.isBoss = boss;
        level.isTutorial = tutorial;
        EditorUtility.SetDirty(level);
        return level;
    }

    public static void BuildLevels()
    {
        var levels = new List<LevelDefinition>
        {
            Level("Level_00_Tutorial", "Garden Lesson", "Optional. Learn the ropes: a throw, a boom, and a split.", "BlockSet_Tutorial",
                Loadout("Tutorial", (HogType.Neutral, 1), (HogType.Exploding, 1), (HogType.Cluster, 1), (HogType.Neutral, 1)), false, true),
            Level("Level_01", "The Garden Gate", "Two hens on a wobbly stand. Knock them off.", "BlockSet_01",
                Loadout("L1", (HogType.Neutral, 4)), false),
            Level("Level_02", "Fence Post", "Same stand, fewer hogs. Make them count.", "BlockSet_02",
                Loadout("L2", (HogType.Neutral, 2), (HogType.Exploding, 1)), false),
            Level("Level_03", "The Big Trellis", "A whole trellis of hens. Time to go boom.", "BlockSet_03",
                Loadout("L3", (HogType.Neutral, 2), (HogType.Exploding, 2), (HogType.Cluster, 1)), false),
            Level("Level_04", "Potting Shelves", "Hens on every shelf. Tap a cluster hog mid-air to shower them.", "BlockSet_04",
                Loadout("L4", (HogType.Cluster, 2), (HogType.Neutral, 1), (HogType.Exploding, 1)), false),
            Level("Level_05", "The Coop", "The big rooster himself. He takes three good hits.", "BlockSet_Boss",
                Loadout("Boss", (HogType.Neutral, 2), (HogType.Exploding, 2), (HogType.Cluster, 2)), true),
        };

        EnsureFolder("Assets/Resources");
        string catPath = $"Assets/Resources/{LevelCatalogue.ResourcePath}.asset";
        var catalogue = AssetDatabase.LoadAssetAtPath<LevelCatalogue>(catPath);
        if (catalogue == null)
        {
            catalogue = ScriptableObject.CreateInstance<LevelCatalogue>();
            AssetDatabase.CreateAsset(catalogue, catPath);
        }
        var so = new SerializedObject(catalogue);
        var list = so.FindProperty("levels");
        list.ClearArray();
        for (int i = 0; i < levels.Count; i++)
        {
            list.InsertArrayElementAtIndex(i);
            list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.SaveAssets();
        Debug.Log("ContentBuilder: levels built");
    }
}
