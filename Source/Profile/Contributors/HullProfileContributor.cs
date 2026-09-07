using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Static whole-hull plating contribution. Current hull HP and damage resolution
    /// are intentionally out of scope for the static profile layer.
    /// </summary>
    public sealed class HullProfileContributor : IShuttleProfileContributor
    {
        private static readonly HullProfileContributor instance = new HullProfileContributor();

        private HullProfileContributor()
        {
        }

        public static HullProfileContributor Instance
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

            ShuttleHullPlatingModuleDef hullDef = context.ModuleDef as ShuttleHullPlatingModuleDef;
            if (hullDef == null)
            {
                return;
            }

            IShuttleHullProfileContributionSink hullSink =
                context.Contributions as IShuttleHullProfileContributionSink;
            if (hullSink == null)
            {
                return;
            }

            ThingDef selectedStuffDef = context.Module != null
                ? ShuttleHullArmorStuffUtility.ResolveSelectedStuffOrFallback(context.Module.SelectedStuffDefName)
                : null;
            ShuttleHullArmorMaterialProfile materialProfile =
                new ShuttleHullArmorMaterialResolver().Resolve(hullDef, selectedStuffDef);
            if (materialProfile == null)
            {
                return;
            }

            hullSink.AddHullPlatingModule(
                materialProfile.HullHitPointsBonus,
                materialProfile.SharpDamageMultiplier,
                materialProfile.BluntDamageMultiplier,
                materialProfile.HeatDamageMultiplier,
                materialProfile.ExplosionDamageMultiplier,
                materialProfile.EmpDamageMultiplier,
                materialProfile.FlatDamageReduction);
            // TODO H6+: materialProfile.MassFactor is intentionally not applied to MassProfile yet.
        }
    }
}
