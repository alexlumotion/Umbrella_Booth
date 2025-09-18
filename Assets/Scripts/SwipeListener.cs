using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Vector2 startPos;
    public float minSwipeDistance = 100f;

    public CarouselButtonClient buttonLeft;
    public CarouselButtonClient buttonRight;

    public void OnPointerDown(PointerEventData eventData)
    {
        startPos = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Vector2 endPos = eventData.position;
        float deltaX = endPos.x - startPos.x;

        if (Mathf.Abs(deltaX) > minSwipeDistance)
        {
            if (deltaX > 0)
                buttonLeft.OnClick(); // свайп вправо
            else
                buttonRight.OnClick(); // свайп вліво
        }
    }
}