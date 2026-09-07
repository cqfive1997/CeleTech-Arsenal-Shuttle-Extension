using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Static navigation profile contribution. It affects flight profile inputs only;
    /// destination choice, launch validation, and runtime launch state stay elsewhere.
    /// </summary>
    public sealed class NavigationComputerProfileContributor : IShuttleProfileContributor
    {
        private static readonly NavigationComputerProfileContributor instance = new NavigationComputerProfileContributor();

        private NavigationComputerProfileContributor()
        {
        }

        public static NavigationComputerProfileContributor Instance
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

            ShuttleNavigationComputerModuleDef navigationDef = context.ModuleDef as ShuttleNavigationComputerModuleDef;
            if (navigationDef == null)
            {
                return;
            }

            context.Contributions.AddRangeBonusTiles(navigationDef.EffectiveRangeBonusTiles);
            context.Contributions.MultiplyLaunchCooldownFactor(navigationDef.EffectiveLaunchCooldownFactor);
        }
    }
}
