using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    public sealed class ShuttleDriveControlModuleDef : ShuttleModuleBaseDef
    {
        public float energyCostFactor = 1f;
        public float loadedMassEfficiencyFactor = 1f;

        public float EffectiveEnergyCostFactor
        {
            get
            {
                if (this.energyCostFactor <= 0f || this.energyCostFactor > 1f)
                {
                    return 1f;
                }

                return this.energyCostFactor;
            }
        }

        public float EffectiveLoadedMassEfficiencyFactor
        {
            get
            {
                if (this.loadedMassEfficiencyFactor <= 0f || this.loadedMassEfficiencyFactor > 1f)
                {
                    return 1f;
                }

                return this.loadedMassEfficiencyFactor;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (!this.IsFiniteFloat(this.energyCostFactor))
            {
                yield return this.defName + " has non-finite energyCostFactor.";
            }
            else if (this.energyCostFactor <= 0f)
            {
                yield return this.defName + " energyCostFactor must be greater than 0.";
            }

            if (this.IsFiniteFloat(this.energyCostFactor) && this.energyCostFactor > 1f)
            {
                yield return this.defName + " energyCostFactor must not be greater than 1.";
            }

            if (this.energyCostFactor > 0f && this.energyCostFactor < 0.25f)
            {
                yield return this.defName + " energyCostFactor is very strong; verify balance.";
            }

            if (!this.IsFiniteFloat(this.loadedMassEfficiencyFactor))
            {
                yield return this.defName + " has non-finite loadedMassEfficiencyFactor.";
            }
            else if (this.loadedMassEfficiencyFactor <= 0f)
            {
                yield return this.defName + " loadedMassEfficiencyFactor must be greater than 0.";
            }

            if (this.IsFiniteFloat(this.loadedMassEfficiencyFactor) && this.loadedMassEfficiencyFactor > 1f)
            {
                yield return this.defName + " loadedMassEfficiencyFactor must not be greater than 1.";
            }

            if (this.loadedMassEfficiencyFactor > 0f && this.loadedMassEfficiencyFactor < 0.25f)
            {
                yield return this.defName + " loadedMassEfficiencyFactor is very strong; verify balance.";
            }
        }
    }
}
