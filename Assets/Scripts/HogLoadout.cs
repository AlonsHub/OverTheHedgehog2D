using System;
using System.Collections.Generic;
using UnityEngine;

//config asset: which hogs (and how many of each) a HogStock starts with.
//entries are loaded in list order, so the first entry is the first thing thrown
[CreateAssetMenu(fileName = "HogLoadout", menuName = "Over The Hedgehog/Hog Loadout")]
public class HogLoadout : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public HogType type;
        [Min(0)] public int amount;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public int TotalCount
    {
        get
        {
            int total = 0;
            foreach (var entry in entries)
                total += entry.amount;
            return total;
        }
    }

    //flattens the entries into one hog type per hog, in queue order
    public IEnumerable<HogType> Expand()
    {
        foreach (var entry in entries)
        {
            for (int i = 0; i < entry.amount; i++)
                yield return entry.type;
        }
    }
}
