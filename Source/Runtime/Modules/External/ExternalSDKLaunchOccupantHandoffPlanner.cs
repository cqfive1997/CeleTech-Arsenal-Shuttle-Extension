using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchOccupantHandoffPlanner
    {
        private const float Epsilon = 0.0001f;

        private readonly VanillaTransporterCargoBackend cargoBackend =
            new VanillaTransporterCargoBackend();

        internal bool TryBuildPlan(
            ShuttleController controller,
            ExternalSDKLaunchOccupantHandoffValidatedRequest request,
            out ExternalSDKLaunchOccupantHandoffPlan plan,
            out ShuttleExternalLaunchOccupantHandoffFailureReason failureReason,
            out string message)
        {
            plan = null;
            failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.None;
            message = null;

            if (controller == null || request == null || request.Pawn == null)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.InvalidRequest;
                message = "handoff planner input is unavailable";
                return false;
            }

            ThingWithComps host = controller.ShuttleHost;
            if (host == null || host.Destroyed)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.HostUnavailable;
                message = "shuttle host is unavailable";
                return false;
            }

            if (this.cargoBackend.HasQueuedLoads(host))
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.QueuedLoadActive;
                message = "normal cargo loading queue is active";
                return false;
            }

            ShuttleExternalOccupantInfo existingOccupant;
            if (this.TryFindExistingShuttleOccupant(host, request.Pawn, out existingOccupant))
            {
                if (existingOccupant != null && existingOccupant.IsLoadedCargo)
                {
                    failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.OccupantAlreadyLoaded;
                    message = "pawn is already loaded in shuttle cargo";
                    return false;
                }

                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.OccupantOwnedByInternalHolder;
                message = "pawn is already owned by an internal shuttle holder";
                return false;
            }

            if (request.Pawn.holdingOwner == null)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.OccupantNotHeld;
                message = "pawn is no longer held by a source holder";
                return false;
            }

            if (request.SourceOwner != null && request.Pawn.holdingOwner != request.SourceOwner)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.OccupantUnavailable;
                message = "pawn source holder changed before handoff";
                return false;
            }

            List<CompTransporter> transporters =
                this.cargoBackend.ResolveTransportersForLaunch(host);
            if (transporters == null || transporters.Count == 0)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.CargoUnavailable;
                message = "normal cargo transporters are unavailable";
                return false;
            }

            float affectedMassKg = CargoDisplayUtility.GetThingMass(request.Pawn, 1);
            if (affectedMassKg < 0f)
            {
                affectedMassKg = 0f;
            }

            float availableMassKg = this.cargoBackend.GetAvailableMass(host);
            if (affectedMassKg > availableMassKg + Epsilon)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.InsufficientCargoCapacity;
                message = "normal cargo mass capacity is insufficient";
                return false;
            }

            CompTransporter targetTransporter;
            ThingOwner targetContents;
            if (!this.TryResolveTarget(
                    transporters,
                    request,
                    affectedMassKg,
                    out targetTransporter,
                    out targetContents))
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.HandoffTargetUnavailable;
                message = "no normal cargo transporter can accept the pawn";
                return false;
            }

            plan = new ExternalSDKLaunchOccupantHandoffPlan(
                request,
                targetTransporter,
                targetContents,
                affectedMassKg);
            return true;
        }

        private bool TryResolveTarget(
            List<CompTransporter> transporters,
            ExternalSDKLaunchOccupantHandoffValidatedRequest request,
            float affectedMassKg,
            out CompTransporter targetTransporter,
            out ThingOwner targetContents)
        {
            targetTransporter = null;
            targetContents = null;

            for (int i = 0; transporters != null && i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter == null)
                {
                    continue;
                }

                ThingOwner contents = transporter.GetDirectlyHeldThings();
                if (contents == null)
                {
                    continue;
                }

                if (request != null && contents == request.SourceOwner)
                {
                    continue;
                }

                float availableMass = transporter.MassCapacity - transporter.MassUsage;
                if (availableMass + Epsilon < affectedMassKg)
                {
                    continue;
                }

                targetTransporter = transporter;
                targetContents = contents;
                return true;
            }

            return false;
        }

        private bool TryFindExistingShuttleOccupant(
            ThingWithComps host,
            Pawn pawn,
            out ShuttleExternalOccupantInfo occupant)
        {
            occupant = null;
            if (host == null || pawn == null)
            {
                return false;
            }

            ShuttleExternalOccupantReadPort readPort =
                new ShuttleExternalOccupantReadPort(host);
            IReadOnlyList<ShuttleExternalOccupantInfo> occupants =
                readPort.GetOccupants(new ShuttleExternalOccupantQuery
                {
                    RoleMask = ShuttleExternalOccupantRole.Any,
                    HumanlikeOnly = false,
                    ServiceableOnly = false
                });

            int pawnId = pawn.thingIDNumber;
            for (int i = 0; occupants != null && i < occupants.Count; i++)
            {
                ShuttleExternalOccupantInfo candidate = occupants[i];
                if (candidate != null && candidate.ThingIdNumber == pawnId)
                {
                    occupant = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
