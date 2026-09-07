using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    public sealed class ShuttlePowerRegulatorModuleDef : ShuttleModuleBaseDef
    {
        public float chargeRateBonusWatts;
        public float chargeEfficiencyFactor = 1f;
        public float dischargeRateBonusWatts;

        public float EffectiveChargeRateBonusWatts
        {
            get
            {
                if (this.chargeRateBonusWatts <= 0f || this.chargeEfficiencyFactor <= 0f)
                {
                    return 0f;
                }

                return this.chargeRateBonusWatts * this.chargeEfficiencyFactor;
            }
        }

        public float EffectiveDischargeRateBonusWatts
        {
            get
            {
                return this.dischargeRateBonusWatts > 0f ? this.dischargeRateBonusWatts : 0f;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (!this.IsFiniteFloat(this.chargeRateBonusWatts))
            {
                yield return this.defName + " has non-finite chargeRateBonusWatts.";
            }
            else if (this.chargeRateBonusWatts < 0f)
            {
                yield return this.defName + " has negative chargeRateBonusWatts.";
            }

            if (!this.IsFiniteFloat(this.chargeEfficiencyFactor))
            {
                yield return this.defName + " has non-finite chargeEfficiencyFactor.";
            }
            else if (this.chargeEfficiencyFactor <= 0f)
            {
                yield return this.defName + " chargeEfficiencyFactor must be greater than 0.";
            }

            if (!this.IsFiniteFloat(this.dischargeRateBonusWatts))
            {
                yield return this.defName + " has non-finite dischargeRateBonusWatts.";
            }
            else if (this.dischargeRateBonusWatts < 0f)
            {
                yield return this.defName + " has negative dischargeRateBonusWatts.";
            }
        }
    }
}
