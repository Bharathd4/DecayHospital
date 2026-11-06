
using UnityEngine;
using UnityEngine.EventSystems;

public class DPadTouch : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    private RectTransform rectTransform;
    private int activePointerId = -1;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        activePointerId = eventData.pointerId;
        UpdateDirection(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
            UpdateDirection(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        ClearDirections();
        activePointerId = -1;
    }

    private void UpdateDirection(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
            return;

        float x = localPoint.x / (rectTransform.rect.width / 2f);
        float y = localPoint.y / (rectTransform.rect.height / 2f);

        ClearDirections();

        if (Mathf.Abs(x) < 0.08f && Mathf.Abs(y) < 0.08f) return;

        if (Mathf.Abs(x) > Mathf.Abs(y))
        {
            MobileInput.SetDirection(
                x > 0 ? MobileDirection.Right : MobileDirection.Left,
                true);
        }
        else
        {
            MobileInput.SetDirection(
                y > 0 ? MobileDirection.Up : MobileDirection.Down,
                true);
        }
    }

    private void ClearDirections()
    {
        MobileInput.SetDirection(MobileDirection.Up, false);
        MobileInput.SetDirection(MobileDirection.Down, false);
        MobileInput.SetDirection(MobileDirection.Left, false);
        MobileInput.SetDirection(MobileDirection.Right, false);
    }

    private void OnDisable()
    {
        ClearDirections();
        activePointerId = -1;
    }
}
