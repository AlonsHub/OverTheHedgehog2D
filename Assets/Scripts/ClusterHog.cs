using UnityEngine;

//cluster bomb: click anywhere while it's in the air and it bursts into a fan of mini hogs.
//if the player never clicks it bursts on whatever it hits instead. the minis are plain Hogs
//(they don't split again), they just fly on and pop like everyone else
public class ClusterHog : Hog
{
    [Header("Cluster")]
    [SerializeField] private Hog miniPrefab;
    [Min(1)]
    [SerializeField] private int pieces = 5;
    [Tooltip("Total fan angle the minis spread over, centred on our current direction of travel")]
    [SerializeField] private float spreadAngle = 70f;
    [Tooltip("How much of our current velocity each mini keeps")]
    [Range(0f, 1.5f)]
    [SerializeField] private float inheritVelocity = 0.8f;
    [Tooltip("Extra speed each mini gets along its own fan direction")]
    [SerializeField] private float burstSpeed = 4f;
    [Tooltip("Spawned where we split")]
    [SerializeField] private GameObject splitVfx;

    bool _split = false;

    void Update()
    {
        if (_split || !IsFlying) return;

        if (Input.GetMouseButtonDown(0))
            Split();
    }

    //landed without being clicked: burst anyway, the minis scatter off the impact point
    public override void Impact()
    {
        Split(true);
    }

    [ContextMenu("Split")]
    public void Split() => Split(false);

    //upwards: we've just hit something, so fan up and out instead of along our (now bounced) velocity
    public void Split(bool upwards)
    {
        if (_split) return;
        _split = true;

        Vector2 velocity = rb.linearVelocity;
        float heading;
        if (upwards)
        {
            heading = 90f;
            velocity = new Vector2(velocity.x * 0.3f, 0f);
        }
        else
        {
            //fan around where we're heading; if we're somehow stationary just fan upwards
            heading = velocity.sqrMagnitude > 0.01f ? Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg : 90f;
        }

        for (int i = 0; i < pieces; i++)
        {
            //evenly spaced across the fan, a single piece goes straight ahead
            float t = pieces == 1 ? 0.5f : (float)i / (pieces - 1);
            float angle = heading + Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, t);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            Hog mini = Instantiate(miniPrefab, transform.position + (Vector3)dir * 0.2f, Quaternion.identity);
            mini.name = miniPrefab.name;
            mini.Fly();
            //Launch adds an impulse, so scale the wanted velocity by the mini's mass
            mini.Launch((velocity * inheritVelocity + dir * burstSpeed) * mini.rb.mass);
        }

        if (splitVfx != null)
            Vfx.Spawn(splitVfx, transform.position);

        //the minis carry on, the parent is done
        Destroy(gameObject);
    }
}
