using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Static mech charger profile contribution. Holder contents and charge progress are
    /// live comp state and are intentionally not contributed here.
    /// </summary>
    public sealed class MechChargerProfileContributor : IShuttleProfileContributor
    {
        private static readonly MechChargerProfileContributor instance = new MechChargerProfileContributor();

        private MechChargerProfileContributor()
        {
        }

        public static MechChargerProfileContributor Instance
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

            ShuttleMechChargerModuleDef mechChargerDef = context.ModuleDef as ShuttleMechChargerModuleDef;
            if (mechChargerDef == null)
            {
                return;
            }

            IShuttleMechChargerProfileContributionSink mechChargerSink =
                context.Contributions as IShuttleMechChargerProfileContributionSink;
            if (mechChargerSink == null)
            {
                return;
            }

            mechChargerSink.AddMechChargerModule(
                mechChargerDef.mechChargeSlots,
                mechChargerDef.chargeRateFactor);
        }
    }
}
