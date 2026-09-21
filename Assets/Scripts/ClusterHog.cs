using System.Collections.Generic;
using UnityEngine;

//cluster bomb: click anywhere while it's in the air and it bursts into a fan of mini hogs that rain
//down and to the right. if the player never clicks it bursts on whatever it hits instead. the minis are
//plain Hogs (they don't split again): they fly until they hit something, then pop like everyone else
public class ClusterHog : Hog
{
    [Header("Cluster")]
    [SerializeField] private Hog miniPrefab;
    [Min(1)]
    [SerializeField] private int pieces = 5;
    [Tooltip("Fan of directions the minis leave along, in degrees (0 = right, -90 = straight down)")]
    [SerializeField] private float fanFrom = -5f;
    [SerializeField] private float fanTo = -80f;
    [Tooltip("How much of our forward (x) speed each mini keeps, so a fast throw still carries them")]
    [Range(0f, 1.5f)]
    [SerializeField] private float inheritForward = 0.6f;
    [Tooltip("Speed each mini gets along its own fan direction")]
    [SerializeField] private float burstSpeed = 5f;
    [Tooltip("Landed without a click: fan the minis up and out instead so they don't just sit there")]
    [SerializeField] private float impactFanFrom = 80f;
    [SerializeField] private float impactFanTo = 10f;
    [Tooltip("Spawned where we split")]
    [SerializeField] private GameObject splitVfx;

    bool _split = false;

    void Update()
    {
        if (_split || !IsFlying) return;

        if (Input.GetMouseButtonDown(0))
            Split();
    }

    //landed without being clicked: burst anyway
    public override void Impact()
    {
        Split(true);
    }

    [ContextMenu("Split")]
    public void Split() => Split(false);

    public void Split(bool onImpact)
    {
        if (_split) return;
        _split = true;

        //nothing else may bump into the husk while the minis are born
        col.enabled = false;
        rb.simulated = false;

        Vector2 velocity = rb.linearVelocity;
        Vector2 carry = new Vector2(Mathf.Max(0f, velocity.x) * inheritForward, 0f);
        float from = onImpact ? impactFanFrom : fanFrom;
        float to = onImpact ? impactFanTo : fanTo;

        var minis = new List<Hog>(pieces);
        var dirs = new List<Vector2>(pieces);
        for (int i = 0; i < pieces; i++)
        {
            //evenly spaced across the fan, a single piece goes down the middle
            float t = pieces == 1 ? 0.5f : (float)i / (pieces - 1);
            float angle = Mathf.Lerp(from, to, t);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            dirs.Add(dir);

            //born a little way out along their own direction so they don't start inside each other
            Hog mini = Instantiate(miniPrefab, transform.position + (Vector3)dir * 0.45f, Quaternion.identity);
            mini.name = miniPrefab.name;
            minis.Add(mini);
        }

        //the litter never collides with itself, only with the world: a mini popping on a sibling's
        //nose the moment it's born is no fun for anyone
        foreach (var a in minis)
        {
            var colA = a.GetComponent<Collider2D>();
            Physics2D.IgnoreCollision(colA, col, true);
            foreach (var b in minis)
                if (a != b) Physics2D.IgnoreCollision(colA, b.GetComponent<Collider2D>(), true);
        }

        for (int i = 0; i < minis.Count; i++)
        {
            minis[i].Fly();
            //Launch adds an impulse, so scale the wanted velocity by the mini's mass
            minis[i].Launch((carry + dirs[i] * burstSpeed) * minis[i].rb.mass);
        }

        if (splitVfx != null)
            Vfx.Spawn(splitVfx, transform.position);
        Sfx.Play("cluster_split");

        //the minis carry on, the parent is done
        Destroy(gameObject);
    }
}
