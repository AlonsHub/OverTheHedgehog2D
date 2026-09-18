using System.Collections.Generic;
using UnityEngine;

public class HogStock : MonoBehaviour
{
    //holds the upcoming Hogs to load
    [SerializeField] private Transform stockPoint_A;
    [SerializeField] private float hogWidth;
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

        Vector3 pos = stockPoint_A.position;
        foreach (var hog in magazine)
        {
            hog.transform.position = pos;
            pos.x -= hogWidth;
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
