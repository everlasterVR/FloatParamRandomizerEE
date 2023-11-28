sealed class StorableString : JSONStorableString
{
    public StorableString(string paramName, string startingValue) : base(paramName, startingValue)
    {
        storeType = StoreType.Full;
    }

    internal void Callback() => setCallbackFunction?.Invoke(val);
    public void RegisterTo(MVRScript script) => script.RegisterString(this);
}
