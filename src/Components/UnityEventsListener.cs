using System;
using UnityEngine;
using UnityEngine.EventSystems;

sealed class UnityEventsListener : MonoBehaviour, IPointerClickHandler
{
    public bool IsEnabled { get; private set; }
    public Action enabledHandlers;
    public Action disabledHandlers;
    public Action clickHandlers;

    public void OnEnable()
    {
        IsEnabled = true;
        enabledHandlers?.Invoke();
    }

    public void OnDisable()
    {
        IsEnabled = false;
        disabledHandlers?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        clickHandlers?.Invoke();
    }
}
