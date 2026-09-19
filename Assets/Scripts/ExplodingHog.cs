using UnityEngine;

public class ExplodingHog : Hog
{
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionForce = 5f;

    public override void Impact()
    {
        base.Impact();
        
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        foreach (Collider2D collider in colliders)
        {
           collider.attachedRigidbody?.AddForce((collider.transform.position - transform.position) * explosionForce, ForceMode2D.Impulse);
        }

    }
}
