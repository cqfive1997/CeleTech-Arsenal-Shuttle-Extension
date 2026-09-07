using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    public sealed class ShuttleCargoSegmentDef : ShuttleSegmentBaseDef
    {
        // Characteristic

        // Error report
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var error in base.ConfigErrors())
            {
                yield return error;
            }
        }
    }
} // CeleTech.ShuttleExtension.ModularShuttle.Defs