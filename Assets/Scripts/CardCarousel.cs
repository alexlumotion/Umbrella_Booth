using UnityEngine;
using DG.Tweening;

public class CardCarousel : MonoBehaviour
{
    public RectTransform[] cards;
    public float spacing = 500f;
    public float depth = 200f;
    public float maxYRotation = 30f;
    public float tweenDuration = 0.4f;

    int currentIndex = 0;

    public void ShowNext() { currentIndex = Mathf.Min(currentIndex+1, cards.Length-1); UpdateCards(); }
    public void ShowPrev() { currentIndex = Mathf.Max(currentIndex-1, 0); UpdateCards(); }

    void Start() { UpdateCards(true); }

    void UpdateCards(bool instant=false)
    {
        for (int i = 0; i < cards.Length; i++)
        {
            float offset = (i - currentIndex);
            Vector3 pos = new Vector3(offset*spacing, 0, -Mathf.Abs(offset)*depth);
            Quaternion rot = Quaternion.Euler(0, offset*maxYRotation, 0);
            Vector3 scale = Vector3.one * Mathf.Lerp(1f, 0.8f, Mathf.Abs(offset));

            if (instant)
            {
                cards[i].localPosition = pos;
                cards[i].localRotation = rot;
                cards[i].localScale = scale;
            }
            else
            {
                cards[i].DOLocalMove(pos, tweenDuration).SetEase(Ease.OutCubic);
                cards[i].DOLocalRotateQuaternion(rot, tweenDuration).SetEase(Ease.OutCubic);
                cards[i].DOScale(scale, tweenDuration).SetEase(Ease.OutCubic);
            }
        }
    }
}
