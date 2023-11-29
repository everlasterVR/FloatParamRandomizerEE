using System.Linq;

static class UIPopupExtensions
{
    const int MAX_VISIBLE_COUNT = 400;

    public static void SetPreviousOrLastValue(this UIPopup uiPopup)
    {
        if(uiPopup.currentValue == uiPopup.popupValues[0])
        {
            uiPopup.currentValue = uiPopup.LastVisibleValue();
        }
        else
        {
            uiPopup.SetPreviousValue();
        }
    }

    public static void SetNextOrFirstValue(this UIPopup uiPopup)
    {
        if(uiPopup.currentValue == uiPopup.LastVisibleValue())
        {
            uiPopup.currentValue = uiPopup.popupValues[0];
        }
        else
        {
            uiPopup.SetNextValue();
        }
    }

    static string LastVisibleValue(this UIPopup uiPopup) => uiPopup.popupValues.Length > MAX_VISIBLE_COUNT
        ? uiPopup.popupValues[MAX_VISIBLE_COUNT - 1]
        : uiPopup.popupValues.Last();
}
