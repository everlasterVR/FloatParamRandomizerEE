using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ColliderEditor;
using static Utils;

/// <summary>
/// Extended edition of FloatParamRandomizer by MeshedVR.
/// Randomizes float values with a smooth transition from current value towards target value.
/// Source: https://github.com/everlasterVR/FloatParamRandomizerEE
/// </summary>
public class FloatParamRandomizerEE : MVRScript
{
    private List<UIPopup> _popups;
    private JSONStorableStringChooser _atomJsc;
    private JSONStorableStringChooser _receiverJsc;
    private JSONStorableStringChooser _receiverTargetJsc;
    private JSONStorableFloat _periodJsf;
    private JSONStorableFloat _quicknessJsf;
    private JSONStorableFloat _lowerValueJsf;
    private JSONStorableFloat _upperValueJsf;
    private JSONStorableFloat _targetValueJsf;
    private JSONStorableFloat _currentValueJsf;
    private JSONStorableFloat _receiverTargetJsf;
    private JSONStorable _receiverStorable;

    private string _receiverTargetName;
    private Atom _receivingAtom;

    public const string VERSION = "v0.0.0";

    public override void Init()
    {
        try
        {
            _popups = new List<UIPopup>();
            CreateAtomChooser();
            CreateReceiverChooser();
            CreateReceiverTargetChooser();

            // set atom to current atom to initialize
            _atomJsc.val = containingAtom.uid;

            // create random value generation period
            _periodJsf = new JSONStorableFloat("period", 1f, 0f, 10f, false);
            RegisterFloat(_periodJsf);
            CreateSlider(_periodJsf, true);

            // quickness (smoothness)
            _quicknessJsf = new JSONStorableFloat("quickness", 1f, 0f, 10f);
            RegisterFloat(_quicknessJsf);
            CreateSlider(_quicknessJsf, true);

            // lower val
            _lowerValueJsf = new JSONStorableFloat("lowerValue", 0f, 0f, 1f, false);
            RegisterFloat(_lowerValueJsf);
            CreateSlider(_lowerValueJsf, true);

            // upper val
            _upperValueJsf = new JSONStorableFloat("upperValue", 0f, 0f, 1f, false);
            RegisterFloat(_upperValueJsf);
            CreateSlider(_upperValueJsf, true);

            // target val
            _targetValueJsf = new JSONStorableFloat("targetValue", 0f, 0f, 1f, false, false);
            // don't register - this is for viewing only and is generated
            var ds = CreateSlider(_targetValueJsf, true);
            ds.defaultButtonEnabled = false;
            ds.quickButtonsEnabled = false;

            // current val
            _currentValueJsf = new JSONStorableFloat("currentValue", 0f, 0f, 1f, false, false);
            // don't register - this is for viewing only and is generated
            ds = CreateSlider(_currentValueJsf, true);
            ds.defaultButtonEnabled = false;
            ds.quickButtonsEnabled = false;

        }
        catch(Exception e)
        {
            LogError($"{e}");
        }
    }

    private void CreateAtomChooser()
    {
        _atomJsc = new JSONStorableStringChooser("atom", SuperController.singleton.GetAtomUIDs(), null, "Atom", SyncAtom);
        _atomJsc.representsAtomUid = true;
        RegisterStringChooser(_atomJsc);
        SyncAtomChoices();
        var uiDynamicPopup = NewPopup(_atomJsc, 1100);
        uiDynamicPopup.popup.onOpenPopupHandlers += SyncAtomChoices;
    }

    private void CreateReceiverChooser()
    {
        _receiverJsc = new JSONStorableStringChooser("receiver", null, null, "Receiver", SyncReceiver);
        RegisterStringChooser(_receiverJsc);
        NewPopup(_receiverJsc, 960);
    }

    private void CreateReceiverTargetChooser()
    {
        _receiverTargetJsc = new JSONStorableStringChooser("receiverTarget", null, null, "Target", SyncReceiverTarget);
        RegisterStringChooser(_receiverTargetJsc);
        NewPopup(_receiverTargetJsc, 820);
    }

    private UIDynamicPopup NewPopup(JSONStorableStringChooser jsc, int panelHeight)
    {
        var uiDynamicPopup = this.CreatePopupAuto(jsc);
        uiDynamicPopup.popupPanelHeight = panelHeight;
        uiDynamicPopup.popup.onOpenPopupHandlers += () => OnBlurPopup(uiDynamicPopup.popup);
        _popups.Add(uiDynamicPopup.popup);
        return uiDynamicPopup;
    }

    private UIListener _uiListener;

    public override void InitUI()
    {
        base.InitUI();
        if(UITransform == null || _uiListener != null)
        {
            return;
        }

        _uiListener = UITransform.gameObject.AddComponent<UIListener>();
        if(_uiListener != null)
        {
            _uiListener.onDisabled.AddListener(OnBlur);
            _uiListener.onClick.AddListener(OnBlur);
        }
    }

    private void OnBlur()
    {
        OnBlurPopup(null);
    }

    private void OnBlurPopup(UIPopup openedPopup)
    {
        _popups.Where(popup => popup != openedPopup)
            .ToList()
            .ForEach(popup => popup.visible = false);
    }

    private void SyncAtomChoices()
    {
        var atomChoices = new List<string>();
        atomChoices.Add("None");
        foreach(string atomUID in SuperController.singleton.GetAtomUIDs())
        {
            atomChoices.Add(atomUID);
        }

        _atomJsc.choices = atomChoices;
    }

    private void SyncAtom(string atomUID)
    {
        var receiverChoices = new List<string>();
        receiverChoices.Add("None");
        if(atomUID != null)
        {
            _receivingAtom = SuperController.singleton.GetAtomByUid(atomUID);
            if(_receivingAtom != null)
            {
                foreach(string receiverChoice in _receivingAtom.GetStorableIDs())
                {
                    receiverChoices.Add(receiverChoice);
                    //SuperController.LogMessage("Found receiver " + receiverChoice);
                }
            }
        }
        else
        {
            _receivingAtom = null;
        }

        _receiverJsc.choices = receiverChoices;
        _receiverJsc.val = "None";
    }

    private string _missingReceiverStoreId = "";

    private void CheckMissingReceiver()
    {
        if(_missingReceiverStoreId != "" && _receivingAtom != null)
        {
            var missingReceiver = _receivingAtom.GetStorableByID(_missingReceiverStoreId);
            if(missingReceiver != null)
            {
                string saveTargetName = _receiverTargetName;
                SyncReceiver(_missingReceiverStoreId);
                _missingReceiverStoreId = "";
                insideRestore = true;
                _receiverTargetJsc.val = saveTargetName;
                insideRestore = false;
            }
        }
    }

    private void SyncReceiver(string receiverID)
    {
        var receiverTargetChoices = new List<string>();
        receiverTargetChoices.Add("None");
        if(_receivingAtom != null && receiverID != null)
        {
            _receiverStorable = _receivingAtom.GetStorableByID(receiverID);
            if(_receiverStorable != null)
            {
                foreach(string floatParam in _receiverStorable.GetFloatParamNames())
                {
                    receiverTargetChoices.Add(floatParam);
                }
            }
            else if(receiverID != "None")
            {
                // some storables can be late loaded, like skin, clothing, hair, etc so must keep track of missing receiver
                _missingReceiverStoreId = receiverID;
            }
        }
        else
        {
            _receiverStorable = null;
        }

        _receiverTargetJsc.choices = receiverTargetChoices;
        _receiverTargetJsc.val = "None";
    }

    private void SyncReceiverTarget(string receiverTargetName)
    {
        _receiverTargetName = receiverTargetName;
        _receiverTargetJsf = null;
        if(_receiverStorable != null && receiverTargetName != null)
        {
            _receiverTargetJsf = _receiverStorable.GetFloatJSONParam(receiverTargetName);
            if(_receiverTargetJsf != null)
            {
                _lowerValueJsf.min = _receiverTargetJsf.min;
                _lowerValueJsf.max = _receiverTargetJsf.max;
                _upperValueJsf.min = _receiverTargetJsf.min;
                _upperValueJsf.max = _receiverTargetJsf.max;
                _currentValueJsf.min = _receiverTargetJsf.min;
                _currentValueJsf.max = _receiverTargetJsf.max;
                _targetValueJsf.min = _receiverTargetJsf.min;
                _targetValueJsf.max = _receiverTargetJsf.max;
                if(!insideRestore)
                {
                    _lowerValueJsf.val = _receiverTargetJsf.val;
                    _upperValueJsf.val = _receiverTargetJsf.val;
                    _currentValueJsf.val = _receiverTargetJsf.val;
                    _targetValueJsf.val = _receiverTargetJsf.val;
                }
            }
        }
    }

    private float _accumulated;
    private float _start;

    protected void Update()
    {
        try
        {
            if(_accumulated > _periodJsf.val)
            {
                _accumulated = 0f;
                _start = _currentValueJsf.val;
                _targetValueJsf.val = UnityEngine.Random.Range(_lowerValueJsf.val, _upperValueJsf.val);
            }

            _accumulated += Time.deltaTime;
            _currentValueJsf.val = Mathf.SmoothStep(_start, _targetValueJsf.val, _accumulated * _quicknessJsf.val / _periodJsf.val);

            CheckMissingReceiver();
            if(_receiverTargetJsf != null)
            {
                _receiverTargetJsf.val = _currentValueJsf.val;
            }
        }
        catch(Exception e)
        {
            LogError($"{e}");
        }
    }

    protected void OnDestroy()
    {
        try
        {
            if(_uiListener != null)
            {
                DestroyImmediate(_uiListener);
            }
        }
        catch(Exception e)
        {
            LogError($"{e}");
        }
    }
}