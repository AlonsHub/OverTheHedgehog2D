using UnityEngine;

//lands, sits there puffing up its cheeks for a beat, then goes off
public class ExplodingHog : Hog
{
    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionForce = 5f;
    [Tooltip("Hens inside this radius pop outright, no need to knock them over")]
    [SerializeField] private float lethalRadius = 2f;
    [SerializeField] private GameObject explosionVfx;
    [Tooltip("Delay between landing and going off, gives the impact anim its puff-up beat")]
    [SerializeField] private float fuseDelay = 0.8f;

    public override void Impact()
    {
        //no popcorn for this one: it stays where it landed and the impact anim carries it to the bang
        anim.SetTrigger("Impact");
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;

        Invoke(nameof(Explode), fuseDelay);
    }

    void Explode()
    {
        Vector3 at = transform.position;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(at, explosionRadius);

        foreach (Collider2D collider in colliders)
        {
            Vector3 away = collider.transform.position - at;

            //force falls off with distance so the blast has a heart to it
            float falloff = 1f - Mathf.Clamp01(away.magnitude / explosionRadius);
            collider.attachedRigidbody?.AddForce(away.normalized * explosionForce * falloff, ForceMode2D.Impulse);

            if (away.magnitude <= lethalRadius && collider.TryGetComponent(out Enemy enemy))
                enemy.TakeHit(1);
        }

        if (explosionVfx != null)
            Vfx.Spawn(explosionVfx, at);

        //the sprite sheet's last frames are the puff cloud dissolving, so let them play out before we go
        Destroy(gameObject, 0.5f);
    }
}
