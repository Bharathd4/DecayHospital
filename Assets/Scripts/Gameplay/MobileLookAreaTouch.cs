
using UnityEngine;
using UnityEngine.EventSystems;

public class MobileLookAreaTouch : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private static Vector2 lookDelta;
    private int activePointerId = -1;

    public void OnPointerDown(PointerEventData eventData)
    {
        activePointerId = eventData.pointerId;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        lookDelta += eventData.delta;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
        {
            activePointerId = -1;
        }
    }

    public static Vector2 ConsumeLookDelta()
    {
        Vector2 result = lookDelta;
        lookDelta = Vector2.zero;
        return result;
    }

    private void OnDisable()
    {
        activePointerId = -1;
        lookDelta = Vector2.zero;
    }
}
