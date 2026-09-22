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
    //a hog is bounding into the pouch but has not settled yet: not grabbable, but not out of hogs either
    public bool IsLoading => !isLoaded && loadedHog != null && !IsThrowing;
    //between letting go and the next hog being loaded: the band is snapping, the hog is on its way
    public bool IsThrowing { get; private set; }
    //[SerializeField] private bool ;
    [SerializeField] public float throwForce;
    [SerializeField] float snapTime;
    [SerializeField] float snapTimeAddPerDistnace;
    [SerializeField] float snapAcceleration;
    [SerializeField] float overshootFactor;
    [SerializeField] float overshootTime;
    [SerializeField] public Transform anchor;
    [SerializeField] private HogStock stock;
    [SerializeField] private Grabber grabber;
    //[SerializeField] private LineRenderer lineRenderer;
    public Hog loadedHog;
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
            isLoaded = false;
            loadedHog = null;
            return;
        }

        loadedHog = hog;
        //it bounds from the front of the line into the pouch; grabbable once it has settled
        isLoaded = false;
        loadedHog.HopInto(grabber.transform, 0.4f, () => isLoaded = true);
    }
    public void Throw()
    {

        Vector3 delta = (anchor.position - grabber.transform.position );

        IsThrowing = true;
        Sfx.Play("launch");
        StartCoroutine(ThrowCoroutine(delta));

        //_loadedHog.rb.AddForce(force, ForceMode2D.Impulse);

        ////start loadingNextHog sequence
        //LoadHogFromStock();
    }

    IEnumerator ThrowCoroutine(Vector3 delta)
    {
        //Sending the Fly here lets the tearie eyed anim to s
        loadedHog.Fly();

        //float delta = (anchor.position - grabber.transform.position).sqrMagnitude;
        float _distnace = delta.sqrMagnitude;
        float t = 0f;
        Vector3 ogPos = grabber.transform.position;
        float fullTime = snapTime + _distnace * snapTimeAddPerDistnace;
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

        grabber.transform.position = anchor.position;

        //this could be a good time to send a message to the hog to play its fly animation
        // _loadedHog.Fly();

        //the hog lets go of us and physics takes over
        loadedHog.Launch(delta * throwForce);

        isLoaded = false;

        //start Warmup animation for the next hog - weave it!
        Vector3 os_Destination = anchor.position + delta * overshootFactor;
        Vector3 og_Pos = grabber.transform.position;

        float _halfOvershootTime = overshootTime/2;
        t = 0f;
        while (t <= _halfOvershootTime)
        {
            //overshoot
            grabber.transform.position = Vector3.Lerp(og_Pos, os_Destination, t / _halfOvershootTime);


            yield return null;
            t += Time.deltaTime;
        }
        t = 0f;
        while (t <= _halfOvershootTime)
        {
            //back
            grabber.transform.position = Vector3.Lerp(os_Destination, og_Pos, t / _halfOvershootTime);


            yield return null;
            t += Time.deltaTime;
        }


        //start loadingNextHog sequence
        LoadHogFromStock();
        IsThrowing = false;
    }

}
