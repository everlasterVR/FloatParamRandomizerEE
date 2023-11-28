using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ColliderEditor;
using SimpleJSON;
using static CurveFunctions;

/// <summary>
/// Extended edition of FloatParamRandomizer by MeshedVR.
/// Randomizes float values with a smooth transition from current value towards target value.
/// Source: https://github.com/everlasterVR/FloatParamRandomizerEE
/// </summary>
sealed class FloatParamRandomizerEE : ScriptBase
{
    public const string VERSION = "0.0.0";
    public override bool ShouldIgnore() => false;

    JSONStorableString _titleJss;
    JSONStorableString _versionJss;
    JSONStorableStringChooser _atomJssc;
    JSONStorableStringChooser _receiverJssc;
    JSONStorableStringChooser _receiverTargetJssc;
    JSONStorableStringChooser _functionJssc;
    JSONStorableFloat _curvatureJsf;
    JSONStorableFloat _periodJsf;
    JSONStorableFloat _quicknessJsf;
    JSONStorableFloat _lowerValueJsf;
    JSONStorableFloat _upperValueJsf;
    JSONStorableBool _enableRandomnessJsb;
    JSONStorableFloat _targetValueJsf;
    JSONStorableFloat _currentValueJsf;
    JSONStorableFloat _receiverTargetJsf;

    UIDynamicSlider _targetValueSlider;

    string _receiverTargetName;
    Atom _receivingAtom;
    JSONStorable _receiverStorable;

    Dictionary<string, Func<float, float>> _functionOptions;
    Func<float, float> _function;
    float _exponent;
    const float MIDPOINT = 0.5f;
    bool _pause = true;

    public override void Init()
    {
        try
        {
            SetupStorables();
            SyncAtomChoices();
            SyncFunction(_functionJssc.val);
            SyncEnableRandomness(_enableRandomnessJsb.val);
            _atomJssc.val = containingAtom.uid;
            SuperController.singleton.onAtomUIDRenameHandlers += OnAtomRenamed;
            _pause = false;
        }
        catch(Exception e)
        {
            Utils.LogError($"{e}");
        }
    }

    void SetupStorables()
    {
        _titleJss = new JSONStorableString("title", $"{"\n".Size(18)}{nameof(FloatParamRandomizerEE)}".Bold());
        _versionJss = new JSONStorableString("version", VERSION)
        {
            storeType = JSONStorableParam.StoreType.Full,
        };
        _atomJssc = new JSONStorableStringChooser("atom", new List<string>(), null, "Atom", SyncAtom)
        {
            representsAtomUid = true,
        };
        _receiverJssc = new JSONStorableStringChooser("receiver", null, null, "Receiver", SyncReceiver);
        _receiverTargetJssc = new JSONStorableStringChooser("receiverTarget", null, null, "Target", SyncReceiverTarget);
        _periodJsf = new JSONStorableFloat("period", 1f, 0f, 10f, false);
        _quicknessJsf = new JSONStorableFloat("quickness", 1f, 0f, 10f);
        _lowerValueJsf = new JSONStorableFloat("lowerValue", 0f, 0f, 1f, false)
        {
            setCallbackFunction = value =>
            {
                if(value > _upperValueJsf.val)
                {
                    _upperValueJsf.val = value;
                }
            },
        };
        _upperValueJsf = new JSONStorableFloat("upperValue", 0f, 0f, 1f, false)
        {
            setCallbackFunction = value =>
            {
                if(value < _lowerValueJsf.val)
                {
                    _lowerValueJsf.val = value;
                }
            },
        };
        _curvatureJsf = new JSONStorableFloat("curvature", 0.25f, 0.0f, 1.0f);

        // any function can be added here as long as it takes an x in range [0, 1] and outputs an y in range [0, 1]
        _functionOptions = new Dictionary<string, Func<float, float>>
        {
            { "Ease In-Out", value => ParametricSmoother(value, _exponent, MIDPOINT) },
            { "Bounce In-Out", value => ParametricSmoother(value, _exponent, MIDPOINT) },
        };
        var options = _functionOptions.Keys.ToList();
        _functionJssc = new JSONStorableStringChooser("function", options, options[0], "Function", SyncFunction);
        _enableRandomnessJsb = new JSONStorableBool("enableRandomness", true, SyncEnableRandomness);
        _targetValueJsf = new JSONStorableFloat("targetValue", 0f, 0f, 1f, false, false);
        _currentValueJsf = new JSONStorableFloat("currentValue", 0f, 0f, 1f, false, false);

        RegisterString(_versionJss);
        RegisterStringChooser(_atomJssc);
        RegisterStringChooser(_receiverJssc);
        RegisterStringChooser(_receiverTargetJssc);
        RegisterFloat(_periodJsf);
        RegisterFloat(_quicknessJsf);
        RegisterFloat(_lowerValueJsf);
        RegisterFloat(_upperValueJsf);
        RegisterFloat(_curvatureJsf);
        RegisterStringChooser(_functionJssc);
        RegisterBool(_enableRandomnessJsb);
    }

    protected override void BuildUI()
    {
        var titleTextField = CreateTitleTextField(_titleJss, 72, false);
        titleTextField.UItext.fontSize = 36;

        var versionTextField = CreateTitleTextField(_versionJss, 72, true);
        versionTextField.UItext.fontSize = 24;
        versionTextField.UItext.alignment = TextAnchor.UpperRight;

        var atomPopup = NewPopup(_atomJssc, 1000);
        atomPopup.popup.onOpenPopupHandlers += SyncAtomChoices;

        NewPopup(_receiverJssc, 860);
        NewPopup(_receiverTargetJssc, 720);

        var periodSlider = CreateSlider(_periodJsf, true);
        periodSlider.label = "Period";

        var quicknessSlider = CreateSlider(_quicknessJsf, true);
        quicknessSlider.label = "Quickness";

        var lowerValueSlider = CreateSlider(_lowerValueJsf, true);
        lowerValueSlider.label = "Lower Value";

        var upperValueSlider = CreateSlider(_upperValueJsf, true);
        upperValueSlider.label = "Upper Value";

        this.NewSpacer(230);

        var functionPopup = CreateScrollablePopup(_functionJssc);
        functionPopup.popupPanelHeight = 160;
        functionPopup.popup.onOpenPopupHandlers += () => OnBlurPopup(functionPopup.popup);
        popups.Add(functionPopup.popup);

        var curvatureSlider = CreateSlider(_curvatureJsf);
        curvatureSlider.label = "Curvature";

        this.NewSpacer(10, true);

        var enableRandomnessToggle = CreateToggle(_enableRandomnessJsb, true);
        enableRandomnessToggle.label = "Enable Randomness";

        _targetValueSlider = CreateSlider(_targetValueJsf, true);
        _targetValueSlider.slider.interactable = false;
        _targetValueSlider.defaultButtonEnabled = false;
        _targetValueSlider.quickButtonsEnabled = false;
        _targetValueSlider.label = "Target Value";

        var currentValueSlider = CreateSlider(_currentValueJsf, true);
        currentValueSlider.defaultButtonEnabled = false;
        currentValueSlider.quickButtonsEnabled = false;
        currentValueSlider.label = "Current Value";
    }

    UIDynamicTextField CreateTitleTextField(JSONStorableString jss, int height, bool rightSide)
    {
        var textField = CreateTextField(jss, rightSide);
        textField.UItext.alignment = TextAnchor.MiddleCenter;
        textField.backgroundColor = Color.clear;

        var layout = textField.GetComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.minHeight = height;

        return textField;
    }

    void CreateFunctionChooser()
    {

        RegisterStringChooser(_functionJssc);
    }

    UIDynamicPopup NewPopup(JSONStorableStringChooser jsc, int panelHeight)
    {
        var uiDynamicPopup = this.CreatePopupAuto(jsc);
        uiDynamicPopup.popupPanelHeight = panelHeight;
        uiDynamicPopup.popup.onOpenPopupHandlers += () => OnBlurPopup(uiDynamicPopup.popup);
        popups.Add(uiDynamicPopup.popup);
        return uiDynamicPopup;
    }

    void SyncAtomChoices()
    {
        var options = new List<string> { "None" };
        foreach(var atom in SuperController.singleton.GetAtoms())
        {
            options.Add(atom.uid);
        }

        _atomJssc.choices = options;
    }

    void SyncAtom(string value)
    {
        var receiverChoices = new List<string> { "None" };
        if(value != null)
        {
            _receivingAtom = SuperController.singleton.GetAtomByUid(value);
            if(_receivingAtom != null)
            {
                receiverChoices.AddRange(_receivingAtom.GetStorableIDs());
            }
        }
        else
        {
            _receivingAtom = null;
        }

        _pause = true;
        _receiverJssc.choices = receiverChoices;
        _receiverJssc.valNoCallback = "None";
    }

    string _missingReceiverStoreId = "";

    void CheckMissingReceiver()
    {
        if(_missingReceiverStoreId != "" && _receivingAtom)
        {
            var missingReceiver = _receivingAtom.GetStorableByID(_missingReceiverStoreId);
            if(missingReceiver)
            {
                string saveTargetName = _receiverTargetName;
                SyncReceiver(_missingReceiverStoreId);
                _missingReceiverStoreId = "";
                insideRestore = true;
                _receiverTargetJssc.val = saveTargetName;
                insideRestore = false;
            }
        }
    }

    void SyncReceiver(string receiverID)
    {
        var receiverTargetChoices = new List<string> { "None" };
        if(_receivingAtom  && receiverID != null)
        {
            _receiverStorable = _receivingAtom.GetStorableByID(receiverID);
            if(_receiverStorable)
            {
                receiverTargetChoices.AddRange(_receiverStorable.GetFloatParamNames());
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

        _pause = true;
        _receiverTargetJssc.choices = receiverTargetChoices;
        _receiverTargetJssc.valNoCallback = "None";
    }

    void SyncReceiverTarget(string receiverTargetName)
    {
        _receiverTargetName = receiverTargetName;
        _receiverTargetJsf = null;
        bool pause = true;
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

                pause = false;
            }
        }

        _pause = pause;
    }

    void SyncEnableRandomness(bool value)
    {
        _targetValueJsf.val = value ? _targetValueJsf.val : _targetValueJsf.min;
        _targetValueSlider.SetActiveStyle(value);
    }

    void SyncFunction(string option)
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

    bool _flip;
    float _accumulated;
    float _start;
    float _end;

    void Update()
    {
        if(_pause)
        {
            return;
        }

        try
        {
            if(_accumulated > _periodJsf.val)
            {
                _accumulated = 0f;
                if(_enableRandomnessJsb.val)
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
            Utils.LogError($"{e}");
        }
    }

    void OnAtomRenamed(string oldName, string newName)
    {
        SyncAtomChoices();
        var renamedAtom = SuperController.singleton.GetAtomByUid(newName);

        if(_atomJssc.val == oldName)
        {
            _atomJssc.valNoCallback = "";
            _atomJssc.valNoCallback = renamedAtom.uid;
        }
        else if(renamedAtom.isSubSceneType)
        {
            foreach(var atom in renamedAtom.subSceneComponent.atomsInSubScene)
            {
                if(_atomJssc.val == atom.uid)
                {
                    _atomJssc.valNoCallback = "";
                    _atomJssc.valNoCallback = atom.uid;
                    break;
                }
            }
        }
    }

    public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
    {
        var jc = base.GetJSON(includePhysical, includeAppearance, forceStore);
        /* representsAtomUid = true causes this to be empty because the val doesn't map to an atom by uid, but by uidWithoutSubScenePath.
         * Setting the value manually forces the value to be stored in the subscene save JSON.
         */
        jc["atom"] = _atomJssc.val;
        return jc;
    }

    public override void RestoreFromJSON(
        JSONClass jc,
        bool restorePhysical = true,
        bool restoreAppearance = true,
        JSONArray presetAtoms = null,
        bool setMissingToDefault = true
    )
    {
        /* Ensure loading a SubScene file sets the correct value to JSONStorableStringChooser. */
        if(jc.HasKey("atom"))
        {
            var subScene = containingAtom.containingSubScene;
            var atom = SuperController.singleton.GetAtomByUid(jc["atom"].Value);
            if(subScene && (!atom || atom.containingSubScene != subScene))
            {
                if(atom)
                {
                    jc["atom"] = atom.uidWithoutSubScenePath;
                }

                subScenePrefix = containingAtom.uid.Replace(containingAtom.uidWithoutSubScenePath, "");
            }
        }

        base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
        subScenePrefix = null;
    }

    void OnDestroy()
    {
        try
        {
            BaseOnDestroy();
            SuperController.singleton.onAtomUIDRenameHandlers -= OnAtomRenamed;
        }
        catch(Exception e)
        {
            Utils.LogError($"{e}");
        }
    }
}
