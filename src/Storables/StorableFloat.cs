sealed class StorableFloat : JSONStorableFloat
{
    public StorableFloat(
        string paramName,
        float startingValue,
        float minValue,
        float maxValue,
        bool constrain = true
    ) : base(paramName, startingValue, minValue, maxValue, constrain)
    {
        storeType = StoreType.Full;
    }

    // ReSharper disable once UnusedMember.Global
    public void Callback() => setCallbackFunction?.Invoke(val);
    public void RegisterTo(MVRScript script) => script.RegisterFloat(this);
}
