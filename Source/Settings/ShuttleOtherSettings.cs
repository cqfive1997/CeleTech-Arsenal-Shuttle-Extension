using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle
{
    internal sealed class ShuttleOtherSettings : IExposable
    {
        internal const float LegacyDefaultPayloadLiftEnergyPerKgWd = 0.05f;
        internal const float DefaultPayloadCapacityMultiplier = 1f;
        internal const float MinimumPayloadCapacityMultiplier = 0.1f;
        internal const float MaximumPayloadCapacityMultiplier = 10f;
        internal const float DefaultConstructionWorkMultiplier = 1f;
        internal const float MinimumConstructionWorkMultiplier = 0.1f;
        internal const float MaximumConstructionWorkMultiplier = 10f;
        internal const bool DefaultShareRefrigeratedCargoMassWithOverallCapacity = false;
        internal const float DefaultIndependentRefrigeratedCargoCapacityRatio = 0.20f;
        internal const float MinimumIndependentRefrigeratedCargoCapacityRatio = 0.05f;
        internal const float MaximumIndependentRefrigeratedCargoCapacityRatio = 1f;
        internal const bool DefaultAllowColonistOnboardDeviceUseAtPlayerHome = false;
        internal const bool DefaultAllowMechOnboardDeviceUseAtPlayerHome = false;

        public float PayloadCapacityMultiplier = DefaultPayloadCapacityMultiplier;
        public float ConstructionWorkMultiplier = DefaultConstructionWorkMultiplier;
        public bool ShareRefrigeratedCargoMassWithOverallCapacity =
            DefaultShareRefrigeratedCargoMassWithOverallCapacity;
        public float IndependentRefrigeratedCargoCapacityRatio =
            DefaultIndependentRefrigeratedCargoCapacityRatio;
        public bool AllowColonistOnboardDeviceUseAtPlayerHome =
            DefaultAllowColonistOnboardDeviceUseAtPlayerHome;
        public bool AllowMechOnboardDeviceUseAtPlayerHome =
            DefaultAllowMechOnboardDeviceUseAtPlayerHome;

        internal float LegacyPayloadLiftEnergyPerKgWd =
            LegacyDefaultPayloadLiftEnergyPerKgWd;
        internal float LegacyPayloadLiftEnergyMultiplier =
            DefaultPayloadCapacityMultiplier;

        internal void ResetToDefaults()
        {
            this.PayloadCapacityMultiplier = DefaultPayloadCapacityMultiplier;
            this.ConstructionWorkMultiplier = DefaultConstructionWorkMultiplier;
            this.ShareRefrigeratedCargoMassWithOverallCapacity =
                DefaultShareRefrigeratedCargoMassWithOverallCapacity;
            this.IndependentRefrigeratedCargoCapacityRatio =
                DefaultIndependentRefrigeratedCargoCapacityRatio;
            this.AllowColonistOnboardDeviceUseAtPlayerHome =
                DefaultAllowColonistOnboardDeviceUseAtPlayerHome;
            this.AllowMechOnboardDeviceUseAtPlayerHome =
                DefaultAllowMechOnboardDeviceUseAtPlayerHome;
        }

        internal void Sanitize()
        {
            if (float.IsNaN(this.PayloadCapacityMultiplier) ||
                float.IsInfinity(this.PayloadCapacityMultiplier))
            {
                this.PayloadCapacityMultiplier = DefaultPayloadCapacityMultiplier;
            }

            if (float.IsNaN(this.ConstructionWorkMultiplier) ||
                float.IsInfinity(this.ConstructionWorkMultiplier))
            {
                this.ConstructionWorkMultiplier = DefaultConstructionWorkMultiplier;
            }

            if (float.IsNaN(this.IndependentRefrigeratedCargoCapacityRatio) ||
                float.IsInfinity(this.IndependentRefrigeratedCargoCapacityRatio))
            {
                this.IndependentRefrigeratedCargoCapacityRatio =
                    DefaultIndependentRefrigeratedCargoCapacityRatio;
            }

            this.PayloadCapacityMultiplier = Mathf.Clamp(
                this.PayloadCapacityMultiplier,
                MinimumPayloadCapacityMultiplier,
                MaximumPayloadCapacityMultiplier);
            this.ConstructionWorkMultiplier = Mathf.Clamp(
                this.ConstructionWorkMultiplier,
                MinimumConstructionWorkMultiplier,
                MaximumConstructionWorkMultiplier);
            this.IndependentRefrigeratedCargoCapacityRatio = Mathf.Clamp(
                this.IndependentRefrigeratedCargoCapacityRatio,
                MinimumIndependentRefrigeratedCargoCapacityRatio,
                MaximumIndependentRefrigeratedCargoCapacityRatio);
            this.PayloadCapacityMultiplier =
                Mathf.Round(this.PayloadCapacityMultiplier * 10f) / 10f;
            this.ConstructionWorkMultiplier =
                Mathf.Round(this.ConstructionWorkMultiplier * 10f) / 10f;
            this.IndependentRefrigeratedCargoCapacityRatio =
                Mathf.Round(this.IndependentRefrigeratedCargoCapacityRatio * 100f) / 100f;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref this.PayloadCapacityMultiplier,
                "PayloadCapacityMultiplier",
                DefaultPayloadCapacityMultiplier);
            Scribe_Values.Look(
                ref this.ConstructionWorkMultiplier,
                "ConstructionWorkMultiplier",
                DefaultConstructionWorkMultiplier);
            Scribe_Values.Look(
                ref this.ShareRefrigeratedCargoMassWithOverallCapacity,
                "ShareRefrigeratedCargoMassWithOverallCapacity",
                DefaultShareRefrigeratedCargoMassWithOverallCapacity);
            Scribe_Values.Look(
                ref this.IndependentRefrigeratedCargoCapacityRatio,
                "IndependentRefrigeratedCargoCapacityRatio",
                DefaultIndependentRefrigeratedCargoCapacityRatio);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Scribe_Values.Look(
                    ref this.LegacyPayloadLiftEnergyPerKgWd,
                    "PayloadLiftEnergyPerKgWd",
                    LegacyDefaultPayloadLiftEnergyPerKgWd);
                Scribe_Values.Look(
                    ref this.LegacyPayloadLiftEnergyMultiplier,
                    "PayloadLiftEnergyMultiplier",
                    DefaultPayloadCapacityMultiplier);
            }
            Scribe_Values.Look(
                ref this.AllowColonistOnboardDeviceUseAtPlayerHome,
                "AllowColonistOnboardDeviceUseAtPlayerHome",
                DefaultAllowColonistOnboardDeviceUseAtPlayerHome);
            Scribe_Values.Look(
                ref this.AllowMechOnboardDeviceUseAtPlayerHome,
                "AllowMechOnboardDeviceUseAtPlayerHome",
                DefaultAllowMechOnboardDeviceUseAtPlayerHome);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.Sanitize();
            }
        }

        internal static float ConvertLegacyEnergyMultiplierToCapacity(
            float energyMultiplier)
        {
            if (float.IsNaN(energyMultiplier) ||
                float.IsInfinity(energyMultiplier) ||
                energyMultiplier < 0f)
            {
                return DefaultPayloadCapacityMultiplier;
            }

            if (energyMultiplier <= 0.0001f)
            {
                return MaximumPayloadCapacityMultiplier;
            }

            float capacityMultiplier = 1f / energyMultiplier;
            capacityMultiplier = Mathf.Clamp(
                capacityMultiplier,
                MinimumPayloadCapacityMultiplier,
                MaximumPayloadCapacityMultiplier);
            return Mathf.Round(capacityMultiplier * 10f) / 10f;
        }
    }
}
