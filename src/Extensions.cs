using UnityEngine;

static class UIDynamicSliderExtensions
{
    public static void SetActiveStyle(this UIDynamicSlider slider, bool active, bool setInteractable = false)
    {
        if(slider == null)
        {
            return;
        }

        if(setInteractable)
        {
            slider.slider.interactable = active;
        }

        slider.labelText.color = active ? Color.black : Color.gray;
    }
}
