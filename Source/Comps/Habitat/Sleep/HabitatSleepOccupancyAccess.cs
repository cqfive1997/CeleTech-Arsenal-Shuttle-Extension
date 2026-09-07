using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class HabitatSleepOccupancyAccess
    {
        private readonly ThingWithComps host;
        private readonly ThingOwner<Thing> habitatHeldThings;
        private readonly List<ShuttleHabitatOccupantRecord> sleepingOccupants;
        private readonly Action ensureInitialized;
        private readonly Action reconcileSleepingRecordsToContainedPawns;
        private readonly HabitatSleepRecordEjectDelegate tryEjectSleepingRecord;

        internal HabitatSleepOccupancyAccess(
            ThingWithComps host,
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatOccupantRecord> sleepingOccupants,
            Action ensureInitialized,
            Action reconcileSleepingRecordsToContainedPawns,
            HabitatSleepRecordEjectDelegate tryEjectSleepingRecord)
        {
            this.host = host;
            this.habitatHeldThings = habitatHeldThings;
            this.sleepingOccupants = sleepingOccupants;
            this.ensureInitialized = ensureInitialized;
            this.reconcileSleepingRecordsToContainedPawns = reconcileSleepingRecordsToContainedPawns;
            this.tryEjectSleepingRecord = tryEjectSleepingRecord;
        }

        internal ThingWithComps Host
        {
            get
            {
                return this.host;
            }
        }

        internal Map HostMap
        {
            get
            {
                return this.host != null ? this.host.Map : null;
            }
        }

        internal bool IsSpawnedHost
        {
            get
            {
                return this.host != null && this.host.Spawned;
            }
        }

        internal int SleepingRecordCount
        {
            get
            {
                return this.sleepingOccupants.Count;
            }
        }

        internal void EnsureInitialized()
        {
            if (this.ensureInitialized != null)
            {
                this.ensureInitialized();
            }
        }

        internal bool TryGetSleepHabitat(out HabitatProfile habitat)
        {
            habitat = null;
            ShuttleProfile profile;
            return HabitatUtility.TryGetPoweredHabitatProfile(this.host, out profile, out habitat) &&
                habitat != null &&
                habitat.SupportsSleep &&
                habitat.SleepSlots > 0;
        }

        internal bool ContainsSleepingPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsContainedPawn(
                this.habitatHeldThings,
                this.sleepingOccupants,
                pawn);
        }

        internal bool IsContainedSleepingPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsContainedPawn(
                this.habitatHeldThings,
                this.sleepingOccupants,
                pawn);
        }

        internal ShuttleHabitatOccupantRecord GetSleepingRecord(int index)
        {
            return this.sleepingOccupants[index];
        }

        internal void AddSleepingRecord(Pawn pawn, int restStartTick)
        {
            this.sleepingOccupants.Add(new ShuttleHabitatOccupantRecord(
                pawn,
                restStartTick));
        }

        internal void RemoveSleepingRecordAt(int index)
        {
            this.sleepingOccupants.RemoveAt(index);
        }

        internal void ReconcileSleepingRecordsToContainedPawns()
        {
            if (this.reconcileSleepingRecordsToContainedPawns != null)
            {
                this.reconcileSleepingRecordsToContainedPawns();
            }
        }

        internal bool TryEjectSleepingRecord(
            ShuttleHabitatOccupantRecord record,
            Map map,
            bool completedRest)
        {
            return this.tryEjectSleepingRecord != null &&
                this.tryEjectSleepingRecord(record, map, completedRest);
        }

        internal PawnHolderTransferResult TryMovePawnIntoHabitatHolder(
            Pawn pawn,
            Map map,
            string operation)
        {
            CompShuttleHolderLaunchTransferState transferState = this.GetHolderTransferState();
            return ShuttlePawnHolderTransferUtility.TryMoveSpawnedPawnIntoHolderSafely(
                pawn,
                this.habitatHeldThings,
                map,
                this.GetEjectCell(map),
                transferState != null ? transferState.EmergencyRecoveryThings : null,
                operation);
        }

        internal void HandleFailedPawnHolderTransfer(
            Pawn pawn,
            PawnHolderTransferResult transferResult,
            string operation)
        {
            if (transferResult == null)
            {
                Log.Error("[CeleTech Shuttle] Habitat pawn holder transfer returned null result. operation=" +
                    (operation ?? "null"));
                return;
            }

            if (transferResult.Status == PawnHolderTransferStatus.FatalOwnerless)
            {
                Log.Error("[CeleTech Shuttle] Habitat pawn holder transfer fatal. operation=" +
                    (operation ?? "null") +
                    " context=" +
                    (transferResult.DebugContext ?? "null") +
                    " reason=" +
                    (transferResult.FailureReason ?? "null"));
                return;
            }

            if (transferResult.Status == PawnHolderTransferStatus.RecoveredToEmergencyOwner)
            {
                Log.Warning("[CeleTech Shuttle] Habitat pawn holder transfer recovered pawn to emergency owner; no Habitat record was created. operation=" +
                    (operation ?? "null") +
                    " context=" +
                    (transferResult.DebugContext ?? "null") +
                    " reason=" +
                    (transferResult.FailureReason ?? "null"));
                if (transferResult.WasSelected && this.host != null)
                {
                    Find.Selector.Select(this.host, false, false);
                }

                return;
            }

            if (transferResult.Status == PawnHolderTransferStatus.RecoveredToMap &&
                transferResult.WasSelected &&
                pawn != null &&
                pawn.Spawned)
            {
                Find.Selector.Select(pawn, false, false);
            }
        }

        internal string GetPawnHolderTransferFailureReason(
            PawnHolderTransferResult transferResult,
            string defaultReason)
        {
            return HabitatOccupancyDiagnostics.GetPawnHolderTransferFailureReason(
                transferResult,
                defaultReason);
        }

        private IntVec3 GetEjectCell(Map map)
        {
            return ShuttleHabitatEjectDropUtility.GetEjectCell(this.host, map);
        }

        private CompShuttleHolderLaunchTransferState GetHolderTransferState()
        {
            return this.host != null
                ? this.host.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
        }
    }
}
