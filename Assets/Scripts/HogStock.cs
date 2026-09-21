using System.Collections.Generic;
using UnityEngine;

public class HogStock : MonoBehaviour
{
    //holds the upcoming Hogs to load
    [SerializeField] private Transform stockPoint_A;
    [SerializeField] private float hogWidth;
    [SerializeField] private HogFactory factory;
    [SerializeField] private HogLoadout loadout;
    [Tooltip("Gap between each hog starting to walk, so the line shuffles up one by one instead of sliding as a block")]
    [SerializeField] private float shuffleDelayPerHog = 0.05f;

    private Queue<Hog> magazine;

    public int Count => magazine == null ? 0 : magazine.Count;

    private void Awake()
    {
        magazine = new Queue<Hog>();

        if (factory == null || loadout == null)
        {
            Debug.LogError("HogStock needs a HogFactory and a HogLoadout assigned", this);
            return;
        }

        foreach (HogType type in loadout.Expand())
        {
            Hog hog = factory.Create(type, transform);
            if (hog != null)
                LoadHogToStock(hog);
        }
    }

    //adds a hog to the back of the line
    public void LoadHogToStock(Hog hog)
    {
        hog.transform.SetParent(transform);
        hog.transform.position = SlotPosition(magazine.Count);

        magazine.Enqueue(hog);
    }

    public Hog GetNextHog()
    {
        if( magazine == null || magazine.Count==0)
            return null;
        Hog toReturn = magazine.Dequeue();
        toReturn.StopWalking(); //might still be shuffling up - the thrower owns it now

        //everyone else walks one spot forward
        int slot = 0;
        foreach (var hog in magazine)
        {
            hog.WalkTo(SlotPosition(slot), slot * shuffleDelayPerHog);
            slot++;
        }

        return toReturn;
    }

    //slot 0 is the front of the line (stockPoint_A), the rest trail off behind it
    private Vector3 SlotPosition(int slot)
    {
        return stockPoint_A.position + Vector3.left * (hogWidth * slot);
    }
}
