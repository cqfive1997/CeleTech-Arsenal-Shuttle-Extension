namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Immutable composition of the runtime facets currently required by the framework.
    /// It owns no gameplay state and performs no orchestration.
    /// </summary>
    internal sealed class ShuttleWeaponBackendBinding
    {
        internal ShuttleWeaponBackendBinding(
            string backendId,
            IShuttleWeaponLifecycleDriver lifecycle,
            IShuttleWeaponTickDriver tick,
            IShuttleWeaponPowerDemandDriver powerDemand,
            IShuttleWeaponForcedTargetEvaluator forcedTargetEvaluator,
            IShuttleWeaponAmmoReadDriver ammoReader,
            IShuttleWeaponAmmoCommandDriver ammoCommands,
            IShuttleWeaponManualReloadDriver manualReload,
            IShuttleWeaponRemovalRefundDriver removalRefund)
            : this(
                backendId,
                lifecycle,
                tick,
                powerDemand,
                forcedTargetEvaluator,
                null,
                null,
                ammoReader,
                ammoCommands,
                manualReload,
                removalRefund)
        {
        }

        internal ShuttleWeaponBackendBinding(
            string backendId,
            IShuttleWeaponLifecycleDriver lifecycle,
            IShuttleWeaponTickDriver tick,
            IShuttleWeaponPowerDemandDriver powerDemand,
            IShuttleWeaponForcedTargetEvaluator forcedTargetEvaluator,
            IShuttleWeaponFireDriver fireDriver,
            IShuttleWeaponChannelDriver channelDriver,
            IShuttleWeaponAmmoReadDriver ammoReader,
            IShuttleWeaponAmmoCommandDriver ammoCommands,
            IShuttleWeaponManualReloadDriver manualReload,
            IShuttleWeaponRemovalRefundDriver removalRefund)
        {
            this.BackendId = backendId;
            this.Lifecycle = lifecycle;
            this.TickDriver = tick;
            this.PowerDemand = powerDemand;
            this.ForcedTargetEvaluator = forcedTargetEvaluator;
            this.FireDriver = fireDriver;
            this.ChannelDriver = channelDriver;
            this.AmmoReader = ammoReader;
            this.AmmoCommands = ammoCommands;
            this.ManualReload = manualReload;
            this.RemovalRefund = removalRefund;
        }

        internal string BackendId { get; private set; }

        internal IShuttleWeaponLifecycleDriver Lifecycle { get; private set; }

        internal IShuttleWeaponTickDriver TickDriver { get; private set; }

        internal IShuttleWeaponPowerDemandDriver PowerDemand { get; private set; }

        internal IShuttleWeaponForcedTargetEvaluator ForcedTargetEvaluator { get; private set; }

        internal IShuttleWeaponFireDriver FireDriver { get; private set; }

        internal IShuttleWeaponChannelDriver ChannelDriver { get; private set; }

        internal IShuttleWeaponAmmoReadDriver AmmoReader { get; private set; }

        internal IShuttleWeaponAmmoCommandDriver AmmoCommands { get; private set; }

        internal IShuttleWeaponManualReloadDriver ManualReload { get; private set; }

        internal IShuttleWeaponRemovalRefundDriver RemovalRefund { get; private set; }

        internal bool IsComplete
        {
            get
            {
                return !string.IsNullOrEmpty(this.BackendId) &&
                    this.Lifecycle != null &&
                    this.TickDriver != null &&
                    this.PowerDemand != null &&
                    this.ForcedTargetEvaluator != null &&
                    this.AmmoReader != null &&
                    this.AmmoCommands != null &&
                    this.ManualReload != null &&
                    this.RemovalRefund != null;
            }
        }
    }
}
