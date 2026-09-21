using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//UI in the three scenes: HUD + result window in GameScene, the start menu, the level map
public static partial class ContentBuilder
{
    static readonly Color Ink = new Color32(0x3B, 0x23, 0x14, 0xFF);        //dark outline brown
    static readonly Color Cream = new Color32(0xF3, 0xE9, 0xD2, 0xFF);
    static readonly Color Yolk = new Color32(0xF5, 0xB9, 0x21, 0xFF);

    static Font UiFont()
    {
        var custom = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Fredoka.ttf");
        if (custom != null) return custom;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    static Sprite UiSprite(string name) => LoadOrThrow<Sprite>($"Assets/Art/UI/{name}.png");

    static RectTransform Rect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }

    static GameObject UiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        return go;
    }

    static Image ImageAt(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size, bool preserveAspect = true)
    {
        var go = UiObject(name, parent);
        Rect(go, anchor, anchor, pos, size);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = preserveAspect;
        img.raycastTarget = false;
        return img;
    }

    static Image Stretched(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = UiObject(name, parent);
        var rt = Rect(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        return img;
    }

    static Text Label(string name, Transform parent, string text, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 box, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
    {
        var go = UiObject(name, parent);
        Rect(go, anchor, anchor, pos, box);
        var t = go.AddComponent<Text>();
        t.font = UiFont();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        //soft cream outline so text reads on the wood
        var outline = go.AddComponent<Outline>();
        outline.effectColor = color == Cream ? Ink : new Color(1f, 1f, 1f, 0.35f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return t;
    }

    static Button ButtonAt(string name, Transform parent, Sprite sprite, string label, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize = 40)
    {
        var go = UiObject(name, parent);
        Rect(go, anchor, anchor, pos, size);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        //the pill is 650px tall in its 1024px sprite and 9-sliced left/right only, so scale it so the pill
        //height matches the button height and the rounded ends keep their shape
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 650f / size.y;
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.0f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.75f);
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        btn.colors = colors;
        Label("Label", go.transform, label, fontSize, Cream, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), size);
        return btn;
    }

    static Canvas MakeCanvas(string name, int sortOrder = 0)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    static Camera EnsureCamera()
    {
        var cam = Camera.main;
        if (cam != null) return cam;
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(0x8E, 0xC5, 0xE8, 0xFF);
        go.AddComponent<AudioListener>();
        return cam;
    }

    // ---------------------------------------------------------------- GameScene

    public static void BuildGameSceneUI()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);

        //the level now spawns its own block set
        var oldSet = GameObject.Find("BlockSet_01");
        if (oldSet != null) Object.DestroyImmediate(oldSet);

        //one long floor under the whole garden: the ground sprite has a collider only under the right half,
        //so anything landing left of the structures used to fall out of the world
        var floor = GameObject.Find("GroundFloor");
        if (floor == null) floor = new GameObject("GroundFloor");
        floor.transform.position = new Vector3(2f, -5.65f, 0f);
        var floorCol = floor.GetComponent<BoxCollider2D>();
        if (floorCol == null) floorCol = floor.AddComponent<BoxCollider2D>();
        floorCol.size = new Vector2(60f, 4f); //top edge at y -3.65, same as the existing ground collider
        floorCol.offset = Vector2.zero;
        var groundCol = GameObject.Find("Ground (1)")?.GetComponent<Collider2D>();
        if (groundCol != null) floorCol.sharedMaterial = groundCol.sharedMaterial;

        var stock = Object.FindFirstObjectByType<HogStock>();
        var thrower = Object.FindFirstObjectByType<Thrower>();
        var canvasGo = GameObject.Find("Canvas");
        var canvas = canvasGo != null ? canvasGo.GetComponent<Canvas>() : MakeCanvas("Canvas");
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        //hog counter: keep it but dress it up
        var counter = GameObject.Find("HogCounter");
        if (counter != null)
        {
            var t = counter.GetComponent<Text>();
            if (t != null) { t.font = UiFont(); t.fontSize = 44; t.color = Cream; t.fontStyle = FontStyle.Bold; if (counter.GetComponent<Outline>() == null) { var o = counter.AddComponent<Outline>(); o.effectColor = Ink; o.effectDistance = new Vector2(2f, -2f); } }
            var rt = counter.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(30f, -24f);
            rt.sizeDelta = new Vector2(500f, 60f);
        }

        //score, top right
        var old = GameObject.Find("ScoreHUD"); if (old != null) Object.DestroyImmediate(old);
        var scoreGo = UiObject("ScoreHUD", canvas.transform);
        var srt = Rect(scoreGo, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -24f), new Vector2(600f, 60f));
        srt.pivot = new Vector2(1f, 1f);
        var scoreText = scoreGo.AddComponent<Text>();
        scoreText.font = UiFont(); scoreText.fontSize = 44; scoreText.fontStyle = FontStyle.Bold; scoreText.color = Cream;
        scoreText.alignment = TextAnchor.UpperRight; scoreText.text = "Score: 0";
        var so2 = scoreGo.AddComponent<Outline>(); so2.effectColor = Ink; so2.effectDistance = new Vector2(2f, -2f);
        var hud = scoreGo.AddComponent<ScoreHUD>();
        { var s = new SerializedObject(hud); s.FindProperty("label").objectReferenceValue = scoreText; s.ApplyModifiedPropertiesWithoutUndo(); }

        //result window
        var oldRw = GameObject.Find("ResultWindow"); if (oldRw != null) Object.DestroyImmediate(oldRw);
        var rwGo = UiObject("ResultWindow", canvas.transform);
        var rwRt = Rect(rwGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        rwRt.offsetMin = Vector2.zero; rwRt.offsetMax = Vector2.zero;
        var dim = rwGo.AddComponent<Image>();
        dim.color = new Color(0.15f, 0.1f, 0.05f, 0.45f);
        dim.raycastTarget = true;
        var rw = rwGo.AddComponent<ResultWindow>();

        var panel = ImageAt("Panel", rwGo.transform, UiSprite("Panel_Planks"), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1000f, 660f), false);
        panel.type = Image.Type.Sliced;
        panel.pixelsPerUnitMultiplier = 1.5f;
        panel.raycastTarget = true;
        var p = panel.transform;

        var title = Label("Title", p, "Level clear!", 64, Ink, new Vector2(0.5f, 1f), new Vector2(0f, -95f), new Vector2(800f, 90f));
        var stars = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            stars[i] = ImageAt($"Star_{i + 1}", p, UiSprite("Star"), new Vector2(0.5f, 1f), new Vector2((i - 1) * 120f, -190f), new Vector2(i == 1 ? 120f : 100f, i == 1 ? 120f : 100f));
        }
        var score = Label("Score", p, "Score  0", 48, Ink, new Vector2(0.5f, 1f), new Vector2(0f, -290f), new Vector2(800f, 60f));
        var best = Label("Best", p, "Best  0", 36, new Color32(0x7A, 0x4A, 0x23, 0xFF), new Vector2(0.5f, 1f), new Vector2(0f, -345f), new Vector2(800f, 50f));
        var bonus = Label("Bonus", p, "+500 for spared hogs", 30, new Color32(0x3E, 0x7F, 0x1E, 0xFF), new Vector2(0.5f, 1f), new Vector2(0f, -390f), new Vector2(800f, 40f));
        var record = Label("Record", p, "New record!", 34, new Color32(0xD6, 0x48, 0x30, 0xFF), new Vector2(0.5f, 1f), new Vector2(300f, -290f), new Vector2(320f, 50f));
        record.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);

        var mapBtn = ButtonAt("MapButton", p, UiSprite("Button_Terracotta"), "Map", new Vector2(0.5f, 0f), new Vector2(-300f, 95f), new Vector2(260f, 110f), 36);
        var againBtn = ButtonAt("PlayAgainButton", p, UiSprite("Button_Green"), "Play again", new Vector2(0.5f, 0f), new Vector2(0f, 95f), new Vector2(300f, 120f), 36);
        var nextBtn = ButtonAt("NextButton", p, UiSprite("Button_Green"), "Next  >", new Vector2(0.5f, 0f), new Vector2(300f, 95f), new Vector2(260f, 110f), 36);

        var rso = new SerializedObject(rw);
        rso.FindProperty("panel").objectReferenceValue = panel.rectTransform;
        rso.FindProperty("titleLabel").objectReferenceValue = title;
        rso.FindProperty("scoreLabel").objectReferenceValue = score;
        rso.FindProperty("bestLabel").objectReferenceValue = best;
        rso.FindProperty("bonusLabel").objectReferenceValue = bonus;
        rso.FindProperty("recordLabel").objectReferenceValue = record;
        rso.FindProperty("playAgainButton").objectReferenceValue = againBtn;
        rso.FindProperty("nextButton").objectReferenceValue = nextBtn;
        rso.FindProperty("mapButton").objectReferenceValue = mapBtn;
        var starsProp = rso.FindProperty("stars");
        starsProp.arraySize = 3;
        for (int i = 0; i < 3; i++) starsProp.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
        rso.ApplyModifiedPropertiesWithoutUndo();
        rwGo.SetActive(false);

        //tutorial plank: top centre, only wakes up on tutorial levels
        var oldTut = GameObject.Find("TutorialHints"); if (oldTut != null) Object.DestroyImmediate(oldTut);
        var tutGo = UiObject("TutorialHints", canvas.transform);
        Rect(tutGo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(10f, 10f));
        var plank = ImageAt("Plank", tutGo.transform, UiSprite("Panel_Planks"), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(980f, 190f), false);
        plank.type = Image.Type.Sliced;
        plank.pixelsPerUnitMultiplier = 3f;
        var hintText = Label("Hint", plank.transform, "", 30, Ink, new Vector2(0.5f, 0.5f), new Vector2(-70f, 6f), new Vector2(720f, 150f));
        var next = ButtonAt("NextButton", plank.transform, UiSprite("Button_Green"), "Next  >", new Vector2(1f, 0.5f), new Vector2(-105f, 0f), new Vector2(150f, 64f), 24);
        //skip the whole lesson: tucked in the bottom-right, out of the way of the slingshot
        var skipAll = ButtonAt("SkipTutorialButton", canvas.transform, UiSprite("Button_Terracotta"), "Skip tutorial", new Vector2(1f, 0f), new Vector2(-150f, 60f), new Vector2(240f, 70f), 24);
        skipAll.transform.SetParent(tutGo.transform, true);
        //the pointing hand lives in the world so it can hover next to hogs and hens
        var oldPtr = GameObject.Find("TutorialPointer"); if (oldPtr != null) Object.DestroyImmediate(oldPtr);
        SpriteSheetImporter.ImportSingle("Assets/Art/UI/Pointer.png", 800f, new Vector2(0.17f, 0.15f));
        var ptrGo = new GameObject("TutorialPointer");
        ptrGo.transform.localScale = Vector3.one * 1.3f;
        var ptr = ptrGo.AddComponent<SpriteRenderer>();
        ptr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Pointer.png");
        ptr.sortingOrder = 20;
        ptr.enabled = false;
        var tut = tutGo.AddComponent<TutorialHints>();
        var tso = new SerializedObject(tut);
        tso.FindProperty("panel").objectReferenceValue = plank.rectTransform;
        tso.FindProperty("label").objectReferenceValue = hintText;
        tso.FindProperty("nextButton").objectReferenceValue = next;
        tso.FindProperty("nextLabel").objectReferenceValue = next.GetComponentInChildren<Text>();
        tso.FindProperty("skipTutorialButton").objectReferenceValue = skipAll;
        tso.FindProperty("pointer").objectReferenceValue = ptr;
        tso.ApplyModifiedPropertiesWithoutUndo();

        AddButtonSounds(canvas.transform);

        //factory: every HogType needs its prefab
        var factory = Object.FindFirstObjectByType<HogFactory>();
        if (factory != null)
        {
            var fso = new SerializedObject(factory);
            var cat = fso.FindProperty("catalogue");
            var wanted = new (HogType type, string prefab)[]
            {
                (HogType.Neutral, "Hedgehog_01"), (HogType.Exploding, "ExplodingHedgehog_01"), (HogType.Cluster, "ClusterHedgehog_01"),
            };
            foreach (var (type, prefabName) in wanted)
            {
                bool found = false;
                for (int i = 0; i < cat.arraySize; i++)
                    if (cat.GetArrayElementAtIndex(i).FindPropertyRelative("type").enumValueIndex == (int)type) { found = true; break; }
                if (found) continue;
                int idx = cat.arraySize;
                cat.InsertArrayElementAtIndex(idx);
                var e = cat.GetArrayElementAtIndex(idx);
                e.FindPropertyRelative("type").enumValueIndex = (int)type;
                e.FindPropertyRelative("prefab").objectReferenceValue = LoadOrThrow<GameObject>($"{PrefabDir}/{prefabName}.prefab").GetComponent<Hog>();
            }
            fso.ApplyModifiedPropertiesWithoutUndo();
        }

        //game manager
        var gmGo = GameObject.Find("GameManager");
        if (gmGo == null) gmGo = new GameObject("GameManager");
        var gm = gmGo.GetComponent<GameManager>();
        if (gm == null) gm = gmGo.AddComponent<GameManager>();
        var gso = new SerializedObject(gm);
        gso.FindProperty("stock").objectReferenceValue = stock;
        gso.FindProperty("thrower").objectReferenceValue = thrower;
        gso.FindProperty("resultWindow").objectReferenceValue = rw;
        gso.FindProperty("fallbackLevel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{LevelDir}/Level_01.asset");
        gso.ApplyModifiedPropertiesWithoutUndo();

        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ContentBuilder: GameScene UI built");
    }

    // ---------------------------------------------------------------- StartMenu

    public static void BuildStartMenu()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/StartMenu.unity", OpenSceneMode.Single);
        EnsureCamera();
        EnsureEventSystem();

        var oldCanvas = GameObject.Find("Canvas"); if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);
        var canvas = MakeCanvas("Canvas");

        //the game's own backdrop, blurred garden
        var bg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Static/Background.png");
        var bgImg = Stretched("Background", canvas.transform, bg, Color.white);
        bgImg.preserveAspect = false;
        var fitter = bgImg.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = bg != null ? bg.rect.width / bg.rect.height : 1.78f;

        var sign = ImageAt("TitleSign", canvas.transform, UiSprite("TitleSign"), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(1100f, 740f));
        sign.rectTransform.pivot = new Vector2(0.5f, 1f);
        Label("Title", sign.transform, "Over The\nHedgehog", 96, Ink, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(800f, 300f));

        var play = ButtonAt("PlayButton", canvas.transform, UiSprite("Button_Green"), "Play", new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(420f, 170f), 56);
        var reset = ButtonAt("ResetButton", canvas.transform, UiSprite("Button_Terracotta"), "Reset progress", new Vector2(0f, 0f), new Vector2(150f, 50f), new Vector2(240f, 80f), 22);

        AddButtonSounds(canvas.transform);
        var menu = canvas.gameObject.AddComponent<StartMenu>();
        var so = new SerializedObject(menu);
        so.FindProperty("sign").objectReferenceValue = sign.rectTransform;
        so.FindProperty("playButton").objectReferenceValue = play;
        so.FindProperty("resetProgressButton").objectReferenceValue = reset;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ContentBuilder: StartMenu built");
    }

    // ---------------------------------------------------------------- LevelMap

    //stepping stones on the map image, in pixels from the top-left of the 1536x1024 painting
    static readonly Vector2[] Stones =
    {
        new Vector2(790, 870), new Vector2(580, 770), new Vector2(530, 630),
        new Vector2(640, 515), new Vector2(765, 435), new Vector2(825, 355),
    };

    static GameObject BuildLevelNodePrefab()
    {
        string path = $"{PrefabDir}/UI/LevelNode.prefab";
        EnsureFolder($"{PrefabDir}/UI");

        var go = new GameObject("LevelNode", typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        Rect(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));
        var pot = go.AddComponent<Image>();
        pot.sprite = UiSprite("LevelNode");
        pot.preserveAspect = true;
        var btn = go.AddComponent<Button>();
        var colors = btn.colors; colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 1f); btn.colors = colors;

        var number = Label("Number", go.transform, "1", 44, Cream, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(150f, 60f));
        var bestT = Label("Best", go.transform, "0", 24, Ink, new Vector2(0.5f, 0f), new Vector2(0f, -18f), new Vector2(220f, 36f));
        var badge = ImageAt("BossBadge", go.transform, UiSprite("Star"), new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(64f, 64f));

        AddButtonSounds(go.transform);
        var node = go.AddComponent<LevelNodeUI>();
        var so = new SerializedObject(node);
        so.FindProperty("button").objectReferenceValue = btn;
        so.FindProperty("pot").objectReferenceValue = pot;
        so.FindProperty("unlockedSprite").objectReferenceValue = UiSprite("LevelNode");
        so.FindProperty("lockedSprite").objectReferenceValue = UiSprite("LevelNode_Locked");
        so.FindProperty("numberLabel").objectReferenceValue = number;
        so.FindProperty("bestLabel").objectReferenceValue = bestT;
        so.FindProperty("bossBadge").objectReferenceValue = badge.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(go, path);
    }

    public static void BuildLevelMap()
    {
        string path = "Assets/Scenes/LevelMap.unity";
        Scene scene;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
        }

        EnsureCamera();
        EnsureEventSystem();
        var nodePrefab = BuildLevelNodePrefab();

        var canvas = MakeCanvas("Canvas");
        var mapSprite = UiSprite("LevelMap_Bg");
        var map = Stretched("Map", canvas.transform, mapSprite, Color.white);
        map.preserveAspect = false;
        var fitter = map.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 1536f / 1024f;

        //anchors sit on the stones as fractions of the painting so they follow it at any screen size
        var anchors = new RectTransform[Stones.Length];
        for (int i = 0; i < Stones.Length; i++)
        {
            var a = UiObject($"Stone_{i + 1}", map.transform);
            var frac = new Vector2(Stones[i].x / 1536f, 1f - Stones[i].y / 1024f);
            anchors[i] = Rect(a, frac, frac, new Vector2(0f, 40f), new Vector2(10f, 10f));
        }

        var back = ButtonAt("BackButton", canvas.transform, UiSprite("Button_Terracotta"), "<  Menu", new Vector2(0f, 1f), new Vector2(150f, -70f), new Vector2(240f, 90f), 30);
        Label("Heading", canvas.transform, "Pick a level", 56, Cream, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(700f, 80f));

        var hogSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/SpriteSheets/Idle.png");
        var marker = ImageAt("HedgehogMarker", canvas.transform, hogSprite, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 110f));

        AddButtonSounds(canvas.transform);
        var ui = canvas.gameObject.AddComponent<LevelMapUI>();
        var so = new SerializedObject(ui);
        var arr = so.FindProperty("nodeAnchors");
        arr.arraySize = anchors.Length;
        for (int i = 0; i < anchors.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = anchors[i];
        so.FindProperty("nodePrefab").objectReferenceValue = nodePrefab.GetComponent<LevelNodeUI>();
        so.FindProperty("backButton").objectReferenceValue = back;
        so.FindProperty("hedgehogMarker").objectReferenceValue = marker.rectTransform;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ContentBuilder: LevelMap built");
    }

    public static void SetBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/StartMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/LevelMap.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity", true),
        };
        Debug.Log("ContentBuilder: build scenes set");
    }

    [MenuItem("Over The Hedgehog/Build/Everything")]
    public static void BuildAll()
    {
        BuildSoundBank();
        BuildVfx();
        BuildHogPrefabs();
        BuildEnemyPrefabs();
        ReplaceEnemiesInBlockSets();
        BuildBlockSets();
        SkinBlockSets();
        BuildLevels();
        BuildStartMenu();
        BuildLevelMap();
        BuildGameSceneUI();
        SetBuildScenes();
    }
}
