using System.Collections.Generic;
using UnityEngine;

//the ordered list of levels. lives in Resources so any scene can grab it without a scene reference
[CreateAssetMenu(fileName = "LevelCatalogue", menuName = "Over The Hedgehog/Level Catalogue")]
public class LevelCatalogue : ScriptableObject
{
    public const string ResourcePath = "LevelCatalogue";

    [SerializeField] private List<LevelDefinition> levels = new List<LevelDefinition>();

    public IReadOnlyList<LevelDefinition> Levels => levels;
    public int Count => levels.Count;

    static LevelCatalogue _instance;
    public static LevelCatalogue Load()
    {
        if (_instance == null)
            _instance = Resources.Load<LevelCatalogue>(ResourcePath);
        if (_instance == null)
            Debug.LogError($"No LevelCatalogue asset at Resources/{ResourcePath}");
        return _instance;
    }

    public LevelDefinition Get(int index)
    {
        if (levels.Count == 0) return null;
        return levels[Mathf.Clamp(index, 0, levels.Count - 1)];
    }

    public int IndexOf(LevelDefinition level) => levels.IndexOf(level);
}
