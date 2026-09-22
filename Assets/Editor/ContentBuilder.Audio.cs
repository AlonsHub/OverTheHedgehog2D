using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

//the SoundBank asset: every wav in Assets/Audio/SFX, with the mix levels below
public static partial class ContentBuilder
{
    const string SfxDir = "Assets/Audio/SFX";

    //name, volume, pitch min, pitch max, cooldown
    static readonly (string name, float vol, float pMin, float pMax, float cd)[] Mix =
    {
        //ui: soft
        ("ui_click", 0.55f, 0.97f, 1.03f, 0.05f),
        ("ui_hover", 0.35f, 0.97f, 1.05f, 0.04f),
        ("ui_back", 0.55f, 0.97f, 1.03f, 0.05f),
        ("node_pop", 0.5f, 0.9f, 1.25f, 0.03f),
        ("score_tick", 0.3f, 0.95f, 1.1f, 0.06f),
        ("star", 0.55f, 0.95f, 1.1f, 0.05f),
        //slingshot
        ("grab", 0.6f, 0.95f, 1.05f, 0.05f),
        ("stretch", 0.45f, 0.95f, 1.05f, 0.15f),
        ("launch", 0.8f, 0.95f, 1.05f, 0.1f),
        ("hog_walk", 0.35f, 0.9f, 1.1f, 0.1f),
        //hogs
        ("hog_impact", 0.75f, 0.9f, 1.1f, 0.05f),
        ("mini_impact", 0.55f, 0.9f, 1.2f, 0.03f),
        ("hog_pop", 0.5f, 0.9f, 1.15f, 0.05f),
        ("mini_squeak", 0.45f, 0.9f, 1.3f, 0.02f),
        ("cluster_split", 0.8f, 0.95f, 1.05f, 0.1f),
        ("fuse", 0.5f, 0.95f, 1.05f, 0.2f),
        ("explosion", 0.7f, 0.95f, 1.05f, 0.2f),
        ("dust_poof", 0.4f, 0.85f, 1.15f, 0.05f),
        ("wood_knock", 0.6f, 0.9f, 1.1f, 0.05f),
        ("wood_break", 0.8f, 0.9f, 1.1f, 0.05f),
        //hens: the stars of the show
        ("hen_cluck", 0.4f, 0.85f, 1.2f, 0.3f),
        ("hen_hit", 1f, 0.92f, 1.1f, 0.08f),
        ("hen_pop", 1f, 0.92f, 1.08f, 0.08f),
        ("boss_hit", 1f, 0.95f, 1.05f, 0.2f),
        ("boss_pop", 1f, 1f, 1f, 0.5f),
        //results
        ("win", 0.8f, 1f, 1f, 0.5f),
        ("boss_win", 0.8f, 1f, 1f, 0.5f),
        ("lose", 0.7f, 1f, 1f, 0.5f),
    };

    //the looping tracks from Tools/Audio/music.mjs: streamed Vorbis, so the 2.5 MB wavs cost nothing in memory
    public static void ImportMusic()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Music" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) continue;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.loadInBackground = true;
            importer.SaveAndReimport();
        }
    }

    public static void BuildSoundBank()
    {
        ImportMusic();
        EnsureFolder("Assets/Resources");
        string path = $"Assets/Resources/{SoundBank.ResourcePath}.asset";
        var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(path);
        if (bank == null)
        {
            bank = ScriptableObject.CreateInstance<SoundBank>();
            AssetDatabase.CreateAsset(bank, path);
        }

        foreach (var (name, vol, pMin, pMax, cd) in Mix)
        {
            string clipPath = $"{SfxDir}/{name}.wav";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null) { Debug.LogWarning($"SoundBank: missing {clipPath}"); continue; }

            //short one-shots: uncompressed, no load delay
            var importer = AssetImporter.GetAtPath(clipPath) as AudioImporter;
            if (importer != null)
            {
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            }

            bank.Set(new SoundBank.Entry { name = name, clips = new[] { clip }, volume = vol, pitchMin = pMin, pitchMax = pMax, cooldown = cd });
        }

        EditorUtility.SetDirty(bank);
        AssetDatabase.SaveAssets();
        Debug.Log($"ContentBuilder: sound bank built ({Mix.Length} sounds)");
    }

    //every button in a hierarchy gets a click; "back"-ish ones get the lower tone
    static void AddButtonSounds(Transform root)
    {
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            var sfx = button.GetComponent<ButtonSfx>();
            if (sfx == null) sfx = button.gameObject.AddComponent<ButtonSfx>();
            string name = button.name.ToLowerInvariant();
            bool back = name.Contains("back") || name.Contains("map") || name.Contains("reset") || name.Contains("skip");
            var so = new SerializedObject(sfx);
            so.FindProperty("sound").stringValue = back ? "ui_back" : "ui_click";
            so.ApplyModifiedPropertiesWithoutUndo();

            //hover/press feel: ButtonFeel drives scale, lift and tint itself, so the Button stops tinting
            if (button.GetComponent<ButtonFeel>() == null) button.gameObject.AddComponent<ButtonFeel>();
            button.transition = Selectable.Transition.None;
        }
    }
}
