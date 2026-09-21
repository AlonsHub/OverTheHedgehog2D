using UnityEngine;

//wooden blocks knock when they bump into things, louder the harder they hit
public class WoodKnock : MonoBehaviour
{
    [SerializeField] private string sound = "wood_knock";
    [Tooltip("Bumps slower than this stay silent (resting contacts, gentle settling)")]
    [SerializeField] private float minSpeed = 1.5f;
    [Tooltip("Speed at which the knock is at full volume")]
    [SerializeField] private float fullSpeed = 8f;
    [SerializeField] private float cooldown = 0.12f;

    float _last = -1f;

    void OnCollisionEnter2D(Collision2D collision)
    {
        float speed = collision.relativeVelocity.magnitude;
        if (speed < minSpeed) return;
        if (Time.time - _last < cooldown) return;
        _last = Time.time;

        float t = Mathf.InverseLerp(minSpeed, fullSpeed, speed);
        //big slow beams thud lower than little crates
        float pitch = Mathf.Lerp(1.15f, 0.8f, Mathf.Clamp01(transform.lossyScale.x * transform.lossyScale.y / 3f));
        Sfx.Play(sound, Mathf.Lerp(0.35f, 1f, t), pitch);
    }
}
