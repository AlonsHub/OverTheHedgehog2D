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
    [Tooltip("Three daisies under the pot: open for each one earned, a closed bud for the rest")]
    [SerializeField] private Image[] stars;
    [SerializeField] private Sprite earnedSprite;
    [SerializeField] private Sprite unearnedSprite;

    int _index;
    bool _picked;

    public void Setup(int index, LevelDefinition level, bool unlocked, int best, int earnedStars = 0)
    {
        _index = index;

        //daisies only show once the level has been cleared at least once
        if (stars != null)
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                stars[i].gameObject.SetActive(unlocked && best > 0);
                stars[i].sprite = i < earnedStars ? earnedSprite : unearnedSprite;
            }

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
        if (_picked) return;
        _picked = true;
        transform.DOPunchScale(Vector3.one * 0.2f, 0.25f, 8, 0.7f).SetLink(gameObject);
        //the load is on a timer of its own, not the punch tween: on touch that tween can be killed by the
        //pointer-exit that follows the tap, which used to swallow the click entirely
        DOVirtual.DelayedCall(0.25f, () => LevelProgress.Play(_index)).SetLink(gameObject);
    }
}
