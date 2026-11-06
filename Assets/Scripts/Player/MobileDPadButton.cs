using UnityEngine;
using UnityEngine.EventSystems;

public class MobileDPadButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler
{
    public MobileDirection direction;

    public void OnPointerDown(PointerEventData eventData)
    {
        MobileInput.SetDirection(direction, true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        MobileInput.SetDirection(direction, false);
    }

    private void OnDisable()
    {
        MobileInput.SetDirection(direction, false);
    }
}