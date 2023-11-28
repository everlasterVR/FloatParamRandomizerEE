using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

class ScriptBase : MVRScript
{
    public override bool ShouldIgnore() => true;

    protected readonly List<UIPopup> popups = new List<UIPopup>();
    UnityEventsListener _pluginUIEventsListener;
    bool _isUIBuilt;

    public override void InitUI()
    {
        base.InitUI();
        if(UITransform == null || _pluginUIEventsListener != null)
        {
            return;
        }

        _pluginUIEventsListener = UITransform.gameObject.AddComponent<UnityEventsListener>();
        if(_pluginUIEventsListener != null)
        {
            _pluginUIEventsListener.enabledHandlers += OnUIEnabled;
            _pluginUIEventsListener.disabledHandlers += OnBlur;
            _pluginUIEventsListener.clickHandlers += OnBlur;
        }
    }

    void OnUIEnabled()
    {
        SetGrayBackground();
        if(!_isUIBuilt)
        {
            BuildUI();
            _isUIBuilt = true;
        }
    }

    void SetGrayBackground()
    {
        var background = rightUIContent.parent.parent.parent.transform.GetComponent<Image>();
        background.color = new Color(0.85f, 0.85f, 0.85f);
    }

    protected virtual void BuildUI()
    {
    }

    void OnBlur() => OnBlurPopup(null);

    protected void OnBlurPopup(UIPopup openedPopup) =>
        popups.Where(popup => popup != openedPopup)
            .ToList()
            .ForEach(popup => popup.visible = false);

    protected void BaseOnDestroy()
    {
        DestroyImmediate(_pluginUIEventsListener);
        _pluginUIEventsListener = null;
    }
}
