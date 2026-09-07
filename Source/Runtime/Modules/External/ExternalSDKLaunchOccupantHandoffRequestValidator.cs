using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchOccupantHandoffRequestValidator
    {
        private const int MaxStringLength = 128;

        internal bool TryValidateRequest(
            ShuttleExternalLaunchOccupantHandoffRequest request,
            out ExternalSDKLaunchOccupantHandoffValidatedRequest validated,
            out ShuttleExternalLaunchOccupantHandoffFailureReason failureReason,
            out string message)
        {
            validated = null;
            failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.None;
            message = null;

            if (request == null)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.InvalidRequest;
                message = "handoff request is null";
                return false;
            }

            ShuttleExternalLaunchOccupantHandoffSource source;
            if (!this.TryValidateSource(request.Source, out source, out failureReason, out message))
            {
                return false;
            }

            Pawn pawn = request.Pawn;
            if (pawn == null)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.OccupantUnavailable;
                message = "pawn is null";
                return false;
            }

            if (pawn.Destroyed)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.OccupantDestroyed;
                message = "pawn is destroyed";
                return false;
            }

            if (pawn.Dead)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.OccupantDead;
                message = "dead pawns cannot be handed off for launch control";
                return false;
            }

            if (!pawn.IsColonist)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.OccupantNotColonist;
                message = "handoff supports colony pawns only";
                return false;
            }

            if (pawn.Spawned)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.OccupantSpawned;
                message = "pawn is spawned on a map; use normal boarding instead";
                return false;
            }

            if (pawn.holdingOwner == null)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.OccupantNotHeld;
                message = "pawn is not held by a source holder";
                return false;
            }

            validated = new ExternalSDKLaunchOccupantHandoffValidatedRequest(
                source,
                pawn,
                pawn.holdingOwner);
            return true;
        }

        private bool TryValidateSource(
            ShuttleExternalLaunchOccupantHandoffSource source,
            out ShuttleExternalLaunchOccupantHandoffSource normalized,
            out ShuttleExternalLaunchOccupantHandoffFailureReason failureReason,
            out string message)
        {
            normalized = null;
            failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.None;
            message = null;

            if (source == null)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.InvalidSource;
                message = "handoff source is required";
                return false;
            }

            string ownerPackageId;
            string localRequesterKey;
            string moduleInstanceId;
            string reasonKey;
            if (!this.TryNormalizeRequiredString(
                    source.OwnerPackageId,
                    "ownerPackageId",
                    out ownerPackageId,
                    out message) ||
                !this.TryNormalizeRequiredString(
                    source.LocalRequesterKey,
                    "localRequesterKey",
                    out localRequesterKey,
                    out message) ||
                !this.TryNormalizeOptionalString(
                    source.ModuleInstanceId,
                    "moduleInstanceId",
                    out moduleInstanceId,
                    out message) ||
                !this.TryNormalizeOptionalString(
                    source.ReasonKey,
                    "reasonKey",
                    out reasonKey,
                    out message))
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.InvalidSource;
                return false;
            }

            normalized = new ShuttleExternalLaunchOccupantHandoffSource(
                ownerPackageId,
                localRequesterKey,
                moduleInstanceId,
                reasonKey);
            return true;
        }

        private bool TryNormalizeRequiredString(
            string value,
            string fieldName,
            out string normalized,
            out string message)
        {
            normalized = null;
            message = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                message = fieldName + " is required";
                return false;
            }

            return this.TryNormalizeOptionalString(value, fieldName, out normalized, out message) &&
                !string.IsNullOrEmpty(normalized);
        }

        private bool TryNormalizeOptionalString(
            string value,
            string fieldName,
            out string normalized,
            out string message)
        {
            normalized = null;
            message = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                normalized = null;
                return true;
            }

            normalized = value.Trim();
            if (normalized.Length > MaxStringLength)
            {
                message = fieldName + " exceeds " + MaxStringLength + " characters";
                return false;
            }

            return true;
        }
    }
}
