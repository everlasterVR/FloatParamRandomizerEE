sealed class StorableBool : JSONStorableBool
{
    public StorableBool(string paramName, bool startingValue) : base(paramName, startingValue)
    {
        storeType = StoreType.Full;
    }

    public void Callback() => setCallbackFunction?.Invoke(val);
    public void RegisterTo(MVRScript script) => script.RegisterBool(this);
}
