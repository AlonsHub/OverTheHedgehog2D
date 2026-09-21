using DG.Tweening;
using UnityEngine;

//a quiet wanderer: flaps between two frames and drifts from spot to spot inside its patch of air,
//resting a little between hops. The art faces right; it flips to face where it's going
public class Butterfly : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Sprite wingsOpen;
    [SerializeField] private Sprite wingsClosed;

    [Header("Flight")]
    [Tooltip("Size of the area it wanders in, centred on where it starts")]
    [SerializeField] private Vector2 roamArea = new Vector2(5f, 2.5f);
    [SerializeField] private float speed = 1f;
    [Tooltip("Pause between hops, random in this range")]
    [SerializeField] private Vector2 restTime = new Vector2(0.5f, 3f);
    [Tooltip("Little up/down flutter while airborne")]
    [SerializeField] private float bobAmount = 0.06f;
    [SerializeField] private float bobsPerSecond = 3f;

    [Header("Wings")]
    [SerializeField] private float flapsPerSecondFlying = 7f;
    [SerializeField] private float flapsPerSecondResting = 1f;

    private Vector3 _home;
    private float _flap;
    private bool _flying;

    void Start()
    {
        _home = transform.position;
        _flap = Random.value; //so several butterflies don't flap in sync
        HopToNewSpot();
    }

    void Update()
    {
        //wings
        _flap += Time.deltaTime * (_flying ? flapsPerSecondFlying : flapsPerSecondResting);
        sprite.sprite = _flap % 1f < 0.5f ? wingsOpen : wingsClosed;

        //flutter
        float bob = _flying ? Mathf.Sin(Time.time * bobsPerSecond * Mathf.PI * 2f) * bobAmount : 0f;
        sprite.transform.localPosition = new Vector3(0f, bob, 0f);
    }

    void HopToNewSpot()
    {
        Vector3 target = _home + new Vector3(
            Random.Range(-roamArea.x, roamArea.x) * 0.5f,
            Random.Range(-roamArea.y, roamArea.y) * 0.5f,
            0f);
        float duration = Vector3.Distance(transform.position, target) / speed;

        _flying = false;
        transform.DOMove(target, duration)
            .SetEase(Ease.InOutSine)
            .SetDelay(Random.Range(restTime.x, restTime.y))
            .OnStart(() =>
            {
                _flying = true;
                sprite.flipX = target.x < transform.position.x;
            })
            .OnComplete(HopToNewSpot)
            .SetLink(gameObject);
    }
}
