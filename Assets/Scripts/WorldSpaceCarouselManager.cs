using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.Events;

public class WorldSpaceCarouselManager : MonoBehaviour
{
    [Header("Cards")]
    public RectTransform[] cards;

    [Header("Layout")]
    public float spacing = 1.65f;
    public float depth = 0.84f;
    public float maxYRotation = 28f;
    public float yOffset = -150f;

    [Header("Tween")]
    public float tweenDuration = 0.4f;
    public Ease tweenEase = Ease.OutCubic;

    [Header("Neighbours styling (optional)")]
    public bool dimNeighbours = false;
    [Range(0, 1)] public float neighbourAlpha = 0.6f;
    public float neighbourScale = 0.9f;

    [Header("Visible band")]
    public int sideCount = 1;

    [Header("Start")]
    public int startIndex = 0;

    // ---------- State ----------
    public int CurrentIndex { get; private set; }
    public RectTransform CurrentCenterCard
        => (cards != null && cards.Length > 0 && CurrentIndex >= 0 && CurrentIndex < cards.Length)
           ? cards[CurrentIndex] : null;

    public RectTransform GetCardAtOffset(int offset)
    {
        if (cards == null || cards.Length == 0) return null;
        int n = cards.Length;
        int idx = (CurrentIndex + offset) % n;
        if (idx < 0) idx += n;
        return cards[idx];
    }

    // ---------- Events ----------
    [System.Serializable] public class CenterChangedEvent : UnityEvent<RectTransform, int> { }
    [System.Serializable] public class CenterWillChangeEvent : UnityEvent<RectTransform, int, int> { } // nextCard, nextIndex, dir

    [Header("UnityEvents (Inspector)")]
    public CenterWillChangeEvent OnCenterWillChange;
    public CenterChangedEvent OnCenterChanged;

    // C# events
    public event System.Action<RectTransform, int, int> CenterWillChange; // (nextCard,nextIndex,dir)
    public event System.Action<RectTransform, int> CenterChanged; // (centerCard,index)

    private bool isAnimating;

    void Start()
    {
        CurrentIndex = Mathf.Clamp(startIndex, 0, (cards?.Length ?? 1) - 1);
        LayoutStatic(CurrentIndex);

        // повідомляємо про стартовий стан один раз
        var center = CurrentCenterCard;
        OnCenterChanged?.Invoke(center, CurrentIndex);
        CenterChanged?.Invoke(center, CurrentIndex);
    }

    public void ShowNext()
    {
        if (isAnimating) return;
        Step(+1);
    }

    public void ShowPrev()
    {
        if (isAnimating) return;
        Step(-1);
    }

    // ---------------- CORE ----------------

    private void Step(int dir)
    {
        if (cards == null || cards.Length == 0) return;

        int n = cards.Length;
        int prevIndex = CurrentIndex;
        int nextIndex = (CurrentIndex + dir + n) % n;
        var nextCard = cards[nextIndex];

        // подія "перед зміною"
        OnCenterWillChange?.Invoke(nextCard, nextIndex, dir);
        CenterWillChange?.Invoke(nextCard, nextIndex, dir);

        isAnimating = true;

        var wasVisible = VisibleSet(prevIndex);
        var willBeVisible = VisibleSet(nextIndex);

        var outgoing = new HashSet<RectTransform>(wasVisible);
        outgoing.ExceptWith(willBeVisible);

        var incoming = new HashSet<RectTransform>(willBeVisible);
        incoming.ExceptWith(wasVisible);

        // --- PRE-POSITION для incoming ---
        int preOffset = (dir > 0) ? (sideCount + 1) : -(sideCount + 1);
        foreach (var rt in incoming)
        {
            if (!rt) continue;
            if (!rt.gameObject.activeSelf) rt.gameObject.SetActive(true);

            Pose pre = ComputePose(preOffset);
            rt.DOKill();
            rt.localPosition = pre.pos;
            rt.localRotation = Quaternion.Euler(pre.euler);
            rt.localScale = pre.scale;

            if (dimNeighbours)
            {
                var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = neighbourAlpha;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }

        // Анімуємо всіх, хто був або стане видимим
        var toAnimate = new HashSet<RectTransform>(wasVisible);
        toAnimate.UnionWith(willBeVisible);

        foreach (var rt in toAnimate)
        {
            if (!rt) continue;
            int i = System.Array.IndexOf(cards, rt);
            if (i < 0) continue;

            float offNext = CyclicOffset(i, nextIndex, n);
            Pose nextPose = ComputePose(offNext);

            rt.DOKill();
            rt.DOLocalMove(nextPose.pos, tweenDuration).SetEase(tweenEase);
            rt.DOScale(nextPose.scale, tweenDuration).SetEase(tweenEase);
            rt.DOLocalRotate(nextPose.euler, tweenDuration).SetEase(tweenEase);

            if (dimNeighbours)
            {
                var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
                float targetAlpha = Mathf.Abs(offNext) < 0.01f ? 1f : neighbourAlpha;
                cg.DOFade(targetAlpha, tweenDuration).SetEase(tweenEase);
                bool isCenter = Mathf.Abs(offNext) < 0.01f;
                cg.interactable = isCenter;
                cg.blocksRaycasts = isCenter;
            }
        }

        // Після твіну
        DOVirtual.DelayedCall(tweenDuration, () =>
        {
            CurrentIndex = nextIndex;

            foreach (var rt in outgoing)
            {
                if (!rt) continue;
                int i = System.Array.IndexOf(cards, rt);
                if (i < 0) continue;

                float offNow = CyclicOffset(i, CurrentIndex, n);
                if (Mathf.Abs(offNow) > sideCount + 0.001f)
                {
                    Pose p = ComputePose(offNow);
                    rt.localPosition = p.pos;
                    rt.localRotation = Quaternion.Euler(p.euler);
                    rt.localScale = p.scale;
                    rt.gameObject.SetActive(false);
                }
            }

            foreach (var rt in willBeVisible)
            {
                if (!rt) continue;
                int i = System.Array.IndexOf(cards, rt);
                if (i < 0) continue;

                float off = CyclicOffset(i, CurrentIndex, n);
                Pose p = ComputePose(off);
                rt.localPosition = p.pos;
                rt.localRotation = Quaternion.Euler(p.euler);
                rt.localScale = p.scale;

                if (dimNeighbours)
                {
                    var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = Mathf.Abs(off) < 0.01f ? 1f : neighbourAlpha;
                    bool isCenter = Mathf.Abs(off) < 0.01f;
                    cg.interactable = isCenter;
                    cg.blocksRaycasts = isCenter;
                }
            }

            isAnimating = false;

            // подія "після зміни"
            var center = CurrentCenterCard;
            OnCenterChanged?.Invoke(center, CurrentIndex);
            CenterChanged?.Invoke(center, CurrentIndex);
        });
    }

    // ---------------- Helpers ----------------

    private struct Pose
    {
        public Vector3 pos;
        public Vector3 euler;
        public Vector3 scale;
    }

    private void LayoutStatic(int index)
    {
        int n = cards?.Length ?? 0;
        for (int i = 0; i < n; i++)
        {
            var rt = cards[i];
            if (!rt) continue;

            float off = CyclicOffset(i, index, n);
            bool visible = Mathf.Abs(off) <= sideCount + 0.001f;

            Pose p = ComputePose(off);

            if (visible)
            {
                if (!rt.gameObject.activeSelf) rt.gameObject.SetActive(true);
                rt.localPosition = p.pos;
                rt.localRotation = Quaternion.Euler(p.euler);
                rt.localScale = p.scale;

                if (dimNeighbours)
                {
                    var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = Mathf.Abs(off) < 0.01f ? 1f : neighbourAlpha;
                    bool isCenter = Mathf.Abs(off) < 0.01f;
                    cg.interactable = isCenter;
                    cg.blocksRaycasts = isCenter;
                }
            }
            else
            {
                rt.localPosition = p.pos;
                rt.localRotation = Quaternion.Euler(p.euler);
                rt.localScale = p.scale;
                if (rt.gameObject.activeSelf) rt.gameObject.SetActive(false);
            }
        }
    }

    private float CyclicOffset(int i, int centerIndex, int n)
    {
        int raw = i - centerIndex;
        if (raw > n / 2) raw -= n;
        if (raw < -n / 2) raw += n;
        return raw;
    }

    private Pose ComputePose(float off)
    {
        Pose p;
        p.pos = new Vector3(off * spacing, yOffset, -Mathf.Abs(off) * depth);
        p.euler = (Mathf.Abs(off) < 0.01f)
            ? Vector3.zero
            : new Vector3(0f, Mathf.Sign(off) * maxYRotation, 0f);

        if (dimNeighbours && Mathf.Abs(off) > 0.01f && Mathf.Abs(off) <= sideCount + 0.001f)
            p.scale = Vector3.one * neighbourScale;
        else
            p.scale = Vector3.one;

        return p;
    }

    private List<RectTransform> VisibleSet(int center)
    {
        var list = new List<RectTransform>();
        int n = cards?.Length ?? 0;
        for (int i = 0; i < n; i++)
        {
            var rt = cards[i];
            if (!rt) continue;
            float off = CyclicOffset(i, center, n);
            if (Mathf.Abs(off) <= sideCount + 0.001f)
                list.Add(rt);
        }
        return list;
    }
}