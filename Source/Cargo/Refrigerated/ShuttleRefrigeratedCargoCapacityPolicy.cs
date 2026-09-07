using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    internal enum ShuttleCargoCapacityFailure
    {
        None,
        PositiveBaseCapacityRequired,
        BaseCargoExceeded,
        RefrigeratedCargoExceeded
    }

    /// <summary>
    /// Read-only capacity policy over global settings, profile capacity, and cold-holder
    /// snapshots. It does not own cargo and must never mutate transporter or cold holders.
    /// </summary>
    internal static class ShuttleRefrigeratedCargoCapacityPolicy
    {
        private const float MassEpsilon = 0.0001f;

        internal static bool SharesOverallCapacity
        {
            get
            {
                ShuttleOtherSettings settings = CeleTechShuttleMod.Settings != null
                    ? CeleTechShuttleMod.Settings.Other
                    : null;
                return settings != null
                    ? settings.ShareRefrigeratedCargoMassWithOverallCapacity
                    : ShuttleOtherSettings.DefaultShareRefrigeratedCargoMassWithOverallCapacity;
            }
        }

        internal static float IndependentCapacityRatio
        {
            get
            {
                ShuttleOtherSettings settings = CeleTechShuttleMod.Settings != null
                    ? CeleTechShuttleMod.Settings.Other
                    : null;
                float ratio = settings != null
                    ? settings.IndependentRefrigeratedCargoCapacityRatio
                    : ShuttleOtherSettings.DefaultIndependentRefrigeratedCargoCapacityRatio;
                if (float.IsNaN(ratio) || float.IsInfinity(ratio))
                {
                    ratio = ShuttleOtherSettings.DefaultIndependentRefrigeratedCargoCapacityRatio;
                }

                return Mathf.Clamp(
                    ratio,
                    ShuttleOtherSettings.MinimumIndependentRefrigeratedCargoCapacityRatio,
                    ShuttleOtherSettings.MaximumIndependentRefrigeratedCargoCapacityRatio);
            }
        }

        internal static float GetBaseCargoCapacityKg(ShuttleProfile profile)
        {
            float capacityKg = profile != null && profile.Cargo != null
                ? profile.Cargo.CargoMassCapacityKg
                : 0f;
            return SanitizeMass(capacityKg);
        }

        internal static float GetRefrigeratedPoolCapacityKg(ShuttleProfile profile)
        {
            float baseCapacityKg = GetBaseCargoCapacityKg(profile);
            return SharesOverallCapacity
                ? baseCapacityKg
                : baseCapacityKg * IndependentCapacityRatio;
        }

        internal static float GetEffectiveTotalCapacityKg(
            ShuttleProfile profile,
            bool hasRefrigeratedCapacity)
        {
            float baseCapacityKg = GetBaseCargoCapacityKg(profile);
            if (!hasRefrigeratedCapacity || SharesOverallCapacity)
            {
                return baseCapacityKg;
            }

            return baseCapacityKg + GetRefrigeratedPoolCapacityKg(profile);
        }

        internal static float GetTotalColdMassKg(
            CompShuttleRefrigeratedCargoRegistry registry)
        {
            float totalMassKg = 0f;
            if (registry == null || registry.Records == null)
            {
                return totalMassKg;
            }

            for (int i = 0; i < registry.Records.Count; i++)
            {
                RefrigeratedCargoRecord record = registry.Records[i];
                if (record != null)
                {
                    totalMassKg += SanitizeMass(record.StoredMassKg);
                }
            }

            return totalMassKg;
        }

        internal static float GetAvailableColdMassKg(
            ShuttleProfile profile,
            CompShuttleRefrigeratedCargoRegistry registry,
            float nonColdCommittedMassKg)
        {
            float capacityKg = GetRefrigeratedPoolCapacityKg(profile);
            float usedMassKg = GetTotalColdMassKg(registry);
            if (SharesOverallCapacity)
            {
                usedMassKg += SanitizeMass(nonColdCommittedMassKg);
            }

            return Mathf.Max(0f, capacityKg - usedMassKg);
        }

        internal static bool CanAddColdMass(
            ShuttleProfile profile,
            CompShuttleRefrigeratedCargoRegistry registry,
            float nonColdCommittedMassKg,
            float addedMassKg,
            out float capacityKg,
            out float projectedUsedMassKg)
        {
            capacityKg = GetRefrigeratedPoolCapacityKg(profile);
            projectedUsedMassKg = GetTotalColdMassKg(registry) +
                SanitizeMass(addedMassKg);
            if (SharesOverallCapacity)
            {
                projectedUsedMassKg += SanitizeMass(nonColdCommittedMassKg);
            }

            return projectedUsedMassKg <= capacityKg + MassEpsilon;
        }

        internal static ShuttleCargoCapacityFailure ValidateLoadedCargo(
            ShuttleProfile profile,
            ShuttleCargoSnapshot snapshot)
        {
            return ValidateCargo(profile, snapshot, false);
        }

        internal static ShuttleCargoCapacityFailure ValidatePlannedCargo(
            ShuttleProfile profile,
            ShuttleCargoSnapshot snapshot)
        {
            return ValidateCargo(profile, snapshot, true);
        }

        private static ShuttleCargoCapacityFailure ValidateCargo(
            ShuttleProfile profile,
            ShuttleCargoSnapshot snapshot,
            bool includeQueuedMass)
        {
            if (snapshot == null)
            {
                return ShuttleCargoCapacityFailure.None;
            }

            float baseCapacityKg = GetBaseCargoCapacityKg(profile);
            float coldMassKg = SanitizeMass(snapshot.RefrigeratedMassKg);
            float nonColdMassKg = SanitizeMass(snapshot.LoadedMassKg) +
                SanitizeMass(snapshot.MedicalBayPatientMassKg) +
                SanitizeMass(snapshot.ExternalRuntimeMassKg);
            if (includeQueuedMass)
            {
                nonColdMassKg += SanitizeMass(snapshot.QueuedMassKg);
            }
            if (baseCapacityKg <= MassEpsilon &&
                nonColdMassKg + coldMassKg > MassEpsilon)
            {
                return ShuttleCargoCapacityFailure.PositiveBaseCapacityRequired;
            }

            if (SharesOverallCapacity)
            {
                return nonColdMassKg + coldMassKg > baseCapacityKg + MassEpsilon
                    ? ShuttleCargoCapacityFailure.BaseCargoExceeded
                    : ShuttleCargoCapacityFailure.None;
            }

            if (nonColdMassKg > baseCapacityKg + MassEpsilon)
            {
                return ShuttleCargoCapacityFailure.BaseCargoExceeded;
            }

            return coldMassKg > GetRefrigeratedPoolCapacityKg(profile) + MassEpsilon
                ? ShuttleCargoCapacityFailure.RefrigeratedCargoExceeded
                : ShuttleCargoCapacityFailure.None;
        }

        private static float SanitizeMass(float massKg)
        {
            return !float.IsNaN(massKg) &&
                !float.IsInfinity(massKg) &&
                massKg > 0f
                    ? massKg
                    : 0f;
        }
    }
}
