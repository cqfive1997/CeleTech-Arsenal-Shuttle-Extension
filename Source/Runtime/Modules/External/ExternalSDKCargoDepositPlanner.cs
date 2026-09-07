using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoDepositPlanner
    {
        private const float Epsilon = 0.0001f;

        internal bool TryBuildPlan(
            ShuttleController controller,
            ExternalSDKCargoValidatedDepositRequest request,
            out ExternalSDKCargoDepositPlan plan,
            out ShuttleExternalCargoTransactionFailureReason failureReason,
            out string message)
        {
            plan = null;
            failureReason = ShuttleExternalCargoTransactionFailureReason.None;
            message = null;

            if (controller == null || request == null)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidRequest;
                message = "deposit planner input is unavailable";
                return false;
            }

            IShuttleCargoResourceBroker cargoBroker =
                controller.GetCargoResourceBrokerForInternalTransactions();
            IReadOnlyList<ShuttleCargoOrdinaryDepositTargetSnapshot> depositTargets =
                cargoBroker != null
                    ? cargoBroker.GetOrdinaryDepositTargets()
                    : null;
            if (depositTargets == null || depositTargets.Count == 0)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.CargoUnavailable;
                message = "normal cargo transporters are unavailable";
                return false;
            }

            ShuttleCargoRegionConfigState cargoRegionConfig = controller.CargoRegionConfig;
            int activeRegionCount = controller.ActiveCargoRegionCount;
            if (cargoRegionConfig == null || activeRegionCount <= 0)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.CargoUnavailable;
                message = "normal cargo regions are unavailable";
                return false;
            }

            int cargoRegionIndex = this.FindFirstAllowedRegion(
                cargoRegionConfig,
                activeRegionCount,
                request.ThingDef);
            if (cargoRegionIndex < 0)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.CargoFilterRejected;
                message = "requested item is rejected by cargo region filters";
                return false;
            }

            float unitMassKg = this.GetUnitMassKg(request.ThingDef);
            float affectedMassKg = unitMassKg * request.Count;
            if (request.MaxMassKg > 0f &&
                affectedMassKg > request.MaxMassKg + Epsilon)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InsufficientCargoCapacity;
                message = "deposit exceeds maxMassKg";
                return false;
            }

            float availableMassKg = this.GetRegularAvailableMassKg(
                controller,
                depositTargets);
            if (affectedMassKg > availableMassKg + Epsilon)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InsufficientCargoCapacity;
                message = "normal cargo mass capacity is insufficient";
                return false;
            }

            List<ExternalSDKCargoDepositTarget> targets;
            if (!this.TryBuildTargets(
                    depositTargets,
                    request,
                    unitMassKg,
                    out targets,
                    out failureReason,
                    out message))
            {
                return false;
            }

            plan = new ExternalSDKCargoDepositPlan(
                request,
                targets,
                request.Count,
                affectedMassKg,
                cargoRegionIndex);
            return true;
        }

        private bool TryBuildTargets(
            IReadOnlyList<ShuttleCargoOrdinaryDepositTargetSnapshot> depositTargets,
            ExternalSDKCargoValidatedDepositRequest request,
            float unitMassKg,
            out List<ExternalSDKCargoDepositTarget> targets,
            out ShuttleExternalCargoTransactionFailureReason failureReason,
            out string message)
        {
            targets = new List<ExternalSDKCargoDepositTarget>();
            failureReason = ShuttleExternalCargoTransactionFailureReason.None;
            message = null;

            Dictionary<int, float> reservedMassByTransporter =
                new Dictionary<int, float>();
            for (int i = 0; i < depositTargets.Count; i++)
            {
                ShuttleCargoOrdinaryDepositTargetSnapshot target = depositTargets[i];
                if (target != null)
                {
                    reservedMassByTransporter[target.TransporterIndex] = 0f;
                }
            }

            int remaining = request.Count;
            int stackLimit = request.ThingDef.stackLimit;
            while (remaining > 0)
            {
                int stackCount = remaining < stackLimit ? remaining : stackLimit;
                float stackMassKg = unitMassKg * stackCount;

                int transporterIndex;
                if (!this.TryResolveTarget(
                        depositTargets,
                        reservedMassByTransporter,
                        stackMassKg,
                        out transporterIndex))
                {
                    failureReason = ShuttleExternalCargoTransactionFailureReason.DepositTargetUnavailable;
                    message = "no normal cargo transporter can accept the planned stack";
                    return false;
                }

                float reservedMass;
                reservedMassByTransporter.TryGetValue(
                    transporterIndex,
                    out reservedMass);
                reservedMassByTransporter[transporterIndex] =
                    reservedMass + stackMassKg;
                targets.Add(new ExternalSDKCargoDepositTarget(
                    transporterIndex,
                    stackCount,
                    stackMassKg));
                remaining -= stackCount;
            }

            return targets.Count > 0;
        }

        private bool TryResolveTarget(
            IReadOnlyList<ShuttleCargoOrdinaryDepositTargetSnapshot> depositTargets,
            Dictionary<int, float> reservedMassByTransporter,
            float stackMassKg,
            out int targetIndex)
        {
            targetIndex = -1;

            for (int i = 0; depositTargets != null && i < depositTargets.Count; i++)
            {
                ShuttleCargoOrdinaryDepositTargetSnapshot target = depositTargets[i];
                if (target == null)
                {
                    continue;
                }

                float reservedMass;
                if (reservedMassByTransporter == null ||
                    !reservedMassByTransporter.TryGetValue(
                        target.TransporterIndex,
                        out reservedMass))
                {
                    reservedMass = 0f;
                }
                float availableMass = target.AvailableMassKg - reservedMass;
                if (availableMass + Epsilon < stackMassKg)
                {
                    continue;
                }

                targetIndex = target.TransporterIndex;
                return true;
            }

            return false;
        }

        private float GetRegularAvailableMassKg(
            ShuttleController controller,
            IReadOnlyList<ShuttleCargoOrdinaryDepositTargetSnapshot> depositTargets)
        {
            float availableMassKg = 0f;
            for (int i = 0; depositTargets != null && i < depositTargets.Count; i++)
            {
                ShuttleCargoOrdinaryDepositTargetSnapshot target = depositTargets[i];
                if (target != null)
                {
                    availableMassKg += target.AvailableMassKg;
                }
            }

            ShuttleRuntimeMassContributionSnapshot externalMass =
                controller.BuildExternalRuntimeMassContributionSnapshotForRead();
            if (externalMass != null && externalMass.TotalMassKg > 0f)
            {
                availableMassKg -= externalMass.TotalMassKg;
            }

            return availableMassKg > 0f ? availableMassKg : 0f;
        }

        private int FindFirstAllowedRegion(
            ShuttleCargoRegionConfigState cargoRegionConfig,
            int activeRegionCount,
            ThingDef thingDef)
        {
            if (cargoRegionConfig == null || activeRegionCount <= 0 || thingDef == null)
            {
                return -1;
            }

            for (int i = 0; i < activeRegionCount; i++)
            {
                if (this.RegionAllowsThingDef(cargoRegionConfig, i, thingDef))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool RegionAllowsThingDef(
            ShuttleCargoRegionConfigState cargoRegionConfig,
            int regionIndex,
            ThingDef thingDef)
        {
            ShuttleCargoRegionSettings settings =
                cargoRegionConfig.GetRegionForRead(regionIndex);
            ThingFilter filter = settings != null ? settings.FilterForRead : null;
            if (filter == null)
            {
                ThingFilter defaultFilter =
                    ThingFilter.CreateOnlyEverStorableThingFilter();
                return defaultFilter != null && defaultFilter.Allows(thingDef);
            }

            return filter.Allows(thingDef);
        }

        private float GetUnitMassKg(ThingDef thingDef)
        {
            if (thingDef == null)
            {
                return 0f;
            }

            float mass = thingDef.GetStatValueAbstract(StatDefOf.Mass, null);
            return mass > 0f ? mass : 0f;
        }
    }
}
