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
        backButton?.onClick.AddListener(LevelProgress.OpenStart);

        var catalogue = LevelCatalogue.Load();
        if (catalogue == null || nodePrefab == null) return;

        int furthest = Mathf.Min(LevelProgress.UnlockedIndex, catalogue.Count - 1);

        for (int i = 0; i < catalogue.Count; i++)
        {
            RectTransform anchor = nodeAnchors.Length == 0 ? (RectTransform)transform : nodeAnchors[Mathf.Min(i, nodeAnchors.Length - 1)];
            LevelNodeUI node = Instantiate(nodePrefab, anchor);
            node.name = $"LevelNode_{i + 1}";
            var rect = (RectTransform)node.transform;
            rect.anchoredPosition = Vector2.zero;

            node.Setup(i, catalogue.Get(i), LevelProgress.IsUnlocked(i), LevelProgress.BestScore(i));

            //stagger them sprouting up the path
            rect.localScale = Vector3.zero;
            rect.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetDelay(0.08f * i).SetLink(node.gameObject);
        }

        //the hedgehog sits on the furthest stone reached
        if (hedgehogMarker != null && nodeAnchors.Length > 0)
        {
            hedgehogMarker.SetParent(nodeAnchors[Mathf.Min(furthest, nodeAnchors.Length - 1)], false);
            hedgehogMarker.anchoredPosition = new Vector2(-70f, 40f);
            hedgehogMarker.SetAsLastSibling();
            hedgehogMarker.DOAnchorPosY(52f, 0.9f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(hedgehogMarker.gameObject);
        }
    }
}
