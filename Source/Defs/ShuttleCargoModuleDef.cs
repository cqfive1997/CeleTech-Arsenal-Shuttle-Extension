using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    public sealed class ShuttleCargoModuleDef : ShuttleModuleBaseDef
    {
        // Cargo modules increase how many visual cargo regions the shuttle can present.
        // They do not directly increase total cargo mass capacity or create extra containers.
        public uint additionalCargoRegionCount;

        // Backward-compatible XML alias for early test defs. New content should use
        // additionalCargoRegionCount so it is not confused with CompTransporter grouping.
        public uint additionalCargoGroupCount;

        public uint EffectiveAdditionalCargoRegionCount
        {
            get
            {
                if (this.additionalCargoRegionCount > 0)
                {
                    return this.additionalCargoRegionCount;
                }

                return this.additionalCargoGroupCount;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var error in base.ConfigErrors())
            {
                yield return error;
            }
        }
    }
} // CeleTech.ShuttleExtension.ModularShuttle.Defs
