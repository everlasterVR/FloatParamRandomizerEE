using System;
using System.Collections;
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
public class FloatParamRandomizerEE : MVRScript
{
    public const string VERSION = "0.0.0";

    List<UIPopup> _popups;
    JSONStorableStringChooser _atomJssc;
    JSONStorableStringChooser _receiverJssc;
    JSONStorableStringChooser _receiverTargetJssc;
    JSONStorableStringChooser _functionJssc;
    JSONStorableFloat _curvatureJsf;
    JSONStorableFloat _periodJsf;
    JSONStorableFloat _quicknessJsf;
    JSONStorableFloat _lowerValueJsf;
    JSONStorableFloat _upperValueJsf;
    JSONStorableBool _enableRandomness;
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

    bool _initialized;
    bool _restoringFromJSON;
    bool _pause;

    public override void Init()
    {
        try
        {
            var titleJss = new JSONStorableString("title", $"{"\n".Size(18)}{nameof(FloatParamRandomizerEE)}".Bold());
            var titleTextField = CreateTitleTextField(titleJss, 72, false);
            titleTextField.UItext.fontSize = 36;

            var versionJss = new JSONStorableString("version", VERSION)
            {
                storeType = JSONStorableParam.StoreType.Full,
            };
            RegisterString(versionJss);
            var versionTextField = CreateTitleTextField(versionJss, 72, true);
            versionTextField.UItext.fontSize = 24;
            versionTextField.UItext.alignment = TextAnchor.UpperRight;

            _popups = new List<UIPopup>();
            CreateAtomChooser();
            CreateReceiverChooser();
            CreateReceiverTargetChooser();

            SyncAtomChoices();

            _periodJsf = new JSONStorableFloat("period", 1f, 0f, 10f, false);
            RegisterFloat(_periodJsf);
            var periodSlider = CreateSlider(_periodJsf, true);
            periodSlider.label = "Period";

            _quicknessJsf = new JSONStorableFloat("quickness", 1f, 0f, 10f);
            RegisterFloat(_quicknessJsf);
            var quicknessSlider = CreateSlider(_quicknessJsf, true);
            quicknessSlider.label = "Quickness";

            _lowerValueJsf = new JSONStorableFloat("lowerValue", 0f, 0f, 1f, false);
            RegisterFloat(_lowerValueJsf);
            var lowerValueSlider = CreateSlider(_lowerValueJsf, true);
            lowerValueSlider.label = "Lower Value";

            _upperValueJsf = new JSONStorableFloat("upperValue", 0f, 0f, 1f, false);
            RegisterFloat(_upperValueJsf);
            var upperValueSlider = CreateSlider(_upperValueJsf, true);
            upperValueSlider.label = "Upper Value";

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

            this.NewSpacer(210);
            CreateFunctionChooser();

            _curvatureJsf = new JSONStorableFloat("curvature", 0.25f, 0.0f, 1.0f);
            RegisterFloat(_curvatureJsf);
            var curvatureSlider = CreateSlider(_curvatureJsf);
            curvatureSlider.label = "Curvature";

            _functionJssc.val = _functionOptions.Keys.First();

            this.NewSpacer(10, true);

            _enableRandomness = new JSONStorableBool("enableRandomness", true, SyncEnableRandomness);
            RegisterBool(_enableRandomness);
            var enableRandomnessToggle = CreateToggle(_enableRandomness, true);
            enableRandomnessToggle.label = "Enable Randomness";

            _targetValueJsf = new JSONStorableFloat("targetValue", 0f, 0f, 1f, false, false);
            _targetValueSlider = CreateSlider(_targetValueJsf, true);
            _targetValueSlider.slider.interactable = false;
            _targetValueSlider.defaultButtonEnabled = false;
            _targetValueSlider.quickButtonsEnabled = false;
            _targetValueSlider.label = "Target Value";

            _currentValueJsf = new JSONStorableFloat("currentValue", 0f, 0f, 1f, false, false);
            var currentValueSlider = CreateSlider(_currentValueJsf, true);
            currentValueSlider.defaultButtonEnabled = false;
            currentValueSlider.quickButtonsEnabled = false;
            currentValueSlider.label = "Current Value";

            SyncEnableRandomness(_enableRandomness.val);

            SuperController.singleton.onAtomUIDRenameHandlers += OnAtomRenamed;

            if(!_restoringFromJSON)
            {
                _atomJssc.val = containingAtom.uid;
            }

            _initialized = true;
        }
        catch(Exception e)
        {
            Utils.LogError($"{e}");
        }
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

    void CreateAtomChooser()
    {
        _atomJssc = new JSONStorableStringChooser("atom", new List<string>(), null, "Atom", SyncAtom)
        {
            representsAtomUid = true,
        };
        RegisterStringChooser(_atomJssc);
        var uiDynamicPopup = NewPopup(_atomJssc, 1000);
        uiDynamicPopup.popup.onOpenPopupHandlers += SyncAtomChoices;
    }

    void CreateReceiverChooser()
    {
        _receiverJssc = new JSONStorableStringChooser("receiver", null, null, "Receiver", SyncReceiver);
        RegisterStringChooser(_receiverJssc);
        NewPopup(_receiverJssc, 860);
    }

    void CreateReceiverTargetChooser()
    {
        _receiverTargetJssc = new JSONStorableStringChooser("receiverTarget", null, null, "Target", SyncReceiverTarget);
        RegisterStringChooser(_receiverTargetJssc);
        NewPopup(_receiverTargetJssc, 720);
    }

    void CreateFunctionChooser()
    {
        // any function can be added here as long as it takes an x in range [0, 1] and outputs an y in range [0, 1]
        _functionOptions = new Dictionary<string, Func<float, float>>
        {
            { "Ease In-Out", value => ParametricSmoother(value, _exponent, MIDPOINT) },
            { "Bounce In-Out", value => ParametricSmoother(value, _exponent, MIDPOINT) },
        };
        _functionJssc = new JSONStorableStringChooser("function", _functionOptions.Keys.ToList(), null, "Function", SyncFunction);
        RegisterStringChooser(_functionJssc);
        NewPopup(_functionJssc, 160);
    }

    UIDynamicPopup NewPopup(JSONStorableStringChooser jsc, int panelHeight)
    {
        var uiDynamicPopup = this.CreatePopupAuto(jsc);
        uiDynamicPopup.popupPanelHeight = panelHeight;
        uiDynamicPopup.popup.onOpenPopupHandlers += () => OnBlurPopup(uiDynamicPopup.popup);
        _popups.Add(uiDynamicPopup.popup);
        return uiDynamicPopup;
    }

    UnityEventsListener _pluginUIEventsListener;

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
            _pluginUIEventsListener.EnableHandlers += () => StartCoroutine(ActionsOnUIOpened());
            _pluginUIEventsListener.DisableHandlers += OnBlur;
            _pluginUIEventsListener.ClickHandlers += OnBlur;
        }
    }

    IEnumerator ActionsOnUIOpened()
    {
        yield return new WaitForEndOfFrame();
        var background = rightUIContent.parent.parent.parent.transform.GetComponent<Image>();
        background.color = new Color(0.85f, 0.85f, 0.85f);
    }

    void OnBlur() => OnBlurPopup(null);

    void OnBlurPopup(UIPopup openedPopup) =>
        _popups.Where(popup => popup != openedPopup)
            .ToList()
            .ForEach(popup => popup.visible = false);

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

    protected void Update()
    {
        if(!_initialized || _pause)
        {
            return;
        }

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
        _restoringFromJSON = true;
        StartCoroutine(RestoreFromJSONCo(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault));
    }

    IEnumerator RestoreFromJSONCo(
        JSONClass jc,
        bool restorePhysical = true,
        bool restoreAppearance = true,
        JSONArray presetAtoms = null,
        bool setMissingToDefault = true
    )
    {
        while(!_initialized)
        {
            yield return null;
        }

        /* Ensure loading a SubScene file sets the correct value to JSONStorableStringChooser */
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
        _restoringFromJSON = false;
    }

    protected void OnDestroy()
    {
        try
        {
            if(_pluginUIEventsListener != null)
            {
                DestroyImmediate(_pluginUIEventsListener);
            }

            SuperController.singleton.onAtomUIDRenameHandlers -= OnAtomRenamed;
        }
        catch(Exception e)
        {
            Utils.LogError($"{e}");
        }
    }
}
