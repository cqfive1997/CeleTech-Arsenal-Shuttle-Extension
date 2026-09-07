using CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatJoyOccupancyService
    {
        internal static bool TryEnterForJoy(
            HabitatJoyOccupancyAccess access,
            Pawn pawn,
            JoyKindDef joyKind,
            float joyGainRate,
            int maxJoyTicks,
            out string failReason)
        {
            failReason = null;
            if (pawn == null || joyKind == null || access.Host == null || !access.IsSpawnedHost)
            {
                failReason = "CT_Shuttle_HabitatUnavailable".Translate().ToString();
                return false;
            }

            HabitatProfile habitat;
            if (!access.TryGetJoyHabitat(out habitat))
            {
                failReason = "CT_Shuttle_HabitatUnavailable".Translate().ToString();
                return false;
            }

            string useFailReason;
            if (!HabitatUtility.CanUseHabitatForJoy(pawn, access.Host, joyKind, out useFailReason))
            {
                failReason = useFailReason;
                return false;
            }

            int currentUsers = HabitatUtility.CountCurrentHabitatJoyUsers(access.Host, pawn);
            if (currentUsers >= habitat.JoySlots)
            {
                failReason = "CT_Shuttle_HabitatSlotsFull".Translate().ToString();
                return false;
            }

            access.EnsureInitialized();
            if (access.ContainsJoyPawn(pawn))
            {
                return true;
            }

            Map map = pawn.Map;
            PawnHolderTransferResult transferResult =
                access.TryMovePawnIntoHabitatHolder(
                pawn,
                map,
                "Habitat joy entry");
            if (transferResult.Status != PawnHolderTransferStatus.MovedToDestination)
            {
                access.HandleFailedPawnHolderTransfer(pawn, transferResult, "Habitat joy entry");
                failReason = access.GetPawnHolderTransferFailureReason(
                    transferResult,
                    "CT_Shuttle_HabitatUnavailable".Translate().ToString());
                return false;
            }

            access.AddJoyRecord(
                pawn,
                joyKind,
                Find.TickManager != null ? Find.TickManager.TicksGame : -1,
                joyGainRate,
                maxJoyTicks);

            if (transferResult.WasSelected)
            {
                Find.Selector.Select(access.Host, false, false);
            }

            return true;
        }

        internal static void TickJoyOccupants(HabitatJoyOccupancyAccess access)
        {
            access.EnsureInitialized();
            access.ReconcileJoyRecordsToContainedPawns();
            if (access.JoyRecordCount == 0)
            {
                return;
            }

            HabitatProfile habitat;
            bool habitatAvailable = access.TryGetJoyHabitat(out habitat);
            Map map = access.HostMap;

            for (int i = access.JoyRecordCount - 1; i >= 0; i--)
            {
                ShuttleHabitatJoyOccupantRecord record = access.GetJoyRecord(i);
                Pawn pawn = record != null ? record.Pawn : null;

                if (!access.IsContainedJoyPawn(pawn))
                {
                    access.RemoveJoyRecordAt(i);
                    continue;
                }

                if (!habitatAvailable ||
                    pawn.Dead ||
                    pawn.Destroyed ||
                    pawn.needs == null ||
                    pawn.needs.joy == null ||
                    record.JoyKind == null ||
                    !habitat.SupportsJoyKind(record.JoyKind))
                {
                    TryAbortJoy(access, record, map);
                    continue;
                }

                float gain = habitat.JoyGainFactor * record.JoyGainRate * 0.36f / 2500f;
                if (gain > 0f)
                {
                    pawn.needs.joy.GainJoy(gain, record.JoyKind);
                }

                record.TickJoy();
                if (pawn.needs.joy.CurLevelPercentage >= 0.999f ||
                    record.JoyTicks >= record.MaxJoyTicks)
                {
                    TryCompleteJoy(access, record, map, habitat);
                }
            }
        }

        internal static bool TryCompleteJoy(
            HabitatJoyOccupancyAccess access,
            ShuttleHabitatJoyOccupantRecord record,
            Map map,
            HabitatProfile habitat)
        {
            if (record == null)
            {
                return false;
            }

            Pawn pawn = record.Pawn;
            if (!access.IsContainedJoyPawn(pawn))
            {
                access.RemoveJoyRecord(record);
                return true;
            }

            if (map == null)
            {
                return false;
            }

            string transferFailureReason;
            ShuttlePassengerInternalTransferResult transferResult =
                access.TryTransferCompletedPassenger(
                    pawn,
                    out transferFailureReason);
            if (transferResult == ShuttlePassengerInternalTransferResult.UnsafeOwnerState)
            {
                Log.ErrorOnce(
                    "[CeleTech Shuttle] Habitat recreation completion stopped because internal passenger transfer could not prove pawn ownership. pawn=" +
                        pawn +
                        " reason=" +
                        (transferFailureReason ?? "null"),
                    832101 + pawn.thingIDNumber);
                return false;
            }

            if (transferResult == ShuttlePassengerInternalTransferResult.Transferred)
            {
                HabitatJoyThoughtUtility.TryApplyHabitatJoyThought(
                    pawn,
                    habitat,
                    record.JoyTicks);

                record.StopJoying();
                access.RemoveJoyRecord(record);
                return true;
            }

            if (!access.TryDropJoyPawn(record, map, out Pawn droppedPawn))
            {
                return false;
            }

            Pawn thoughtPawn = droppedPawn ?? pawn;
            HabitatJoyThoughtUtility.TryApplyHabitatJoyThought(
                thoughtPawn,
                habitat,
                record.JoyTicks);

            record.StopJoying();
            access.RemoveJoyRecord(record);
            return true;
        }

        internal static bool TryAbortJoy(
            HabitatJoyOccupancyAccess access,
            ShuttleHabitatJoyOccupantRecord record,
            Map map)
        {
            if (record == null)
            {
                return false;
            }

            Pawn pawn = record.Pawn;
            if (!access.IsContainedJoyPawn(pawn))
            {
                access.RemoveJoyRecord(record);
                return true;
            }

            if (map == null)
            {
                return false;
            }

            Pawn droppedPawn;
            if (!access.TryDropJoyPawn(record, map, out droppedPawn))
            {
                return false;
            }

            record.StopJoying();
            access.RemoveJoyRecord(record);
            return true;
        }
    }
}
