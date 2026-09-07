using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static reactor module contribution. It feeds the internal shuttle power system and may
    /// expose surplus through the power-plant bridge, but it is not runtime energy storage.
    /// </summary>
    public sealed class ShuttleReactorModuleDef : ShuttleModuleBaseDef
    {
        // Internal shuttle generation before grid export is considered.
        public float reactorGenerationWatts;

        // Maximum surplus that the bridge may expose to RimWorld PowerNet.
        // A non-positive value falls back to reactorGenerationWatts.
        public float gridExportCapacityWatts;

        public float EffectiveReactorGenerationWatts
        {
            get
            {
                if (this.reactorGenerationWatts > 0f)
                {
                    return this.reactorGenerationWatts;
                }

                return 0f;
            }
        }

        public float EffectiveGridExportCapacityWatts
        {
            get
            {
                if (this.gridExportCapacityWatts > 0f)
                {
                    return this.gridExportCapacityWatts;
                }

                return this.EffectiveReactorGenerationWatts;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var error in base.ConfigErrors())
            {
                yield return error;
            }

            if (!this.IsFiniteFloat(this.reactorGenerationWatts))
            {
                yield return this.defName + " has non-finite reactorGenerationWatts.";
            }
            else if (this.reactorGenerationWatts < 0f)
            {
                yield return this.defName + " has negative reactorGenerationWatts.";
            }

            if (!this.IsFiniteFloat(this.gridExportCapacityWatts))
            {
                yield return this.defName + " has non-finite gridExportCapacityWatts.";
            }
            else if (this.gridExportCapacityWatts < 0f)
            {
                yield return this.defName + " has negative gridExportCapacityWatts.";
            }
        }
    }
} // CeleTech.ShuttleExtension.ModularShuttle.Defs
