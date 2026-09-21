using UnityEngine;
using UnityEngine.UI;

//shows how many hogs are still to be thrown: the ones waiting in the stock plus the one on the thrower
public class HogCounterHUD : MonoBehaviour
{
    [SerializeField] private HogStock stock;
    [SerializeField] private Thrower thrower;
    [SerializeField] private Text label;
    [SerializeField] private string format = "Hogs: {0}";

    private int _shown = -1;

    private void Update()
    {
        int remaining = stock.Count + (thrower.IsLoaded ? 1 : 0);
        if (remaining == _shown) return;

        _shown = remaining;
        label.text = string.Format(format, remaining);
    }
}
