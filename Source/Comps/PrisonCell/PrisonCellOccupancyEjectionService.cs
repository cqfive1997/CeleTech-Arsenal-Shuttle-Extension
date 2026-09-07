using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttlePrisonCellOccupancy
    {
        internal static class PrisonCellOccupancyEjectionService
        {
            internal static bool TryEjectPrisonerByThingID(
                CompShuttlePrisonCellOccupancy owner,
                int pawnThingIDNumber,
                out bool hadPrisoner,
                out string failReason)
            {
                failReason = null;
                hadPrisoner = false;
                owner.EnsureInitialized();
                owner.ReconcilePrisonerRecords();
                Pawn prisoner = owner.FindHeldPrisonerByThingIDNoReconcile(pawnThingIDNumber);
                if (prisoner == null)
                {
                    owner.RemovePrisonerRecordByThingID(pawnThingIDNumber);
                    return true;
                }

                hadPrisoner = true;
                return TryEjectPrisoner(owner, prisoner, out failReason);
            }

            internal static bool TryEjectPrisoner(
                CompShuttlePrisonCellOccupancy owner,
                Pawn prisoner,
                out string failReason)
            {
                failReason = null;
                Map map = owner.parent != null ? owner.parent.Map : null;
                return TryEjectPrisoner(owner, prisoner, map, out failReason);
            }

            internal static bool TryEjectAllPrisoners(
                CompShuttlePrisonCellOccupancy owner,
                out string failReason)
            {
                Map map = owner.parent != null ? owner.parent.Map : null;
                return TryEjectAllPrisoners(owner, map, out failReason);
            }

            internal static bool TryEjectAllPrisoners(
                CompShuttlePrisonCellOccupancy owner,
                Map map,
                out string failReason)
            {
                failReason = null;
                if (map == null)
                {
                    failReason = "CT_Shuttle_PrisonCell_EjectFailed".Translate().ToString();
                    return false;
                }

                owner.EnsureInitialized();
                owner.ReconcilePrisonerRecords();
                List<Pawn> prisoners = owner.GetHeldPrisonersForReading();
                bool allEjected = true;
                for (int i = prisoners.Count - 1; i >= 0; i--)
                {
                    string prisonerFailure;
                    if (!TryEjectPrisoner(owner, prisoners[i], map, out prisonerFailure))
                    {
                        allEjected = false;
                        failReason = prisonerFailure;
                    }
                }

                if (!allEjected && string.IsNullOrEmpty(failReason))
                {
                    failReason = "CT_Shuttle_PrisonCell_EjectFailed".Translate().ToString();
                }

                return allEjected && !owner.HasPrisoners;
            }

            internal static bool TryRecoverAllPrisonersDuringDestroy(
                CompShuttlePrisonCellOccupancy owner,
                Map previousMap,
                out string failReason)
            {
                failReason = null;
                owner.EnsureInitialized();
                owner.ReconcilePrisonerRecords();

                List<Pawn> prisoners = owner.GetHeldPrisonersForReading();
                bool allRecovered = true;
                for (int i = prisoners.Count - 1; i >= 0; i--)
                {
                    string prisonerFailure;
                    if (!TryRecoverPrisonerDuringDestroy(
                        owner,
                        prisoners[i],
                        previousMap,
                        out prisonerFailure))
                    {
                        allRecovered = false;
                        failReason = prisonerFailure;
                    }
                }

                if (!allRecovered && string.IsNullOrEmpty(failReason))
                {
                    failReason = "CT_Shuttle_PrisonCell_EjectFailed".Translate().ToString();
                }

                return allRecovered && !owner.HasPrisoners;
            }

            private static bool TryRecoverPrisonerDuringDestroy(
                CompShuttlePrisonCellOccupancy owner,
                Pawn prisoner,
                Map previousMap,
                out string failReason)
            {
                failReason = null;
                if (!owner.IsHeldPawn(prisoner))
                {
                    return true;
                }

                if (previousMap != null)
                {
                    return TryEjectPrisoner(owner, prisoner, previousMap, out failReason);
                }

                if (TryMovePrisonerToEmergencyRecovery(owner, prisoner, out failReason))
                {
                    owner.RemovePrisonerRecord(prisoner);
                    return true;
                }

                Log.Error("[CeleTech Shuttle] Prison Cell could not recover prisoner during destroy. prisoner=" +
                    prisoner +
                    " parent=" +
                    owner.parent +
                    " reason=" +
                    (failReason ?? "unknown"));
                return false;
            }

            private static bool TryMovePrisonerToEmergencyRecovery(
                CompShuttlePrisonCellOccupancy owner,
                Pawn prisoner,
                out string failReason)
            {
                failReason = null;
                if (!owner.IsHeldPawn(prisoner))
                {
                    return true;
                }

                CompShuttleHolderLaunchTransferState transferState = owner.GetHolderTransferState();
                ThingOwner<Thing> emergencyOwner = transferState != null
                    ? transferState.EmergencyRecoveryThings
                    : null;
                if (emergencyOwner == null)
                {
                    failReason = "CT_Shuttle_PrisonCell_EjectFailed".Translate().ToString();
                    return false;
                }

                if (!emergencyOwner.TryAddOrTransfer(prisoner, false) ||
                    !emergencyOwner.Contains(prisoner))
                {
                    failReason = "CT_Shuttle_PrisonCell_EjectFailed".Translate().ToString();
                    return false;
                }

                Log.Warning("[CeleTech Shuttle] Prison Cell moved prisoner to emergency recovery holder during destroy because no map was available. prisoner=" +
                    prisoner +
                    " parent=" +
                    owner.parent);
                return true;
            }

            private static bool TryEjectPrisoner(
                CompShuttlePrisonCellOccupancy owner,
                Pawn prisoner,
                Map map,
                out string failReason)
            {
                failReason = null;
                owner.EnsureInitialized();
                if (!owner.IsHeldPawn(prisoner))
                {
                    owner.RemovePrisonerRecord(prisoner);
                    return true;
                }

                if (map == null)
                {
                    failReason = "CT_Shuttle_PrisonCell_EjectFailed".Translate().ToString();
                    return false;
                }

                Thing resultingThing;
                if (!owner.prisonCellHeldThings.TryDrop(
                    prisoner,
                    owner.GetEjectCell(map),
                    map,
                    ThingPlaceMode.Near,
                    out resultingThing,
                    null,
                    null))
                {
                    failReason = "CT_Shuttle_PrisonCell_EjectFailed".Translate().ToString();
                    return false;
                }

                owner.RemovePrisonerRecord(prisoner);
                return true;
            }
        }
    }
}
