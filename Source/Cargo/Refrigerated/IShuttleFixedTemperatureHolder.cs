namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    /// <summary>
    /// Only dedicated refrigerated cargo holders and refrigerated launch staging holders may
    /// implement this interface.
    /// </summary>
    internal interface IShuttleFixedTemperatureHolder
    {
        bool TryGetFixedTemperature(out float temperatureC);
    }
}
