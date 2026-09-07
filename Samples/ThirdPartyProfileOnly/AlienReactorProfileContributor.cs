using CeleTech.ShuttleExtension.ModularShuttle.API.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;
using Verse;

namespace MyCoolMod.ShuttleProfileSample
{
    [StaticConstructorOnStartup]
    public static class MyModShuttleProfileBootstrap
    {
        static MyModShuttleProfileBootstrap()
        {
            ShuttleProfileAPI.RegisterProfileContributor(
                "my.cool.mod",
                "alien-reactor-profile",
                new AlienReactorProfileContributor());
        }
    }

    public sealed class AlienReactorProfileContributor : IShuttleProfileContributor
    {
        public void Contribute(ShuttleProfileContributionContext context)
        {
            if (context == null || context.Contributions == null)
            {
                return;
            }

            // This is static profile data only. Runtime charge, ticks, commands, and UI are
            // outside the first profile-only SDK stage.
            context.Contributions.AddEnergyStorageWd(5000f);
            context.Contributions.AddRangeBonusTiles(8);

            ShuttleModuleContributionView module = context.ModuleView;
            if (module != null && module.InstanceConfig.ContainsKey("profileSampleEnabled"))
            {
                context.Contributions.AddMaxBatteryDischargeWatts(1000f);
            }
        }
    }
}
