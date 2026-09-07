using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Built-in reactor profile contribution. Runtime availability and grid export application
    /// are handled elsewhere; this only reports static watts from the module def.
    /// </summary>
    public sealed class ReactorProfileContributor : IShuttleProfileContributor
    {
        private static readonly ReactorProfileContributor instance = new ReactorProfileContributor();

        private ReactorProfileContributor()
        {
        }

        public static ReactorProfileContributor Instance
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

            ShuttleReactorModuleDef reactorDef = context.ModuleDef as ShuttleReactorModuleDef;
            if (reactorDef == null)
            {
                return;
            }

            context.Contributions.AddReactorGenerationWatts(reactorDef.EffectiveReactorGenerationWatts);
            context.Contributions.AddGridExportCapacityWatts(reactorDef.EffectiveGridExportCapacityWatts);
        }
    }
}
