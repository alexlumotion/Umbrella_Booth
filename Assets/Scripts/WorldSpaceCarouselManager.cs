using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class WorldSpaceCarouselManager : MonoBehaviour
{
    [Header("Cards")]
    public RectTransform[] cards;

    [Header("Layout")]
    public float spacing = 1.65f;     // зміщення по X між слотами (world units)
    public float depth = 0.84f;       // відступ за Z для нецентрових
    public float maxYRotation = 28f;  // |кут| для нецентрових

    [Header("Tween")]
    public float tweenDuration = 0.4f;
    public Ease tweenEase = Ease.OutCubic;

    [Header("Neighbours styling (optional)")]
    public bool dimNeighbours = false;
    [Range(0,1)] public float neighbourAlpha = 0.6f;
    public float neighbourScale = 0.9f;

    [Header("Visible band")]
    [Tooltip("Скільки карток видно ліворуч/праворуч від центру. Для «трійки» став 1.")]
    public int sideCount = 1;

    [Header("Start")]
    public int startIndex = 0;

    private int currentIndex;
    private bool isAnimating;

    void Start()
    {
        currentIndex = Mathf.Clamp(startIndex, 0, cards.Length - 1);
        // Початкове розміщення: центр Y=0, сусіди Y=±max
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

        // Збираємо множини видимих "до" та "після"
        var wasVisible = VisibleSet(prevIndex);
        var willBeVisible = VisibleSet(nextIndex);

        // Для плавного виїзду старої крайної: хто був видимим, але стане невидимим
        var outgoing = new HashSet<RectTransform>(wasVisible);
        outgoing.ExceptWith(willBeVisible);

        // Хто стане видимим, але був невидимим (потрібно активувати перед анімацією)
        var incoming = new HashSet<RectTransform>(willBeVisible);
        incoming.ExceptWith(wasVisible);

        // Активуємо "incoming", щоб вони анімувалися з офскріну в слот
        foreach (var rt in incoming)
            if (rt && !rt.gameObject.activeSelf) rt.gameObject.SetActive(true);

        // Твінимо ВСІ елементи, які або були видимими, або стануть видимими (щоб рух/поворот відпрацювали коректно)
        var toAnimate = new HashSet<RectTransform>(wasVisible);
        toAnimate.UnionWith(willBeVisible);

        // Готуємо цілі для всіх
        foreach (var rt in toAnimate)
        {
            if (!rt) continue;
            int i = System.Array.IndexOf(cards, rt);
            if (i < 0) continue;

            // Поточні й цільові офсети
            float offPrev = CyclicOffset(i, prevIndex, n);
            float offNext = CyclicOffset(i, nextIndex, n);

            // Старт (як є). Ціль:
            Pose nextPose = ComputePose(offNext);

            // Kill, анімації позиції/скейлу/повороту до цілі
            rt.DOKill();
            rt.DOLocalMove(nextPose.pos, tweenDuration).SetEase(tweenEase);
            rt.DOScale(nextPose.scale, tweenDuration).SetEase(tweenEase);
            rt.DOLocalRotate(nextPose.euler, tweenDuration).SetEase(tweenEase);

            // Опційно альфа/інтеракція
            CanvasGroup cg = null;
            if (dimNeighbours)
            {
                cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
                float targetAlpha = Mathf.Abs(offNext) < 0.01f ? 1f : neighbourAlpha;
                cg.DOFade(targetAlpha, tweenDuration).SetEase(tweenEase);
            }
        }

        // По завершенні: вимикаємо ті, що стали невидимими. Іншим — гарантуємо коректну «статичну» позу.
        DOVirtual.DelayedCall(tweenDuration, () =>
        {
            // Оновлюємо індекс
            currentIndex = nextIndex;

            // Вимкнути аутгоїнг після доїзду
            foreach (var rt in outgoing)
            {
                if (!rt) continue;
                // остаточно ставимо їх у «позакадрову» статичну позу і вимикаємо
                int i = System.Array.IndexOf(cards, rt);
                if (i >= 0)
                {
                    float offNow = CyclicOffset(i, currentIndex, n);
                    if (Mathf.Abs(offNow) > sideCount + 0.001f)
                    {
                        Pose p = ComputePose(offNow);
                        rt.localPosition = p.pos;
                        rt.localRotation = Quaternion.Euler(p.euler);
                        rt.localScale = p.scale;
                        rt.gameObject.SetActive(false);
                    }
                }
            }

            // Для всіх, хто видимий зараз — зафіксувати «статичну» позу (Y=0 для центру, ±max для сусідів)
            foreach (var rt in willBeVisible)
            {
                if (!rt) continue;
                int i = System.Array.IndexOf(cards, rt);
                if (i < 0) continue;

                float off = CyclicOffset(i, currentIndex, n);
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
        });
    }

    // ---------------- Helpers ----------------

    private struct Pose
    {
        public Vector3 pos;
        public Vector3 euler;
        public Vector3 scale;
    }

    // Статична розкладка для старту (без твінів)
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

    // Обчислює офсет у кільці (…,-2,-1,0,+1,+2,…)
    private float CyclicOffset(int i, int centerIndex, int n)
    {
        int raw = i - centerIndex;
        if (raw > n / 2) raw -= n;
        if (raw < -n / 2) raw += n;
        return raw;
    }

    // Цільова поза для заданого офсету:
    // - позиція: X = off*spacing; Z = -|off|*depth
    // - поворот: центр 0; лівий сусід -maxYRotation; правий сусід +maxYRotation.
    //   (для |off|>1 значення не критичні; ставимо той самий знак ±max, аби не було стрибків)
    private Pose ComputePose(float off)
    {
        Pose p;
        p.pos = new Vector3(off * spacing, 0f, -Mathf.Abs(off) * depth);

        if (Mathf.Abs(off) < 0.01f)
            p.euler = Vector3.zero; // центр = 0
        else
            p.euler = new Vector3(0f, Mathf.Sign(off) * maxYRotation, 0f); // ліва = −max, права = +max

        if (dimNeighbours && Mathf.Abs(off) > 0.01f && Mathf.Abs(off) <= sideCount + 0.001f)
            p.scale = Vector3.one * neighbourScale;
        else
            p.scale = Vector3.one;

        return p;
    }

    // Повертає список видимих при даному центрі
    private List<RectTransform> VisibleSet(int center)
    {
        var list = new List<RectTransform>();
        int n = cards.Length;
        for (int i = 0; i < n; i++)
        {
            var rt = cards[i];
            if (!rt) continue;
            float off = CyclicOffset(i, center, n);
            if (Mathf.Abs(off) <= sideCount + 0.001f) list.Add(rt);
        }
        return list;
    }
}