namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Narrow internal read port for aggregate supply status. Module transaction callers keep
    /// using IShuttleCargoResourceBroker and do not receive issue-oriented projections.
    /// </summary>
    internal interface IShuttleCargoSupplyReadPort
    {
        ShuttleCargoSupplySnapshot GetSupplySnapshot();
    }
}
