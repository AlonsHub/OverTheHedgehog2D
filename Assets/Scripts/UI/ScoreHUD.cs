using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//score readout that counts up and gives a little hop when it changes
public class ScoreHUD : MonoBehaviour
{
    [SerializeField] private Text label;
    [SerializeField] private string format = "Score: {0:N0}";
    [SerializeField] private float countUpTime = 0.4f;

    int _shown;
    Tween _count;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ScoreChanged += OnScoreChanged;
            _shown = GameManager.Instance.Score;
        }
        Refresh(_shown);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ScoreChanged -= OnScoreChanged;
    }

    void OnScoreChanged(int score)
    {
        _count?.Kill();
        _count = DOVirtual.Int(_shown, score, countUpTime, Refresh).SetEase(Ease.OutQuad).SetLink(gameObject);

        transform.DOKill(true);
        transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 6, 0.6f).SetLink(gameObject);
    }

    void Refresh(int value)
    {
        _shown = value;
        if (label != null) label.text = string.Format(format, value);
    }
}
