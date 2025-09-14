using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class WorldSpaceCarouselManager : MonoBehaviour
{
    [Header("Cards")]
    public RectTransform[] cards;

    [Header("Layout")]
    public float spacing = 1.65f;
    public float depth = 0.84f;
    public float maxYRotation = 28f;

    [Header("Tween (move/scale)")]
    public float tweenDuration = 0.4f;
    public Ease tweenEase = Ease.OutCubic;

    [Header("Flip (Y-rotation)")]
    public float flipDuration = 0.5f;

    [Header("Optional Fade/Scale")]
    public bool dimNeighbours = false;
    [Range(0f,1f)] public float neighbourAlpha = 0.6f;
    public float neighbourScale = 0.9f;

    [Header("Visible Range")]
    public int sideCount = 1;

    [Header("Start index")]
    public int startIndex = 0;

    private int currentIndex;
    private bool isAnimating;

    void Start()
    {
        currentIndex = Mathf.Clamp(startIndex, 0, cards.Length - 1);
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

    private void RunFlipSequence(bool next)
    {
        isAnimating = true;

        float closingAngle = next ? -maxYRotation : +maxYRotation;

        // 1) закриваємо видимі
        var visibleNow = GetVisibleSet(currentIndex);
        foreach (var rt in visibleNow)
        {
            if (!rt) continue;
            rt.DOKill();
            rt.DOLocalRotate(new Vector3(0f, closingAngle, 0f), flipDuration).SetEase(Ease.OutCubic);
        }

        // 2) після закриття: зсув індексу + підготовка
        DOVirtual.DelayedCall(flipDuration, () =>
        {
            // визначаємо множину "колишніх видимих"
            var prevVisibleSet = new HashSet<RectTransform>(visibleNow); // *****

            currentIndex = next
                ? (currentIndex + 1) % cards.Length
                : (currentIndex - 1 + cards.Length) % cards.Length;

            // готуємо тільки НEвидимі, але НЕ чіпаємо ті, що були видимі до цього (щоб вони могли красиво виїхати)  // *****
            LayoutForIndex(currentIndex,
                           instant: true,
                           forceYZeroForVisible: false,
                           keepClosedRotationForVisible: true,
                           prepareInvisibleOnly: true,
                           customClosedAngle: closingAngle,
                           excludeFromSnap: prevVisibleSet); // *****

            // 3) відкриття: анімуємо видимі та "аутгоїнг" (які щойно стали невидимими)
            var visibleAfter = new HashSet<RectTransform>(GetVisibleSet(currentIndex));

            var toDisableLater = new List<RectTransform>(); // вимкнемо після руху

            for (int i = 0; i < cards.Length; i++)
            {
                RectTransform rt = cards[i];
                if (!rt) continue;

                // параметри для нового індексу
                float off;
                Vector3 targetPos, targetScale;
                float targetAlpha;
                ComputeParamsFor(rt, currentIndex, out off, out targetPos, out targetScale, out targetAlpha);

                bool willBeVisible = Mathf.Abs(off) <= sideCount + 0.001f;
                bool wasVisible = prevVisibleSet.Contains(rt); // *****

                rt.DOKill();

                if (willBeVisible)
                {
                    // звичайні видимі: їдуть у нову позицію + розкриваються до 0
                    rt.DOLocalMove(targetPos, tweenDuration).SetEase(tweenEase);
                    rt.DOScale(targetScale, tweenDuration).SetEase(tweenEase);
                    rt.DOLocalRotate(Vector3.zero, flipDuration).SetEase(Ease.OutCubic);

                    if (dimNeighbours)
                    {
                        var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
                        cg.DOFade(targetAlpha, tweenDuration).SetEase(tweenEase);
                        bool isCenter = Mathf.Abs(off) < 0.01f;
                        cg.interactable = isCenter;
                        cg.blocksRaycasts = isCenter;
                    }
                }
                else
                {
                    // ТІЛЬКИ якщо вона була видимою щойно (а зараз стала невидимою) — даємо їй виїхати tween'ом  // *****
                    if (wasVisible)
                    {
                        rt.DOLocalMove(targetPos, tweenDuration).SetEase(tweenEase);
                        rt.DOScale(targetScale, tweenDuration).SetEase(tweenEase);
                        rt.DOLocalRotate(Vector3.zero, flipDuration).SetEase(Ease.OutCubic);

                        if (dimNeighbours)
                        {
                            var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
                            cg.DOFade(targetAlpha, tweenDuration).SetEase(tweenEase);
                            cg.interactable = false;
                            cg.blocksRaycasts = false;
                        }

                        toDisableLater.Add(rt); // вимкнемо ПІСЛЯ руху
                    }
                    else
                    {
                        // ті, що й до того були невидимі, уже заснейплені та можуть лишатись вимкненими
                        // нічого не робимо
                    }
                }
            }

            // 4) вимикаємо лише ті, що щойно виїхали за межі
            DOVirtual.DelayedCall(Mathf.Max(flipDuration, tweenDuration), () =>
            {
                foreach (var rt in toDisableLater)
                {
                    if (rt) rt.gameObject.SetActive(false);
                }
                isAnimating = false;
            });
        });
    }

    // instant=true: миттєва розкладка
    // forceYZeroForVisible: видимим ставимо Y=0
    // keepClosedRotationForVisible: якщо true — видимим залишаємо Y=customClosedAngle (для старту відкриття)
    // prepareInvisibleOnly: якщо true — обробляємо ЛИШЕ невидимі
    // excludeFromSnap: НЕ чіпати ці карти (залишити як є), навіть якщо вони стали невидимими зараз  // *****
    private void LayoutForIndex(int index, bool instant, bool forceYZeroForVisible,
                                bool keepClosedRotationForVisible = false, bool prepareInvisibleOnly = false,
                                float customClosedAngle = 0f, HashSet<RectTransform> excludeFromSnap = null) // *****
    {
        int n = cards.Length;
        for (int i = 0; i < n; i++)
        {
            RectTransform card = cards[i];
            if (!card) continue;

            int rawOffset = i - index;
            if (rawOffset > n / 2) rawOffset -= n;
            if (rawOffset < -n / 2) rawOffset += n;
            float offset = rawOffset;

            bool isVisible = Mathf.Abs(offset) <= sideCount + 0.001f;

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
                // якщо картка була видима щойно — НЕ чіпаємо її зараз (вона виїде tween'ом)  // *****
                if (prepareInvisibleOnly && excludeFromSnap != null && excludeFromSnap.Contains(card))
                {
                    // leave as is
                }
                else
                {
                    if (instant)
                    {
                        card.localPosition = pos;
                        card.localScale = scale;
                        card.localRotation = Quaternion.identity;
                    }
                    // ця категорія може бути вимкнена відразу (бо вона не "стара крайня")
                    if (card.gameObject.activeSelf) card.gameObject.SetActive(false);
                }

                if (dimNeighbours)
                {
                    var cgHidden = card.GetComponent<CanvasGroup>() ?? card.gameObject.AddComponent<CanvasGroup>();
                    cgHidden.alpha = isVisible ? alpha : cgHidden.alpha;
                    cgHidden.interactable = false;
                    cgHidden.blocksRaycasts = false;
                }
                continue;
            }
            else
            {
                if (!card.gameObject.activeSelf) card.gameObject.SetActive(true);
            }

            if (prepareInvisibleOnly) continue;

            Quaternion targetRot = Quaternion.identity;
            if (keepClosedRotationForVisible) targetRot = Quaternion.Euler(0f, customClosedAngle, 0f);
            if (forceYZeroForVisible)         targetRot = Quaternion.identity;

            var cg = dimNeighbours ? (card.GetComponent<CanvasGroup>() ?? card.gameObject.AddComponent<CanvasGroup>()) : null;
            if (dimNeighbours && cg != null) cg.alpha = alpha;

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

    private RectTransform[] GetVisibleSet(int index)
    {
        List<RectTransform> list = new List<RectTransform>();
        int n = cards.Length;
        for (int i = 0; i < n; i++)
        {
            var rt = cards[i];
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