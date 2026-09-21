using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//one flower pot on the map
public class LevelNodeUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image pot;
    [SerializeField] private Sprite unlockedSprite;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Text numberLabel;
    [SerializeField] private Text bestLabel;
    [SerializeField] private GameObject bossBadge;

    int _index;

    public void Setup(int index, LevelDefinition level, bool unlocked, int best)
    {
        _index = index;

        if (pot != null) pot.sprite = unlocked ? unlockedSprite : lockedSprite;
        if (numberLabel != null)
        {
            //tutorials don't count towards the numbering
            int shown = index + 1 - (LevelProgress.FirstRealLevel > index ? 0 : LevelProgress.FirstRealLevel);
            numberLabel.text = level != null && level.isTutorial ? "?" : level != null && level.isBoss ? "!" : shown.ToString();
        }
        if (bestLabel != null)
        {
            bestLabel.gameObject.SetActive(best > 0);
            bestLabel.text = best.ToString("N0");
        }
        if (bossBadge != null) bossBadge.SetActive(level != null && level.isBoss);

        if (button != null)
        {
            button.interactable = unlocked;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Pick);
        }

        //locked pots sit a bit dull
        if (pot != null) pot.color = unlocked ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f);
    }

    void Pick()
    {
        transform.DOKill(true);
        transform.DOPunchScale(Vector3.one * 0.2f, 0.25f, 8, 0.7f)
            .SetLink(gameObject)
            .OnComplete(() => LevelProgress.Play(_index));
    }
}
