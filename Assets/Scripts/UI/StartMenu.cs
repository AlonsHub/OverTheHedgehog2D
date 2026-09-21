using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//title screen: the hanging sign sways in, Play takes you to the map
public class StartMenu : MonoBehaviour
{
    [SerializeField] private RectTransform sign;
    [SerializeField] private Button playButton;
    [SerializeField] private Button resetProgressButton;

    void Start()
    {
        playButton?.onClick.AddListener(LevelProgress.OpenMap);
        resetProgressButton?.onClick.AddListener(() => { PlayerPrefs.DeleteAll(); PlayerPrefs.Save(); });

        if (sign != null)
        {
            //a lazy swing on its ropes, forever
            sign.localRotation = Quaternion.Euler(0f, 0f, -2f);
            sign.DOLocalRotate(new Vector3(0f, 0f, 2f), 2.4f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }
    }
}
