using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

public enum SlideDirection { Left, Right, Up, Down, Fade, Instant }

public class TransitionManager : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private RectTransform startScreen;

    [Header("Defaults")]
    [SerializeField] private float defaultDuration = 0.45f;
    [SerializeField] private Ease defaultEaseIn = Ease.OutCubic;
    [SerializeField] private Ease defaultEaseOut = Ease.OutCubic;

    [Header("Overlay / Input Guard")]
    [SerializeField] private bool useOverlayFade = false;
    [Range(0f, 1f)]
    [SerializeField] private float overlayMaxAlpha = 0.3f;
    [SerializeField] private Image overlay; // повноекранний Image (Raycast Target = true)

    [Header("Unity Events (інспектор)")]
    public UnityEvent onBeforeTransition;
    public UnityEvent onAfterTransition;
    public UnityEvent onScreenShown;
    public UnityEvent onScreenHidden;

    // C# events
    public event System.Action<RectTransform, RectTransform> BeforeTransition;
    public event System.Action<RectTransform, RectTransform> AfterTransition;
    public event System.Action<RectTransform> ScreenShownEvent;
    public event System.Action<RectTransform> ScreenHiddenEvent;

    public RectTransform CurrentScreen { get; private set; }
    public bool IsTransitioning { get; private set; }

    private CanvasGroup _overlayCg;
    private Sequence _seq;

    void Awake() => EnsureOverlay();

    void Start()
    {
        if (startScreen != null)
        {
            startScreen.gameObject.SetActive(true);
            startScreen.anchoredPosition = Vector2.zero;
            CurrentScreen = startScreen;
        }
    }

    /// <summary>
    /// Головний метод показу екрана. Підтримує Slide/Fade/Instant та onComplete-колбек.
    /// </summary>
    public void Show(
        RectTransform target,
        SlideDirection direction,
        Ease? easeIn = null,
        Ease? easeOut = null,
        float? duration = null,
        UnityAction onComplete = null
    )
    {
        if (IsTransitioning && direction != SlideDirection.Instant) return;
        if (!target) return;

        // Якщо ціль = поточний — просто викликаємо колбек і все
        if (CurrentScreen == target)
        {
            onComplete?.Invoke();
            return;
        }

        // ---------- МОМЕНТАЛЬНИЙ ПЕРЕХІД ----------
        if (direction == SlideDirection.Instant)
        {
            // before-callbacks
            onBeforeTransition?.Invoke();
            BeforeTransition?.Invoke(CurrentScreen, target);

            // активуємо ціль миттєво в (0,0)
            target.gameObject.SetActive(true);
            target.anchoredPosition = Vector2.zero;

            // скидаємо можливий fade на попередньому
            if (CurrentScreen)
            {
                var prevCg = CurrentScreen.GetComponent<CanvasGroup>();
                if (prevCg) prevCg.alpha = 1f;

                CurrentScreen.gameObject.SetActive(false);
                onScreenHidden?.Invoke();
                ScreenHiddenEvent?.Invoke(CurrentScreen);
            }

            CurrentScreen = target;

            // after-callbacks
            onAfterTransition?.Invoke();
            AfterTransition?.Invoke(CurrentScreen, target);
            onScreenShown?.Invoke();
            ScreenShownEvent?.Invoke(CurrentScreen);

            // користувацький колбек
            onComplete?.Invoke();
            return;
        }

        RectTransform parentRt = GetParentRectTransform(target);
        if (!parentRt)
        {
            Debug.LogWarning("[TransitionManager] Target має бути під RectTransform (Canvas/Container).");
            return;
        }

        KillActiveSequence();
        target.DOKill();
        CurrentScreen?.DOKill();

        float dur = Mathf.Max(0f, duration ?? defaultDuration);
        Ease inE = easeIn ?? defaultEaseIn;
        Ease outE = easeOut ?? defaultEaseOut;

        EnableOverlay(true, instant: !useOverlayFade);

        onBeforeTransition?.Invoke();
        BeforeTransition?.Invoke(CurrentScreen, target);

        IsTransitioning = true;

        // Розрахунок розміру полотна для slide
        Vector2 canvasSize = parentRt.rect.size;
        float w = canvasSize.x;
        float h = canvasSize.y;

        Vector2 outPos = Vector2.zero;
        Vector2 inStartPos = Vector2.zero;

        bool isFade = (direction == SlideDirection.Fade);

        if (!isFade)
        {
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
                case SlideDirection.Down:
                    outPos = new Vector2(0f, -h);
                    inStartPos = new Vector2(0f,  h);
                    break;
            }
        }

        target.gameObject.SetActive(true);

        _seq = DOTween.Sequence();

        if (isFade)
        {
            // Fade: позиція миттєво 0,0; альфа 0 -> 1, старий 1 -> 0
            var targetCg = EnsureCanvasGroup(target);
            target.anchoredPosition = Vector2.zero;
            targetCg.alpha = 0f;

            if (CurrentScreen)
            {
                var curCg = EnsureCanvasGroup(CurrentScreen);
                _seq.Join(curCg.DOFade(0f, dur).SetEase(outE));
            }
            _seq.Join(targetCg.DOFade(1f, dur).SetEase(inE));
        }
        else
        {
            // Slide
            target.anchoredPosition = inStartPos;

            if (CurrentScreen != null)
            {
                _seq.Join(CurrentScreen.DOAnchorPos(outPos, dur).SetEase(outE));
            }
            _seq.Join(target.DOAnchorPos(Vector2.zero, dur).SetEase(inE));
        }

        // Overlay fade (опційно)
        if (useOverlayFade && _overlayCg != null)
        {
            _overlayCg.alpha = 0f;
            _seq.Join(_overlayCg.DOFade(overlayMaxAlpha, dur * 0.5f).SetEase(Ease.OutQuad));
            _seq.Append(_overlayCg.DOFade(0f, dur * 0.5f).SetEase(Ease.InQuad));
        }

        _seq.OnComplete(() =>
        {
            if (CurrentScreen != null)
            {
                // повертаємо альфу, якщо був fade
                var prevCg = CurrentScreen.GetComponent<CanvasGroup>();
                if (prevCg) prevCg.alpha = 1f;

                CurrentScreen.gameObject.SetActive(false);
                onScreenHidden?.Invoke();
                ScreenHiddenEvent?.Invoke(CurrentScreen);
            }

            CurrentScreen = target;

            EnableOverlay(false, instant: true);

            onAfterTransition?.Invoke();
            AfterTransition?.Invoke(CurrentScreen, target);

            onScreenShown?.Invoke();
            ScreenShownEvent?.Invoke(CurrentScreen);

            IsTransitioning = false;

            onComplete?.Invoke();
        });
    }

    // Шорткати
    public void ShowLeft(RectTransform target, Ease? inE = null, Ease? outE = null, float? dur = null, UnityAction onComplete = null)
        => Show(target, SlideDirection.Left, inE, outE, dur, onComplete);
    public void ShowRight(RectTransform target, Ease? inE = null, Ease? outE = null, float? dur = null, UnityAction onComplete = null)
        => Show(target, SlideDirection.Right, inE, outE, dur, onComplete);
    public void ShowUp(RectTransform target, Ease? inE = null, Ease? outE = null, float? dur = null, UnityAction onComplete = null)
        => Show(target, SlideDirection.Up, inE, outE, dur, onComplete);
    public void ShowDown(RectTransform target, Ease? inE = null, Ease? outE = null, float? dur = null, UnityAction onComplete = null)
        => Show(target, SlideDirection.Down, inE, outE, dur, onComplete);
    public void ShowFade(RectTransform target, float? dur = null, UnityAction onComplete = null)
        => Show(target, SlideDirection.Fade, null, null, dur, onComplete);
    public void ShowInstant(RectTransform target, UnityAction onComplete = null)
        => Show(target, SlideDirection.Instant, null, null, null, onComplete);

    // Helpers
    private void EnsureOverlay()
    {
        if (overlay == null)
        {
            var parentCanvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
            if (parentCanvas != null)
            {
                var go = new GameObject("TransitionOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                go.transform.SetParent(parentCanvas.transform, false);

                overlay = go.GetComponent<Image>();
                overlay.color = new Color(0f, 0f, 0f, 0f);
                overlay.raycastTarget = true;

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                _overlayCg = go.GetComponent<CanvasGroup>();
                _overlayCg.alpha = 0f;
            }
        }
        else
        {
            _overlayCg = overlay.GetComponent<CanvasGroup>() ?? overlay.gameObject.AddComponent<CanvasGroup>();
            overlay.color = new Color(0f, 0f, 0f, overlay.color.a);
        }
        EnableOverlay(false, instant: true);
    }

    private void EnableOverlay(bool enabled, bool instant)
    {
        if (!overlay) return;
        overlay.raycastTarget = enabled;
        if (_overlayCg && (instant || !useOverlayFade)) _overlayCg.alpha = 0f;
    }

    private RectTransform GetParentRectTransform(RectTransform rt) => rt ? rt.parent as RectTransform : null;

    private void KillActiveSequence()
    {
        if (_seq != null && _seq.IsActive())
        {
            _seq.Kill();
            _seq = null;
        }
    }

    private CanvasGroup EnsureCanvasGroup(RectTransform rt)
    {
        var cg = rt.GetComponent<CanvasGroup>();
        if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }
}