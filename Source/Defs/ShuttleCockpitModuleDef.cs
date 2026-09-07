using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    public sealed class ShuttleCockpitModuleDef : ShuttleModuleBaseDef
    {
        // Static command capability. Installing a cockpit module makes launch validation eligible
        // once all other launch-readiness gates also pass.
        public bool providesCockpit = true;

        // Cockpit modules provide crew control stations. This is static profile capacity,
        // not live passenger/cargo state.
        public int additionalCrewCapacity;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.additionalCrewCapacity < 0)
            {
                yield return this.defName + " has negative additionalCrewCapacity.";
            }

            if (!this.providesCockpit)
            {
                yield return this.defName + " must provide cockpit capability.";
            }
        }
    }
}
