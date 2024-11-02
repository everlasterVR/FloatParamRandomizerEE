using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    const string VERSION = "0.0.0";
    readonly LogBuilder _logBuilder = new LogBuilder();
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
            _functionJssc.Callback();
            _enableRandomnessJsb.Callback();
            _atomJssc.val = containingAtom.uid;

            SuperController.singleton.onAtomUIDRenameHandlers += OnAtomRenamed;
            SuperController.singleton.onAtomRemovedHandlers += OnAtomRemoved;
            initialized = true;
        }
        catch(Exception e)
        {
            _logBuilder.Exception(e);
        }
    }

    void SetupStorables()
    {
        _titleJss = new JSONStorableString("title", $"{"\n".Size(18)}{nameof(FloatParamRandomizerEE)}".Bold());
        _versionJss = new StorableString(Strings.VERSION, VERSION);
        _atomJssc = new StorableStringChooser("atom", null, null, "Atom")
        {
            representsAtomUid = true,
        };
        SyncAtomOptions();
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

    protected override void CreateUI()
    {
        var titleTextField = CreateTitleTextField(_titleJss, 72, false);
        titleTextField.UItext.fontSize = 36;

        var versionTextField = CreateTitleTextField(_versionJss, 72, true);
        versionTextField.UItext.fontSize = 24;
        versionTextField.UItext.alignment = TextAnchor.UpperRight;

        var atomPopup = CreatePopup(_atomJssc, 1000);
        atomPopup.popup.onOpenPopupHandlers += SyncAtomOptions;

        var receiverPopup = CreatePopup(_receiverJssc, 860);
        receiverPopup.popup.onOpenPopupHandlers += SyncReceiverOptions;

        var receiverTargetPopup = CreatePopup(_receiverTargetJssc, 720);
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
        ConfigurePopup(functionPopup, 160);
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

    UIDynamicPopup CreatePopup(JSONStorableStringChooser jssc, int panelHeight)
    {
        var uiDynamicPopup = CreateFilterablePopup(jssc);
        ConfigurePopup(uiDynamicPopup, panelHeight);
        AddPrevNextButtons(uiDynamicPopup);
        popups.Add(uiDynamicPopup.popup);
        return uiDynamicPopup;
    }

    void ConfigurePopup(UIDynamicPopup uiDynamic, float height, float offsetX = 0, bool upwards = false)
    {
        uiDynamic.popup.labelText.color = Color.black;

        if(height > 0f)
        {
            uiDynamic.popupPanelHeight = height;
        }

        float offsetY = upwards ? height + 60 : 0;
        uiDynamic.popup.popupPanel.offsetMin += new Vector2(offsetX, offsetY);
        uiDynamic.popup.popupPanel.offsetMax += new Vector2(offsetX, offsetY);
        uiDynamic.popup.onOpenPopupHandlers += () => OnBlurPopup(uiDynamic.popup);
    }

    void AddPrevNextButtons(UIDynamicPopup uiDynamic, bool cycle = false)
    {
        var popup = uiDynamic.popup;
        popup.labelText.alignment = TextAnchor.UpperCenter;
        popup.labelText.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.89f);

        /* Prev */
        {
            var t = this.InstantiateButton(uiDynamic.transform);
            var uiDynamicButton = t.GetComponent<UIDynamicButton>();
            uiDynamicButton.label = "<";
            if(cycle)
            {
                uiDynamicButton.AddListener(() =>
                {
                    popup.visible = false;
                    popup.SetPreviousOrLastValue();
                });
            }
            else
            {
                uiDynamicButton.AddListener(() =>
                {
                    popup.visible = false;
                    popup.SetPreviousValue();
                });
            }

            var rectT = t.GetComponent<RectTransform>();
            rectT.pivot = Vector2.zero;
            rectT.anchoredPosition = new Vector2(10, 0);
            rectT.sizeDelta = Vector2.zero;
            rectT.offsetMin = new Vector2(5, 5f);
            rectT.offsetMax = new Vector2(80, 70);
            rectT.anchorMin = Vector2.zero;
            rectT.anchorMax = Vector2.zero;
        }

        /* Next */
        {
            var t = this.InstantiateButton(uiDynamic.transform);
            var uiDynamicButton = t.GetComponent<UIDynamicButton>();
            uiDynamicButton.label = ">";
            if(cycle)
            {
                uiDynamicButton.AddListener(() =>
                {
                    popup.visible = false;
                    popup.SetNextOrFirstValue();
                });
            }
            else
            {
                uiDynamicButton.AddListener(() =>
                {
                    popup.visible = false;
                    popup.SetNextValue();
                });
            }

            var rectT = t.GetComponent<RectTransform>();
            rectT.pivot = Vector2.zero;
            rectT.anchoredPosition = Vector2.zero;
            rectT.sizeDelta = Vector2.zero;
            rectT.offsetMin = new Vector2(82, 5);
            rectT.offsetMax = new Vector2(157, 70);
            rectT.anchorMin = Vector2.zero;
            rectT.anchorMax = Vector2.zero;
        }
    }

    void SyncAtomOptions()
    {
        _atomJssc.choices = SuperController.singleton.GetAtoms().Select(atom => atom.uid).ToList();
    }

    void SyncReceiverOptions()
    {
        var options = new List<string>();
        if(_receivingAtom != null)
        {
            options.AddRange(_receivingAtom.GetStorableIDs().Where(id => !string.Equals(id, storeId)));
        }

        _receiverJssc.choices = options;
    }

    void SyncReceiverTargetOptions()
    {
        var options = new List<string>();
        if(_receiverStorable != null)
        {
            options.AddRange(_receiverStorable.GetFloatParamNames());
        }

        _receiverTargetJssc.choices = options;
    }

    void SyncAtom(string value)
    {
        if(insideRestore)
        {
            return;
        }

        if(string.IsNullOrEmpty(value) || value == Strings.SELECT)
        {
            _receivingAtom = null;
            _atomJssc.valNoCallback = Strings.SELECT;
        }
        else
        {
            _receivingAtom = SuperController.singleton.GetAtomByUid(value);
            if(_receivingAtom == null)
            {
                _logBuilder.Error($"SyncAtom: Atom with uid {value} not found", false);
            }
        }

        SyncReceiverOptions();
        if(!_receiverJssc.choices.Contains(_receiverJssc.val))
        {
            _receiverJssc.val = Strings.SELECT;
        }

        _receiverJssc.Callback();
    }

    void SyncReceiver(string value)
    {
        if(insideRestore)
        {
            return;
        }

        if(_receivingAtom == null || string.IsNullOrEmpty(value) || value == Strings.SELECT)
        {
            _receiverStorable = null;
            _receiverJssc.valNoCallback = Strings.SELECT;
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

        _receiverTargetJssc.Callback();
    }

    void SyncReceiverTarget(string value)
    {
        if(insideRestore)
        {
            return;
        }

        if(_receivingAtom == null || _receiverStorable == null || string.IsNullOrEmpty(value) || value == Strings.SELECT)
        {
            _receiverTarget = null;
            _receiverTargetJssc.valNoCallback = Strings.SELECT;
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

    bool _resetValues = true;

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
        if(_resetValues)
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
        if(string.IsNullOrEmpty(_missingReceiverStoreId) || _receivingAtom == null)
        {
            return;
        }

        _receiverStorable = _receivingAtom.GetStorableByID(_missingReceiverStoreId);
        if(_receiverStorable)
        {
            _missingReceiverStoreId = null;
            _receiverTargetJssc.choices = new List<string>(_receiverStorable.GetFloatParamNames());
            _resetValues = false;
            _receiverTargetJssc.Callback();
            _resetValues = true;
        }
    }

    void CheckMissingReceiverTarget()
    {
        if(string.IsNullOrEmpty(_missingReceiverTargetName) || _receiverStorable == null || _receivingAtom == null)
        {
            return;
        }

        _receiverTarget = _receiverStorable.GetFloatJSONParam(_missingReceiverTargetName);
        if(_receiverTarget != null)
        {
            _missingReceiverTargetName = null;
            _resetValues = false;
            UpdateStorableFloatsForTarget();
            _resetValues = true;
        }
    }

    float _timer;
    const float INTERVAL = 0.1f;
    bool _flip;
    float _accumulated;
    float _start;
    float _end;

    void Update()
    {
        if(!initialized)
        {
            return;
        }

        try
        {
            _timer += Time.deltaTime;
            if (_timer >= INTERVAL)
            {
                CheckMissingReceiver();
                CheckMissingReceiverTarget();
                _timer = 0f;
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
            _logBuilder.Exception(e);
            enabledJSON.val = false;
        }
    }

    void OnAtomRenamed(string oldName, string newName)
    {
        try
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
        catch(Exception e)
        {
            _logBuilder.Exception(e);
        }
    }

    void OnAtomRemoved(Atom atom)
    {
        try
        {
            if(_atomJssc.val == atom.uid)
            {
                _atomJssc.val = Strings.SELECT;
            }
        }
        catch(Exception e)
        {
            _logBuilder.Exception(e);
        }
    }

    public override void RestoreFromJSON(
        JSONClass jc,
        bool restorePhysical = true,
        bool restoreAppearance = true,
        JSONArray presetAtoms = null,
        bool setMissingToDefault = true
    )
    {
        try
        {
            /* Prevent overriding versionJss.val from JSON. Version stored in JSON just for information,
             * but could be intercepted here and used to save a "loadedFromVersion" value.
             */
            if(jc.HasKey(Strings.VERSION))
            {
                jc[Strings.VERSION] = VERSION;
            }

            FixRestoreFromSubscene(jc);
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

            _resetValues = false;
            // ensure correct order for restoring atom, receiver and receiverTarget
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
            _resetValues = true;
        }
        catch(Exception e)
        {
            _logBuilder.Exception(e);
        }
    }

    /* Ensure loading a SubScene file sets the correct value to JSONStorableStringChooser. */
    void FixRestoreFromSubscene(JSONClass jc)
    {
        if(!jc.HasKey("atom"))
        {
            return;
        }

        var subScene = containingAtom.containingSubScene;
        if(subScene != null)
        {
            var atom = SuperController.singleton.GetAtomByUid(jc["atom"].Value);
            if(atom == null || atom.containingSubScene != subScene)
            {
                subScenePrefix = containingAtom.uid.Replace(containingAtom.uidWithoutSubScenePath, "");
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SuperController.singleton.onAtomUIDRenameHandlers -= OnAtomRenamed;
        SuperController.singleton.onAtomRemovedHandlers -= OnAtomRemoved;
    }
}
