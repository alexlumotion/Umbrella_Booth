using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class WorldSpaceCarouselManager : MonoBehaviour
{
    [Header("Cards")]
    public RectTransform[] cards;

    [Header("Layout")]
    public float spacing = 1.65f;     // X-відстань між слотами (world units)
    public float depth = 0.84f;       // Z-відступ для нецентрових
    public float maxYRotation = 28f;  // |кут| для нецентрових

    [Header("Tween")]
    public float tweenDuration = 0.4f;
    public Ease tweenEase = Ease.OutCubic;

    [Header("Neighbours styling (optional)")]
    public bool dimNeighbours = false;
    [Range(0,1)] public float neighbourAlpha = 0.6f;
    public float neighbourScale = 0.9f;

    [Header("Visible band")]
    [Tooltip("Скільки карток видно ліворуч/праворуч від центру (1 = трійка).")]
    public int sideCount = 1;

    [Header("Start")]
    public int startIndex = 0;

    private int currentIndex;
    private bool isAnimating;

    void Start()
    {
        currentIndex = Mathf.Clamp(startIndex, 0, cards.Length - 1);
        LayoutStatic(currentIndex);
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

    private void Step(int dir) // dir = +1 (вправо) або -1 (вліво)
    {
        if (cards == null || cards.Length == 0) return;

        isAnimating = true;
        int n = cards.Length;
        int prevIndex = currentIndex;
        int nextIndex = (currentIndex + dir + n) % n;

        var wasVisible   = VisibleSet(prevIndex);
        var willBeVisible= VisibleSet(nextIndex);

        var outgoing = new HashSet<RectTransform>(wasVisible);     // були видимі → стануть невидимі
        outgoing.ExceptWith(willBeVisible);

        var incoming = new HashSet<RectTransform>(willBeVisible);  // стануть видимі → раніше були невидимі
        incoming.ExceptWith(wasVisible);

        // --- PRE-POSITION для incoming ---
        // Ставимо миттєво за межі видимого ряду: preOffset = ±(sideCount+1)
        int preOffset = (dir > 0) ? (sideCount + 1) : -(sideCount + 1);
        foreach (var rt in incoming)
        {
            if (!rt) continue;
            if (!rt.gameObject.activeSelf) rt.gameObject.SetActive(true);

            Pose pre = ComputePose(preOffset);
            rt.DOKill();
            rt.localPosition = pre.pos;                      // миттєво
            rt.localRotation = Quaternion.Euler(pre.euler);  // з правильним знаком кута
            rt.localScale    = pre.scale;

            if (dimNeighbours)
            {
                var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = neighbourAlpha;                   // як у сусідів
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }

        // Анімуємо всіх, хто був або стане видимим (щоб синхронно їхали)
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

        // Після твіну: вимкнути тих, хто став невидимим; зафіксувати позу видимих
        DOVirtual.DelayedCall(tweenDuration, () =>
        {
            currentIndex = nextIndex;

            foreach (var rt in outgoing)
            {
                if (!rt) continue;
                int i = System.Array.IndexOf(cards, rt);
                if (i < 0) continue;

                float offNow = CyclicOffset(i, currentIndex, n);
                if (Mathf.Abs(offNow) > sideCount + 0.001f)
                {
                    Pose p = ComputePose(offNow);
                    rt.localPosition = p.pos;
                    rt.localRotation = Quaternion.Euler(p.euler);
                    rt.localScale    = p.scale;
                    rt.gameObject.SetActive(false);
                }
            }

            foreach (var rt in willBeVisible)
            {
                if (!rt) continue;
                int i = System.Array.IndexOf(cards, rt);
                if (i < 0) continue;

                float off = CyclicOffset(i, currentIndex, n);
                Pose p = ComputePose(off);
                rt.localPosition = p.pos;
                rt.localRotation = Quaternion.Euler(p.euler);
                rt.localScale    = p.scale;

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
        int n = cards.Length;
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
                rt.localScale    = p.scale;
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
                rt.localScale    = p.scale;
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

    // Поза для офсету:
    // центр: rot Y = 0; ліві: -maxYRotation; праві: +maxYRotation
    private Pose ComputePose(float off)
    {
        Pose p;
        p.pos   = new Vector3(off * spacing, 0f, -Mathf.Abs(off) * depth);
        p.euler = (Mathf.Abs(off) < 0.01f) ? Vector3.zero
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
        int n = cards.Length;
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