using CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal delegate bool HabitatSleepRecordEjectDelegate(
        ShuttleHabitatOccupantRecord record,
        Map map,
        bool completedRest);

    internal delegate bool HabitatDiningPawnDropDelegate(
        ShuttleHabitatDiningOccupantRecord record,
        Map map);

    internal delegate bool HabitatJoyPawnDropDelegate(
        ShuttleHabitatJoyOccupantRecord record,
        Map map,
        out Pawn droppedPawn);

    internal static class HabitatEjectionService
    {
        internal static bool TryEjectAllToMap(
            HabitatEjectionAccess access,
            Map map,
            bool completedRest)
        {
            if (map == null)
            {
                return false;
            }

            access.EnsureInitialized();
            access.ReconcileSleepingRecords();
            bool allEjected = true;
            for (int i = access.SleepingRecordCount - 1; i >= 0; i--)
            {
                if (!TryEject(access, access.GetSleepingRecord(i), map, completedRest))
                {
                    allEjected = false;
                }
            }

            access.ReconcileDiningRecords();
            for (int i = access.DiningRecordCount - 1; i >= 0; i--)
            {
                if (!access.TryAbortDining(access.GetDiningRecord(i), map))
                {
                    allEjected = false;
                }
            }

            access.ReconcileJoyRecords();
            for (int i = access.JoyRecordCount - 1; i >= 0; i--)
            {
                if (!access.TryAbortJoy(access.GetJoyRecord(i), map))
                {
                    allEjected = false;
                }
            }

            return allEjected && !access.HasAnyOccupants;
        }

        internal static bool TryEjectOccupantByThingID(
            HabitatEjectionAccess access,
            int pawnThingID,
            Map map,
            out bool hadOccupant,
            out string failureReason)
        {
            hadOccupant = false;
            failureReason = null;
            if (pawnThingID <= 0)
            {
                failureReason = "CT_Shuttle_Command_EjectHabitatOccupant_Failed".Translate().ToString();
                return false;
            }

            if (map == null)
            {
                failureReason = "CT_Shuttle_Command_EjectHabitatOccupant_Failed".Translate().ToString();
                return false;
            }

            access.EnsureInitialized();
            access.ReconcileSleepingRecords();
            for (int i = access.SleepingRecordCount - 1; i >= 0; i--)
            {
                ShuttleHabitatOccupantRecord record = access.GetSleepingRecord(i);
                Pawn pawn = record != null ? record.Pawn : null;
                if (pawn != null && pawn.thingIDNumber == pawnThingID)
                {
                    hadOccupant = true;
                    return TryEject(access, record, map, false);
                }
            }

            access.ReconcileDiningRecords();
            for (int i = access.DiningRecordCount - 1; i >= 0; i--)
            {
                ShuttleHabitatDiningOccupantRecord record = access.GetDiningRecord(i);
                Pawn pawn = record != null ? record.Pawn : null;
                if (pawn != null && pawn.thingIDNumber == pawnThingID)
                {
                    hadOccupant = true;
                    return access.TryAbortDining(record, map);
                }
            }

            access.ReconcileJoyRecords();
            for (int i = access.JoyRecordCount - 1; i >= 0; i--)
            {
                ShuttleHabitatJoyOccupantRecord record = access.GetJoyRecord(i);
                Pawn pawn = record != null ? record.Pawn : null;
                if (pawn != null && pawn.thingIDNumber == pawnThingID)
                {
                    hadOccupant = true;
                    return access.TryAbortJoy(record, map);
                }
            }

            Pawn unclassifiedPawn = access.FindHeldPawnByThingID(pawnThingID);
            if (unclassifiedPawn == null)
            {
                return true;
            }

            // This fallback only drops a pawn already contained in the Habitat holder.
            // It keeps unclassified holder recovery behind the holder boundary.
            hadOccupant = true;
            return TryDropUnclassifiedPawn(access, unclassifiedPawn, map);
        }

        internal static bool TryEject(
            HabitatEjectionAccess access,
            ShuttleHabitatOccupantRecord record,
            Map map,
            bool completedRest)
        {
            if (record == null)
            {
                return false;
            }

            Pawn pawn = record.Pawn;
            if (!access.IsContainedSleepingPawn(pawn))
            {
                access.RemoveSleepingRecord(record);
                return true;
            }

            if (map == null)
            {
                return false;
            }

            Pawn thoughtPawn = pawn;
            if (completedRest)
            {
                string transferFailureReason;
                ShuttlePassengerInternalTransferResult transferResult =
                    access.TryTransferCompletedPassenger(
                        pawn,
                        out transferFailureReason);
                if (transferResult == ShuttlePassengerInternalTransferResult.UnsafeOwnerState)
                {
                    Log.ErrorOnce(
                        "[CeleTech Shuttle] Habitat sleep completion stopped because internal passenger transfer could not prove pawn ownership. pawn=" +
                            pawn +
                            " reason=" +
                            (transferFailureReason ?? "null"),
                        832201 + pawn.thingIDNumber);
                    return false;
                }

                if (transferResult != ShuttlePassengerInternalTransferResult.Transferred)
                {
                    Thing resultingThing;
                    if (!access.TryDropPawn(pawn, map, out resultingThing))
                    {
                        return false;
                    }

                    thoughtPawn = resultingThing as Pawn ?? pawn;
                }
            }
            else
            {
                Thing resultingThing;
                if (!access.TryDropPawn(pawn, map, out resultingThing))
                {
                    return false;
                }

                thoughtPawn = resultingThing as Pawn ?? pawn;
            }

            HabitatProfile habitat;
            access.TryGetSleepHabitat(out habitat);

            if (completedRest && thoughtPawn != null && habitat != null)
            {
                HabitatRestThoughtUtility.TryApplyHabitatSleepThought(
                    thoughtPawn,
                    habitat,
                    record.RestedTicks);
            }

            if (thoughtPawn != null && habitat != null)
            {
                HabitatRestThoughtUtility.RemoveSuppressedNegativeSleepThoughts(thoughtPawn, habitat);
            }

            record.StopSleeping();
            access.RemoveSleepingRecord(record);
            return true;
        }

        internal static bool TryDropDiningPawn(
            HabitatEjectionAccess access,
            ShuttleHabitatDiningOccupantRecord record,
            Map map)
        {
            Pawn pawn = record != null ? record.Pawn : null;
            if (!access.IsContainedDiningPawn(pawn) || map == null)
            {
                return false;
            }

            Thing resultingThing;
            return access.TryDropPawn(pawn, map, out resultingThing);
        }

        internal static bool TryDropJoyPawn(
            HabitatEjectionAccess access,
            ShuttleHabitatJoyOccupantRecord record,
            Map map,
            out Pawn droppedPawn)
        {
            droppedPawn = null;
            Pawn pawn = record != null ? record.Pawn : null;
            if (!access.IsContainedJoyPawn(pawn) || map == null)
            {
                return false;
            }

            Thing resultingThing;
            bool dropped = access.TryDropPawn(pawn, map, out resultingThing);
            droppedPawn = resultingThing as Pawn;

            return dropped;
        }

        internal static bool TryDropUnclassifiedPawn(
            HabitatEjectionAccess access,
            Pawn pawn,
            Map map)
        {
            if (!access.IsHeldPawn(pawn) || map == null)
            {
                return false;
            }

            Thing resultingThing;
            return access.TryDropPawn(pawn, map, out resultingThing);
        }
    }
}
