using UnityEngine;
using UnityEngine.UI;

//plays a soft click when the button on this object is pressed
[RequireComponent(typeof(Button))]
public class ButtonSfx : MonoBehaviour
{
    [SerializeField] private string sound = "ui_click";

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => Sfx.Play(sound));
    }
}
