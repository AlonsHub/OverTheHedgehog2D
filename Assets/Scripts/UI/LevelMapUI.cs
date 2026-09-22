using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//the garden path: one flower pot per level, planted on the stepping stones. locked levels get the
//empty pot, done ones get a daisy and their best score
public class LevelMapUI : MonoBehaviour
{
    [Tooltip("One per level, in catalogue order. Extra anchors are ignored, missing ones are stacked at the last")]
    [SerializeField] private RectTransform[] nodeAnchors;
    [SerializeField] private LevelNodeUI nodePrefab;
    [SerializeField] private Button backButton;
    [SerializeField] private RectTransform hedgehogMarker;

    void Start()
    {
        Music.Play("music_garden", 1.5f, 0.5f);
        backButton?.onClick.AddListener(LevelProgress.OpenStart);

        var catalogue = LevelCatalogue.Load();
        if (catalogue == null || nodePrefab == null) return;

        int furthest = Mathf.Min(Mathf.Max(LevelProgress.UnlockedIndex, LevelProgress.FirstRealLevel), catalogue.Count - 1);

        for (int i = 0; i < catalogue.Count; i++)
        {
            RectTransform anchor = nodeAnchors.Length == 0 ? (RectTransform)transform : nodeAnchors[Mathf.Min(i, nodeAnchors.Length - 1)];
            LevelNodeUI node = Instantiate(nodePrefab, anchor);
            node.name = $"LevelNode_{i + 1}";
            var rect = (RectTransform)node.transform;
            rect.anchoredPosition = Vector2.zero;

            node.Setup(i, catalogue.Get(i), LevelProgress.IsUnlocked(i), LevelProgress.BestScore(i), LevelProgress.BestStars(i));

            //stagger them sprouting up the path
            rect.localScale = Vector3.zero;
            rect.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetDelay(0.08f * i).SetLink(node.gameObject);
            //each pot pops up a touch higher than the last
            Sfx.PlayDelayed("node_pop", 0.08f * i, 0.7f);
        }

        //the hedgehog sits on the furthest stone reached
        if (hedgehogMarker != null && nodeAnchors.Length > 0)
        {
            hedgehogMarker.SetParent(nodeAnchors[Mathf.Min(furthest, nodeAnchors.Length - 1)], false);
            //to the left of the pot at its base, clear of the daisies under the pot above
            hedgehogMarker.anchoredPosition = new Vector2(-105f, 6f);
            hedgehogMarker.SetAsLastSibling();
            hedgehogMarker.DOAnchorPosY(16f, 0.9f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(hedgehogMarker.gameObject);
        }
    }
}
