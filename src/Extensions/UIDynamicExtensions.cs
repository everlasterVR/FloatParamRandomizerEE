using System;
using UnityEngine;
using UnityEngine.Events;

static class UIDynamicExtensions
{
    public static void AddListener(this UIDynamic uiDynamic, UnityAction callback)
    {
        if(!uiDynamic)
        {
            return;
        }

        var uiDynamicButton = uiDynamic as UIDynamicButton;
        if(!uiDynamicButton)
        {
            SuperController.LogError($"UIDynamic {uiDynamic.name} was null or not an UIDynamicButton");
            return;
        }

        uiDynamicButton.button.onClick.AddListener(callback);
    }

    public static void SetActiveStyle(this UIDynamic element, bool active, bool setInteractable = false)
    {
        if(element == null)
        {
            return;
        }

        var color = active ? Color.black : new Color(0.4f, 0.4f, 0.4f);
        var uiDynamicSlider = element as UIDynamicSlider;
        if(uiDynamicSlider != null)
        {
            if(setInteractable)
            {
                uiDynamicSlider.slider.interactable = active;
            }

            uiDynamicSlider.labelText.color = color;
            return;
        }

        var uiDynamicToggle = element as UIDynamicToggle;
        if(uiDynamicToggle != null)
        {
            if(setInteractable)
            {
                uiDynamicToggle.toggle.interactable = active;
            }

            uiDynamicToggle.labelText.color = color;
            return;
        }

        var uiDynamicButton = element as UIDynamicButton;
        if(uiDynamicButton != null)
        {
            if(setInteractable)
            {
                uiDynamicButton.button.interactable = active;
            }

            var colors = uiDynamicButton.button.colors;
            colors.disabledColor = colors.normalColor;
            uiDynamicButton.button.colors = colors;
            uiDynamicButton.textColor = color;
            return;
        }

        throw new ArgumentException($"UIDynamic {element.name} was null, or not an expected type");
    }
}
