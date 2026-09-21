using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//the plank of advice at the top of the tutorial level. hints are an ordered list the player can page
//through with Next; playing also moves it forward (you never get told about the cluster hog after
//you've thrown it). a pointing hand in the world shows what each hint is on about. hidden on any
//other level
public class TutorialHints : MonoBehaviour
{
    public enum Target { None, LoadedHog, NearestHen, FlyingCluster }

    [Serializable]
    public class Step
    {
        [TextArea] public string text;
        public Target pointAt;
    }

    [SerializeField] private RectTransform panel;
    [SerializeField] private Text label;
    [SerializeField] private Button nextButton;
    [SerializeField] private Text nextLabel;
    [SerializeField] private Button skipTutorialButton;
    [Tooltip("World-space pointing hand, fingertip at its pivot")]
    [SerializeField] private SpriteRenderer pointer;
    [Tooltip("Where the fingertip sits relative to the thing it's pointing at")]
    [SerializeField] private Vector2 pointerOffset = new Vector2(0.45f, 0.75f);

    [SerializeField] private List<Step> steps = new List<Step>
    {
        new Step { text = "Drag the hedgehog back, aim with the dotted line, and let go!", pointAt = Target.LoadedHog },
        new Step { text = "Pop every hen to clear the garden. Hogs you don't need are worth bonus points!", pointAt = Target.NearestHen },
        new Step { text = "This red one goes BOOM a moment after it lands. Drop it right next to a hen!", pointAt = Target.LoadedHog },
        new Step { text = "A cluster hog! Launch it, then tap anywhere while it's flying to split it into five.", pointAt = Target.LoadedHog },
        new Step { text = "TAP NOW to split it!", pointAt = Target.FlyingCluster },
        new Step { text = "The minis rain down and to the right. Now finish off the hens!", pointAt = Target.NearestHen },
    };

    //which steps the game itself unlocks
    const int StepAfterFirstThrow = 1;
    const int StepExploding = 2;
    const int StepCluster = 3;
    const int StepClusterFlying = 4;
    const int StepAfterCluster = 5;

    Thrower _thrower;
    int _step;
    bool _dismissed;
    bool _thrownOnce;
    bool _clusterFlew;
    string _shown;
    Tween _tween;
    Tween _pointerBob;

    void Start()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Level == null || !gm.Level.isTutorial)
        {
            if (pointer != null) pointer.gameObject.SetActive(false);
            gameObject.SetActive(false);
            return;
        }

        _thrower = FindFirstObjectByType<Thrower>();
        nextButton?.onClick.AddListener(Next);
        skipTutorialButton?.onClick.AddListener(LevelProgress.PlayNext);
        if (panel != null) panel.localScale = Vector3.zero;
        if (pointer != null) pointer.enabled = false;
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsOver || _thrower == null)
        {
            Show(null);
            if (skipTutorialButton != null) skipTutorialButton.gameObject.SetActive(false);
            return;
        }

        if (_thrower.IsThrowing) _thrownOnce = true;
        Hog flyingCluster = FlyingCluster();
        if (flyingCluster != null) _clusterFlew = true;

        //the game pulls the sequence forward, Next lets the player read ahead
        int floor = 0;
        if (_thrownOnce) floor = StepAfterFirstThrow;
        if (_thrower.IsLoaded && _thrower.loadedHog is ExplodingHog) floor = StepExploding;
        if (_thrower.IsLoaded && _thrower.loadedHog is ClusterHog) floor = StepCluster;
        if (_clusterFlew) floor = StepAfterCluster;
        if (floor > _step)
        {
            _step = floor;
            _dismissed = false;
        }

        //TAP NOW cuts in over everything while the cluster is airborne
        int showing = flyingCluster != null ? StepClusterFlying : _step;
        bool visible = flyingCluster != null || (!_dismissed && showing < steps.Count);

        Show(visible ? steps[showing].text : null);
        if (nextLabel != null) nextLabel.text = showing >= steps.Count - 1 ? "Got it" : "Next  >";

        PointAt(visible ? steps[showing].pointAt : Target.None, flyingCluster);
    }

    void Next()
    {
        if (_step >= steps.Count - 1)
        {
            _dismissed = true;
            return;
        }
        _step++;
    }

    Hog FlyingCluster()
    {
        foreach (var hog in Hog.airborne)
            if (hog is ClusterHog && hog.IsFlying) return hog;
        return null;
    }

    Transform ResolveTarget(Target target, Hog flyingCluster)
    {
        switch (target)
        {
            case Target.LoadedHog:
                return _thrower.IsLoaded && _thrower.loadedHog != null ? _thrower.loadedHog.transform : null;
            case Target.FlyingCluster:
                return flyingCluster != null ? flyingCluster.transform : null;
            case Target.NearestHen:
                Transform best = null; float bestD = float.MaxValue;
                Vector3 from = _thrower.anchor != null ? _thrower.anchor.position : Vector3.zero;
                foreach (var hen in Enemy.enemies)
                {
                    if (hen == null || hen.IsDead) continue;
                    float d = (hen.transform.position - from).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = hen.transform; }
                }
                return best;
        }
        return null;
    }

    void PointAt(Target target, Hog flyingCluster)
    {
        if (pointer == null) return;
        Transform t = ResolveTarget(target, flyingCluster);

        if (t == null)
        {
            if (pointer.enabled)
            {
                pointer.enabled = false;
                _pointerBob?.Kill();
                _pointerBob = null;
            }
            return;
        }

        if (!pointer.enabled)
        {
            pointer.enabled = true;
            pointer.transform.localScale = Vector3.one;
            //a little tap-tap towards whatever it's pointing at
            _pointerBob?.Kill();
            _pointerBob = pointer.transform.DOScale(0.88f, 0.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(pointer.gameObject);
        }
        //follows every frame so it tracks a flying hog
        pointer.transform.position = t.position + (Vector3)pointerOffset;
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
        panel.localScale = Vector3.one * 0.85f;
        _tween = panel.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetLink(gameObject);
        Sfx.Play("node_pop", 0.5f);
    }
}
