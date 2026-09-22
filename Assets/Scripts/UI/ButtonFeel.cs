using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//makes a button feel like a thing you can pick up: it grows and brightens when the pointer lands on it (with
//a soft tick), sinks when pressed, and springs back when released or left. sits beside ButtonSfx, which
//owns the click sound
[RequireComponent(typeof(Button))]
public class ButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float pressScale = 0.94f;
    [SerializeField] private float hoverBrightness = 1.12f;
    [SerializeField] private float hoverLift = 4f;
    [SerializeField] private string hoverSound = "ui_hover";

    Button _button;
    Graphic _graphic;
    Vector3 _baseScale;
    Vector2 _basePos;
    Color _baseColor;
    bool _hovered, _pressed;

    void Awake()
    {
        _button = GetComponent<Button>();
        _graphic = GetComponent<Graphic>();
        _baseScale = transform.localScale;
        _basePos = ((RectTransform)transform).anchoredPosition;
        if (_graphic != null) _baseColor = _graphic.color;
    }

    void OnDisable()
    {
        transform.DOKill();
        if (_graphic != null) { _graphic.DOKill(); _graphic.color = _baseColor; }
        transform.localScale = _baseScale;
        ((RectTransform)transform).anchoredPosition = _basePos;
        _hovered = _pressed = false;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!_button.interactable) return;
        _hovered = true;
        if (!string.IsNullOrEmpty(hoverSound)) Sfx.Play(hoverSound);
        Apply();
    }

    public void OnPointerExit(PointerEventData e)
    {
        _hovered = false;
        Apply();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!_button.interactable) return;
        _pressed = true;
        Apply();
    }

    public void OnPointerUp(PointerEventData e)
    {
        _pressed = false;
        Apply();
    }

    //one target per state, tweened with a little overshoot so it feels springy
    void Apply()
    {
        float scale = _pressed ? pressScale : _hovered ? hoverScale : 1f;
        float lift = _pressed ? -hoverLift * 0.5f : _hovered ? hoverLift : 0f;
        float bright = _hovered && !_pressed ? hoverBrightness : _pressed ? 0.9f : 1f;
        var rt = (RectTransform)transform;
        transform.DOKill();
        transform.DOScale(_baseScale * scale, 0.18f).SetEase(_pressed ? Ease.OutQuad : Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        rt.DOAnchorPos(_basePos + Vector2.up * lift, 0.18f).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
        if (_graphic != null)
        {
            _graphic.DOKill();
            _graphic.DOColor(new Color(_baseColor.r * bright, _baseColor.g * bright, _baseColor.b * bright, _baseColor.a), 0.15f).SetUpdate(true).SetLink(gameObject);
        }
    }
}
