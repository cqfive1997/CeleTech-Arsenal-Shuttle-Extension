using CeleTech.ShuttleExtension.ModularShuttle.MechCharging;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMechChargerOccupancy
    {
        internal static class MechChargerOccupancyAdmissionService
        {
            internal static bool TryReserveCharging(
                CompShuttleMechChargerOccupancy owner,
                Pawn actor,
                Pawn mech,
                out string failReason)
            {
                failReason = null;
                owner.EnsureInitialized();
                owner.ClearStaleChargingReservations();

                if (!MechChargerAdmissionValidator.CanUseShuttleMechChargerForCharging(
                    mech,
                    owner.parent,
                    out failReason,
                    actor))
                {
                    return false;
                }

                int mechThingID = mech != null ? mech.thingIDNumber : -1;
                int actorThingID = actor != null && actor.thingIDNumber > 0
                    ? actor.thingIDNumber
                    : mechThingID;
                int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : -1;
                ShuttleMechChargingReservation existing = owner.FindChargingReservation(mechThingID);

                string moduleInstanceID;
                float chargeRateFactor;
                if (existing != null &&
                    owner.IsModuleAvailableForCharging(existing.ModuleInstanceID, mechThingID))
                {
                    moduleInstanceID = existing.ModuleInstanceID;
                    chargeRateFactor = existing.ChargeRateFactor;
                }
                else if (!owner.TryFindAvailableChargerModule(
                    mechThingID,
                    out moduleInstanceID,
                    out chargeRateFactor))
                {
                    failReason = "CT_Shuttle_MechCharger_Full".Translate().ToString();
                    return false;
                }

                if (existing != null)
                {
                    existing.ActorThingID = actorThingID;
                    existing.ReservationTick = currentTick;
                    existing.SetModuleAssignment(moduleInstanceID, chargeRateFactor);
                    return true;
                }

                owner.chargingReservations.Add(new ShuttleMechChargingReservation(
                    mechThingID,
                    actorThingID,
                    moduleInstanceID,
                    chargeRateFactor,
                    currentTick));
                return true;
            }

            internal static bool TryEnterForCharging(
                CompShuttleMechChargerOccupancy owner,
                Pawn mech,
                out string failureReason)
            {
                failureReason = null;
                string useFailureReason;
                if (!MechChargerAdmissionValidator.CanUseShuttleMechChargerForCharging(
                    mech,
                    owner.parent,
                    out useFailureReason,
                    mech))
                {
                    failureReason = useFailureReason;
                    return false;
                }

                owner.EnsureInitialized();
                owner.ReconcileChargingRecordsToHeldMechs();
                if (owner.ContainsChargingMech(mech))
                {
                    return true;
                }

                ShuttleMechChargingReservation reservation = owner.FindChargingReservation(
                    mech != null ? mech.thingIDNumber : -1);
                if (reservation == null &&
                    !owner.TryReserveCharging(mech, mech, out failureReason))
                {
                    return false;
                }

                reservation = owner.FindChargingReservation(mech != null ? mech.thingIDNumber : -1);
                if (reservation == null)
                {
                    failureReason = "CT_Shuttle_MechCharger_Full".Translate().ToString();
                    return false;
                }

                string moduleInstanceID = reservation.ModuleInstanceID;
                float chargeRateFactor = reservation.ChargeRateFactor;
                if (!owner.IsModuleAvailableForCharging(moduleInstanceID, mech.thingIDNumber) &&
                    !owner.TryFindAvailableChargerModule(
                        mech.thingIDNumber,
                        out moduleInstanceID,
                        out chargeRateFactor))
                {
                    failureReason = "CT_Shuttle_MechCharger_Full".Translate().ToString();
                    return false;
                }

                Map map = mech.Map;
                IntVec3 fallbackCell = owner.GetEjectCell(map);
                CompShuttleHolderLaunchTransferState transferState = owner.GetHolderTransferState();
                PawnHolderTransferResult transferResult =
                    ShuttlePawnHolderTransferUtility.TryMoveSpawnedPawnIntoHolderSafely(
                    mech,
                    owner.mechChargingHeldThings,
                    map,
                    fallbackCell,
                    transferState != null ? transferState.EmergencyRecoveryThings : null,
                    "MechCharger enter");
                if (transferResult.Status != PawnHolderTransferStatus.MovedToDestination)
                {
                    owner.HandleFailedPawnHolderTransfer(mech, transferResult, "MechCharger enter");
                    failureReason = owner.GetPawnHolderTransferFailureReason(
                        transferResult,
                        "CT_Shuttle_MechCharger_EnterFailed".Translate().ToString());
                    return false;
                }

                owner.chargingRecords.Add(new ShuttleMechChargingRecord(
                    mech,
                    moduleInstanceID,
                    chargeRateFactor,
                    Find.TickManager != null ? Find.TickManager.TicksGame : -1));
                owner.ReleaseChargingReservation(mech);

                if (transferResult.WasSelected)
                {
                    Find.Selector.Select(owner.parent, false, false);
                }

                return true;
            }
        }
    }
}
