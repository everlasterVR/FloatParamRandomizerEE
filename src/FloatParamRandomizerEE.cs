using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ColliderEditor;
using UnityEngine.UI;
using static Utils;
using static CurveFunctions;

/// <summary>
/// Extended edition of FloatParamRandomizer by MeshedVR.
/// Randomizes float values with a smooth transition from current value towards target value.
/// Source: https://github.com/everlasterVR/FloatParamRandomizerEE
/// </summary>
public class FloatParamRandomizerEE : MVRScript
{
    public const string VERSION = "v0.0.0";

    private List<UIPopup> _popups;
    private JSONStorableStringChooser _atomJsc;
    private JSONStorableStringChooser _receiverJsc;
    private JSONStorableStringChooser _receiverTargetJsc;
    private JSONStorableStringChooser _functionJsc;
    private JSONStorableFloat _curvatureJsf;
    private JSONStorableFloat _periodJsf;
    private JSONStorableFloat _quicknessJsf;
    private JSONStorableFloat _lowerValueJsf;
    private JSONStorableFloat _upperValueJsf;
    private JSONStorableBool _enableRandomness;
    private JSONStorableFloat _targetValueJsf;
    private JSONStorableFloat _currentValueJsf;
    private JSONStorableFloat _receiverTargetJsf;
    private JSONStorable _receiverStorable;

    private UIDynamicSlider _targetValueSlider;

    private string _receiverTargetName;
    private Atom _receivingAtom;

    private Dictionary<string, Func<float, float>> _functionOptions;
    private Func<float, float> _function;
    private float _exponent;
    private const float MIDPOINT = 0.5f;

    public override void Init()
    {
        try
        {
            var titleJss = new JSONStorableString("title", $"{"\n".Size(18)}{nameof(FloatParamRandomizerEE)}".Bold());
            var titleTextField = CreateTitleTextField(titleJss, 72, false);
            titleTextField.UItext.fontSize = 36;

            _popups = new List<UIPopup>();
            CreateAtomChooser();
            CreateReceiverChooser();
            CreateReceiverTargetChooser();

            // set atom to current atom to initialize
            _atomJsc.val = containingAtom.uid;

            this.NewSpacer(72, true);
            _periodJsf = new JSONStorableFloat("period", 1f, 0f, 10f, false);
            RegisterFloat(_periodJsf);
            CreateSlider(_periodJsf, true);

            _quicknessJsf = new JSONStorableFloat("quickness", 1f, 0f, 10f);
            RegisterFloat(_quicknessJsf);
            CreateSlider(_quicknessJsf, true);

            _lowerValueJsf = new JSONStorableFloat("lowerValue", 0f, 0f, 1f, false);
            RegisterFloat(_lowerValueJsf);
            CreateSlider(_lowerValueJsf, true);

            _upperValueJsf = new JSONStorableFloat("upperValue", 0f, 0f, 1f, false);
            RegisterFloat(_upperValueJsf);
            CreateSlider(_upperValueJsf, true);

            this.NewSpacer(210);
            CreateFunctionChooser();

            _curvatureJsf = new JSONStorableFloat("curvature", 0.25f, 0.0f, 1.0f);
            RegisterFloat(_curvatureJsf);
            CreateSlider(_curvatureJsf);

            _functionJsc.val = _functionOptions.Keys.First();

            this.NewSpacer(10, true);

            _enableRandomness = new JSONStorableBool("enableRandomness", true, SyncEnableRandomness);
            var enableRandomnessToggle = CreateToggle(_enableRandomness, true);

            _targetValueJsf = new JSONStorableFloat("targetValue", 0f, 0f, 1f, false, false);
            _targetValueSlider = CreateSlider(_targetValueJsf, true);
            _targetValueSlider.slider.interactable = false;
            _targetValueSlider.defaultButtonEnabled = false;
            _targetValueSlider.quickButtonsEnabled = false;

            _currentValueJsf = new JSONStorableFloat("currentValue", 0f, 0f, 1f, false, false);
            var currentValueSlider = CreateSlider(_currentValueJsf, true);
            currentValueSlider.defaultButtonEnabled = false;
            currentValueSlider.quickButtonsEnabled = false;

            SyncEnableRandomness(_enableRandomness.val);
        }
        catch(Exception e)
        {
            LogError($"{e}");
        }
    }

    private UIDynamicTextField CreateTitleTextField(JSONStorableString jss, int height, bool rightSide)
    {
        var textField = CreateTextField(jss, rightSide);
        textField.UItext.alignment = TextAnchor.MiddleCenter;
        textField.backgroundColor = Color.clear;

        var layout = textField.GetComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.minHeight = height;

        return textField;
    }

    private void CreateAtomChooser()
    {
        _atomJsc = new JSONStorableStringChooser("atom", SuperController.singleton.GetAtomUIDs(), null, "Atom", SyncAtom);
        _atomJsc.representsAtomUid = true;
        RegisterStringChooser(_atomJsc);
        SyncAtomChoices();
        var uiDynamicPopup = NewPopup(_atomJsc, 1000);
        uiDynamicPopup.popup.onOpenPopupHandlers += SyncAtomChoices;
    }

    private void CreateReceiverChooser()
    {
        _receiverJsc = new JSONStorableStringChooser("receiver", null, null, "Receiver", SyncReceiver);
        RegisterStringChooser(_receiverJsc);
        NewPopup(_receiverJsc, 860);
    }

    private void CreateReceiverTargetChooser()
    {
        _receiverTargetJsc = new JSONStorableStringChooser("receiverTarget", null, null, "Target", SyncReceiverTarget);
        RegisterStringChooser(_receiverTargetJsc);
        NewPopup(_receiverTargetJsc, 720);
    }

    private void CreateFunctionChooser()
    {
        // any function can be added here as long as it takes an x in range [0, 1] and outputs an y in range [0, 1]
        _functionOptions = new Dictionary<string, Func<float, float>>()
        {
            { "Ease In-Out", value => ParametricSmoother(value, _exponent, MIDPOINT) },
            { "Bounce In-Out", value => ParametricSmoother(value, _exponent, MIDPOINT) },
        };
        _functionJsc = new JSONStorableStringChooser("function", _functionOptions.Keys.ToList(), null, "Function", SyncFunction);
        RegisterStringChooser(_functionJsc);
        NewPopup(_functionJsc, 160); //425
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

    private void SyncEnableRandomness(bool value)
    {
        _targetValueJsf.val = value ? _targetValueJsf.val : _targetValueJsf.min;
        _targetValueSlider.SetActiveStyle(value);
    }

    private void SyncFunction(string option)
    {
        _function = _functionOptions[option];
        switch(option)
        {
            case "Ease In-Out":
                _curvatureJsf.setCallbackFunction = value => _exponent = EaseInOutExponent(value, MIDPOINT);
                _exponent = EaseInOutExponent(_curvatureJsf.val, MIDPOINT);
                break;
            case "Bounce In-Out":
                _curvatureJsf.setCallbackFunction = value => _exponent = BounceInOutExponent(value, MIDPOINT);
                _exponent = BounceInOutExponent(_curvatureJsf.val, MIDPOINT);
                break;
        }
    }

    private bool _flip;
    private float _accumulated;
    private float _start;
    private float _end;

    protected void Update()
    {
        try
        {
            if(_accumulated > _periodJsf.val)
            {
                _accumulated = 0f;
                if(_enableRandomness.val)
                {
                    _start = _currentValueJsf.val;
                    _end = _targetValueJsf.val = UnityEngine.Random.Range(_lowerValueJsf.val, _upperValueJsf.val);
                }
                else
                {
                    _start = _flip ? _lowerValueJsf.val : _upperValueJsf.val;
                    _end = _flip ? _upperValueJsf.val : _lowerValueJsf.val;
                    _flip = !_flip;
                }
            }

            _accumulated += Time.deltaTime;

            float value = _accumulated * _quicknessJsf.val / _periodJsf.val;
            _currentValueJsf.val = Mathf.Lerp(_start, _end, _function(value));

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
