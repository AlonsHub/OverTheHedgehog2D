using DG.Tweening;
using UnityEngine;

//lands, sits there puffing up its cheeks for a beat, then goes off
public class ExplodingHog : Hog
{
    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionForce = 5f;
    [Tooltip("Hens inside this radius pop outright, no need to knock them over")]
    [SerializeField] private float lethalRadius = 2f;
    [Tooltip("Wooden posts and beams inside this radius splinter; crates only get shoved")]
    [SerializeField] private float shatterRadius = 3f;
    [SerializeField] private GameObject explosionVfx;
    [Tooltip("Delay between landing and going off, gives the impact anim its puff-up beat")]
    [SerializeField] private float fuseDelay = 0.8f;
    [Tooltip("How much the hog swells while the fuse burns; the tell that it is about to go")]
    [SerializeField] private float puffScale = 1.3f;
    [SerializeField] private Color puffTint = new Color(1f, 0.75f, 0.7f);

    public override void Impact()
    {
        //no popcorn for this one: it stays where it landed and the impact anim carries it to the bang
        anim.SetTrigger("Impact");
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;

        Sfx.Play("fuse");
        //swell and flush over the fuse, with a quickening wobble, then a bright blink right before the bang
        var gfx = anim.transform;
        var sr = gfx.GetComponent<SpriteRenderer>();
        Vector3 baseScale = gfx.localScale;
        DOTween.Sequence().SetLink(gameObject)
            .Append(gfx.DOScale(baseScale * puffScale, fuseDelay * 0.85f).SetEase(Ease.InQuad))
            .Join(gfx.DOPunchScale(baseScale * 0.06f, fuseDelay * 0.85f, 8, 0.5f))
            .Append(gfx.DOScale(baseScale * (puffScale + 0.15f), fuseDelay * 0.15f).SetEase(Ease.OutBack));
        if (sr != null)
            DOTween.Sequence().SetLink(gameObject)
                .Append(sr.DOColor(puffTint, fuseDelay * 0.8f))
                .Append(sr.DOColor(new Color(1.6f, 1.5f, 1.3f, 1f), fuseDelay * 0.2f));
        Invoke(nameof(Explode), fuseDelay);
    }

    void Explode()
    {
        Vector3 at = transform.position;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(at, explosionRadius);
        var planks = new System.Collections.Generic.HashSet<Splinterable>();

        foreach (Collider2D collider in colliders)
        {
            Vector3 away = collider.transform.position - at;

            //force falls off with distance so the blast has a heart to it
            float falloff = 1f - Mathf.Clamp01(away.magnitude / explosionRadius);
            collider.attachedRigidbody?.AddForce(away.normalized * explosionForce * falloff, ForceMode2D.Impulse);

            if (away.magnitude <= lethalRadius && collider.TryGetComponent(out Enemy enemy))
                enemy.TakeHit(1);
            if (away.magnitude <= shatterRadius && collider.TryGetComponent(out Splinterable plank))
                planks.Add(plank);
        }
        //break the planks after the push so whatever stood on them gets its shove first
        foreach (var plank in planks) plank.Shatter(at);

        if (explosionVfx != null)
            Vfx.Spawn(explosionVfx, at);
        Sfx.Play("explosion");

        //the explosion VFX is the cloud now, so the hog itself vanishes in the flash
        var sr = anim.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        Destroy(gameObject, 0.1f);
    }
}
