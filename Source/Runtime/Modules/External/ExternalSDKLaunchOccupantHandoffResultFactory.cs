using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchOccupantHandoffResultFactory
    {
        internal ShuttleExternalLaunchOccupantHandoffResult BuildFailureFromRequest(
            ShuttleExternalLaunchOccupantHandoffRequest request,
            ShuttleExternalLaunchOccupantHandoffKind kind,
            bool dryRun,
            bool available,
            ShuttleExternalLaunchOccupantHandoffFailureReason failureReason,
            string message)
        {
            Pawn pawn = request != null ? request.Pawn : null;
            return new ShuttleExternalLaunchOccupantHandoffResult(
                available,
                false,
                dryRun,
                kind,
                failureReason,
                message,
                pawn != null ? pawn.thingIDNumber : -1,
                pawn != null ? pawn.ThingID : null,
                pawn != null ? pawn.LabelShort : null,
                0f,
                request != null ? request.Source : null,
                ShuttleTickUtility.TicksGameOrMinusOne(),
                this.BuildDiagnostics(kind, dryRun, failureReason, message));
        }

        internal ShuttleExternalLaunchOccupantHandoffResult BuildFailure(
            ExternalSDKLaunchOccupantHandoffValidatedRequest request,
            ExternalSDKLaunchOccupantHandoffPlan plan,
            ShuttleExternalLaunchOccupantHandoffKind kind,
            bool dryRun,
            bool available,
            ShuttleExternalLaunchOccupantHandoffFailureReason failureReason,
            string message)
        {
            return new ShuttleExternalLaunchOccupantHandoffResult(
                available,
                false,
                dryRun,
                kind,
                failureReason,
                message,
                request != null ? request.PawnThingIdNumber : -1,
                request != null ? request.PawnThingId : null,
                request != null ? request.PawnLabel : null,
                plan != null ? plan.AffectedMassKg : 0f,
                request != null ? request.Source : null,
                ShuttleTickUtility.TicksGameOrMinusOne(),
                this.BuildDiagnostics(kind, dryRun, failureReason, message));
        }

        internal ShuttleExternalLaunchOccupantHandoffResult BuildSuccess(
            ExternalSDKLaunchOccupantHandoffValidatedRequest request,
            ExternalSDKLaunchOccupantHandoffPlan plan,
            ShuttleExternalLaunchOccupantHandoffKind kind,
            bool dryRun,
            float affectedMassKg,
            string message)
        {
            return new ShuttleExternalLaunchOccupantHandoffResult(
                true,
                true,
                dryRun,
                kind,
                ShuttleExternalLaunchOccupantHandoffFailureReason.None,
                message,
                request != null ? request.PawnThingIdNumber : -1,
                request != null ? request.PawnThingId : null,
                request != null ? request.PawnLabel : null,
                affectedMassKg,
                request != null ? request.Source : null,
                ShuttleTickUtility.TicksGameOrMinusOne(),
                this.BuildDiagnostics(
                    kind,
                    dryRun,
                    ShuttleExternalLaunchOccupantHandoffFailureReason.None,
                    message));
        }

        private string BuildDiagnostics(
            ShuttleExternalLaunchOccupantHandoffKind kind,
            bool dryRun,
            ShuttleExternalLaunchOccupantHandoffFailureReason failureReason,
            string message)
        {
            return "kind=" +
                kind +
                ";dryRun=" +
                dryRun +
                ";failureReason=" +
                failureReason +
                ";message=" +
                (message ?? string.Empty);
        }
    }
}
