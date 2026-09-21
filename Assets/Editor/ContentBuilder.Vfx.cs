using UnityEditor;
using UnityEngine;

//particle prefabs: dust poof, explosion cloud, feather burst
public static partial class ContentBuilder
{
    static Material VfxMaterial(string name, Texture2D texture)
    {
        EnsureFolder("Assets/Materials/VFX");
        string path = $"Assets/Materials/VFX/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.mainTexture = texture;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static ParticleSystem NewSystem(string name, Material mat, Sprite[] sprites, int sortingOrder)
    {
        var go = new GameObject(name);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.duration = 1f;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.sortingOrder = sortingOrder;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;

        var tsa = ps.textureSheetAnimation;
        tsa.enabled = true;
        tsa.mode = ParticleSystemAnimationMode.Sprites;
        tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
        //random start frame picks a random sprite for the whole life
        tsa.startFrame = new ParticleSystem.MinMaxCurve(0f, 0.999f);
        tsa.cycleCount = 1;
        //clear, then add
        while (tsa.spriteCount > 0) tsa.RemoveSprite(0);
        foreach (var s in sprites) tsa.AddSprite(s);

        return ps;
    }

    static ParticleSystem.MinMaxGradient FadeOut(Color tint)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(tint, 0f), new GradientColorKey(tint, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.45f), new GradientAlphaKey(0f, 1f) });
        return new ParticleSystem.MinMaxGradient(g);
    }

    //Unity insists every limit curve is in the same mode, so set the axes along with the magnitude
    static void Damp(ParticleSystem ps, float dampen, float limit)
    {
        var lim = ps.limitVelocityOverLifetime;
        lim.enabled = true;
        lim.separateAxes = false;
        lim.dampen = dampen;
        var c = new ParticleSystem.MinMaxCurve(limit);
        lim.limitX = c; lim.limitY = c; lim.limitZ = c; lim.limit = c;
    }

    static void Spin(ParticleSystem ps, float min, float max)
    {
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.separateAxes = false;
        var c = new ParticleSystem.MinMaxCurve(min, max);
        rot.x = c; rot.y = c; rot.z = c;
    }

    static AnimationCurve Grow(float from, float to)
    {
        return AnimationCurve.EaseInOut(0f, from, 1f, to);
    }

    static GameObject SavePrefab(GameObject go, string path)
    {
        EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
        var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return saved;
    }

    public static void BuildVfx()
    {
        var dustTex = LoadOrThrow<Texture2D>("Assets/Art/VFX/DustPoof.png");
        var dustSprite = LoadOrThrow<Sprite>("Assets/Art/VFX/DustPoof.png");
        var cloudTex = LoadOrThrow<Texture2D>("Assets/Art/VFX/ExplosionCloud.png");
        var cloudSprite = LoadOrThrow<Sprite>("Assets/Art/VFX/ExplosionCloud.png");
        var featherTex = LoadOrThrow<Texture2D>("Assets/Art/VFX/Feathers.png");
        var featherSprites = SpriteSheetImporter.LoadSprites("Assets/Art/VFX/Feathers.png");

        //dust poof: a handful of puffs that swell and fade
        {
            var ps = NewSystem("VFX_DustPoof", VfxMaterial("VFX_Dust", dustTex), new[] { dustSprite }, 6);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.gravityModifier = -0.15f;
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 8) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeOut(Color.white);
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, Grow(0.6f, 1.4f));
            Spin(ps, -1.5f, 1.5f);
            Damp(ps, 0.35f, 0.5f);
            SavePrefab(ps.gameObject, $"{VfxDir}/VFX_DustPoof.prefab");
        }

        //explosion: a big orange bloom of clouds plus a ring of small cream puffs
        {
            var ps = NewSystem("VFX_Explosion", VfxMaterial("VFX_Explosion", cloudTex), new[] { cloudSprite }, 7);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.gravityModifier = -0.1f;
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 10) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeOut(Color.white);
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, Grow(0.5f, 1.3f));
            Damp(ps, 0.5f, 0.5f);

            var puffs = NewSystem("Puffs", VfxMaterial("VFX_Dust", dustTex), new[] { dustSprite }, 8);
            puffs.transform.SetParent(ps.transform, false);
            var pm = puffs.main;
            pm.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            pm.startSpeed = new ParticleSystem.MinMaxCurve(5f, 8f);
            pm.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            pm.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            pm.startDelay = 0.03f;
            var pe = puffs.emission; pe.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 12) });
            var pshape = puffs.shape; pshape.shapeType = ParticleSystemShapeType.Circle; pshape.radius = 0.2f;
            var pcol = puffs.colorOverLifetime; pcol.enabled = true; pcol.color = FadeOut(Color.white);
            Damp(puffs, 0.6f, 0.3f);

            SavePrefab(ps.gameObject, $"{VfxDir}/VFX_Explosion.prefab");
        }

        //feathers: a flurry that drifts down
        {
            var ps = NewSystem("VFX_Feathers", VfxMaterial("VFX_Feathers", featherTex), featherSprites, 6);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.gravityModifier = 0.25f;
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14, 18) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeOut(Color.white);
            Spin(ps, -3f, 3f);
            Damp(ps, 0.15f, 1.2f);
            //a sideways wobble so they flutter rather than drop
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.8f; noise.scrollSpeed = 0.5f;
            SavePrefab(ps.gameObject, $"{VfxDir}/VFX_Feathers.prefab");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("ContentBuilder: VFX built");
    }
}
