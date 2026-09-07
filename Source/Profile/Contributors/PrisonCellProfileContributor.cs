using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Static Prison Cell capability contribution. Prisoner containment and warden
    /// interactions are later runtime concerns and stay outside the profile.
    /// </summary>
    public sealed class PrisonCellProfileContributor : IShuttleProfileContributor
    {
        private static readonly PrisonCellProfileContributor instance = new PrisonCellProfileContributor();

        private PrisonCellProfileContributor()
        {
        }

        public static PrisonCellProfileContributor Instance
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

            ShuttlePrisonCellModuleDef prisonCellDef =
                context.ModuleDef as ShuttlePrisonCellModuleDef;
            if (prisonCellDef == null)
            {
                return;
            }

            if (context.Module != null && !context.Module.IsEnabled)
            {
                return;
            }

            IShuttlePrisonCellProfileContributionSink prisonCellSink =
                context.Contributions as IShuttlePrisonCellProfileContributionSink;
            if (prisonCellSink == null)
            {
                return;
            }

            prisonCellSink.AddPrisonCellModule(
                prisonCellDef.prisonerSlots,
                prisonCellDef.security,
                prisonCellDef.comfort,
                prisonCellDef.supportsFeeding,
                prisonCellDef.supportsTending,
                prisonCellDef.allowCargoFoodSupply,
                prisonCellDef.allowRefrigeratedCargoFoodSupply,
                prisonCellDef.requireCargoLogisticsForFoodSupply,
                prisonCellDef.maximumCargoFoodPreferability);
        }
    }
}
