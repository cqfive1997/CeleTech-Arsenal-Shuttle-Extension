using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    public sealed class FireControlProfileContributor : IShuttleProfileContributor
    {
        private static readonly FireControlProfileContributor instance = new FireControlProfileContributor();

        private FireControlProfileContributor()
        {
        }

        public static FireControlProfileContributor Instance
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

            ShuttleFireControlRadarModuleDef fireControlDef =
                context.ModuleDef as ShuttleFireControlRadarModuleDef;
            if (fireControlDef == null)
            {
                return;
            }

            if (context.Module != null && !context.Module.IsEnabled)
            {
                return;
            }

            IShuttleFireControlProfileContributionSink fireControlSink =
                context.Contributions as IShuttleFireControlProfileContributionSink;
            if (fireControlSink == null)
            {
                return;
            }

            fireControlSink.AddFireControlRadarModule(
                fireControlDef.supportsAutoDefense,
                fireControlDef.supportsPointDefense,
                fireControlDef.pointDefenseRadius,
                fireControlDef.directFireAccuracyMultiplier,
                fireControlDef.directFireAccuracyBonus,
                fireControlDef.directFireAccuracyFloor,
                fireControlDef.forcedMissRadiusMultiplier);
        }
    }
}
