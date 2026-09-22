using DG.Tweening;
using UnityEngine;

//one looping music bed that lives across scenes. Play(name) crossfades to Resources/Music/<name>; asking
//for the track that is already playing does nothing, so the garden theme carries from the map into a level
public class Music : MonoBehaviour
{
    const string ResourceDir = "Music/";
    static Music _instance;

    AudioSource _a, _b;   //current and next, swapped on every crossfade
    string _current;

    static Music Instance
    {
        get
        {
            if (_instance != null) return _instance;
            var go = new GameObject("Music");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<Music>();
            _instance._a = Make(go); _instance._b = Make(go);
            return _instance;
        }
    }

    static AudioSource Make(GameObject go)
    {
        var src = go.AddComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = 0f;
        return src;
    }

    public static void Play(string name, float fade = 1.2f, float level = 0.5f) => Instance.PlayInternal(name, fade, level);
    public static void Stop(float fade = 1f) => Instance.PlayInternal(null, fade, 0f);
    public static string Current => _instance != null ? _instance._current : null;

    void PlayInternal(string name, float fade, float level)
    {
        if (name == _current) { if (name != null) _a.DOFade(level, fade); return; }
        _current = name;
        var clip = name != null ? Resources.Load<AudioClip>(ResourceDir + name) : null;
        if (name != null && clip == null) Debug.LogWarning($"Music: no clip at Resources/{ResourceDir}{name}");

        //fade the old one out on its own source, bring the new one up on the other, then swap
        var old = _a; _a = _b; _b = old;
        old.DOKill(); _a.DOKill();
        old.DOFade(0f, fade).SetUpdate(true).OnComplete(() => old.Stop());
        if (clip != null)
        {
            _a.clip = clip;
            _a.volume = 0f;
            _a.Play();
            _a.DOFade(level, fade).SetUpdate(true);
        }
    }
}
