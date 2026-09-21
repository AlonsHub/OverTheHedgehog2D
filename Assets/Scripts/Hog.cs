using DG.Tweening;
using UnityEngine;

public class Hog : MonoBehaviour
{
    public Rigidbody2D rb;
    [SerializeField] private Animator anim;
    [SerializeField] private float walkSpeed;
    [SerializeField] private Collider2D col;

    [Header("Death pop")]
    [Tooltip("Knocked back off the impact point by this much (x is 'back', so negative = left) before popping")]
    [SerializeField] private Vector2 bumpOffset = new Vector2(-1.5f, 1f);
    [SerializeField] private float bumpDuration = 0.2f;
    [Tooltip("How high the pop rises above the bump point")]
    [SerializeField] private float popHeight = 4f;
    [Tooltip("How far below the bump point it lands - enough to fall out of frame")]
    [SerializeField] private float popFall = 8f;
    [Tooltip("Sideways drift during the pop, picked at random in [-x, x]")]
    [SerializeField] private float popSideways = 1.5f;
    [Tooltip("Depth travelled towards the camera (grows) or away from it (shrinks)")]
    [SerializeField] private float popDepth = 8f;
    [Range(0f, 1f)]
    [SerializeField] private float towardCameraChance = 0.6f;
    [SerializeField] private float popDuration = 1.2f;
    [Tooltip("Degrees of tumble over the pop, direction picked at random")]
    [SerializeField] private float popSpin = 540f;
    [Tooltip("0 = every hog pops identically, 1 = wild. Scales distances/height/spin by up to this fraction")]
    [Range(0f, 1f)]
    [SerializeField] private float popRandomness = 0.35f;

    //state?
    bool _impacted = false;
    Tween _walkTween;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(_impacted) return;

        _impacted = true;
        Impact();
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
    public virtual void Impact()
    {
        anim.SetTrigger("Impact");
        PopOff();
    }

    //popcorn: a quick knock back off whatever we hit, then up and out of the frame -
    //either towards the camera (looming) or away from it (shrinking), tumbling as it goes.
    //destroys the hog when done
    protected Sequence PopOff()
    {
        //the tween owns the transform from here on, physics would only fight it
        rb.simulated = false;
        col.enabled = false;

        Vector3 start = transform.position;
        Vector3 bumpTarget = start + new Vector3(Jitter(bumpOffset.x), Jitter(bumpOffset.y), 0f);

        //pop geometry: apex above the bump point, landing well below it, drifting sideways and in depth
        float apexY = bumpTarget.y + Jitter(popHeight);
        float landY = bumpTarget.y - Jitter(popFall);
        float endX = bumpTarget.x + Random.Range(-popSideways, popSideways);
        float toCamera = Mathf.Sign(Camera.main.transform.position.z - start.z);
        float endZ = start.z + (Random.value < towardCameraChance ? toCamera : -toCamera) * Jitter(popDepth);
        float spin = Jitter(popSpin) * (Random.value < 0.5f ? -1f : 1f);

        //split the pop between rise and fall like gravity would: time in the air scales with sqrt(distance)
        float rise = Mathf.Sqrt(Mathf.Max(0f, apexY - bumpTarget.y));
        float fall = Mathf.Sqrt(Mathf.Max(0f, apexY - landY));
        float riseTime = popDuration * rise / Mathf.Max(rise + fall, 0.001f);

        Sequence seq = DOTween.Sequence();
        //1. knocked back off whatever we hit
        seq.Append(transform.DOMove(bumpTarget, bumpDuration).SetEase(Ease.OutQuad));
        //2. up and over: OutQuad up + InQuad down = a parabola
        seq.Append(transform.DOMoveY(apexY, riseTime).SetEase(Ease.OutQuad));
        seq.Append(transform.DOMoveY(landY, popDuration - riseTime).SetEase(Ease.InQuad));
        //3. meanwhile drift sideways, travel in depth and tumble
        seq.Insert(bumpDuration, transform.DOMoveX(endX, popDuration).SetEase(Ease.Linear));
        seq.Insert(bumpDuration, transform.DOMoveZ(endZ, popDuration).SetEase(Ease.Linear));
        seq.Insert(bumpDuration, transform.DORotate(new Vector3(0f, 0f, spin), popDuration, RotateMode.FastBeyond360).SetEase(Ease.OutSine));
        seq.SetLink(gameObject); //killed if we get destroyed early
        seq.OnComplete(() => Destroy(gameObject));

        return seq;
    }

    //scales a value by a random amount within popRandomness
    private float Jitter(float value)
    {
        return value * (1f + Random.Range(-popRandomness, popRandomness));
    }
}
