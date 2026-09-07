namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels
{
    internal struct ShuttleCargoLogisticsReadState
    {
        internal bool Installed;
        internal bool Enabled;
        internal bool SupportsItemTransfer;
        internal bool SupportsItemConsumption;
        internal bool SupportsItemDeposit;
        internal bool InternalBusPowered;

        internal bool SupportsPoweredItemTransfer
        {
            get
            {
                return this.Installed &&
                    this.Enabled &&
                    this.SupportsItemTransfer &&
                    this.InternalBusPowered;
            }
        }

        internal bool SupportsPoweredItemConsumptionAndDeposit
        {
            get
            {
                return this.Installed &&
                    this.Enabled &&
                    this.SupportsItemConsumption &&
                    this.SupportsItemDeposit &&
                    this.InternalBusPowered;
            }
        }
    }
}
