using DG.Tweening;
using UnityEngine;

//a wooden plank that an explosion can break. the block goes away and a few shards tumble out from the
//blast, spin, and fade in under a second so the wreckage never litters the level. crates don't get this
public class Splinterable : MonoBehaviour
{
    [Tooltip("Shard sprites to pick from; assigned by the content builder from Assets/Art/VFX/WoodShards.png")]
    [SerializeField] private Sprite[] shardSprites;
    [SerializeField] private int shardCount = 5;
    [Tooltip("Seconds a shard lives; it fades over the second half")]
    [SerializeField] private float shardLife = 0.7f;
    [Tooltip("Units/sec the shards fly away from the blast")]
    [SerializeField] private float burstSpeed = 6f;
    [SerializeField] private GameObject dustVfx;
    [SerializeField] private string sound = "wood_break";

    public void Shatter(Vector2 blastFrom)
    {
        var gfx = GetComponentInChildren<SpriteRenderer>();
        Vector3 scale = transform.lossyScale;
        float w = Mathf.Abs(scale.x), h = Mathf.Abs(scale.y);
        //shards are about a plank-thickness and a half long, whatever the plank's size
        float shardSize = Mathf.Clamp(Mathf.Min(w, h) * 1.5f, 0.25f, 0.6f);
        Vector2 away = ((Vector2)transform.position - blastFrom).normalized;
        if (away == Vector2.zero) away = Vector2.up;

        for (int i = 0; i < shardCount; i++)
        {
            //spread the shards along the plank so a long beam breaks all over, not just at its centre
            Vector2 local = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
            Vector3 at = transform.TransformPoint(local);
            var shard = new GameObject("Shard");
            shard.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));

            var sr = shard.AddComponent<SpriteRenderer>();
            sr.sprite = shardSprites != null && shardSprites.Length > 0 ? shardSprites[Random.Range(0, shardSprites.Length)] : null;
            if (gfx != null) { sr.sharedMaterial = gfx.sharedMaterial; sr.sortingLayerID = gfx.sortingLayerID; sr.sortingOrder = gfx.sortingOrder + 1; }
            if (sr.sprite != null)
            {
                float longest = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                shard.transform.localScale = Vector3.one * (shardSize / longest) * Random.Range(0.8f, 1.2f);
            }

            var rb = shard.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1.5f;
            rb.linearDamping = 0.5f;
            //mostly away from the blast, with a bit of up and scatter so it reads as a burst
            Vector2 dir = (away + Random.insideUnitCircle * 0.6f + Vector2.up * 0.4f).normalized;
            rb.linearVelocity = dir * burstSpeed * Random.Range(0.6f, 1.2f);
            rb.angularVelocity = Random.Range(-540f, 540f);

            sr.DOFade(0f, shardLife * 0.5f).SetDelay(shardLife * 0.5f).SetLink(shard);
            Destroy(shard, shardLife);
        }

        if (dustVfx != null) Vfx.Spawn(dustVfx, transform.position, Mathf.Clamp(Mathf.Max(w, h) / 2f, 0.6f, 1.6f));
        Sfx.Play(sound, 1f, Mathf.Lerp(1.1f, 0.85f, Mathf.Clamp01(w * h / 3f)));
        Destroy(gameObject);
    }
}
