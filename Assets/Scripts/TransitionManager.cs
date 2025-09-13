using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

public enum SlideDirection { Left, Right, Up, Down }

public class TransitionManager : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private RectTransform startScreen;

    [Header("Defaults")]
    [SerializeField] private float defaultDuration = 0.45f;
    [SerializeField] private Ease defaultEaseIn = Ease.OutCubic;
    [SerializeField] private Ease defaultEaseOut = Ease.OutCubic;

    [Header("Overlay / Input Guard")]
    [Tooltip("Блокує ввід під час переходу; додатково може робити легкий темний fade (вимкнено за замовчуванням).")]
    [SerializeField] private bool useOverlayFade = false;
    [Range(0f, 1f)]
    [SerializeField] private float overlayMaxAlpha = 0.3f;
    [SerializeField] private Image overlay; // Повноекранний Image (Raycast Target = true)

    [Header("Unity Events (інспекторні)")]
    public UnityEvent onBeforeTransition;
    public UnityEvent onAfterTransition;
    public UnityEvent onScreenShown;
    public UnityEvent onScreenHidden;

    // C# події (якщо зручно підписуватись у коді)
    public event System.Action<RectTransform, RectTransform> BeforeTransition;
    public event System.Action<RectTransform, RectTransform> AfterTransition;
    public event System.Action<RectTransform> ScreenShown;
    public event System.Action<RectTransform> ScreenHidden;

    public RectTransform CurrentScreen { get; private set; }
    public bool IsTransitioning { get; private set; }

    private CanvasGroup _overlayCg;
    private Sequence _seq;

    void Awake()
    {
        EnsureOverlay();
    }

    void Start()
    {
        if (startScreen != null)
        {
            // Стартовий екран на (0,0), активний.
            startScreen.gameObject.SetActive(true);
            startScreen.anchoredPosition = Vector2.zero;
            CurrentScreen = startScreen;
        }
    }

    /// <summary>
    /// Показати target екран слайдом у вказаному напрямку. Тривалість/ізинг — кастомні або дефолтні.
    /// </summary>
    public void Show(
        RectTransform target,
        SlideDirection direction,
        Ease? easeIn = null,
        Ease? easeOut = null,
        float? duration = null
    )
    {
        if (IsTransitioning) return;
        if (target == null) return;
        if (CurrentScreen == target) return;

        RectTransform parentRt = GetParentRectTransform(target);
        if (parentRt == null)
        {
            Debug.LogWarning("[TransitionManager] Target має бути під RectTransform (Canvas/Container).");
            return;
        }

        // На випадок перших показів або поспіль викликів — гасимо попередні твіни.
        KillActiveSequence();
        target.DOKill();
        CurrentScreen?.DOKill();

        Vector2 canvasSize = parentRt.rect.size;
        float w = canvasSize.x;
        float h = canvasSize.y;

        // Визначаємо, куди виїде старий і звідки заїде новий.
        Vector2 outPos;
        Vector2 inStartPos;
        switch (direction)
        {
            case SlideDirection.Left:
                outPos = new Vector2(-w, 0f);
                inStartPos = new Vector2( w, 0f);
                break;
            case SlideDirection.Right:
                outPos = new Vector2( w, 0f);
                inStartPos = new Vector2(-w, 0f);
                break;
            case SlideDirection.Up:
                outPos = new Vector2(0f,  h);
                inStartPos = new Vector2(0f, -h);
                break;
            default: // Down
                outPos = new Vector2(0f, -h);
                inStartPos = new Vector2(0f,  h);
                break;
        }

        float dur = Mathf.Max(0f, duration ?? defaultDuration);
        Ease inE = easeIn ?? defaultEaseIn;
        Ease outE = easeOut ?? defaultEaseOut;

        // Активуємо таргет і ставимо його за межі кадру.
        target.gameObject.SetActive(true);
        target.anchoredPosition = inStartPos;

        // Готуємо Overlay/Guard.
        EnableOverlay(true, instant: !useOverlayFade);

        // Колбеки "перед"
        onBeforeTransition?.Invoke();
        BeforeTransition?.Invoke(CurrentScreen, target);

        IsTransitioning = true;

        _seq = DOTween.Sequence();

        // Анімації екранів
        if (CurrentScreen != null)
        {
            _seq.Join(CurrentScreen.DOAnchorPos(outPos, dur).SetEase(outE));
        }
        _seq.Join(target.DOAnchorPos(Vector2.zero, dur).SetEase(inE));

        // Опційний overlay fade (0 -> overlayMaxAlpha -> 0)
        if (useOverlayFade && _overlayCg != null)
        {
            _overlayCg.alpha = 0f;
            _seq.Join(_overlayCg.DOFade(overlayMaxAlpha, dur * 0.5f).SetEase(Ease.OutQuad));
            _seq.Append(_overlayCg.DOFade(0f, dur * 0.5f).SetEase(Ease.InQuad));
        }

        _seq.OnComplete(() =>
        {
            // Після завершення — ховаємо попередній
            if (CurrentScreen != null)
            {
                CurrentScreen.gameObject.SetActive(false);
                onScreenHidden?.Invoke();
                ScreenHidden?.Invoke(CurrentScreen);
            }

            CurrentScreen = target;

            // Після — вимикаємо оверлей/гард
            EnableOverlay(false, instant: true);

            // Колбеки "після"
            onAfterTransition?.Invoke();
            AfterTransition?.Invoke(CurrentScreen, target);

            onScreenShown?.Invoke();
            ScreenShown?.Invoke(CurrentScreen);

            IsTransitioning = false;
        });
    }

    // Зручні шорткати
    public void ShowLeft(RectTransform target, Ease? easeIn = null, Ease? easeOut = null, float? duration = null)
        => Show(target, SlideDirection.Left, easeIn, easeOut, duration);
    public void ShowRight(RectTransform target, Ease? easeIn = null, Ease? easeOut = null, float? duration = null)
        => Show(target, SlideDirection.Right, easeIn, easeOut, duration);
    public void ShowUp(RectTransform target, Ease? easeIn = null, Ease? easeOut = null, float? duration = null)
        => Show(target, SlideDirection.Up, easeIn, easeOut, duration);
    public void ShowDown(RectTransform target, Ease? easeIn = null, Ease? easeOut = null, float? duration = null)
        => Show(target, SlideDirection.Down, easeIn, easeOut, duration);

    // Допоміжні
    private void EnsureOverlay()
    {
        if (overlay == null)
        {
            // Створюємо простий повноекранний guard під той самий Canvas
            var parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null) parentCanvas = FindObjectOfType<Canvas>();
            if (parentCanvas == null) return;

            var go = new GameObject("TransitionOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(parentCanvas.transform, worldPositionStays: false);

            overlay = go.GetComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0f); // прозорий чорний
            overlay.raycastTarget = true;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _overlayCg = go.GetComponent<CanvasGroup>();
            _overlayCg.alpha = 0f;
        }
        else
        {
            _overlayCg = overlay.GetComponent<CanvasGroup>();
            if (_overlayCg == null) _overlayCg = overlay.gameObject.AddComponent<CanvasGroup>();
            overlay.color = new Color(0f, 0f, 0f, overlay.color.a); // гарантуємо чорний відтінок
        }

        // На старті — вимкнутий як guard (але активний об’єкт лишається)
        EnableOverlay(false, instant: true);
    }

    private void EnableOverlay(bool enabled, bool instant)
    {
        if (overlay == null) return;
        overlay.raycastTarget = enabled;

        if (_overlayCg != null)
        {
            if (instant || !useOverlayFade)
            {
                _overlayCg.alpha = 0f;
            }
        }
    }

    private RectTransform GetParentRectTransform(RectTransform rt)
    {
        if (rt == null) return null;
        var p = rt.parent as RectTransform;
        return p;
    }

    private void KillActiveSequence()
    {
        if (_seq != null && _seq.IsActive())
        {
            _seq.Kill();
            _seq = null;
        }
    }
}