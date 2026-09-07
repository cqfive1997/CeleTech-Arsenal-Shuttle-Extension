using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static capability module that enables runtime cargo-resource broker access.
    /// It does not store item counts, reservations, bills, or live transfer state.
    /// </summary>
    public sealed class ShuttleCargoLogisticsModuleDef : ShuttleModuleBaseDef
    {
        public bool enablesCargoResourceBroker = true;
        public bool supportsItemTransfer = true;
        public bool supportsItemConsumption = true;
        public bool supportsItemDeposit = true;
        public bool supportsNutritionDistribution;
        public int maxStacksMovedPerTick = 1;
        public float transferEfficiency = 1f;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.maxStacksMovedPerTick < 0)
            {
                yield return this.defName + " has negative maxStacksMovedPerTick.";
            }

            if (!this.IsFiniteFloat(this.transferEfficiency))
            {
                yield return this.defName + " has non-finite transferEfficiency.";
            }
            else if (this.transferEfficiency < 0f)
            {
                yield return this.defName + " has negative transferEfficiency.";
            }
        }
    }
}
