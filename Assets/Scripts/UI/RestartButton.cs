using UnityEngine;
using UnityEngine.UI;

//reloads the current level from scratch. lives on the HUD so any level can be retried mid-attempt
[RequireComponent(typeof(Button))]
public class RestartButton : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(LevelProgress.Replay);
    }
}
