using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ColliderEditor;
using SimpleJSON;
using System.Linq;
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
    StorableString _versionJss;
    StorableStringChooser _atomJssc;
    StorableStringChooser _receiverJssc;
    StorableStringChooser _receiverTargetJssc;
    StorableStringChooser _functionJssc;
    StorableFloat _curvatureJsf;
    StorableFloat _periodJsf;
    StorableFloat _quicknessJsf;
    StorableFloat _lowerValueJsf;
    StorableFloat _upperValueJsf;
    StorableBool _enableRandomnessJsb;
    JSONStorableFloat _targetValueJsf;
    JSONStorableFloat _currentValueJsf;
    JSONStorableFloat _receiverTarget;

    UIDynamicSlider _targetValueSlider;

    string _missingReceiverStoreId;
    string _missingReceiverTargetName;
    Atom _receivingAtom;
    JSONStorable _receiverStorable;

    Dictionary<string, Func<float, float>> _functionOptions;
    Func<float, float> _function;
    float _exponent;
    const float MIDPOINT = 0.5f;

    public override void Init()
    {
        try
        {
            SetupStorables();
            SyncAtomOptions();
            SuperController.singleton.onAtomUIDRenameHandlers += OnAtomRenamed;
            StartOrPostponeCoroutine(FinishInitCo());
        }
        catch(Exception e)
        {
            Utils.LogError($"{e}");
        }
    }

    IEnumerator FinishInitCo()
    {
        yield return new WaitForEndOfFrame();
        while(SuperController.singleton.isLoading)
        {
            yield return null;
        }

        SetupCallbackFunctions();
        _functionJssc.Callback();
        _enableRandomnessJsb.Callback();

        if(!isRestoringFromJSON)
        {
            _atomJssc.val = containingAtom.uid;
        }

        isInitialized = true;
    }

    void SetupStorables()
    {
        _titleJss = new JSONStorableString("title", $"{"\n".Size(18)}{nameof(FloatParamRandomizerEE)}".Bold());
        _versionJss = new StorableString("version", VERSION);
        _atomJssc = new StorableStringChooser("atom", new List<string>(), null, "Atom")
        {
            representsAtomUid = true,
        };
        _receiverJssc = new StorableStringChooser("receiver", null, null, "Receiver");
        _receiverTargetJssc = new StorableStringChooser("receiverTarget", null, null, "Target");

        // any function can be added here as long as it takes an x in range [0, 1] and outputs an y in range [0, 1]
        _functionOptions = new Dictionary<string, Func<float, float>>
        {
            { "Ease In-Out", value => ParametricSmoother(value, _exponent, MIDPOINT) },
            { "Bounce In-Out", value => ParametricSmoother(value, _exponent, MIDPOINT) },
        };
        var options = _functionOptions.Keys.ToList();
        _functionJssc = new StorableStringChooser("function", options, options[0], "Function");

        _periodJsf = new StorableFloat("period", 1f, 0f, 10f, false);
        _quicknessJsf = new StorableFloat("quickness", 1f, 0f, 10f);
        _lowerValueJsf = new StorableFloat("lowerValue", 0f, 0f, 1f, false);
        _upperValueJsf = new StorableFloat("upperValue", 0f, 0f, 1f, false);
        _curvatureJsf = new StorableFloat("curvature", 0.25f, 0.0f, 1.0f);
        _enableRandomnessJsb = new StorableBool("enableRandomness", true);
        _targetValueJsf = new JSONStorableFloat("targetValue", 0f, 0f, 1f, false, false);
        _currentValueJsf = new JSONStorableFloat("currentValue", 0f, 0f, 1f, false, false);

        _versionJss.RegisterTo(this);
        _atomJssc.RegisterTo(this);
        _receiverJssc.RegisterTo(this);
        _receiverTargetJssc.RegisterTo(this);
        _periodJsf.RegisterTo(this);
        _quicknessJsf.RegisterTo(this);
        _lowerValueJsf.RegisterTo(this);
        _upperValueJsf.RegisterTo(this);
        _curvatureJsf.RegisterTo(this);
        _functionJssc.RegisterTo(this);
        _enableRandomnessJsb.RegisterTo(this);
    }

    void SetupCallbackFunctions()
    {
        _atomJssc.setCallbackFunction = SyncAtom;
        _receiverJssc.setCallbackFunction = SyncReceiver;
        _receiverTargetJssc.setCallbackFunction = SyncReceiverTarget;
        _functionJssc.setCallbackFunction = SyncFunction;
        _lowerValueJsf.setCallbackFunction = value =>
        {
            if(value > _upperValueJsf.val)
            {
                _upperValueJsf.val = value;
            }
        };
        _upperValueJsf.setCallbackFunction = value =>
        {
            if(value < _lowerValueJsf.val)
            {
                _lowerValueJsf.val = value;
            }
        };
        _enableRandomnessJsb.setCallbackFunction = SyncEnableRandomness;
    }

    protected override void BuildUI()
    {
        var titleTextField = CreateTitleTextField(_titleJss, 72, false);
        titleTextField.UItext.fontSize = 36;

        var versionTextField = CreateTitleTextField(_versionJss, 72, true);
        versionTextField.UItext.fontSize = 24;
        versionTextField.UItext.alignment = TextAnchor.UpperRight;

        var atomPopup = NewPopup(_atomJssc, 1000);
        atomPopup.popup.onOpenPopupHandlers += SyncAtomOptions;

        var receiverPopup = NewPopup(_receiverJssc, 860);
        receiverPopup.popup.onOpenPopupHandlers += SyncReceiverOptions;

        var receiverTargetPopup = NewPopup(_receiverTargetJssc, 720);
        receiverTargetPopup.popup.onOpenPopupHandlers += SyncReceiverTargetOptions;

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
        functionPopup.popup.labelText.color = Color.black;
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

    UIDynamicPopup NewPopup(JSONStorableStringChooser jsc, int panelHeight)
    {
        var uiDynamicPopup = this.CreatePopupAuto(jsc);
        uiDynamicPopup.popupPanelHeight = panelHeight;
        uiDynamicPopup.popup.onOpenPopupHandlers += () => OnBlurPopup(uiDynamicPopup.popup);
        uiDynamicPopup.popup.labelText.color = Color.black;
        popups.Add(uiDynamicPopup.popup);
        return uiDynamicPopup;
    }

    void SyncAtomOptions()
    {
        var options = new List<string>();
        foreach(var atom in SuperController.singleton.GetAtoms())
        {
            options.Add(atom.uid);
        }

        _atomJssc.choices = options;
    }

    void SyncReceiverOptions()
    {
        var options = new List<string>();
        if(_receivingAtom)
        {
            options.AddRange(_receivingAtom.GetStorableIDs().Where(id => !string.Equals(id, storeId)));
        }

        _receiverJssc.choices = options;
    }

    void SyncReceiverTargetOptions()
    {
        var options = new List<string>();
        if(_receiverStorable)
        {
            options.AddRange(_receiverStorable.GetFloatParamNames());
        }

        _receiverTargetJssc.choices = options;
    }

    void SyncAtom(string value)
    {
        if(string.IsNullOrEmpty(value) || string.Equals(value, Strings.SELECT))
        {
            _receivingAtom = null;
        }
        else
        {
            _receivingAtom = SuperController.singleton.GetAtomByUid(value);
            if(_receivingAtom == null)
            {
                Utils.LogError($"SyncAtom: Atom with uid {value} not found");
            }
        }

        SyncReceiverOptions();
        if(!_receiverJssc.choices.Contains(_receiverJssc.val))
        {
            _receiverJssc.val = Strings.SELECT;
        }
    }

    void SyncReceiver(string value)
    {
        if(!_receivingAtom || string.IsNullOrEmpty(value) || string.Equals(value, Strings.SELECT))
        {
            _receiverStorable = null;
        }
        else
        {
            _receiverStorable = _receivingAtom.GetStorableByID(value);
            if(_receiverStorable == null)
            {
                _missingReceiverStoreId = value;
            }
        }

        SyncReceiverTargetOptions();
        if(!_receiverTargetJssc.choices.Contains(_receiverTargetJssc.val))
        {
            _receiverTargetJssc.val = Strings.SELECT;
        }
    }

    void SyncReceiverTarget(string value)
    {
        if(!_receivingAtom || !_receiverStorable || string.IsNullOrEmpty(value) || string.Equals(value, Strings.SELECT))
        {
            _receiverTarget = null;
        }
        else
        {
            _receiverTarget = _receiverStorable.GetFloatJSONParam(value);
            if(_receiverTarget == null)
            {
                _missingReceiverTargetName = value;
            }
            else
            {
                UpdateStorableFloatsForTarget();
            }
        }
    }

    void UpdateStorableFloatsForTarget()
    {
        _lowerValueJsf.min = _receiverTarget.min;
        _lowerValueJsf.max = _receiverTarget.max;
        _upperValueJsf.min = _receiverTarget.min;
        _upperValueJsf.max = _receiverTarget.max;
        _currentValueJsf.min = _receiverTarget.min;
        _currentValueJsf.max = _receiverTarget.max;
        _targetValueJsf.min = _receiverTarget.min;
        _targetValueJsf.max = _receiverTarget.max;
        if(!insideRestore && !isRestoringFromJSON)
        {
            _lowerValueJsf.val = _receiverTarget.val;
            _upperValueJsf.val = _receiverTarget.val;
            _currentValueJsf.val = _receiverTarget.val;
            _targetValueJsf.val = _receiverTarget.val;
        }

        _start = _receiverTarget.val;
        _accumulated = _periodJsf.val + Time.deltaTime;
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

    void CheckMissingReceiver()
    {
        if(string.IsNullOrEmpty(_missingReceiverStoreId) || !_receivingAtom)
        {
            return;
        }

        _receiverStorable = _receivingAtom.GetStorableByID(_missingReceiverStoreId);
        if(_receiverStorable)
        {
            _missingReceiverStoreId = null;
            _receiverTargetJssc.choices = new List<string>(_receiverStorable.GetFloatParamNames());
            insideRestore = true;
            _receiverTargetJssc.Callback();
            insideRestore = false;
        }
    }

    void CheckMissingReceiverTarget()
    {
        if(string.IsNullOrEmpty(_missingReceiverTargetName) || !_receiverStorable || !_receivingAtom)
        {
            return;
        }

        _receiverTarget = _receiverStorable.GetFloatJSONParam(_missingReceiverTargetName);
        if(_receiverTarget != null)
        {
            _missingReceiverTargetName = null;
            insideRestore = true;
            UpdateStorableFloatsForTarget();
            insideRestore = false;
        }
    }

    bool _flip;
    float _accumulated;
    float _start;
    float _end;

    void Update()
    {
        if(!isInitialized || isRestoringFromJSON)
        {
            return;
        }

        try
        {
            if(Time.frameCount % 6 == 0)
            {
                CheckMissingReceiver();
                CheckMissingReceiverTarget();
            }

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
            float currentValue = Mathf.Lerp(_start, _end, _function(value));

            if(_receiverTarget != null)
            {
                _currentValueJsf.val = currentValue;
                _receiverTarget.val = currentValue;
            }
        }
        catch(Exception e)
        {
            Utils.LogError($"{e}");
        }
    }

    void OnAtomRenamed(string oldName, string newName)
    {
        SyncAtomOptions();
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
        isRestoringFromJSON = true;

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

        StartOrPostponeCoroutine(RestoreFromJSONCo(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault));
    }

    // ensure correct order for restoring atom, receiver and receiverTarget
    IEnumerator RestoreFromJSONCo(
        JSONClass jc,
        bool restorePhysical = true,
        bool restoreAppearance = true,
        JSONArray presetAtoms = null,
        bool setMissingToDefault = true
    )
    {
        while(!isInitialized)
        {
            yield return null;
        }

        string receiverStoreId = null;
        if(jc.HasKey("receiver"))
        {
            receiverStoreId = jc["receiver"].Value;
        }

        string receiverTargetName = null;
        if(jc.HasKey("receiverTarget"))
        {
            receiverTargetName = jc["receiverTarget"].Value;
        }

        base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
        subScenePrefix = null;

        _atomJssc.Callback();
        if(!string.IsNullOrEmpty(receiverStoreId))
        {
            _receiverJssc.valNoCallback = receiverStoreId;
            _receiverJssc.Callback();

            if(!string.IsNullOrEmpty(receiverTargetName))
            {
                _receiverTargetJssc.valNoCallback = receiverTargetName;
                _receiverTargetJssc.Callback();
            }
        }

        isRestoringFromJSON = false;
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
