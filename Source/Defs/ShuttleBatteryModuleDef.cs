using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static battery module contribution. It defines capacity and charge/discharge limits for
    /// profile/runtime power systems, but does not store current charge.
    /// </summary>
    public sealed class ShuttleBatteryModuleDef : ShuttleModuleBaseDef
    {
        // Installed battery capacity in watt-days. Runtime current charge lives in RuntimeState.
        public float energyStorageCapacityWd;

        // Maximum internal bus charge rate. A non-positive value falls back to full capacity.
        public float maxChargeWatts;

        // Maximum internal bus discharge rate. A non-positive value falls back to full capacity.
        public float maxDischargeWatts;

        public float EffectiveEnergyStorageCapacityWd
        {
            get
            {
                if (this.energyStorageCapacityWd > 0f)
                {
                    return this.energyStorageCapacityWd;
                }

                return 0f;
            }
        }

        public float EffectiveMaxChargeWatts
        {
            get
            {
                if (this.maxChargeWatts > 0f)
                {
                    return this.maxChargeWatts;
                }

                return this.EffectiveEnergyStorageCapacityWd;
            }
        }

        public float EffectiveMaxDischargeWatts
        {
            get
            {
                if (this.maxDischargeWatts > 0f)
                {
                    return this.maxDischargeWatts;
                }

                return this.EffectiveEnergyStorageCapacityWd;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var error in base.ConfigErrors())
            {
                yield return error;
            }

            if (!this.IsFiniteFloat(this.energyStorageCapacityWd))
            {
                yield return this.defName + " has non-finite energyStorageCapacityWd.";
            }
            else if (this.energyStorageCapacityWd < 0f)
            {
                yield return this.defName + " has negative energyStorageCapacityWd.";
            }

            if (!this.IsFiniteFloat(this.maxChargeWatts))
            {
                yield return this.defName + " has non-finite maxChargeWatts.";
            }
            else if (this.maxChargeWatts < 0f)
            {
                yield return this.defName + " has negative maxChargeWatts.";
            }

            if (!this.IsFiniteFloat(this.maxDischargeWatts))
            {
                yield return this.defName + " has non-finite maxDischargeWatts.";
            }
            else if (this.maxDischargeWatts < 0f)
            {
                yield return this.defName + " has negative maxDischargeWatts.";
            }
        }
    }
} // CeleTech.ShuttleExtension.ModularShuttle.Defs
