using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//the "Level clear!" / "Oh no!" plank that drops in when the level ends
public class ResultWindow : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private Text titleLabel;
    [SerializeField] private Text scoreLabel;
    [SerializeField] private Text bestLabel;
    [SerializeField] private Text bonusLabel;
    [SerializeField] private Text recordLabel;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button mapButton;
    [SerializeField] private Image[] stars;
    [SerializeField] private Sprite earnedSprite;
    [SerializeField] private Sprite unearnedSprite;

    [Header("Copy")]
    [SerializeField] private string winTitle = "Level clear!";
    [SerializeField] private string bossWinTitle = "Boss plucked!";
    [SerializeField] private string loseTitle = "Out of hogs...";

    [Header("Motion")]
    [SerializeField] private float dropDuration = 0.6f;

    //the window starts inactive in the scene, so this runs the first time Show() wakes it up
    void Awake()
    {
        playAgainButton?.onClick.AddListener(LevelProgress.Replay);
        nextButton?.onClick.AddListener(LevelProgress.PlayNext);
        mapButton?.onClick.AddListener(LevelProgress.OpenMap);
    }

    public void Show(bool won, int score, int best, bool isRecord, int bonus, bool hasNext)
    {
        gameObject.SetActive(true);
        //the HUD restart pill has no job once the results are up; Play again covers it
        var restart = FindFirstObjectByType<RestartButton>();
        if (restart != null) restart.gameObject.SetActive(false);

        bool boss = GameManager.Instance != null && GameManager.Instance.Level != null && GameManager.Instance.Level.isBoss;
        if (titleLabel) titleLabel.text = won ? (boss ? bossWinTitle : winTitle) : loseTitle;
        Sfx.PlayDelayed(won ? (boss ? "boss_win" : "win") : "lose", dropDuration * 0.5f);
        if (scoreLabel) scoreLabel.text = $"Score  {score:N0}";
        if (bestLabel) bestLabel.text = $"Best  {best:N0}";
        if (bonusLabel)
        {
            bonusLabel.gameObject.SetActive(won && bonus > 0);
            bonusLabel.text = $"+{bonus:N0} for spared hogs";
        }
        if (recordLabel) recordLabel.gameObject.SetActive(won && isRecord);
        if (nextButton) nextButton.gameObject.SetActive(won && hasNext);

        //stars: one for the win, more for doing it with hogs to spare
        int earned = won ? LevelProgress.StarsFor(score, GameManager.Instance != null ? GameManager.Instance.HensAtStart : 1) : 0;
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;
            stars[i].gameObject.SetActive(true);
            //an open daisy for each one earned, a closed bud for the rest
            if (earnedSprite != null && unearnedSprite != null) stars[i].sprite = i < earned ? earnedSprite : unearnedSprite;
            stars[i].color = i < earned ? Color.white : new Color(0.85f, 0.85f, 0.8f, 0.9f);
            stars[i].transform.localScale = Vector3.zero;
            stars[i].transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetDelay(dropDuration + 0.15f * i).SetUpdate(true).SetLink(gameObject);
            if (i < earned) Sfx.PlayDelayed("star", dropDuration + 0.15f * i, 1f);
        }

        //swing in from above
        if (panel != null)
        {
            Vector2 target = panel.anchoredPosition;
            panel.anchoredPosition = target + Vector2.up * 900f;
            panel.DOKill();
            panel.DOAnchorPos(target, dropDuration).SetEase(Ease.OutBounce).SetUpdate(true).SetLink(gameObject);
        }
    }
}
