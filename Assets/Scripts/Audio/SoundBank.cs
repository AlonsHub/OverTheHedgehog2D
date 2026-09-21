using System;
using System.Collections.Generic;
using UnityEngine;

//every sound the game can play, by name. lives in Resources so Sfx can find it without scene wiring.
//volume/pitch/cooldown live here so mixing is a data tweak, not a code change
[CreateAssetMenu(fileName = "SoundBank", menuName = "Over The Hedgehog/Sound Bank")]
public class SoundBank : ScriptableObject
{
    public const string ResourcePath = "SoundBank";

    [Serializable]
    public class Entry
    {
        public string name;
        [Tooltip("One is picked at random when there are several")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Random pitch range so repeats don't sound like a machine gun")]
        public float pitchMin = 0.95f;
        public float pitchMax = 1.05f;
        [Tooltip("Won't play again within this many seconds")]
        public float cooldown = 0.03f;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();
    [Range(0f, 1f)] public float masterVolume = 1f;

    Dictionary<string, Entry> _lookup;

    public Entry Get(string name)
    {
        if (_lookup == null)
        {
            _lookup = new Dictionary<string, Entry>();
            foreach (var e in entries)
                if (e != null && !string.IsNullOrEmpty(e.name) && !_lookup.ContainsKey(e.name))
                    _lookup.Add(e.name, e);
        }
        _lookup.TryGetValue(name, out var entry);
        return entry;
    }

    public void Set(Entry entry)
    {
        int i = entries.FindIndex(e => e.name == entry.name);
        if (i >= 0) entries[i] = entry; else entries.Add(entry);
        _lookup = null;
    }
}
