using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Hog : MonoBehaviour
{
    //every hog that has been launched and hasn't popped yet (mini hogs included). the GameManager
    //waits for this to empty before calling the level lost
    public static readonly List<Hog> airborne = new List<Hog>();

    public Rigidbody2D rb;
    [SerializeField] protected Animator anim;
    [SerializeField] private float walkSpeed;
    [SerializeField] protected Collider2D col;

    [Header("VFX")]
    [Tooltip("Spawned at the contact point when we hit something (dust poof)")]
    [SerializeField] private GameObject impactVfx;
    [Tooltip("Below this world y we're gone for good and get cleaned up")]
    [SerializeField] private float killY = -15f;

    [Header("Death pop")]
    [Tooltip("Launch velocity off the impact point, units/sec. Negative x = knocked back the way it came, y = up. Gravity takes it from there")]
    [SerializeField] private Vector2 popVelocity = new Vector2(-3f, 8f);
    [Tooltip("Units/sec towards the camera (looms) or away from it (shrinks)")]
    [SerializeField] private float popDepthSpeed = 6f;
    [Range(0f, 1f)]
    [SerializeField] private float towardCameraChance = 0.6f;
    [Tooltip("Degrees/sec of tumble, direction picked at random")]
    [SerializeField] private float popSpin = 300f;
    [SerializeField] private float popDuration = 1.5f;
    [Tooltip("Fades out over this last part of the pop so it doesn't just blink away")]
    [Range(0f, 1f)]
    [SerializeField] private float popFadeFraction = 0.3f;
    [Tooltip("0 = every hog pops identically, 1 = wild. Scales velocity/depth/spin by up to this fraction")]
    [Range(0f, 1f)]
    [SerializeField] private float popRandomness = 0.35f;

    //state?
    bool _impacted = false;
    bool _launched = false;
    Tween _walkTween;

    public bool IsLaunched => _launched;
    public bool HasImpacted => _impacted;
    //in the air between the throw and whatever it hits first
    public bool IsFlying => _launched && !_impacted;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(_impacted) return;

        _impacted = true;

        if (impactVfx != null)
        {
            Vector3 at = collision.contactCount > 0 ? (Vector3)collision.GetContact(0).point : transform.position;
            Vfx.Spawn(impactVfx, at);
        }

        Impact();
    }

    void Update()
    {
        //fell off the world without ever hitting anything
        if (_launched && !_impacted && transform.position.y < killY)
            Destroy(gameObject);
    }

    void OnDestroy()
    {
        airborne.Remove(this);
    }

    //shuffles forward to a new spot in the stock line at a steady walkSpeed (no speed set = snap there)
    public void WalkTo(Vector3 target, float delay = 0f)
    {
        StopWalking();

        float duration = walkSpeed > 0f ? Vector3.Distance(transform.position, target) / walkSpeed : 0f;
        _walkTween = transform.DOMove(target, duration)
            .SetEase(Ease.Linear)
            .SetDelay(delay)
            .SetLink(gameObject);
    }
    public void StopWalking()
    {
        _walkTween?.Kill();
        _walkTween = null;
    }
    public void Fly()
    {
        anim.SetTrigger("Fly");
    }

    //lets go of whatever was holding us and sends us off with an impulse. from here on physics owns the hog
    public void Launch(Vector2 impulse)
    {
        transform.SetParent(null);
        rb.simulated = true;
        rb.AddForce(impulse, ForceMode2D.Impulse);

        _launched = true;
        if (!airborne.Contains(this))
            airborne.Add(this);
    }

    public virtual void Impact()
    {
        anim.SetTrigger("Impact");
        PopOff();
    }

    //popcorn: knocked back off whatever we hit and up, then gravity bends that into an arc while it
    //flies towards the camera (looming) or away from it (shrinking), tumbling as it goes.
    //destroys the hog when done
    protected Tween PopOff()
    {
        //the tween owns the transform from here on, physics would only fight it
        rb.simulated = false;
        col.enabled = false;

        //one launch velocity: back the way it came, up, and in or out of the screen
        float toCamera = Mathf.Sign(Camera.main.transform.position.z - transform.position.z);
        Vector3 velocity = new Vector3(
            Jitter(popVelocity.x),
            Jitter(popVelocity.y),
            (Random.value < towardCameraChance ? toCamera : -toCamera) * Jitter(popDepthSpeed));
        float spin = Jitter(popSpin) * (Random.value < 0.5f ? -1f : 1f);
        float gravity = Physics2D.gravity.y * rb.gravityScale;
        Vector3 start = transform.position;

        //plain ballistic motion, p = p0 + v*t + g*t^2/2, with the tween driving t
        Tween pop = DOVirtual.Float(0f, popDuration, popDuration, t =>
            {
                transform.SetPositionAndRotation(
                    start + velocity * t + Vector3.up * (0.5f * gravity * t * t),
                    Quaternion.Euler(0f, 0f, spin * t));
            })
            .SetEase(Ease.Linear)
            .SetLink(gameObject) //killed if we get destroyed early
            .OnComplete(() => Destroy(gameObject));

        //fade out over the tail end so it doesn't just blink away
        float fadeTime = popDuration * popFadeFraction;
        GetComponentInChildren<SpriteRenderer>()
            .DOFade(0f, fadeTime)
            .SetDelay(popDuration - fadeTime)
            .SetLink(gameObject);

        return pop;
    }

    //scales a value by a random amount within popRandomness
    private float Jitter(float value)
    {
        return value * (1f + Random.Range(-popRandomness, popRandomness));
    }
}
