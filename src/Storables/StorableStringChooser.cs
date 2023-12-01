using System.Collections.Generic;

sealed class StorableStringChooser : JSONStorableStringChooser
{
    public StorableStringChooser(
        string paramName,
        List<string> options,
        string startingValue,
        string displayName
    ) : base(paramName, options, startingValue, displayName)
    {
        storeType = StoreType.Full;
    }

    public void Callback() => setCallbackFunction?.Invoke(val);
    public void RegisterTo(MVRScript script) => script.RegisterStringChooser(this);
}
