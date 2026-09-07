using CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatSleepOccupancyService
    {
        private const int ThoughtCleanupIntervalTicks = 250;
        private const float FullRestLevel = 0.99f;

        internal static bool TryEnterForSleep(
            HabitatSleepOccupancyAccess access,
            Pawn pawn,
            out string failReason)
        {
            failReason = null;
            if (pawn == null || !access.IsSpawnedHost)
            {
                failReason = "CT_Shuttle_HabitatUnavailable".Translate().ToString();
                return false;
            }

            HabitatProfile habitat;
            if (!access.TryGetSleepHabitat(out habitat))
            {
                failReason = "CT_Shuttle_HabitatUnavailable".Translate().ToString();
                return false;
            }

            string useFailReason;
            if (!HabitatUtility.CanUseHabitatForSleep(pawn, access.Host, out useFailReason))
            {
                failReason = useFailReason;
                return false;
            }

            int currentUsers = HabitatUtility.CountCurrentHabitatSleepUsers(access.Host, pawn);
            if (currentUsers >= habitat.SleepSlots)
            {
                failReason = "CT_Shuttle_HabitatSlotsFull".Translate().ToString();
                return false;
            }

            access.EnsureInitialized();
            if (access.ContainsSleepingPawn(pawn))
            {
                return true;
            }

            Map map = pawn.Map;
            PawnHolderTransferResult transferResult =
                access.TryMovePawnIntoHabitatHolder(
                pawn,
                map,
                "Habitat sleep entry");
            if (transferResult.Status != PawnHolderTransferStatus.MovedToDestination)
            {
                access.HandleFailedPawnHolderTransfer(pawn, transferResult, "Habitat sleep entry");
                failReason = access.GetPawnHolderTransferFailureReason(
                    transferResult,
                    "CT_Shuttle_HabitatUnavailable".Translate().ToString());
                return false;
            }

            access.AddSleepingRecord(
                pawn,
                Find.TickManager.TicksGame);

            if (transferResult.WasSelected)
            {
                Find.Selector.Select(access.Host, false, false);
            }

            return true;
        }

        internal static void TickSleepingOccupants(HabitatSleepOccupancyAccess access)
        {
            access.EnsureInitialized();
            access.ReconcileSleepingRecordsToContainedPawns();
            if (access.SleepingRecordCount == 0)
            {
                return;
            }

            HabitatProfile habitat;
            bool habitatAvailable = access.TryGetSleepHabitat(out habitat);
            Map map = access.HostMap;

            MarkContainedPawnsResting(access, habitatAvailable, habitat, map);
            access.ReconcileSleepingRecordsToContainedPawns();

            for (int i = access.SleepingRecordCount - 1; i >= 0; i--)
            {
                ShuttleHabitatOccupantRecord record = access.GetSleepingRecord(i);
                Pawn pawn = record != null ? record.Pawn : null;
                if (!access.IsContainedSleepingPawn(pawn))
                {
                    access.RemoveSleepingRecordAt(i);
                    continue;
                }

                if (!habitatAvailable || pawn.needs == null || pawn.needs.rest == null)
                {
                    access.TryEjectSleepingRecord(record, map, false);
                    continue;
                }

                if (RestUtility.ShouldWakeUp(pawn) ||
                    pawn.needs.rest.CurLevelPercentage >= FullRestLevel)
                {
                    access.TryEjectSleepingRecord(record, map, true);
                }
            }
        }

        private static void MarkContainedPawnsResting(
            HabitatSleepOccupancyAccess access,
            bool habitatAvailable,
            HabitatProfile habitat,
            Map map)
        {
            for (int i = access.SleepingRecordCount - 1; i >= 0; i--)
            {
                ShuttleHabitatOccupantRecord record = access.GetSleepingRecord(i);
                Pawn pawn = record != null ? record.Pawn : null;
                if (!access.IsContainedSleepingPawn(pawn))
                {
                    access.RemoveSleepingRecordAt(i);
                    continue;
                }

                if (!habitatAvailable || pawn.needs == null || pawn.needs.rest == null)
                {
                    access.TryEjectSleepingRecord(record, map, false);
                    continue;
                }

                pawn.needs.rest.TickResting(habitat.RestEffectiveness);
                record.TickRested();

                if (record.RestedTicks % ThoughtCleanupIntervalTicks == 0)
                {
                    HabitatRestThoughtUtility.RemoveSuppressedNegativeSleepThoughts(pawn, habitat);
                }
            }
        }
    }
}
