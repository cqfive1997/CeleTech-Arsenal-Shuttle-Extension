using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static Medical Bay capability definition. Patient containment, treatment
    /// reservations, medicine sources, and health mutation belong to occupancy and
    /// treatment services, not to the module def.
    /// </summary>
    public sealed class ShuttleMedicalBayModuleDef : ShuttleModuleBaseDef
    {
        public int medicalPatientSlots;
        public bool supportsMedevacPriority;
        public bool supportsStabilization;
        public bool supportsPassiveComfort;
        public float passiveJoyGainFactor = 1f;
        public float passiveJoyCapPct = 0.5f;
        public int comfortThoughtStageIndex;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.medicalPatientSlots < 0)
            {
                yield return this.defName + " has negative medicalPatientSlots.";
            }

            if (!this.supportsPassiveComfort)
            {
                yield break;
            }

            if (!this.IsFinitePositive(this.passiveJoyGainFactor))
            {
                yield return this.defName + " must define finite positive passiveJoyGainFactor.";
            }

            if (!this.IsFinitePositive(this.passiveJoyCapPct) || this.passiveJoyCapPct > 1f)
            {
                yield return this.defName + " must define passiveJoyCapPct in the range (0, 1].";
            }

            if (this.comfortThoughtStageIndex < 0)
            {
                yield return this.defName + " has negative comfortThoughtStageIndex.";
            }
        }

        private bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }
    }
}
