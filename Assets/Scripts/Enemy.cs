using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

//a moody hen. hogs pop her on touch, anything else has to hit her hard enough. the boss is just a
//hen with more hit points and a bigger score
public class Enemy : MonoBehaviour
{
    public static List<Enemy> enemies = new List<Enemy>();

    //hens that are still standing (popping ones are already spoken for)
    public static int AliveCount
    {
        get
        {
            int alive = 0;
            foreach (var enemy in enemies)
                if (enemy != null && !enemy.IsDead) alive++;
            return alive;
        }
    }

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator anim;
    [SerializeField] private Collider2D col;
    [SerializeField] private Rigidbody2D rb;

    [Header("Toughness")]
    [Min(1)]
    [SerializeField] private int hitPoints = 1;
    [Tooltip("A non-hog (block, another hen...) has to hit us at least this fast to hurt us")]
    [SerializeField] private float crushSpeed = 5f;
    [Tooltip("No crush damage for this long after spawning, so settling onto the structure doesn't count")]
    [SerializeField] private float settleGrace = 1.5f;
    [Tooltip("Can't be hurt again for this long after a hit, so a tumble doesn't chain-hit")]
    [SerializeField] private float hitCooldown = 0.5f;

    [Header("Score")]
    [SerializeField] private int scoreOnPop = 1000;
    [SerializeField] private int scoreOnHit = 250;

    [Header("Pop")]
    [Tooltip("How long the Pop animation runs before we're removed")]
    [SerializeField] private float popDuration = 1.2f;
    [SerializeField] private GameObject feathersVfx;
    [SerializeField] private GameObject hitVfx;

    bool _dying = false;
    float _lastHitTime = -10f;
    float _spawnTime;
    int _hp;

    public bool IsDead => _dying;
    public int HitPoints => _hp;

    void Awake()
    {
        _hp = hitPoints;
        _spawnTime = Time.time;
        enemies.Add(this);

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (col == null) col = GetComponent<Collider2D>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    void OnDestroy()
    {
        enemies.Remove(this);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (_dying) return;

        if (collision.gameObject.CompareTag("Hog"))
        {
            TakeHit(1);
            return;
        }

        //something heavy landed on us / we got knocked into a block. the ground itself (no rigidbody)
        //never hurts, and neither does settling into place right after spawning
        if (collision.rigidbody == null) return;
        if (Time.time - _spawnTime < settleGrace) return;
        if (collision.relativeVelocity.magnitude >= crushSpeed)
            TakeHit(1);
    }

    public void TakeHit(int damage)
    {
        if (_dying) return;
        if (Time.time - _lastHitTime < hitCooldown) return;
        _lastHitTime = Time.time;

        _hp -= damage;

        if (_hp <= 0)
        {
            Die();
            return;
        }

        //still standing: flinch, so the player sees the hit landed
        GameManager.Instance?.AddScore(scoreOnHit);
        if (hitVfx != null) Vfx.Spawn(hitVfx, transform.position, 0.6f);

        Transform gfx = spriteRenderer != null ? spriteRenderer.transform : transform;
        gfx.DOKill(true);
        gfx.DOPunchScale(new Vector3(0.25f, -0.25f, 0f), 0.35f, 8, 0.8f).SetLink(gameObject);
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill(true);
            spriteRenderer.DOColor(new Color(1f, 0.6f, 0.6f), 0.08f).SetLoops(2, LoopType.Yoyo).SetLink(gameObject);
        }
    }

    private void Die()
    {
        //a second hit mid-pop shouldn't restart the pop or count us out twice
        if (_dying) return;
        _dying = true;

        GameManager.Instance?.AddScore(scoreOnPop);

        //out of the physics world right away so nothing else bounces off a hen that's popping
        if (col != null) col.enabled = false;
        if (rb != null) rb.simulated = false;

        if (feathersVfx != null) Vfx.Spawn(feathersVfx, transform.position, transform.lossyScale.x);
        if (anim != null) anim.SetTrigger("Pop");

        DOVirtual.DelayedCall(popDuration, () =>
            {
                enemies.Remove(this);
                GameManager.Instance?.OnEnemyDied(this);
                Destroy(gameObject);
            })
            .SetLink(gameObject); //killed if we get destroyed early
    }
}
