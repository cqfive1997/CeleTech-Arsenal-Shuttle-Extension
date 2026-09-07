using CeleTech.ShuttleExtension.ModularShuttle.API.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;
using Verse;

namespace MyCoolMod.ShuttleRuntimeSample
{
    [StaticConstructorOnStartup]
    public static class MyModShuttleProfileBootstrap
    {
        static MyModShuttleProfileBootstrap()
        {
            ShuttleProfileAPI.RegisterProfileContributor(
                "my.cool.mod",
                "alien-scanner-profile",
                new AlienScannerProfileContributor());
        }
    }

    public sealed class AlienScannerProfileContributor : IShuttleProfileContributor
    {
        public void Contribute(ShuttleProfileContributionContext context)
        {
            if (context == null || context.Contributions == null)
            {
                return;
            }

            // Use the public read-only view. Do not read live ShuttleModule or ShuttleSegment.
            if (context.ModuleView == null ||
                context.ModuleView.ModuleTypeId != "scanner")
            {
                return;
            }

            context.Contributions.AddRangeBonusTiles(4);
        }
    }
}
