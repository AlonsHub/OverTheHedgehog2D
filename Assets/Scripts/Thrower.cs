using UnityEngine;

public class Thrower : MonoBehaviour
{
    //recieve hogs of all types

    //active when hog is loaded, deactivates self after throwing

    //pulling on loaded hog: adjust rubber band display, pre-calc route and display dots (with gardient fade, but with option for full line)

    //releasing loaded hog: check if pulled enough, throw hog according to pull, deactivate

    //request new hog from magazine?

    [SerializeField] private bool isLoaded; //this is also isActive
    public bool IsLoaded { get { return isLoaded; } }
    //[SerializeField] private bool ;
    [SerializeField] float throwForce;
    [SerializeField] private Transform anchor;
    [SerializeField] private HogStock stock;
    [SerializeField] private Grabber grabber;

    private Hog _loadedHog;
    //private bool isGrabbing => grabber.isGrabbing;

    private void Start()
    {
        LoadHogFromStock();
    }

    [ContextMenu("Load Next Hog!")]
    public void LoadHogFromStock()
    {
        Hog hog = stock.GetNextHog();

        if(hog == null)
        {
            //game over
        }

        _loadedHog = hog;
        _loadedHog.transform.parent = grabber.transform;
        _loadedHog.transform.localPosition = Vector3.zero;

        isLoaded = true;
    }
    public void Throw()
    {
        isLoaded = false;

        _loadedHog.transform.SetParent(null);
        _loadedHog.rb.simulated = true;

        Vector3 force = (anchor.position - grabber.transform.position ) * throwForce;

        _loadedHog.rb.AddForce(force, ForceMode2D.Impulse);

        //start loadingNextHog sequence
        LoadHogFromStock();
    }

}
