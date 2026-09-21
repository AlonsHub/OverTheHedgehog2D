using DG.Tweening;
using UnityEngine;

//slow squash-and-stretch breathing on a sprite, so a single-frame character still feels alive.
//put it on the GFX child, not the physics root, so the collider doesn't wobble
public class Breathing : MonoBehaviour
{
    [SerializeField] private float amount = 0.04f;
    [SerializeField] private float period = 2.2f;
    [Tooltip("Random start offset so a row of hens doesn't breathe in unison")]
    [SerializeField] private bool randomPhase = true;

    Vector3 _baseScale;
    Tween _tween;

    void OnEnable()
    {
        _baseScale = transform.localScale;
        Vector3 puffed = new Vector3(_baseScale.x * (1f - amount * 0.5f), _baseScale.y * (1f + amount), _baseScale.z);

        _tween = transform.DOScale(puffed, period * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);

        if (randomPhase)
            _tween.Goto(Random.Range(0f, period), true);
    }

    void OnDisable()
    {
        _tween?.Kill();
        transform.localScale = _baseScale;
    }
}
