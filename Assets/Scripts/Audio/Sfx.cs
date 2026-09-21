using System.Collections.Generic;
using UnityEngine;

//fire-and-forget sound effects: Sfx.Play("hen_pop"). spawns itself on first use, survives scene loads,
//and round-robins a small pool of AudioSources. everything is 2D (it's a side-on game, panning would just
//be distracting) and the mix lives in the SoundBank asset
public class Sfx : MonoBehaviour
{
    const int PoolSize = 12;

    static Sfx _instance;
    SoundBank _bank;
    readonly List<AudioSource> _pool = new List<AudioSource>();
    readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
    int _next;

    static Sfx Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("Sfx");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<Sfx>();
                _instance.Setup();
            }
            return _instance;
        }
    }

    void Setup()
    {
        _bank = Resources.Load<SoundBank>(SoundBank.ResourcePath);
        if (_bank == null) Debug.LogWarning($"Sfx: no SoundBank at Resources/{SoundBank.ResourcePath}, sounds are off");

        for (int i = 0; i < PoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            _pool.Add(src);
        }
    }

    //volumeScale lets a caller duck a sound (a light tap plays a quieter knock), pitchScale shifts it
    public static AudioSource Play(string name, float volumeScale = 1f, float pitchScale = 1f)
    {
        var sfx = Instance;
        if (sfx._bank == null || string.IsNullOrEmpty(name)) return null;

        var entry = sfx._bank.Get(name);
        if (entry == null)
        {
            Debug.LogWarning($"Sfx: no sound named '{name}' in the SoundBank");
            return null;
        }
        if (entry.clips == null || entry.clips.Length == 0) return null;

        //cooldown: a pile of blocks landing at once shouldn't stack twenty knocks
        if (sfx._lastPlayed.TryGetValue(name, out float last) && Time.unscaledTime - last < entry.cooldown)
            return null;
        sfx._lastPlayed[name] = Time.unscaledTime;

        var clip = entry.clips[Random.Range(0, entry.clips.Length)];
        if (clip == null) return null;

        var src = sfx._pool[sfx._next];
        sfx._next = (sfx._next + 1) % sfx._pool.Count;

        src.Stop();
        src.clip = clip;
        src.volume = Mathf.Clamp01(entry.volume * volumeScale * sfx._bank.masterVolume);
        src.pitch = Random.Range(entry.pitchMin, entry.pitchMax) * pitchScale;
        //UI sounds should still play at normal speed when the game is paused or slowed
        src.ignoreListenerPause = true;
        src.Play();
        return src;
    }

    //same, but a little later. handy for syncing with a tween
    public static void PlayDelayed(string name, float delay, float volumeScale = 1f)
    {
        Instance.StartCoroutine(Instance.DelayedRoutine(name, delay, volumeScale));
    }

    System.Collections.IEnumerator DelayedRoutine(string name, float delay, float volumeScale)
    {
        yield return new WaitForSecondsRealtime(delay);
        Play(name, volumeScale);
    }
}
