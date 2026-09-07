using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMechChargerOccupancy
    {
        internal static class MechChargerOccupancyEjectionService
        {
            internal static bool TryEjectChargingMech(
                CompShuttleMechChargerOccupancy owner,
                Pawn mech,
                out string failureReason)
            {
                Map map = owner.parent != null ? owner.parent.Map : null;
                return TryEjectChargingMech(owner, mech, map, out failureReason);
            }

            internal static bool TryEjectAllChargingMechs(
                CompShuttleMechChargerOccupancy owner,
                Map map,
                out string failureReason)
            {
                failureReason = null;
                if (map == null)
                {
                    failureReason = "CT_Shuttle_MechCharger_EjectFailed".Translate().ToString();
                    return false;
                }

                owner.EnsureInitialized();
                owner.ReconcileChargingRecordsToHeldMechs();

                bool allEjected = true;
                for (int i = owner.chargingRecords.Count - 1; i >= 0; i--)
                {
                    ShuttleMechChargingRecord record = owner.chargingRecords[i];
                    Pawn mech = record != null ? record.Pawn : null;
                    if (mech == null || !owner.IsHeldPawn(mech))
                    {
                        owner.ReleaseChargingReservation(record != null ? record.PawnThingID : -1);
                        owner.chargingRecords.RemoveAt(i);
                        continue;
                    }

                    string mechFailure;
                    if (!TryEjectChargingMech(owner, mech, map, out mechFailure))
                    {
                        allEjected = false;
                        failureReason = mechFailure;
                    }
                }

                if (!allEjected && string.IsNullOrEmpty(failureReason))
                {
                    failureReason = "CT_Shuttle_MechCharger_EjectFailed".Translate().ToString();
                }

                return allEjected && !owner.HasChargingMechs;
            }

            internal static bool TryEjectChargingMech(
                CompShuttleMechChargerOccupancy owner,
                Pawn mech,
                Map map,
                out string failureReason)
            {
                failureReason = null;
                owner.EnsureInitialized();
                if (!owner.IsHeldPawn(mech))
                {
                    owner.RemoveChargingRecord(mech);
                    owner.ReleaseChargingReservation(mech);
                    return true;
                }

                if (map == null)
                {
                    failureReason = "CT_Shuttle_MechCharger_EjectFailed".Translate().ToString();
                    return false;
                }

                Thing resultingThing;
                if (!owner.mechChargingHeldThings.TryDrop(
                    mech,
                    owner.GetEjectCell(map),
                    map,
                    ThingPlaceMode.Near,
                    out resultingThing,
                    null,
                    null))
                {
                    failureReason = "CT_Shuttle_MechCharger_EjectFailed".Translate().ToString();
                    return false;
                }

                owner.RemoveChargingRecord(mech);
                owner.ReleaseChargingReservation(mech);
                return true;
            }

            internal static bool TryCompleteChargingMech(
                CompShuttleMechChargerOccupancy owner,
                Pawn mech,
                Map map,
                out string failureReason)
            {
                failureReason = null;
                owner.EnsureInitialized();
                if (!owner.IsHeldPawn(mech))
                {
                    owner.RemoveChargingRecord(mech);
                    owner.ReleaseChargingReservation(mech);
                    return true;
                }

                CompModularShuttleCore core = owner.parent != null
                    ? owner.parent.TryGetComp<CompModularShuttleCore>()
                    : null;
                ShuttlePassengerInternalTransferResult transferResult =
                    core != null
                        ? core.TryTransferCompletedPassengerToCockpit(
                            mech,
                            owner.mechChargingHeldThings,
                            out failureReason)
                        : ShuttlePassengerInternalTransferResult.FallbackToMap;

                if (transferResult == ShuttlePassengerInternalTransferResult.Transferred)
                {
                    owner.RemoveChargingRecord(mech);
                    owner.ReleaseChargingReservation(mech);
                    return true;
                }

                if (transferResult == ShuttlePassengerInternalTransferResult.UnsafeOwnerState)
                {
                    failureReason = failureReason ??
                        "Internal mech passenger transfer could not prove final ownership.";
                    Log.ErrorOnce(
                        "[CeleTech Shuttle] Mech Charger completion stopped because internal passenger transfer could not prove mech ownership. mech=" +
                        mech +
                        " reason=" +
                        failureReason,
                        831901 + mech.thingIDNumber);
                    return false;
                }

                return TryEjectChargingMech(
                    owner,
                    mech,
                    map,
                    out failureReason);
            }
        }
    }
}
