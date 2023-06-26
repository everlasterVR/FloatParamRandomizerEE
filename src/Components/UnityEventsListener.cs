using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnityEventsListener : MonoBehaviour, IPointerClickHandler
{
    public Action DisableHandlers { get; set; }
    public Action EnableHandlers { get; set; }
    public Action ClickHandlers { get; set; }

    public void OnDisable()
    {
        DisableHandlers?.Invoke();
    }

    public void OnEnable()
    {
        EnableHandlers?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ClickHandlers?.Invoke();
    }
}
