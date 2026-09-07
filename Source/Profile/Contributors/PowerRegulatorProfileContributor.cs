using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Static power-regulator contribution. It changes profile charge/discharge capability only;
    /// stored charge remains durable runtime truth in ShuttleRuntimeState.Power.
    /// </summary>
    public sealed class PowerRegulatorProfileContributor : IShuttleProfileContributor
    {
        private static readonly PowerRegulatorProfileContributor instance = new PowerRegulatorProfileContributor();

        private PowerRegulatorProfileContributor()
        {
        }

        public static PowerRegulatorProfileContributor Instance
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

            ShuttlePowerRegulatorModuleDef regulatorDef = context.ModuleDef as ShuttlePowerRegulatorModuleDef;
            if (regulatorDef == null)
            {
                return;
            }

            context.Contributions.AddMaxBatteryChargeWatts(regulatorDef.EffectiveChargeRateBonusWatts);
            context.Contributions.AddMaxBatteryDischargeWatts(regulatorDef.EffectiveDischargeRateBonusWatts);
        }
    }
}
