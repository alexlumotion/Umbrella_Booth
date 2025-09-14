using UnityEngine;
using DG.Tweening;

public class WorldSpaceCarouselManager : MonoBehaviour
{
    [Header("Cards")]
    public RectTransform[] cards;

    [Header("Layout")]
    public float spacing = 1.65f;       // X зсув бічних
    public float depth = 0.84f;         // Z зсув бічних
    public float maxYRotation = 28f;    // кут Y бічних

    [Header("Tween")]
    public float tweenDuration = 0.4f;
    public Ease tweenEase = Ease.OutCubic;

    [Header("Optional Fade/Scale")]
    public bool dimNeighbours = false;
    [Range(0f,1f)] public float neighbourAlpha = 0.6f;
    public float neighbourScale = 0.9f;

    private int currentIndex = 1;

    void Start()
    {
        UpdateCards(true);
    }

    public void ShowNext()
    {
        if (currentIndex < cards.Length - 1)
        {
            currentIndex++;
            UpdateCards();
        }
    }

    public void ShowPrev()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateCards();
        }
    }

    void UpdateCards(bool instant = false)
    {
        for (int i = 0; i < cards.Length; i++)
        {
            RectTransform card = cards[i];
            float offset = (i - currentIndex);

            Vector3 pos = new Vector3(offset * spacing, 0, -Mathf.Abs(offset) * depth);
            Quaternion rot = Quaternion.Euler(0, offset * maxYRotation, 0);
            Vector3 scale = Vector3.one;
            float alpha = 1f;

            if (dimNeighbours && Mathf.Abs(offset) > 0.01f)
            {
                scale = Vector3.one * neighbourScale;
                alpha = neighbourAlpha;
            }

            // CanvasGroup для альфи
            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            if (cg == null && dimNeighbours)
                cg = card.gameObject.AddComponent<CanvasGroup>();

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

            // блокування raycast для бічних (щоб клік проходив у центр)
            if (cg != null)
            {
                cg.interactable = Mathf.Abs(offset) < 0.01f;
                cg.blocksRaycasts = Mathf.Abs(offset) < 0.01f;
            }
        }
    }
}