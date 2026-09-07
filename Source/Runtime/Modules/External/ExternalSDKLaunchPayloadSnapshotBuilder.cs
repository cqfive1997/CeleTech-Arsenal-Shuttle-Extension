using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchPayloadSnapshotBuilder
    {
        internal ShuttleExternalLaunchPayloadSnapshot Build(
            ShuttleProfile profile,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            float structuralMassKg = this.GetStructuralMassKg(profile, cargoSnapshot);
            float loadedNormalMassKg = cargoSnapshot != null ? cargoSnapshot.LoadedMassKg : 0f;
            float queuedMassKg = cargoSnapshot != null ? cargoSnapshot.QueuedMassKg : 0f;
            float refrigeratedMassKg = cargoSnapshot != null ? cargoSnapshot.RefrigeratedMassKg : 0f;
            float medicalBayPatientMassKg =
                cargoSnapshot != null ? cargoSnapshot.MedicalBayPatientMassKg : 0f;
            float externalRuntimeMassKg =
                cargoSnapshot != null ? cargoSnapshot.ExternalRuntimeMassKg : 0f;
            float payloadMassKg =
                loadedNormalMassKg +
                queuedMassKg +
                refrigeratedMassKg +
                medicalBayPatientMassKg +
                externalRuntimeMassKg;
            bool hasRefrigeratedCapacity = cargoSnapshot != null &&
                (cargoSnapshot.RefrigeratedMassCapacityKg > 0f ||
                    cargoSnapshot.RefrigeratedMassKg > 0f);
            float cargoCapacityKg =
                ShuttleRefrigeratedCargoCapacityPolicy.GetEffectiveTotalCapacityKg(
                    profile,
                    hasRefrigeratedCapacity);
            bool exceedsCargoCapacity =
                ShuttleRefrigeratedCargoCapacityPolicy.ValidatePlannedCargo(
                    profile,
                    cargoSnapshot) != ShuttleCargoCapacityFailure.None;

            return new ShuttleExternalLaunchPayloadSnapshot(
                structuralMassKg,
                loadedNormalMassKg,
                queuedMassKg,
                refrigeratedMassKg,
                medicalBayPatientMassKg,
                externalRuntimeMassKg,
                payloadMassKg,
                structuralMassKg + payloadMassKg,
                cargoCapacityKg,
                exceedsCargoCapacity,
                cargoSnapshot != null ? cargoSnapshot.LoadedStackCount : 0,
                cargoSnapshot != null ? cargoSnapshot.AssignedStackCount : 0);
        }

        internal static ShuttleExternalLaunchPayloadSnapshot Unavailable()
        {
            return new ShuttleExternalLaunchPayloadSnapshot(
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                0,
                0);
        }

        private float GetStructuralMassKg(
            ShuttleProfile profile,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            if (profile != null && profile.Mass != null)
            {
                return profile.Mass.TotalMass;
            }

            return cargoSnapshot != null ? cargoSnapshot.StructuralMass : 0f;
        }
    }
}
