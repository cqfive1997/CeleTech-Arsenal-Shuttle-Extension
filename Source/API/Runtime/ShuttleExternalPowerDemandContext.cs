namespace CeleTech.ShuttleExtension.ModularShuttle.API.Runtime
{
    /// <summary>
    /// Read-only state context used while an external runtime contributes transient
    /// internal power demand.
    /// </summary>
    public sealed class ShuttleExternalPowerDemandContext
    {
        private const float MaxInternalPowerDemandWatts = 1E+12f;

        private float internalPowerDemandWatts;

        public ShuttleExternalPowerDemandContext(ShuttleExternalModuleInfo module)
            : this(module, null)
        {
        }

        public ShuttleExternalPowerDemandContext(
            ShuttleExternalModuleInfo module,
            IShuttleExternalRuntimeStateReader state)
        {
            this.Module = module;
            this.State = state ?? NullShuttleExternalRuntimeStateStore.Instance;
        }

        public ShuttleExternalModuleInfo Module { get; private set; }

        public IShuttleExternalRuntimeStateReader State { get; private set; }

        public float InternalPowerDemandWatts
        {
            get
            {
                return this.internalPowerDemandWatts;
            }
        }

        public void AddInternalPowerDemandWatts(float watts)
        {
            if (float.IsNaN(watts) || float.IsInfinity(watts) || watts <= 0f)
            {
                return;
            }

            double current = float.IsNaN(this.internalPowerDemandWatts) ||
                float.IsInfinity(this.internalPowerDemandWatts) ||
                this.internalPowerDemandWatts < 0f
                ? 0d
                : this.internalPowerDemandWatts;
            double projected = current + watts;
            this.internalPowerDemandWatts = projected >= MaxInternalPowerDemandWatts
                ? MaxInternalPowerDemandWatts
                : (float)projected;
        }
    }
}
