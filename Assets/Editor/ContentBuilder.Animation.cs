using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

//builds the generated animation clips and animator controllers from the sliced sheets.
//re-runnable: assets are overwritten in place so prefab references survive
public static partial class ContentBuilder
{
    const string AnimDir = "Assets/Animations/Generated";

    //sprite flipbook clip on the SpriteRenderer of the object the Animator sits on
    public static AnimationClip MakeClip(string name, Sprite[] sprites, float fps, bool loop)
    {
        string path = $"{AnimDir}/{name}.anim";
        EnsureFolder(AnimDir);

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.ClearCurves();
        clip.frameRate = fps;

        var binding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };
        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    static Sprite[] Range(Sprite[] all, int from, int toInclusive)
    {
        var list = new List<Sprite>();
        for (int i = from; i <= toInclusive && i < all.Length; i++) list.Add(all[i]);
        return list.ToArray();
    }
    static Sprite[] Pick(Sprite[] all, params int[] indices)
    {
        var list = new List<Sprite>();
        foreach (int i in indices) if (i < all.Length) list.Add(all[i]);
        return list.ToArray();
    }

    //same shape as the hand-made GFX.controller: Idle -(Fly)-> Flying -> FlyingLoop, AnyState -(Impact)-> Impact
    public static AnimatorController MakeHogController(string name, AnimationClip idle, AnimationClip flying, AnimationClip flyingLoop, AnimationClip impact)
    {
        string path = $"{AnimDir}/{name}.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        //wipe and rebuild the layer
        var sm = controller.layers[0].stateMachine;
        foreach (var s in sm.states.Clone() as ChildAnimatorState[]) sm.RemoveState(s.state);
        foreach (var t in sm.anyStateTransitions) sm.RemoveAnyStateTransition(t);

        if (!HasParam(controller, "Fly")) controller.AddParameter("Fly", AnimatorControllerParameterType.Trigger);
        if (!HasParam(controller, "Impact")) controller.AddParameter("Impact", AnimatorControllerParameterType.Trigger);

        var idleState = sm.AddState("Idle", new Vector3(0, 0));
        idleState.motion = idle;
        sm.defaultState = idleState;

        var flyState = sm.AddState("Flying", new Vector3(300, 0));
        flyState.motion = flying;
        var loopState = sm.AddState("FlyingLoop", new Vector3(600, 0));
        loopState.motion = flyingLoop;
        var impactState = sm.AddState("Impact", new Vector3(300, 150));
        impactState.motion = impact;

        var toFly = idleState.AddTransition(flyState);
        toFly.hasExitTime = false; toFly.duration = 0f; toFly.AddCondition(AnimatorConditionMode.If, 0, "Fly");

        var toLoop = flyState.AddTransition(loopState);
        toLoop.hasExitTime = true; toLoop.exitTime = 1f; toLoop.duration = 0f;

        var toImpact = sm.AddAnyStateTransition(impactState);
        toImpact.hasExitTime = false; toImpact.duration = 0f; toImpact.canTransitionToSelf = false;
        toImpact.AddCondition(AnimatorConditionMode.If, 0, "Impact");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    //hens: a still Idle and a Pop
    public static AnimatorController MakeEnemyController(string name, AnimationClip idle, AnimationClip pop)
    {
        string path = $"{AnimDir}/{name}.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        var sm = controller.layers[0].stateMachine;
        foreach (var s in sm.states.Clone() as ChildAnimatorState[]) sm.RemoveState(s.state);
        foreach (var t in sm.anyStateTransitions) sm.RemoveAnyStateTransition(t);
        if (!HasParam(controller, "Pop")) controller.AddParameter("Pop", AnimatorControllerParameterType.Trigger);

        var idleState = sm.AddState("Idle", new Vector3(0, 0));
        idleState.motion = idle;
        sm.defaultState = idleState;
        var popState = sm.AddState("Pop", new Vector3(300, 0));
        popState.motion = pop;

        var toPop = sm.AddAnyStateTransition(popState);
        toPop.hasExitTime = false; toPop.duration = 0f; toPop.canTransitionToSelf = false;
        toPop.AddCondition(AnimatorConditionMode.If, 0, "Pop");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    static bool HasParam(AnimatorController c, string name)
    {
        foreach (var p in c.parameters) if (p.name == name) return true;
        return false;
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    //the exploding hog sheets are 4x4 grids on 1024 px: uniform cells so the body never jumps between
    //frames, PPU 160 so it is as wide as the neutral hog, pivot a little below centre so the fuse spark
    //on top doesn't push the body down in the pouch
    public static void ImportExplodingHogArt()
    {
        var pivot = new Vector2(0.5f, 0.42f);
        foreach (var sheet in new[] { "ExplodingHog_Idle", "ExplodingHog_Fly", "ExplodingHog_Impact" })
            SpriteSheetImporter.ImportBands($"Assets/Art/Hogs/Exploding/{sheet}.png", 160f, 24, 4, 1.6f, pivot, true, false);
    }

    //all hog + hen clips and controllers. returns the controllers keyed by name
    public static Dictionary<string, AnimatorController> BuildAnimations()
    {
        var result = new Dictionary<string, AnimatorController>();

        ImportExplodingHogArt();
        var expIdle = SpriteSheetImporter.LoadSprites("Assets/Art/Hogs/Exploding/ExplodingHog_Idle.png");
        var expFly = SpriteSheetImporter.LoadSprites("Assets/Art/Hogs/Exploding/ExplodingHog_Fly.png");
        var expImpact = SpriteSheetImporter.LoadSprites("Assets/Art/Hogs/Exploding/ExplodingHog_Impact.png");
        //landing = the fly sheet's curl played backwards, then the impact sheet's wide-eyed puffing frames
        //while the fuse burns (the ExplodingHog script swells and flushes the sprite on top of this)
        var landing = new List<Sprite> { expFly[7], expFly[6], expFly[5], expFly[4] };
        landing.AddRange(Range(expImpact, 8, 15));
        result["ExplodingHog"] = MakeHogController("ExplodingHog",
            MakeClip("ExplodingHog_Idle", expIdle, 6f, true),
            MakeClip("ExplodingHog_Flying", Range(expFly, 0, 5), 12f, false),
            MakeClip("ExplodingHog_FlyingLoop", Range(expFly, 6, 15), 12f, true),
            MakeClip("ExplodingHog_Impact", landing.ToArray(), 10f, false));

        var cluIdle = SpriteSheetImporter.LoadSprites("Assets/Art/Hogs/Cluster/ClusterHog_Idle.png");
        var cluFly = SpriteSheetImporter.LoadSprites("Assets/Art/Hogs/Cluster/ClusterHog_Fly.png");
        //the cluster hog never lands in one piece (it bursts on impact), so Impact just holds the ball
        result["ClusterHog"] = MakeHogController("ClusterHog",
            MakeClip("ClusterHog_Idle", cluIdle, 6f, true),
            MakeClip("ClusterHog_Flying", Range(cluFly, 0, 5), 12f, false),
            MakeClip("ClusterHog_FlyingLoop", Range(cluFly, 6, 15), 12f, true),
            MakeClip("ClusterHog_Impact", Range(cluFly, 12, 15), 12f, true));

        var mini = SpriteSheetImporter.LoadSprites("Assets/Art/Hogs/Cluster/MiniHog.png");
        result["MiniHog"] = MakeHogController("MiniHog",
            MakeClip("MiniHog_Idle", Pick(mini, 0), 6f, true),
            MakeClip("MiniHog_Flying", Range(mini, 0, 2), 12f, false),
            MakeClip("MiniHog_FlyingLoop", Range(mini, 3, 10), 14f, true),
            MakeClip("MiniHog_Impact", Range(mini, 11, 14), 8f, true));

        var henIdle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Enemies/Chicken_Single.png");
        var henPop = SpriteSheetImporter.LoadSprites("Assets/Art/Enemies/Chicken_Pop.png");
        result["Hen"] = MakeEnemyController("Hen",
            MakeClip("Hen_Idle", new[] { henIdle }, 1f, true),
            MakeClip("Hen_Pop", henPop, 14f, false));

        var bossIdle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Enemies/BossChicken_Single.png");
        result["BossHen"] = MakeEnemyController("BossHen",
            MakeClip("BossHen_Idle", new[] { bossIdle }, 1f, true),
            MakeClip("BossHen_Pop", henPop, 14f, false));

        AssetDatabase.SaveAssets();
        return result;
    }
}
