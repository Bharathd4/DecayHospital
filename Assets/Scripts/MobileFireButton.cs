using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class MobileFireButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Connect your pistol shooting function here")]
    public UnityEvent onFire;

    public void OnPointerDown(PointerEventData eventData)
    {
        onFire?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Stops the touch input.
    }
}