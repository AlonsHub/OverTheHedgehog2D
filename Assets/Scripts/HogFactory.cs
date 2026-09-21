using System;
using System.Collections.Generic;
using UnityEngine;

//the one place that knows which prefab belongs to which HogType
public class HogFactory : MonoBehaviour
{
    [Serializable]
    private struct HogPrefabEntry
    {
        public HogType type;
        public Hog prefab;
    }

    [SerializeField] private List<HogPrefabEntry> catalogue = new List<HogPrefabEntry>();

    private Dictionary<HogType, Hog> _prefabsByType;

    private void Awake()
    {
        BuildLookup();
    }

    public Hog Create(HogType type, Transform parent = null)
    {
        //someone (probably a HogStock) asked before our Awake ran
        if (_prefabsByType == null)
            BuildLookup();

        if (!_prefabsByType.TryGetValue(type, out Hog prefab))
        {
            Debug.LogError($"HogFactory has no prefab for HogType.{type} - add it to the catalogue", this);
            return null;
        }

        Hog hog = Instantiate(prefab, parent);
        hog.name = prefab.name; //drop the "(Clone)"
        return hog;
    }

    private void BuildLookup()
    {
        _prefabsByType = new Dictionary<HogType, Hog>();

        foreach (var entry in catalogue)
        {
            if (entry.prefab == null)
            {
                Debug.LogWarning($"HogFactory: HogType.{entry.type} has no prefab assigned", this);
                continue;
            }
            if (_prefabsByType.ContainsKey(entry.type))
            {
                Debug.LogWarning($"HogFactory: HogType.{entry.type} is in the catalogue twice, using the first one", this);
                continue;
            }

            _prefabsByType.Add(entry.type, entry.prefab);
        }
    }
}
