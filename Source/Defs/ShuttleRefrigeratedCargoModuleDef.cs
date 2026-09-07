using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    public sealed class ShuttleRefrigeratedCargoModuleDef : ShuttleModuleBaseDef
    {
        public float targetTemperatureC = -10f;
        public float coldCargoMassCapacityKg = 100f;
        public bool requirePoweredCooling = true;
        public float inactiveTemperatureFallbackC = 14f;
        public bool blockLaunchIfColdCargoCannotBeTransferred = true;
        public bool blockRemovalWhenColdCargoNotEmpty = true;
        public bool autoTransferEnabledByDefault;
        public int autoTransferIntervalTicks = 600;
        public int maxStacksMovedPerInterval = 1;
        public bool autoTransferOnlyWhenCoolingActive = true;
        public bool requiresCargoLogisticsForAutoTransfer = true;
        public bool autoTransferRottableItems = true;
        public bool allowCorpses;
        public bool allowPartialStackTransfer;

        // Automatic transfer candidate filter only. Manual transfer intentionally
        // ignores this until a separate coldCargoAcceptanceFilter is introduced.
        public ThingFilter autoTransferFilter;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (!this.IsFiniteFloat(this.targetTemperatureC))
            {
                yield return this.defName + " has non-finite targetTemperatureC.";
            }

            if (!this.IsFiniteFloat(this.coldCargoMassCapacityKg))
            {
                yield return this.defName + " has non-finite coldCargoMassCapacityKg.";
            }
            else if (this.coldCargoMassCapacityKg < 0f)
            {
                yield return this.defName + " has negative coldCargoMassCapacityKg.";
            }

            if (!this.IsFiniteFloat(this.inactiveTemperatureFallbackC))
            {
                yield return this.defName + " has non-finite inactiveTemperatureFallbackC.";
            }

            if (this.autoTransferIntervalTicks < 0)
            {
                yield return this.defName + " has negative autoTransferIntervalTicks.";
            }

            if (this.autoTransferEnabledByDefault && this.autoTransferIntervalTicks <= 0)
            {
                yield return this.defName +
                    " enables refrigerated cargo auto-transfer by default but autoTransferIntervalTicks is not positive.";
            }

            if (this.maxStacksMovedPerInterval < 0)
            {
                yield return this.defName + " has negative maxStacksMovedPerInterval.";
            }
        }
    }
}
