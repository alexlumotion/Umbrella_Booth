using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

// Повісь на об'єкт з Button (Transition=None). 
// Налаштуй target (RectTransform), optional: bg Image, label (TMP або Text).
[RequireComponent(typeof(Button))]
public class UIButtonFx : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Targets")]
    public RectTransform target;      // якщо null — візьме свій RectTransform
    public Graphic bg;                // опціонально: Image/RawImage/Text для підсвітки кольором
    public Graphic label;             // опціонально: текст/іконка для легкого тінту

    [Header("Hover (наведення)")]
    public bool enableHoverScale = true;
    public float hoverScale = 1.05f;
    public float hoverDuration = 0.15f;
    public Ease hoverEase = Ease.OutQuad;

    [Header("Press (натискання)")]
    public bool enablePressBounce = true;
    public float pressScale = 0.94f;          // "втиснути" кнопку
    public float pressInDuration = 0.08f;     // швидко
    public float releaseDuration = 0.18f;     // пружинкою назад
    public Ease pressInEase = Ease.OutQuad;
    public Ease releaseEase = Ease.OutBack;

    [Header("Tint (легке підсвітлення)")]
    public bool enableTint = true;
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 1f, 1f, 1f);
    public Color pressedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    public float tintDuration = 0.12f;

    [Header("Ripple Blink (за кліком)")]
    public bool enableRipple = false;
    public Image ripplePrefab;               // прозорий білий кружок/градієнт
    public float rippleDuration = 0.35f;
    public float rippleStartScale = 0.4f;
    public float rippleEndScale = 1.4f;
    public float rippleStartAlpha = 0.25f;

    Vector3 _baseScale;
    Tween _scaleTween, _tintBgTween, _tintLabelTween;

    void Reset()
    {
        target = GetComponent<RectTransform>();
        bg = GetComponent<Graphic>();
    }

    void Awake()
    {
        if (target == null) target = transform as RectTransform;
        _baseScale = target.localScale;

        // ініціальний колір
        if (enableTint)
        {
            if (bg) bg.color = normalColor;
            if (label) label.color = normalColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (enableHoverScale)
            PlayScale(_baseScale * hoverScale, hoverDuration, hoverEase);

        if (enableTint)
            PlayTint(hoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // повертаємося в нормальний стан
        if (enableHoverScale)
            PlayScale(_baseScale, hoverDuration, hoverEase);

        if (enableTint)
            PlayTint(normalColor);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (enablePressBounce)
            PlayScale(_baseScale * pressScale, pressInDuration, pressInEase);

        if (enableTint)
            PlayTint(pressedColor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (enablePressBounce)
        {
            // якщо курсор досі над кнопкою — відпружинити до hoverScale, інакше — до нормального
            bool stillHover = RectTransformUtility.RectangleContainsScreenPoint(
                target, eventData.position, eventData.enterEventCamera);
            var to = _baseScale * (stillHover && enableHoverScale ? hoverScale : 1f);
            PlayScale(to, releaseDuration, releaseEase);
        }

        if (enableTint)
        {
            // повернутись до hover або normal
            bool stillHover = RectTransformUtility.RectangleContainsScreenPoint(
                target, eventData.position, eventData.enterEventCamera);
            PlayTint(stillHover ? hoverColor : normalColor);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (enableRipple && ripplePrefab != null)
            PlayRipple(eventData);
    }

    // --- helpers ---

    void PlayScale(Vector3 to, float duration, Ease ease)
    {
        _scaleTween?.Kill();
        _scaleTween = target.DOScale(to, duration).SetEase(ease);
    }

    void PlayTint(Color to)
    {
        if (bg)
        {
            _tintBgTween?.Kill();
            _tintBgTween = bg.DOColor(to, tintDuration);
        }
        if (label)
        {
            _tintLabelTween?.Kill();
            _tintLabelTween = label.DOColor(to, tintDuration);
        }
    }

    void PlayRipple(PointerEventData ev)
    {
        // Створюємо імпульс у координатах кнопки
        var ripple = Instantiate(ripplePrefab, target);
        var rt = ripple.rectTransform;

        // позиція всередині кнопки
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(target, ev.position, ev.pressEventCamera, out localPos);
        rt.anchoredPosition = localPos;
        rt.localScale = Vector3.one * rippleStartScale;

        var c = ripple.color;
        c.a = rippleStartAlpha;
        ripple.color = c;

        // анімація: масштаб + затухання, потім знищення
        Sequence seq = DOTween.Sequence();
        seq.Join(rt.DOScale(rippleEndScale, rippleDuration).SetEase(Ease.OutQuart));
        seq.Join(ripple.DOFade(0f, rippleDuration).SetEase(Ease.OutQuad));
        seq.OnComplete(() => Destroy(ripple.gameObject));
    }

    void OnDisable()
    {
        _scaleTween?.Kill();
        _tintBgTween?.Kill();
        _tintLabelTween?.Kill();
        if (target) target.localScale = _baseScale;
        if (enableTint)
        {
            if (bg) bg.color = normalColor;
            if (label) label.color = normalColor;
        }
    }
}