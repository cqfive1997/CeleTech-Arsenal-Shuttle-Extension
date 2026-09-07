using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Built-in cockpit profile contribution. Crew capacity is derived from static cockpit def
    /// data and does not mutate pawns, cargo, or runtime state.
    /// </summary>
    public sealed class CockpitProfileContributor : IShuttleProfileContributor
    {
        private static readonly CockpitProfileContributor instance = new CockpitProfileContributor();

        private CockpitProfileContributor()
        {
        }

        public static CockpitProfileContributor Instance
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

            ShuttleCockpitModuleDef cockpitDef = context.ModuleDef as ShuttleCockpitModuleDef;
            if (cockpitDef == null)
            {
                return;
            }

            context.Contributions.AddCrewCapacity(cockpitDef.additionalCrewCapacity);
        }
    }
}
