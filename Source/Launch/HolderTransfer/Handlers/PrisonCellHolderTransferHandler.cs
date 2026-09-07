using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class PrisonCellHolderTransferHandler
    {
        internal static bool TryRestorePrisonersBeforeImpact(
            ThingWithComps shuttleHost,
            ThingOwner primarySource,
            ThingOwner incomingSkyfallerContainer,
            out string failureReason,
            out ShuttleHolderIncomingRestoreFailureStatus failureStatus)
        {
            failureReason = null;
            failureStatus = ShuttleHolderIncomingRestoreFailureStatus.None;
            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null || !state.HasPrisonCellPrisonerManifestEntries)
            {
                return true;
            }

            CompShuttlePrisonCellOccupancy prisonCell = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttlePrisonCellOccupancy>()
                : null;
            if (prisonCell == null)
            {
                failureReason = "[CeleTech Shuttle] PrisonCell prisoner incoming restore failed: CompShuttlePrisonCellOccupancy missing. " +
                    state.DumpManifestForDebug();
                Log.Error(failureReason);
                int missingCompQuarantinedCount;
                string missingCompQuarantineNotice;
                bool missingCompQuarantined = ShuttleHolderLaunchTransferService.TryQuarantinePrisonCellPrisonerManifestPawnsFromIncoming(
                    state,
                    primarySource,
                    incomingSkyfallerContainer,
                    out missingCompQuarantinedCount,
                    out missingCompQuarantineNotice);
                ShuttleHolderLaunchTransferService.SetPrisonCellIncomingFailureStatus(
                    missingCompQuarantined,
                    missingCompQuarantinedCount,
                    ref failureStatus);
                return false;
            }

            ThingOwner<Thing> primaryThingSource = primarySource as ThingOwner<Thing>;
            ThingOwner<Thing> incomingThingSource = incomingSkyfallerContainer as ThingOwner<Thing>;
            if (primaryThingSource == null && incomingThingSource == null)
            {
                failureReason = "[CeleTech Shuttle] PrisonCell prisoner incoming restore failed: no typed source owner is available. " +
                    state.DumpManifestForDebug();
                Log.Error(failureReason);
                int untypedSourceQuarantinedCount;
                string untypedSourceQuarantineNotice;
                bool untypedSourceQuarantined = ShuttleHolderLaunchTransferService.TryQuarantinePrisonCellPrisonerManifestPawnsFromIncoming(
                    state,
                    primarySource,
                    incomingSkyfallerContainer,
                    out untypedSourceQuarantinedCount,
                    out untypedSourceQuarantineNotice);
                ShuttleHolderLaunchTransferService.SetPrisonCellIncomingFailureStatus(
                    untypedSourceQuarantined,
                    untypedSourceQuarantinedCount,
                    ref failureStatus);
                return false;
            }

            string notice;
            if (prisonCell.TryRestorePrisonersFromLaunchStaging(
                state.Manifest,
                primaryThingSource,
                incomingThingSource,
                out notice))
            {
                if (Prefs.DevMode)
                {
                    Log.Message("[CeleTech Shuttle] PrisonCell prisoner incoming restore succeeded before base.Impact: " +
                        (notice ?? "null"));
                }

                return true;
            }

            failureReason = "[CeleTech Shuttle] PrisonCell prisoner incoming restore failed before base.Impact: " +
                (notice ?? "null") +
                " " +
                state.DumpManifestForDebug();
            Log.Error(failureReason);

            int postRestoreQuarantinedCount;
            string postRestoreQuarantineNotice;
            bool postRestoreQuarantined = ShuttleHolderLaunchTransferService.TryQuarantinePrisonCellPrisonerManifestPawnsFromIncoming(
                state,
                primarySource,
                incomingSkyfallerContainer,
                out postRestoreQuarantinedCount,
                out postRestoreQuarantineNotice);
            if (!postRestoreQuarantined &&
                postRestoreQuarantinedCount == 0 &&
                ShuttleHolderManifestQueryUtility.PrisonCellManifestEntriesAreQuarantined(state.Manifest))
            {
                postRestoreQuarantined = true;
                postRestoreQuarantinedCount = state.Manifest.FindEntriesByHolderKind(
                    ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind).Count;
            }

            ShuttleHolderLaunchTransferService.SetPrisonCellIncomingFailureStatus(
                postRestoreQuarantined,
                postRestoreQuarantinedCount,
                ref failureStatus);
            return false;
        }

        internal static bool CanUsePrisonCellPrisonerLaunchTransferCore(
            ThingWithComps shuttleHost,
            bool allowActiveKnownTransactionManifest,
            out string failureReason)
        {
            failureReason = null;
            CompShuttlePrisonCellOccupancy prisonCell = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttlePrisonCellOccupancy>()
                : null;
            if (prisonCell == null || !prisonCell.HasPrisoners)
            {
                return true;
            }

            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] PrisonCell prisoner launch transfer requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (state.HasActiveManifest)
            {
                string manifestFailureReason = null;
                bool activeManifestAllowed = allowActiveKnownTransactionManifest &&
                    ShuttleHolderManifestClassificationUtility.ManifestContainsOnlyExportActiveKnownHolderEntries(
                        state.Manifest,
                        out manifestFailureReason);
                if (!activeManifestAllowed)
                {
                    failureReason = "[CeleTech Shuttle] PrisonCell prisoner launch transfer refused because a holder transfer manifest is already active. " +
                        state.DumpManifestForDebug();
                    if (!string.IsNullOrEmpty(manifestFailureReason))
                    {
                        failureReason = failureReason + " " + manifestFailureReason;
                    }

                    return false;
                }
            }

            if (!prisonCell.CanTransferPrisonersForLaunch(out failureReason))
            {
                failureReason = "[CeleTech Shuttle] PrisonCell prisoner launch transfer prevalidation failed: " + failureReason;
                return false;
            }

            return true;
        }

        internal static bool TryExportPrisonersToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            failureReason = null;
            if (!HolderTransferPreflightService.NeedsPrisonCellPrisonerLaunchTransfer(shuttleHost))
            {
                return true;
            }

            if (!CanUsePrisonCellPrisonerLaunchTransferCore(
                shuttleHost,
                true,
                out failureReason))
            {
                return false;
            }

            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            CompShuttlePrisonCellOccupancy prisonCell = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttlePrisonCellOccupancy>()
                : null;
            if (state == null || prisonCell == null)
            {
                failureReason = "[CeleTech Shuttle] PrisonCell prisoner launch export requires holder transfer state and PrisonCell occupancy comps.";
                return false;
            }

            ShuttleLaunchCargoHandoff handoff = ShuttleHolderTransferLookupUtility.FindShuttleHandoff(handoffs);
            ThingOwner<Thing> destination = ShuttleHolderTransferLookupUtility.GetActiveTransporterThingContainer(handoff);
            if (destination == null)
            {
                failureReason = "[CeleTech Shuttle] PrisonCell prisoner launch export failed: active transporter container unavailable.";
                return false;
            }

            if (!prisonCell.TryExportPrisonersForLaunch(state.Manifest, destination, false, out failureReason))
            {
                if (!failureReason.NullOrEmpty())
                {
                    state.MarkManifestFailed(failureReason);
                }

                string rollbackNotice;
                prisonCell.TryRollbackPrisonerLaunchExport(state.Manifest, destination, out rollbackNotice);
                failureReason = "[CeleTech Shuttle] PrisonCell prisoner launch export failed: " +
                    failureReason +
                    " rollback=" +
                    rollbackNotice;
                return false;
            }

            if (Prefs.DevMode)
            {
                Log.Message("[CeleTech Shuttle] PrisonCell prisoners exported to real launch handoff. " + state.DumpManifestForDebug());
            }

            return true;
        }

        internal static bool TryRollbackPrisonerLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            notice = null;
            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null || !state.HasPrisonCellPrisonerManifestEntries)
            {
                notice = "No PrisonCell prisoner launch manifest to roll back.";
                return true;
            }

            CompShuttlePrisonCellOccupancy prisonCell = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttlePrisonCellOccupancy>()
                : null;
            if (prisonCell == null)
            {
                notice = "[CeleTech Shuttle] PrisonCell prisoner launch rollback requires CompShuttlePrisonCellOccupancy.";
                return false;
            }

            ShuttleLaunchCargoHandoff handoff = ShuttleHolderTransferLookupUtility.FindShuttleHandoff(handoffs);
            ThingOwner<Thing> source = ShuttleHolderTransferLookupUtility.GetActiveTransporterThingContainer(handoff);
            if (source == null)
            {
                notice = "[CeleTech Shuttle] PrisonCell prisoner launch rollback could not find active transporter container.";
                return false;
            }

            bool result = prisonCell.TryRollbackPrisonerLaunchExport(state.Manifest, source, out notice);
            if (result && Prefs.DevMode)
            {
                Log.Message("[CeleTech Shuttle] PrisonCell prisoner launch export rolled back: " + notice);
            }

            return result;
        }

        internal static bool TryRollbackPrisonerLaunchExportWithRecovery(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out bool rollbackSucceeded,
            out bool prisonersQuarantined,
            out string notice)
        {
            rollbackSucceeded = false;
            prisonersQuarantined = false;

            if (TryRollbackPrisonerLaunchExport(shuttleHost, handoffs, out notice))
            {
                rollbackSucceeded = true;
                return true;
            }

            string rollbackFailureNotice = notice;
            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null || !state.HasPrisonCellPrisonerManifestEntries)
            {
                notice = "[CeleTech Shuttle] PrisonCell rollback failed but no PrisonCell manifest remains active. rollback=" +
                    (rollbackFailureNotice ?? "null");
                return true;
            }

            int remainingBefore = ShuttleHolderTransferLookupUtility.CountPrisonCellPrisonerManifestThingsInHandoffs(state.Manifest, handoffs);
            if (remainingBefore <= 0)
            {
                notice = "[CeleTech Shuttle] PrisonCell rollback failed, but no PrisonCell manifest pawns remain in committed handoffs. Manifest remains active for diagnostics/recovery. rollback=" +
                    (rollbackFailureNotice ?? "null");
                return true;
            }

            int quarantinedCount;
            string quarantineNotice;
            bool quarantineSucceeded = ShuttleHolderLaunchTransferService.TryQuarantinePrisonCellPrisonerManifestPawnsFromHandoffs(
                state,
                handoffs,
                out quarantinedCount,
                out quarantineNotice);

            int remainingAfter = ShuttleHolderTransferLookupUtility.CountPrisonCellPrisonerManifestThingsInHandoffs(state.Manifest, handoffs);
            prisonersQuarantined = quarantinedCount > 0 && remainingAfter == 0;
            if (quarantineSucceeded && remainingAfter == 0)
            {
                notice = "[CeleTech Shuttle] PrisonCell rollback failed, but " +
                    quarantinedCount +
                    " prisoner pawn(s) were moved from committed handoffs to emergency recovery. Manifest remains active for recovery. rollback=" +
                    (rollbackFailureNotice ?? "null") +
                    " quarantine=" +
                    (quarantineNotice ?? "null");
                return true;
            }

            notice = "[CeleTech Shuttle] PrisonCell rollback failed and " +
                remainingAfter +
                " PrisonCell manifest pawn(s) remain in committed handoffs. Normal cargo rollback must not proceed. rollback=" +
                (rollbackFailureNotice ?? "null") +
                " quarantine=" +
                (quarantineNotice ?? "null");
            return false;
        }
    }
}
