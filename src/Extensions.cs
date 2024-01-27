using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[SuppressMessage("ReSharper", "InconsistentNaming")]
static class IEnumerableExtensions
{
    [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
    public static string ToPrettyString<T>(this IEnumerable<T> enumerable, string separator = "\n")
    {
        if(enumerable == null)
        {
            return "null";
        }

        return string.Join(separator, enumerable.Select(item => item?.ToString() ?? "null").ToArray());
    }
}

static class MVRScriptExtensions
{
    public static UIDynamic NewSpacer(
        this MVRScript script,
        float height,
        bool rightSide = false
    )
    {
        if(height <= 0)
        {
            return null;
        }

        var spacer = script.CreateSpacer(rightSide);
        spacer.height = height;
        return spacer;
    }

    public static Transform InstantiateButton(this MVRScript script, Transform parent = null) =>
        Instantiate(script.manager.configurableButtonPrefab, parent);

    static Transform Instantiate(Transform prefab, Transform parent = null)
    {
        var transform = UnityEngine.Object.Instantiate(prefab, parent, false);
        UnityEngine.Object.Destroy(transform.GetComponent<LayoutElement>());
        return transform;
    }
}

static class StringExtensions
{
    public static string Bold(this string str) => $"<b>{str}</b>";

    public static string Italic(this string str) => $"<i>{str}</i>";

    public static string Size(this string str, int size) => $"<size={size}>{str}</size>";

    public static string Color(this string str, string color) => $"<color={color}>{str}</color>";

    public static string Color(this string str, Color color) => str.Color($"#{ColorUtility.ToHtmlStringRGB(color)}");
}

static class StringBuilderExtensions
{
    public static StringBuilder AppendBold(this StringBuilder sb, string str) =>
        sb.AppendFormat("<b>{0}</b>", str);

    public static StringBuilder AppendItalic(this StringBuilder sb, string str) =>
        sb.AppendFormat("<i>{0}</i>", str);

    public static StringBuilder AppendSize(this StringBuilder sb, string str, int size) =>
        sb.AppendFormat("<size={0}>{1}</size>", size, str);

    public static StringBuilder AppendColor(this StringBuilder sb, string str, Color color) =>
        sb.AppendFormat("<color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGB(color), str);

    public static StringBuilder Clear(this StringBuilder sb)
    {
        sb.Length = 0;
        return sb;
    }
}

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
