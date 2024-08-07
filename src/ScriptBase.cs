using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

class ScriptBase : MVRScript
{
    public override bool ShouldIgnore() => true;

    protected bool isInitialized;
    protected bool isRestoringFromJSON;
    protected readonly List<UIPopup> popups = new List<UIPopup>();
    UnityEventsListener _pluginUIEventsListener;
    UIDynamicTextField _postponedInfoField;
    bool _isUIBuilt;
    Action _postponedActions;

    void Start()
    {
        _postponedActions?.Invoke();
        _postponedActions = null;
    }

    protected void StartOrPostponeCoroutine(IEnumerator coroutine, Action onPostpone = null)
    {
        if(gameObject.activeInHierarchy)
        {
            StartCoroutine(coroutine);
        }
        else
        {
            onPostpone?.Invoke();
            _postponedActions += () => StartCoroutine(coroutine);
        }
    }

    public override void InitUI()
    {
        if(ShouldIgnore())
        {
            return;
        }

        base.InitUI();
        if(UITransform == null)
        {
            return;
        }

        SetGrayBackground();
        if(!_pluginUIEventsListener)
        {
            _pluginUIEventsListener = UITransform.gameObject.AddComponent<UnityEventsListener>();
            _pluginUIEventsListener.enabledHandlers += OnUIEnabled;
            _pluginUIEventsListener.disabledHandlers += OnBlur;
            _pluginUIEventsListener.clickHandlers += OnBlur;
        }
    }

    // TODO test postpone after initialized
    void OnUIEnabled() => StartOrPostponeCoroutine(OnUIEnabledCo(), () =>
    {
        _postponedInfoField = CreateTextField(new JSONStorableString("info", "Enable the atom to initialize.".Bold()));
        _postponedInfoField.backgroundColor = Color.clear;
    });

    IEnumerator OnUIEnabledCo()
    {
        if(_postponedInfoField != null)
        {
            RemoveTextField(_postponedInfoField);
        }

        while(!isInitialized)
        {
            yield return null;
        }

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

    protected void OnBlurPopup(UIPopup openedPopup)
    {
        for(int i = 0; i < popups.Count; i++)
        {
            var popup = popups[i];
            if(popup != openedPopup)
            {
                popup.visible = false;
            }
        }
    }

    protected void BaseOnDestroy()
    {
        DestroyImmediate(_pluginUIEventsListener);
        _pluginUIEventsListener = null;
    }
}
