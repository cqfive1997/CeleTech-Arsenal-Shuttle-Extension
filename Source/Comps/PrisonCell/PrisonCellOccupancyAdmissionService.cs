using CeleTech.ShuttleExtension.ModularShuttle.AI;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttlePrisonCellOccupancy
    {
        internal static class PrisonCellOccupancyAdmissionService
        {
            internal static bool TryAdmitSpawnedPrisonerForInternalUse(
                CompShuttlePrisonCellOccupancy owner,
                Pawn prisoner,
                ShuttlePrisonerAdmissionContext context,
                out string failReason)
            {
                failReason = null;
                if (!CanAdmitPrisoner(owner, prisoner, out failReason))
                {
                    return false;
                }

                owner.EnsureInitialized();
                owner.ReconcilePrisonerRecords();
                if (owner.IsHeldPawn(prisoner))
                {
                    EnsureRecordForHeldPawn(owner, prisoner, context);
                    return true;
                }

                Map map = prisoner.Map;
                IntVec3 fallbackCell = owner.GetEjectCell(map);
                CompShuttleHolderLaunchTransferState transferState = owner.GetHolderTransferState();
                PawnHolderTransferResult transferResult =
                    ShuttlePawnHolderTransferUtility.TryMoveSpawnedPawnIntoHolderSafely(
                        prisoner,
                        owner.prisonCellHeldThings,
                        map,
                        fallbackCell,
                        transferState != null ? transferState.EmergencyRecoveryThings : null,
                        "PrisonCell admit prisoner");
                if (transferResult.Status != PawnHolderTransferStatus.MovedToDestination)
                {
                    HandleFailedPawnHolderTransfer(owner, prisoner, transferResult, "PrisonCell admit prisoner");
                    failReason = GetPawnHolderTransferFailureReason(
                        transferResult,
                        "CT_Shuttle_PrisonCell_AdmitFailed".Translate().ToString());
                    return false;
                }

                StopPawnJobs(prisoner);
                owner.prisonerRecords.Add(ShuttlePrisonerRecord.FromPawn(
                    prisoner,
                    context.HostFaction,
                    context.InteractionMode,
                    context.WasPrisonerOnAdmission,
                    context.AdmissionTick >= 0
                        ? context.AdmissionTick
                        : (Find.TickManager != null ? Find.TickManager.TicksGame : -1)));

                if (transferResult.WasSelected)
                {
                    Find.Selector.Select(owner.parent, false, false);
                }

                return true;
            }

            internal static bool TryAdmitCarriedPrisonerForInternalUse(
                CompShuttlePrisonCellOccupancy owner,
                Pawn carrier,
                Pawn prisoner,
                ShuttlePrisonerAdmissionContext context,
                out string failReason)
            {
                failReason = null;
                if (!CanAdmitCarriedPrisoner(owner, carrier, prisoner, out failReason))
                {
                    return false;
                }

                owner.EnsureInitialized();
                owner.ReconcilePrisonerRecords();
                if (owner.IsHeldPawn(prisoner))
                {
                    EnsureRecordForHeldPawn(owner, prisoner, context);
                    return true;
                }

                bool added = owner.prisonCellHeldThings.TryAddOrTransfer(prisoner, true);
                if (!added)
                {
                    RecoverFailedCarriedAdmission(owner, carrier, prisoner);
                    failReason = "CT_Shuttle_PrisonCell_CarriedAdmitFailed".Translate().ToString();
                    return false;
                }

                EnsureRecordForHeldPawn(owner, prisoner, context);
                return true;
            }

            private static bool CanAdmitPrisoner(
                CompShuttlePrisonCellOccupancy owner,
                Pawn prisoner,
                out string failReason)
            {
                failReason = null;
                if (prisoner == null || prisoner.Destroyed || prisoner.Dead)
                {
                    failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                    return false;
                }

                if (!prisoner.Spawned || prisoner.Map == null)
                {
                    failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                    return false;
                }

                if (owner.parent == null ||
                    !owner.parent.Spawned ||
                    owner.parent.Map == null ||
                    prisoner.Map != owner.parent.Map)
                {
                    failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                    return false;
                }

                if (!ShuttlePassengerFacilityAdmissionPolicy.AllowsEntry(
                    prisoner,
                    owner.parent,
                    out failReason))
                {
                    return false;
                }

                if (owner.GetPrisonerSlots() <= 0)
                {
                    failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                    return false;
                }

                if (!owner.IsHeldPawn(prisoner) && owner.FreePrisonerSlots <= 0)
                {
                    failReason = "CT_Shuttle_PrisonCell_Full".Translate().ToString();
                    return false;
                }

                return true;
            }

            private static bool CanAdmitCarriedPrisoner(
                CompShuttlePrisonCellOccupancy owner,
                Pawn carrier,
                Pawn prisoner,
                out string failReason)
            {
                failReason = null;
                if (carrier == null ||
                    carrier.Destroyed ||
                    carrier.Dead ||
                    !carrier.Spawned ||
                    carrier.Map == null ||
                    carrier.carryTracker == null)
                {
                    failReason = "CT_Shuttle_PrisonCell_CarrierUnavailable".Translate().ToString();
                    return false;
                }

                if (prisoner == null || prisoner.Destroyed || prisoner.Dead)
                {
                    failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                    return false;
                }

                if (carrier.carryTracker.CarriedThing != prisoner)
                {
                    failReason = "CT_Shuttle_PrisonCell_CarriedAdmitFailed".Translate().ToString();
                    return false;
                }

                if (owner.parent == null ||
                    !owner.parent.Spawned ||
                    owner.parent.Map == null ||
                    carrier.Map != owner.parent.Map)
                {
                    failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                    return false;
                }

                if (!ShuttlePassengerFacilityAdmissionPolicy.AllowsEntry(
                    prisoner,
                    owner.parent,
                    out failReason))
                {
                    return false;
                }

                if (owner.GetPrisonerSlots() <= 0)
                {
                    failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                    return false;
                }

                if (!owner.IsHeldPawn(prisoner) && owner.FreePrisonerSlots <= 0)
                {
                    failReason = "CT_Shuttle_PrisonCell_Full".Translate().ToString();
                    return false;
                }

                return true;
            }

            private static void EnsureRecordForHeldPawn(
                CompShuttlePrisonCellOccupancy owner,
                Pawn pawn,
                ShuttlePrisonerAdmissionContext context)
            {
                if (pawn == null)
                {
                    return;
                }

                for (int i = 0; i < owner.prisonerRecords.Count; i++)
                {
                    ShuttlePrisonerRecord record = owner.prisonerRecords[i];
                    if (record != null && record.Matches(pawn))
                    {
                        record.RefreshFromPawn(pawn);
                        return;
                    }
                }

                owner.prisonerRecords.Add(ShuttlePrisonerRecord.FromPawn(
                    pawn,
                    context.HostFaction,
                    context.InteractionMode,
                    context.WasPrisonerOnAdmission,
                    context.AdmissionTick >= 0
                        ? context.AdmissionTick
                        : (Find.TickManager != null ? Find.TickManager.TicksGame : -1)));
            }

            private static void StopPawnJobs(Pawn pawn)
            {
                if (pawn == null || pawn.jobs == null || pawn.CurJob == null)
                {
                    return;
                }

                try
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, false, true);
                }
                catch (System.Exception exception)
                {
                    Log.Warning("[CeleTech Shuttle] Could not stop prison-cell pawn job after holder entry: " + exception);
                }
            }

            private static void HandleFailedPawnHolderTransfer(
                CompShuttlePrisonCellOccupancy owner,
                Pawn pawn,
                PawnHolderTransferResult transferResult,
                string operation)
            {
                if (transferResult == null)
                {
                    Log.Error("[CeleTech Shuttle] Prison Cell pawn holder transfer returned null result. operation=" +
                        (operation ?? "null"));
                    return;
                }

                if (transferResult.Status == PawnHolderTransferStatus.FatalOwnerless)
                {
                    Log.Error("[CeleTech Shuttle] Prison Cell pawn holder transfer fatal. operation=" +
                        (operation ?? "null") +
                        " context=" +
                        (transferResult.DebugContext ?? "null") +
                        " reason=" +
                        (transferResult.FailureReason ?? "null") +
                        " pawn=" +
                        pawn);
                    return;
                }

                if (transferResult.Status == PawnHolderTransferStatus.RecoveredToEmergencyOwner)
                {
                    Log.Warning("[CeleTech Shuttle] Prison Cell pawn holder transfer recovered pawn to emergency owner; no prisoner record was created. operation=" +
                        (operation ?? "null") +
                        " context=" +
                        (transferResult.DebugContext ?? "null") +
                        " reason=" +
                        (transferResult.FailureReason ?? "null") +
                        " pawn=" +
                        pawn);
                    if (transferResult.WasSelected && owner.parent != null)
                    {
                        Find.Selector.Select(owner.parent, false, false);
                    }
                }
            }

            private static void RecoverFailedCarriedAdmission(
                CompShuttlePrisonCellOccupancy owner,
                Pawn carrier,
                Pawn prisoner)
            {
                if (prisoner == null || prisoner.Destroyed)
                {
                    return;
                }

                bool secured =
                    (carrier != null &&
                        carrier.carryTracker != null &&
                        carrier.carryTracker.CarriedThing == prisoner) ||
                    prisoner.Spawned ||
                    prisoner.holdingOwner != null;
                if (secured)
                {
                    return;
                }

                Map fallbackMap = carrier != null ? carrier.Map : (owner.parent != null ? owner.parent.Map : null);
                IntVec3 fallbackCell = owner.GetEjectCell(fallbackMap);
                if (fallbackMap != null &&
                    fallbackCell.IsValid &&
                    GenPlace.TryPlaceThing(prisoner, fallbackCell, fallbackMap, ThingPlaceMode.Near))
                {
                    Log.Warning("[CeleTech Shuttle] Prison Cell recovered prisoner to map after carried admit failure. prisoner=" +
                        prisoner);
                    return;
                }

                CompShuttleHolderLaunchTransferState transferState = owner.GetHolderTransferState();
                ShuttleTransferRecoveryStatus recoveryStatus;
                string recoveryFailureReason = null;
                if (transferState != null &&
                    transferState.TryRecoverTransferThing(
                        prisoner,
                        "PrisonCell carried admit failed after carrier handoff.",
                        null,
                        null,
                        fallbackMap,
                        fallbackCell,
                        out recoveryStatus,
                        out recoveryFailureReason))
                {
                    Log.Warning("[CeleTech Shuttle] Prison Cell recovered prisoner after carried admit failure. status=" +
                        recoveryStatus +
                        " prisoner=" +
                        prisoner +
                        " reason=" +
                        (recoveryFailureReason ?? "null"));
                    return;
                }

                Log.Error("[CeleTech Shuttle] Prison Cell carried admit failed and prisoner final owner could not be confirmed. prisoner=" +
                    prisoner +
                    " recovery=" +
                    (recoveryFailureReason ?? "transfer state unavailable"));
            }

            private static string GetPawnHolderTransferFailureReason(
                PawnHolderTransferResult transferResult,
                string defaultReason)
            {
                if (transferResult != null && !string.IsNullOrEmpty(transferResult.FailureReason))
                {
                    return transferResult.FailureReason;
                }

                return defaultReason;
            }
        }
    }
}
