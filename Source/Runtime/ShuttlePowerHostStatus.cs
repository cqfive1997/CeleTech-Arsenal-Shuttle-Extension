namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime
{
    /// <summary>
    /// Host-derived power availability for the internal shuttle bus.
    /// Keep this as data only; host comp probing stays outside PowerSystem.
    /// </summary>
    public sealed class ShuttlePowerHostStatus
    {
        // Derived from the external RimWorld host comps for this tick only.
        // It does not own internal shuttle energy truth.
        public bool ReactorOperational;
    }
}
