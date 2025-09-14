using UnityEngine;
using DG.Tweening;

public class WorldSpaceCarouselManager : MonoBehaviour
{
    [Header("Cards")]
    public RectTransform[] cards;

    [Header("Layout")]
    public float spacing = 1.65f;
    public float depth = 0.84f;
    public float maxYRotation = 28f;

    [Header("Tween")]
    public float tweenDuration = 0.4f;
    public Ease tweenEase = Ease.OutCubic;

    [Header("Optional Fade/Scale")]
    public bool dimNeighbours = false;
    [Range(0f,1f)] public float neighbourAlpha = 0.6f;
    public float neighbourScale = 0.9f;

    [Header("Visible Range")]
    [Tooltip("Скільки карток показувати ліворуч і праворуч від центру")]
    public int sideCount = 1; // наприклад 1 = показуємо центр + по 1 ліворуч/праворуч

    [Header("Start index")]
    public int startIndex = 0;

    private int currentIndex;

    void Start()
    {
        currentIndex = Mathf.Clamp(startIndex, 0, cards.Length - 1);
        UpdateCards(true);
    }

    public void ShowNext()
    {
        currentIndex = (currentIndex + 1) % cards.Length;
        UpdateCards();
    }

    public void ShowPrev()
    {
        currentIndex = (currentIndex - 1 + cards.Length) % cards.Length;
        UpdateCards();
    }

    void UpdateCards(bool instant = false)
    {
        int n = cards.Length;

        for (int i = 0; i < n; i++)
        {
            RectTransform card = cards[i];

            // обчислюємо циклічний offset
            int rawOffset = i - currentIndex;
            if (rawOffset > n / 2) rawOffset -= n;
            if (rawOffset < -n / 2) rawOffset += n;
            float offset = rawOffset;

            // позиція/кут/масштаб
            Vector3 pos = new Vector3(offset * spacing, 0, -Mathf.Abs(offset) * depth);
            Quaternion rot = Quaternion.Euler(0, offset * maxYRotation, 0);
            Vector3 scale = Vector3.one;
            float alpha = 1f;

            if (dimNeighbours && Mathf.Abs(offset) > 0.01f)
            {
                scale = Vector3.one * neighbourScale;
                alpha = neighbourAlpha;
            }

            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            if (cg == null && dimNeighbours)
                cg = card.gameObject.AddComponent<CanvasGroup>();

            // якщо картка поза видимим діапазоном — приховуємо й snap'имо
            if (Mathf.Abs(offset) > sideCount + 0.01f)
            {
                // миттєво ставимо в нове місце та ховаємо
                card.localPosition = pos;
                card.localRotation = rot;
                card.localScale = scale;
                card.gameObject.SetActive(false);
                if (cg != null) cg.alpha = alpha;
                continue;
            }
            else
            {
                // показуємо, якщо вона була прихована
                if (!card.gameObject.activeSelf)
                    card.gameObject.SetActive(true);
            }

            // tween або миттєво
            if (instant)
            {
                card.localPosition = pos;
                card.localRotation = rot;
                card.localScale = scale;
                if (cg != null) cg.alpha = alpha;
            }
            else
            {
                card.DOLocalMove(pos, tweenDuration).SetEase(tweenEase);
                card.DOLocalRotateQuaternion(rot, tweenDuration).SetEase(tweenEase);
                card.DOScale(scale, tweenDuration).SetEase(tweenEase);
                if (cg != null) cg.DOFade(alpha, tweenDuration).SetEase(tweenEase);
            }

            if (cg != null)
            {
                cg.interactable = Mathf.Abs(offset) < 0.01f;
                cg.blocksRaycasts = Mathf.Abs(offset) < 0.01f;
            }
        }
    }
}