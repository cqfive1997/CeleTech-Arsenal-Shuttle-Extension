using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Owns the narrow holder-to-cockpit transaction used when an intended passenger
    /// naturally completes a shuttle-module activity. The cockpit is logically distinct
    /// from cargo while sharing the vanilla transporter holder.
    /// </summary>
    internal sealed class VanillaPassengerInternalTransferAdapter
    {
        private const float MassEpsilon = 0.001f;

        private readonly VanillaTransporterAdapter transporterAdapter;

        internal VanillaPassengerInternalTransferAdapter(
            VanillaTransporterAdapter transporterAdapter)
        {
            this.transporterAdapter = transporterAdapter;
        }

        internal ShuttlePassengerInternalTransferResult TryTransfer(
            ThingWithComps host,
            Pawn pawn,
            ThingOwner<Thing> source,
            out string failureReason)
        {
            failureReason = null;
            if (host == null ||
                host.Destroyed ||
                pawn == null ||
                pawn.Destroyed ||
                pawn.Dead ||
                source == null ||
                !source.Contains(pawn) ||
                pawn.holdingOwner != source)
            {
                failureReason = "Passenger or source holder is no longer eligible for internal transfer.";
                return ShuttlePassengerInternalTransferResult.FallbackToMap;
            }

            List<CompTransporter> transporters =
                this.transporterAdapter.ResolveTransportersForLaunch(host);
            CompTransporter destination = FindDestination(transporters);
            if (destination == null)
            {
                failureReason = "No cockpit backing holder is available for internal transfer.";
                return ShuttlePassengerInternalTransferResult.FallbackToMap;
            }

            float pawnMass = CargoDisplayUtility.GetThingMass(pawn, 1);
            float existingMass = this.transporterAdapter.ExistingMassUsage(transporters);
            float pendingMass = GetPendingMass(transporters);
            float massCapacity = this.transporterAdapter.MassCapacity(transporters);
            if (!IsValidMass(pawnMass) ||
                !IsValidMass(existingMass) ||
                !IsValidMass(pendingMass) ||
                !IsValidMass(massCapacity))
            {
                failureReason = "The transporter mass state is invalid.";
                return ShuttlePassengerInternalTransferResult.FallbackToMap;
            }

            float plannedMass = existingMass + pendingMass + pawnMass;
            if (plannedMass > massCapacity + MassEpsilon)
            {
                failureReason = "The cockpit backing holder has no reserved mass for this passenger.";
                return ShuttlePassengerInternalTransferResult.FallbackToMap;
            }

            bool moved = destination.innerContainer.TryAddOrTransfer(pawn, false);
            bool inSource = source.Contains(pawn);
            bool inDestination = destination.innerContainer.Contains(pawn);
            if (moved && inDestination && !inSource)
            {
                destination.Notify_ThingAdded(pawn);
                return ShuttlePassengerInternalTransferResult.Transferred;
            }

            if (!inSource && inDestination)
            {
                destination.Notify_ThingAdded(pawn);
                return ShuttlePassengerInternalTransferResult.Transferred;
            }

            if (inSource && !inDestination)
            {
                failureReason = "The cockpit backing holder rejected the passenger.";
                return ShuttlePassengerInternalTransferResult.FallbackToMap;
            }

            if (inDestination && source.TryAddOrTransfer(pawn, false))
            {
                if (source.Contains(pawn) && !destination.innerContainer.Contains(pawn))
                {
                    failureReason = "Internal transfer failed its postcondition and was rolled back.";
                    return ShuttlePassengerInternalTransferResult.FallbackToMap;
                }
            }
            else if (!inSource &&
                !inDestination &&
                source.TryAddOrTransfer(pawn, false) &&
                source.Contains(pawn) &&
                !destination.innerContainer.Contains(pawn))
            {
                failureReason = "Internal transfer lost its destination postcondition and was recovered to the source holder.";
                return ShuttlePassengerInternalTransferResult.FallbackToMap;
            }

            inSource = source.Contains(pawn);
            inDestination = destination.innerContainer.Contains(pawn);
            if (inSource && !inDestination)
            {
                failureReason = "Internal transfer failed its postcondition and was rolled back.";
                return ShuttlePassengerInternalTransferResult.FallbackToMap;
            }

            if (!inSource && inDestination)
            {
                destination.Notify_ThingAdded(pawn);
                return ShuttlePassengerInternalTransferResult.Transferred;
            }

            failureReason = "Internal transfer could not prove a single final pawn owner.";
            return ShuttlePassengerInternalTransferResult.UnsafeOwnerState;
        }

        private static bool IsValidMass(float value)
        {
            return !float.IsNaN(value) &&
                !float.IsInfinity(value) &&
                value >= 0f;
        }

        private static CompTransporter FindDestination(List<CompTransporter> transporters)
        {
            if (transporters == null)
            {
                return null;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null && transporter.innerContainer != null)
                {
                    return transporter;
                }
            }

            return null;
        }

        private static float GetPendingMass(List<CompTransporter> transporters)
        {
            float mass = 0f;
            if (transporters == null)
            {
                return mass;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter == null || transporter.leftToLoad == null)
                {
                    continue;
                }

                for (int j = 0; j < transporter.leftToLoad.Count; j++)
                {
                    TransferableOneWay transferable = transporter.leftToLoad[j];
                    if (transferable == null ||
                        transferable.CountToTransfer <= 0 ||
                        transferable.AnyThing == null)
                    {
                        continue;
                    }

                    mass += CargoDisplayUtility.GetThingMass(
                        transferable.AnyThing,
                        transferable.CountToTransfer);
                }
            }

            return mass;
        }
    }
}
