using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Built-in cargo module profile contribution. Cargo ownership stays in the cargo backend;
    /// this only adds profile cargo-region capacity.
    /// </summary>
    public sealed class CargoProfileContributor : IShuttleProfileContributor
    {
        private static readonly CargoProfileContributor instance = new CargoProfileContributor();

        private CargoProfileContributor()
        {
        }

        public static CargoProfileContributor Instance
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

            ShuttleCargoModuleDef cargoDef = context.ModuleDef as ShuttleCargoModuleDef;
            if (cargoDef == null)
            {
                return;
            }

            context.Contributions.AddCargoRegionCount(cargoDef.EffectiveAdditionalCargoRegionCount);
        }
    }
}
