using System.Collections.Generic;
using UnityEngine;

public class HogStock : MonoBehaviour
{
    //holds the upcoming Hogs to load
    [SerializeField] private List<Hog> hogsToQueue;
    [SerializeField] private Queue<Hog> magazine;

    private void Awake()
    {
        magazine = new Queue<Hog>();

        foreach (var hog in hogsToQueue)
        {
            //magazine.Enqueue(hog);
            LoadHogToStock(hog);
        }
    }

    public void LoadHogToStock(Hog hog)
    {
        //if(magazine == null) magazine = new Queue<Hog>();

        magazine.Enqueue(hog);
    }

    public Hog GetNextHog()
    {
        if( magazine == null || magazine.Count==0)
            return null;

        return magazine.Dequeue();
    }
}
