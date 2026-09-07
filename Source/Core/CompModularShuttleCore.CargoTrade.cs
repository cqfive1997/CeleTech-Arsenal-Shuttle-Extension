namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class CompModularShuttleCore
    {
        internal void NotifyRefrigeratedCargoTradeChanged()
        {
            this.EnsureController();
            if (this.controller != null)
            {
                this.controller.InvalidateCargoInventorySnapshotCaches();
            }
        }
    }
}
