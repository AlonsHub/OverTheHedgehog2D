using UnityEngine;

//an invisible wall just outside the camera's right edge so nothing tumbles out of view. placed from the
//real camera at start, so it lands on the actual edge whatever the aspect ratio
[RequireComponent(typeof(BoxCollider2D))]
public class ScreenWall : MonoBehaviour
{
    [Tooltip("How far past the visible edge the wall's inner face sits, so things can lean on the edge, not float in front of it")]
    [SerializeField] private float inset = 0.1f;
    [SerializeField] private float thickness = 2f;

    void Awake()
    {
        var cam = Camera.main;
        if (cam == null) return;
        float halfW = cam.orthographicSize * cam.aspect;
        float halfH = cam.orthographicSize;
        float right = cam.transform.position.x + halfW - inset;
        var box = GetComponent<BoxCollider2D>();
        //tall enough to catch anything lobbed high
        box.size = new Vector2(thickness, halfH * 6f);
        transform.position = new Vector3(right + thickness * 0.5f, cam.transform.position.y, 0f);
    }
}
