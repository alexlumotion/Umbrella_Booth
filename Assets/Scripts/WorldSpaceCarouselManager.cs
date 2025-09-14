using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class WorldSpaceCarouselManager : MonoBehaviour
{
    [Header("Cards")]
    public RectTransform[] cards;

    [Header("Layout")]
    public float spacing = 1.65f;        // X зсув між сусідами (у world units)
    public float depth = 0.84f;          // Z зсув сусідів назад
    public float maxYRotation = 28f;     // кут "закриття" (фліпу)

    [Header("Tween (move/scale)")]
    public float tweenDuration = 0.4f;
    public Ease tweenEase = Ease.OutCubic;

    [Header("Flip (Y-rotation)")]
    [Tooltip("Тривалість фліп-анімації (закриття/відкриття)")]
    public float flipDuration = 0.5f;

    [Header("Optional Fade/Scale")]
    public bool dimNeighbours = false;
    [Range(0f,1f)] public float neighbourAlpha = 0.6f;
    public float neighbourScale = 0.9f;

    [Header("Visible Range")]
    [Tooltip("Скільки карток видно з кожного боку від центру (1 = центр+ліва+права)")]
    public int sideCount = 1;

    [Header("Start index")]
    public int startIndex = 0;

    private int currentIndex;
    private bool isAnimating;

    void Start()
    {
        currentIndex = Mathf.Clamp(startIndex, 0, cards.Length - 1);
        // Початкове розміщення: усі видимі Y=0, невидимі — snap і вимкнені
        LayoutForIndex(currentIndex, instant: true, forceYZeroForVisible: true);
    }

    public void ShowNext()
    {
        if (isAnimating) return;
        RunFlipSequence(next: true);
    }

    public void ShowPrev()
    {
        if (isAnimating) return;
        RunFlipSequence(next: false);
    }

    // ---------------- Core sequence: Flip -> Reindex (prepare invisibles) -> Move+Unflip ----------------
    private void RunFlipSequence(bool next)
    {
        isAnimating = true;

        float closingAngle = next ? -maxYRotation : +maxYRotation;  // ← додали

        // 1) Закриття
        var visibleNow = GetVisibleSet(currentIndex);
        foreach (var rt in visibleNow)
        {
            if (!rt) continue;
            rt.DOKill();
            rt.DOLocalRotate(new Vector3(0f, closingAngle, 0f), flipDuration).SetEase(Ease.OutCubic);
        }

        // 2) Після закриття
        DOVirtual.DelayedCall(flipDuration, () =>
        {
            currentIndex = next
                ? (currentIndex + 1) % cards.Length
                : (currentIndex - 1 + cards.Length) % cards.Length;

            // Тут теж передаємо closingAngle у LayoutForIndex, щоб видимі залишились у правильному знакові
            LayoutForIndex(currentIndex, instant: true, forceYZeroForVisible: false,
                        keepClosedRotationForVisible: true, prepareInvisibleOnly: true,
                        customClosedAngle: closingAngle);

            // 3) Відкриття: видимі одночасно рухаються у свої НОВІ позиції + крутяться назад до 0°
            var visibleAfter = GetVisibleSet(currentIndex);
            foreach (var rt in visibleAfter)
            {
                if (!rt) continue;
                rt.DOKill();

                // Обчислити нові параметри (позицію/масштаб/альфу) для ЦЬОГО rt при currentIndex
                float off;
                Vector3 targetPos, targetScale;
                float targetAlpha;
                ComputeParamsFor(rt, currentIndex, out off, out targetPos, out targetScale, out targetAlpha);

                // Рух у нову позицію (позиція змінюється тут, а не під час LayoutForIndex)
                rt.DOLocalMove(targetPos, tweenDuration).SetEase(tweenEase);
                rt.DOScale(targetScale, tweenDuration).SetEase(tweenEase);

                // Обертання назад до 0
                rt.DOLocalRotate(Vector3.zero, flipDuration).SetEase(Ease.OutCubic);

                if (dimNeighbours)
                {
                    var cg = rt.GetComponent<CanvasGroup>();
                    if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
                    cg.DOFade(targetAlpha, tweenDuration).SetEase(tweenEase);
                    bool isCenter = Mathf.Abs(off) < 0.01f;
                    cg.interactable = isCenter;
                    cg.blocksRaycasts = isCenter;
                }
            }

            // Кінець послідовності
            DOVirtual.DelayedCall(Mathf.Max(flipDuration, tweenDuration), () => { isAnimating = false; });
        });
    }

    // ---------------- Layout helper ----------------
    // instant=true: миттєво ставимо трансформи
    // forceYZeroForVisible: у стабільному стані видимим ставимо Y=0
    // keepClosedRotationForVisible: якщо true — видимим залишаємо Y=-max (перед відкриттям)
    // prepareInvisibleOnly: якщо true — видимі НЕ чіпаємо (залишаються на старих позиціях для красивого руху на фазі відкриття)
    private void LayoutForIndex(int index, bool instant, bool forceYZeroForVisible,
                            bool keepClosedRotationForVisible = false,
                            bool prepareInvisibleOnly = false,
                            float customClosedAngle = 0f)
    {
        int n = cards.Length;
        for (int i = 0; i < n; i++)
        {
            RectTransform card = cards[i];
            if (!card) continue;

            // Обчислюємо offset у кільці
            int rawOffset = i - index;
            if (rawOffset > n / 2) rawOffset -= n;
            if (rawOffset < -n / 2) rawOffset += n;
            float offset = rawOffset;

            bool isVisible = Mathf.Abs(offset) <= sideCount + 0.001f;

            // Параметри steady-state (позиція/масштаб/альфа)
            Vector3 pos = new Vector3(offset * spacing, 0f, -Mathf.Abs(offset) * depth);
            Vector3 scale = Vector3.one;
            float alpha = 1f;

            if (dimNeighbours && Mathf.Abs(offset) > 0.01f && isVisible)
            {
                scale = Vector3.one * neighbourScale;
                alpha = neighbourAlpha;
            }

            if (!isVisible)
            {
                // Невидимі: завжди snap у правильне місце і вимкнути, щоб не мигтіли
                if (instant)
                {
                    card.localPosition = pos;
                    card.localScale = scale;
                    card.localRotation = Quaternion.identity; // steady 0
                }
                if (card.gameObject.activeSelf) card.gameObject.SetActive(false);

                var cgHidden = card.GetComponent<CanvasGroup>();
                if (dimNeighbours)
                {
                    if (cgHidden == null) cgHidden = card.gameObject.AddComponent<CanvasGroup>();
                    cgHidden.alpha = alpha;
                    cgHidden.interactable = false;
                    cgHidden.blocksRaycasts = false;
                }
                continue;
            }
            else
            {
                if (!card.gameObject.activeSelf) card.gameObject.SetActive(true);
            }

            // Якщо готуємо лише невидимі — видимі не рухаємо тут (щоб вони плавно ЇХАЛИ під час відкриття)
            if (prepareInvisibleOnly) continue;

            // Видимі: steady rotation = 0, але якщо старт відкриття — залишаємо -max
             Quaternion targetRot = Quaternion.identity;
            if (keepClosedRotationForVisible)
                targetRot = Quaternion.Euler(0f, customClosedAngle, 0f);
            if (forceYZeroForVisible)
                targetRot = Quaternion.identity;

            var cg = card.GetComponent<CanvasGroup>();
            if (dimNeighbours)
            {
                if (cg == null) cg = card.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = alpha;
            }

            if (instant)
            {
                card.localPosition = pos;
                card.localScale = scale;
                card.localRotation = targetRot;
            }
            else
            {
                card.DOKill();
                card.DOLocalMove(pos, tweenDuration).SetEase(tweenEase);
                card.DOScale(scale, tweenDuration).SetEase(tweenEase);
                card.DOLocalRotateQuaternion(targetRot, tweenDuration).SetEase(tweenEase);
                if (dimNeighbours && cg != null) cg.DOFade(alpha, tweenDuration).SetEase(tweenEase);
            }

            if (cg != null)
            {
                bool isCenter = Mathf.Abs(offset) < 0.01f;
                cg.interactable = isCenter;
                cg.blocksRaycasts = isCenter;
            }
        }
    }

    // Повертає видимі RectTransform для поточного індексу
    private RectTransform[] GetVisibleSet(int index)
    {
        List<RectTransform> list = new List<RectTransform>();
        int n = cards.Length;
        for (int i = 0; i < n; i++)
        {
            RectTransform rt = cards[i];
            if (!rt) continue;

            int rawOffset = i - index;
            if (rawOffset > n / 2) rawOffset -= n;
            if (rawOffset < -n / 2) rawOffset += n;
            float off = rawOffset;

            if (Mathf.Abs(off) <= sideCount + 0.001f)
                list.Add(rt);
        }
        return list.ToArray();
    }

    // Обчислює target-параметри для конкретної картки rt при даному index (для фази "відкриття")
    private void ComputeParamsFor(RectTransform rt, int index,
                                  out float offset, out Vector3 targetPos, out Vector3 targetScale, out float targetAlpha)
    {
        offset = 999f;
        targetPos = Vector3.zero;
        targetScale = Vector3.one;
        targetAlpha = 1f;

        int i = System.Array.IndexOf(cards, rt);
        if (i < 0) return;

        int n = cards.Length;
        int rawOffset = i - index;
        if (rawOffset > n / 2) rawOffset -= n;
        if (rawOffset < -n / 2) rawOffset += n;
        offset = rawOffset;

        targetPos = new Vector3(offset * spacing, 0f, -Mathf.Abs(offset) * depth);
        targetScale = Vector3.one;

        if (dimNeighbours && Mathf.Abs(offset) > 0.01f)
        {
            targetScale = Vector3.one * neighbourScale;
            targetAlpha = neighbourAlpha;
        }
        else
        {
            targetAlpha = 1f;
        }
    }
}