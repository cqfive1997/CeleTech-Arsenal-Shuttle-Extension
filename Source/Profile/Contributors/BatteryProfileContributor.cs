using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Built-in battery profile contribution. Stored charge remains runtime state; this reports
    /// static capacity and charge/discharge limits only.
    /// </summary>
    public sealed class BatteryProfileContributor : IShuttleProfileContributor
    {
        private static readonly BatteryProfileContributor instance = new BatteryProfileContributor();

        private BatteryProfileContributor()
        {
        }

        public static BatteryProfileContributor Instance
        {
            get
            {
                return instance;
            }
        }

        public void Contribute(ShuttleProfileContributionContext context)
        {
            if (context == null || context.Contributions == null)
            {
                return;
            }

            ShuttleBatteryModuleDef batteryDef = context.ModuleDef as ShuttleBatteryModuleDef;
            if (batteryDef == null)
            {
                return;
            }

            context.Contributions.AddEnergyStorageWd(batteryDef.EffectiveEnergyStorageCapacityWd);
            context.Contributions.AddMaxBatteryChargeWatts(batteryDef.EffectiveMaxChargeWatts);
            context.Contributions.AddMaxBatteryDischargeWatts(batteryDef.EffectiveMaxDischargeWatts);
        }
    }
}
