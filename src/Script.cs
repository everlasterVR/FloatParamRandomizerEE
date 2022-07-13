using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// FloatParamRandomizerEE
/// Extended edition of FloatParamRandomizer by MeshedVR.
/// Randomizes float values with a smooth transition from current value towards target value.
/// Source: https://github.com/everlasterVR/FloatParamRandomizerEE
/// </summary>
public class Script : MVRScript
{
    private void SyncAtomChoices()
    {
        var atomChoices = new List<string>();
        atomChoices.Add("None");
        foreach(string atomUID in SuperController.singleton.GetAtomUIDs())
        {
            atomChoices.Add(atomUID);
        }

        _atomJSON.choices = atomChoices;
    }

    private Atom _receivingAtom;

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

        _receiverJSON.choices = receiverChoices;
        _receiverJSON.val = "None";
    }

    private JSONStorableStringChooser _atomJSON;

    private string _missingReceiverStoreId = "";

    private void CheckMissingReceiver()
    {
        if(_missingReceiverStoreId != "" && _receivingAtom != null)
        {
            var missingReceiver = _receivingAtom.GetStorableByID(_missingReceiverStoreId);
            if(missingReceiver != null)
            {
                //Debug.Log("Found late-loading receiver " + _missingReceiverStoreId);
                string saveTargetName = _receiverTargetName;
                SyncReceiver(_missingReceiverStoreId);
                _missingReceiverStoreId = "";
                insideRestore = true;
                _receiverTargetJSON.val = saveTargetName;
                insideRestore = false;
            }
        }
    }

    private JSONStorable _receiver;

    private void SyncReceiver(string receiverID)
    {
        var receiverTargetChoices = new List<string>();
        receiverTargetChoices.Add("None");
        if(_receivingAtom != null && receiverID != null)
        {
            _receiver = _receivingAtom.GetStorableByID(receiverID);
            if(_receiver != null)
            {
                foreach(string floatParam in _receiver.GetFloatParamNames())
                {
                    receiverTargetChoices.Add(floatParam);
                }
            }
            else if(receiverID != "None")
            {
                // some storables can be late loaded, like skin, clothing, hair, etc so must keep track of missing receiver
                //Debug.Log("Missing receiver " + receiverID);
                _missingReceiverStoreId = receiverID;
            }
        }
        else
        {
            _receiver = null;
        }

        _receiverTargetJSON.choices = receiverTargetChoices;
        _receiverTargetJSON.val = "None";
    }

    private JSONStorableStringChooser _receiverJSON;

    private string _receiverTargetName;
    private JSONStorableFloat _receiverTarget;

    private void SyncReceiverTarget(string receiverTargetName)
    {
        _receiverTargetName = receiverTargetName;
        _receiverTarget = null;
        if(_receiver != null && receiverTargetName != null)
        {
            _receiverTarget = _receiver.GetFloatJSONParam(receiverTargetName);
            if(_receiverTarget != null)
            {
                _lowerValueJSON.min = _receiverTarget.min;
                _lowerValueJSON.max = _receiverTarget.max;
                _upperValueJSON.min = _receiverTarget.min;
                _upperValueJSON.max = _receiverTarget.max;
                _currentValueJSON.min = _receiverTarget.min;
                _currentValueJSON.max = _receiverTarget.max;
                _targetValueJSON.min = _receiverTarget.min;
                _targetValueJSON.max = _receiverTarget.max;
                if(!insideRestore)
                {
                    // only sync up val if not in restore
                    _lowerValueJSON.val = _receiverTarget.val;
                    _upperValueJSON.val = _receiverTarget.val;
                    _currentValueJSON.val = _receiverTarget.val;
                    _targetValueJSON.val = _receiverTarget.val;
                }
            }
        }
    }

    private JSONStorableStringChooser _receiverTargetJSON;

    private JSONStorableFloat _periodJSON;
    private JSONStorableFloat _quicknessJSON;
    private JSONStorableFloat _lowerValueJSON;
    private JSONStorableFloat _upperValueJSON;
    private JSONStorableFloat _targetValueJSON;
    private JSONStorableFloat _currentValueJSON;

    public override void Init()
    {
        try
        {
            // make atom selector
            _atomJSON = new JSONStorableStringChooser("atom", SuperController.singleton.GetAtomUIDs(), null, "Atom", SyncAtom);
            _atomJSON.representsAtomUid = true;
            RegisterStringChooser(_atomJSON);
            SyncAtomChoices();
            var dp = CreateFilterablePopup(_atomJSON);
            dp.popupPanelHeight = 1100f;
            // want to always resync the atom choices on opening popup since atoms can be added/removed
            dp.popup.onOpenPopupHandlers += SyncAtomChoices;

            // make receiver selector
            _receiverJSON = new JSONStorableStringChooser("receiver", null, null, "Receiver", SyncReceiver);
            RegisterStringChooser(_receiverJSON);
            dp = CreateFilterablePopup(_receiverJSON);
            dp.popupPanelHeight = 960f;

            // make receiver target selector
            _receiverTargetJSON = new JSONStorableStringChooser("receiverTarget", null, null, "Target", SyncReceiverTarget);
            RegisterStringChooser(_receiverTargetJSON);
            dp = CreateFilterablePopup(_receiverTargetJSON);
            dp.popupPanelHeight = 820f;

            // set atom to current atom to initialize
            _atomJSON.val = containingAtom.uid;

            // create random value generation period
            _periodJSON = new JSONStorableFloat("period", 1f, 0f, 10f, false);
            RegisterFloat(_periodJSON);
            CreateSlider(_periodJSON, true);

            // quickness (smoothness)
            _quicknessJSON = new JSONStorableFloat("quickness", 1f, 0f, 10f);
            RegisterFloat(_quicknessJSON);
            CreateSlider(_quicknessJSON, true);

            // lower val
            _lowerValueJSON = new JSONStorableFloat("lowerValue", 0f, 0f, 1f, false);
            RegisterFloat(_lowerValueJSON);
            CreateSlider(_lowerValueJSON, true);

            // upper val
            _upperValueJSON = new JSONStorableFloat("upperValue", 0f, 0f, 1f, false);
            RegisterFloat(_upperValueJSON);
            CreateSlider(_upperValueJSON, true);

            // target val
            _targetValueJSON = new JSONStorableFloat("targetValue", 0f, 0f, 1f, false, false);
            // don't register - this is for viewing only and is generated
            var ds = CreateSlider(_targetValueJSON, true);
            ds.defaultButtonEnabled = false;
            ds.quickButtonsEnabled = false;

            // current val
            _currentValueJSON = new JSONStorableFloat("currentValue", 0f, 0f, 1f, false, false);
            // don't register - this is for viewing only and is generated
            ds = CreateSlider(_currentValueJSON, true);
            ds.defaultButtonEnabled = false;
            ds.quickButtonsEnabled = false;

        }
        catch(Exception e)
        {
            SuperController.LogError("Exception caught: " + e);
        }
    }

    private float _accumulated;
    private float _start;

    protected void Update()
    {
        try
        {
            if(_accumulated > _periodJSON.val)
            {
                _accumulated = 0f;
                _start = _currentValueJSON.val;
                _targetValueJSON.val = UnityEngine.Random.Range(_lowerValueJSON.val, _upperValueJSON.val);
            }

            _accumulated += Time.deltaTime;
            _currentValueJSON.val = Mathf.SmoothStep(_start, _targetValueJSON.val, _accumulated * _quicknessJSON.val / _periodJSON.val);

            CheckMissingReceiver();
            if(_receiverTarget != null)
            {
                _receiverTarget.val = _currentValueJSON.val;
            }
        }
        catch(Exception e)
        {
            SuperController.LogError("Exception caught: " + e);
        }
    }
}
