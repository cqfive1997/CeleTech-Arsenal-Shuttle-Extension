using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Static drive-control profile contribution. It tunes flight energy inputs only;
    /// cargo truth, launch spending, and runtime state stay in their own systems.
    /// </summary>
    public sealed class DriveControlProfileContributor : IShuttleProfileContributor
    {
        private static readonly DriveControlProfileContributor instance = new DriveControlProfileContributor();

        private DriveControlProfileContributor()
        {
        }

        public static DriveControlProfileContributor Instance
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

            ShuttleDriveControlModuleDef driveControlDef = context.ModuleDef as ShuttleDriveControlModuleDef;
            if (driveControlDef == null)
            {
                return;
            }

            context.Contributions.MultiplyEnergyCostFactor(driveControlDef.EffectiveEnergyCostFactor);
            context.Contributions.MultiplyLoadedMassEfficiencyFactor(driveControlDef.EffectiveLoadedMassEfficiencyFactor);
        }
    }
}
