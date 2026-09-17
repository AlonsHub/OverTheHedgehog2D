using UnityEngine;

public class Thrower : MonoBehaviour
{
    //recieve hogs of all types

    //active when hog is loaded, deactivates self after throwing

    //pulling on loaded hog: adjust rubber band display, pre-calc route and display dots (with gardient fade, but with option for full line)

    //releasing loaded hog: check if pulled enough, throw hog according to pull, deactivate

    //request new hog from magazine?

    [SerializeField] private bool isLoaded; //this is also isActive
    //[SerializeField] private bool ;

    [SerializeField] private HogStock stock;
    [SerializeField] private Grabber grabber;

    //private bool isGrabbing => grabber.isGrabbing;


}
