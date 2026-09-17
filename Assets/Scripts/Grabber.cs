using UnityEngine;

public class Grabber : MonoBehaviour
{
    [SerializeField] private Thrower thrower;

    public bool isGrabbing;

    private void OnMouseDown()
    {
        isGrabbing = true;
    }
    private void OnMouseDrag()
    {
        //limit from thrower
    }

    private void OnMouseUp()
    {
        isGrabbing = false;
    }
}
