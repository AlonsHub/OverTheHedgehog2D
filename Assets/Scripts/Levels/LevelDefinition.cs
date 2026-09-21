using UnityEngine;

//one level: which structure to drop into the garden, where, and which hogs the player gets for it
[CreateAssetMenu(fileName = "Level", menuName = "Over The Hedgehog/Level")]
public class LevelDefinition : ScriptableObject
{
    public string title = "Level";
    [TextArea] public string blurb;
    [Tooltip("BlockSet prefab (blocks + hens) spawned into the GameScene")]
    public GameObject blockSet;
    public Vector3 blockSetPosition;
    public HogLoadout loadout;
    [Tooltip("Marks it on the map and ups the fanfare")]
    public bool isBoss;
}
