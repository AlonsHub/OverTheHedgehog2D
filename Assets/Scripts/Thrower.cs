using System.Collections;
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
    [SerializeField] float snapTime;
    [SerializeField] float snapTimeAddPerDistnace;
    [SerializeField] float snapAcceleration;
    [SerializeField] private Transform anchor;
    [SerializeField] private HogStock stock;
    [SerializeField] private Grabber grabber;
    //[SerializeField] private LineRenderer lineRenderer;
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

        Vector3 delta = (anchor.position - grabber.transform.position );

        StartCoroutine(ThrowCoroutine(delta));

        //_loadedHog.rb.AddForce(force, ForceMode2D.Impulse);

        ////start loadingNextHog sequence
        //LoadHogFromStock();
    }

    IEnumerator ThrowCoroutine(Vector3 delta)
    {
        //float delta = (anchor.position - grabber.transform.position).sqrMagnitude;
        float distnace = delta.sqrMagnitude;
        float t = 0f;
        Vector3 ogPos = grabber.transform.position;
        float fullTime = snapTime + distnace * snapTimeAddPerDistnace;
        //while (Mathf.Approximately(distnace, 0.0f))
        float accel = 0f;
        while (t<= fullTime)
        {
            grabber.transform.position = Vector3.Lerp(ogPos, anchor.position, t/fullTime) ;
            
            yield return null;
            t += Time.deltaTime + accel;
            accel += snapAcceleration * Time.deltaTime;
            //distnace = (anchor.position - grabber.transform.position).sqrMagnitude;
        }

        _loadedHog.transform.SetParent(null);
        _loadedHog.rb.simulated = true;

        _loadedHog.rb.AddForce(delta * throwForce, ForceMode2D.Impulse);

        isLoaded = false;

        //start loadingNextHog sequence
        LoadHogFromStock();
    }

}
