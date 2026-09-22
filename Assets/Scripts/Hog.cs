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

    [Header("Sounds")]
    [SerializeField] private string impactSound = "hog_impact";
    [SerializeField] private string popSound = "hog_pop";

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

        //harder landings thud louder
        Sfx.Play(impactSound, Mathf.Clamp(collision.relativeVelocity.magnitude / 8f, 0.4f, 1f));

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

    [Header("Walk")]
    [Tooltip("Length of one waddle hop in world units; the walk is chopped into hops of this size")]
    [SerializeField] private float hopLength = 0.45f;
    [SerializeField] private float hopHeight = 0.12f;
    [Tooltip("How much the body squashes on landing and stretches at the top of a hop")]
    [SerializeField] private float hopSquash = 0.08f;
    [Tooltip("Degrees the body rocks side to side while waddling")]
    [SerializeField] private float waddleTilt = 5f;

    //shuffles forward to a new spot in the stock line: a string of little hops with a squash on each
    //landing and a side-to-side rock, at walkSpeed (no speed set = snap there)
    public void WalkTo(Vector3 target, float delay = 0f)
    {
        StopWalking();

        float distance = Vector3.Distance(transform.position, target);
        if (walkSpeed <= 0f || distance < 0.01f) { transform.position = target; return; }

        float duration = distance / walkSpeed;
        int hops = Mathf.Max(1, Mathf.RoundToInt(distance / hopLength));
        var seq = DOTween.Sequence().SetDelay(delay).SetLink(gameObject);
        seq.Append(transform.DOJump(target, hopHeight, hops, duration).SetEase(Ease.Linear));
        Transform body = anim != null ? anim.transform : null;
        if (body != null)
        {
            Vector3 baseScale = body.localScale;
            float hop = duration / hops;
            //stretch going up, squash on landing, once per hop; rock the other way each hop
            for (int i = 0; i < hops; i++)
            {
                float tilt = (i % 2 == 0 ? 1f : -1f) * waddleTilt;
                seq.Insert(i * hop, body.DOScale(new Vector3(baseScale.x * (1f - hopSquash), baseScale.y * (1f + hopSquash), 1f), hop * 0.4f).SetEase(Ease.OutQuad));
                seq.Insert(i * hop + hop * 0.4f, body.DOScale(new Vector3(baseScale.x * (1f + hopSquash), baseScale.y * (1f - hopSquash), 1f), hop * 0.3f).SetEase(Ease.InQuad));
                seq.Insert(i * hop + hop * 0.7f, body.DOScale(baseScale, hop * 0.3f).SetEase(Ease.OutBack));
                seq.Insert(i * hop, body.DOLocalRotate(new Vector3(0f, 0f, tilt), hop * 0.5f).SetEase(Ease.InOutSine));
                seq.Insert(i * hop + hop * 0.5f, body.DOLocalRotate(Vector3.zero, hop * 0.5f).SetEase(Ease.InOutSine));
            }
            seq.OnKill(() => { if (body != null) { body.localScale = baseScale; body.localRotation = Quaternion.identity; } });
        }
        _walkTween = seq;
    }

    //one eager bound from the front of the line into the pouch: a high hop with a stretch, then a big
    //squash and settle on landing. the callback fires when it has settled
    public Tween HopInto(Transform holder, float duration = 0.4f, System.Action onLanded = null)
    {
        StopWalking();
        transform.SetParent(holder);
        Transform body = anim != null ? anim.transform : null;
        Vector3 baseScale = body != null ? body.localScale : Vector3.one;
        var seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(transform.DOLocalJump(Vector3.zero, 0.7f, 1, duration).SetEase(Ease.Linear));
        if (body != null)
        {
            seq.Insert(0f, body.DOScale(new Vector3(baseScale.x * 0.85f, baseScale.y * 1.15f, 1f), duration * 0.5f).SetEase(Ease.OutQuad));
            seq.Insert(duration * 0.5f, body.DOScale(new Vector3(baseScale.x * 1.2f, baseScale.y * 0.8f, 1f), duration * 0.5f).SetEase(Ease.InQuad));
            seq.Append(body.DOScale(baseScale, 0.25f).SetEase(Ease.OutElastic, 1.2f, 0.4f));
            seq.OnKill(() => { if (body != null) body.localScale = baseScale; });
        }
        seq.OnComplete(() => onLanded?.Invoke());
        _walkTween = seq;
        return seq;
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

        Sfx.Play(popSound);

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
