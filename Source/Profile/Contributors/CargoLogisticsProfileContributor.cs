using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Static cargo-logistics capability contribution. Runtime item movement stays broker-owned.
    /// </summary>
    public sealed class CargoLogisticsProfileContributor : IShuttleProfileContributor
    {
        private static readonly CargoLogisticsProfileContributor instance = new CargoLogisticsProfileContributor();

        private CargoLogisticsProfileContributor()
        {
        }

        public static CargoLogisticsProfileContributor Instance
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

            ShuttleCargoLogisticsModuleDef logisticsDef = context.ModuleDef as ShuttleCargoLogisticsModuleDef;
            if (logisticsDef == null || !logisticsDef.enablesCargoResourceBroker)
            {
                return;
            }

            IShuttleCargoLogisticsProfileContributionSink logisticsSink =
                context.Contributions as IShuttleCargoLogisticsProfileContributionSink;
            if (logisticsSink == null)
            {
                return;
            }

            logisticsSink.AddCargoLogisticsModule(
                logisticsDef.supportsItemTransfer,
                logisticsDef.supportsItemConsumption,
                logisticsDef.supportsItemDeposit,
                logisticsDef.supportsNutritionDistribution,
                logisticsDef.maxStacksMovedPerTick);
        }
    }
}
