using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//the little plank of advice at the top of the tutorial level. picks a hint from what the player is
//holding / what's in the air, and gets out of the way on any other level
public class TutorialHints : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private Text label;
    [SerializeField] private Button skipButton;

    [Header("Copy")]
    [TextArea] [SerializeField] private string firstThrow = "Drag the hedgehog back, aim with the dotted line, and let go!";
    [TextArea] [SerializeField] private string exploding = "This red one goes BOOM a moment after it lands. Drop it right next to a hen!";
    [TextArea] [SerializeField] private string cluster = "A cluster hog! Launch it, then tap anywhere while it's flying to split it into five.";
    [TextArea] [SerializeField] private string clusterFlying = "TAP NOW to split it!";
    [TextArea] [SerializeField] private string general = "Pop every hen to clear the garden. Hogs you don't need are worth bonus points!";

    Thrower _thrower;
    string _shown;
    bool _thrownOnce;
    Tween _tween;

    void Start()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Level == null || !gm.Level.isTutorial)
        {
            gameObject.SetActive(false);
            return;
        }

        _thrower = FindFirstObjectByType<Thrower>();
        skipButton?.onClick.AddListener(LevelProgress.PlayNext);
        if (panel != null) panel.localScale = Vector3.zero;
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsOver || _thrower == null)
        {
            Show(null);
            return;
        }

        if (_thrower.IsThrowing) _thrownOnce = true;

        string hint = null;
        bool clusterInAir = false;
        foreach (var hog in Hog.airborne)
            if (hog is ClusterHog && hog.IsFlying) { clusterInAir = true; break; }

        if (clusterInAir) hint = clusterFlying;
        else if (_thrower.IsLoaded && _thrower.loadedHog is ClusterHog) hint = cluster;
        else if (_thrower.IsLoaded && _thrower.loadedHog is ExplodingHog) hint = exploding;
        else if (_thrower.IsLoaded && !_thrownOnce) hint = firstThrow;
        else if (_thrower.IsLoaded) hint = general;

        Show(hint);
    }

    void Show(string hint)
    {
        if (hint == _shown) return;
        _shown = hint;
        if (panel == null) return;

        _tween?.Kill();
        if (hint == null)
        {
            _tween = panel.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetLink(gameObject);
            return;
        }

        if (label != null) label.text = hint;
        //a pop-in for a fresh hint, a wobble if we're already showing one
        panel.localScale = Vector3.one * 0.85f;
        _tween = panel.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetLink(gameObject);
        Sfx.Play("node_pop", 0.5f);
    }
}
