using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class HabitatJoyOccupancyAccess
    {
        private readonly ThingWithComps host;
        private readonly ThingOwner<Thing> habitatHeldThings;
        private readonly List<ShuttleHabitatJoyOccupantRecord> joyOccupants;
        private readonly Action ensureInitialized;
        private readonly Action reconcileJoyRecordsToContainedPawns;
        private readonly HabitatJoyPawnDropDelegate tryDropJoyPawn;

        internal HabitatJoyOccupancyAccess(
            ThingWithComps host,
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatJoyOccupantRecord> joyOccupants,
            Action ensureInitialized,
            Action reconcileJoyRecordsToContainedPawns,
            HabitatJoyPawnDropDelegate tryDropJoyPawn)
        {
            this.host = host;
            this.habitatHeldThings = habitatHeldThings;
            this.joyOccupants = joyOccupants;
            this.ensureInitialized = ensureInitialized;
            this.reconcileJoyRecordsToContainedPawns = reconcileJoyRecordsToContainedPawns;
            this.tryDropJoyPawn = tryDropJoyPawn;
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

        internal int JoyRecordCount
        {
            get
            {
                return this.joyOccupants.Count;
            }
        }

        internal void EnsureInitialized()
        {
            if (this.ensureInitialized != null)
            {
                this.ensureInitialized();
            }
        }

        internal bool TryGetJoyHabitat(out HabitatProfile habitat)
        {
            habitat = null;
            ShuttleProfile profile;
            return HabitatUtility.TryGetPoweredHabitatProfile(this.host, out profile, out habitat) &&
                habitat != null &&
                habitat.SupportsJoy &&
                habitat.JoySlots > 0 &&
                habitat.JoyKinds != null &&
                habitat.JoyKinds.Count > 0;
        }

        internal bool ContainsJoyPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            this.EnsureInitialized();
            return this.IsContainedJoyPawn(pawn);
        }

        internal bool IsContainedJoyPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsContainedJoyPawn(
                this.habitatHeldThings,
                this.joyOccupants,
                pawn);
        }

        internal ShuttleHabitatJoyOccupantRecord GetJoyRecord(int index)
        {
            return this.joyOccupants[index];
        }

        internal void AddJoyRecord(
            Pawn pawn,
            JoyKindDef joyKind,
            int joyStartTick,
            float joyGainRate,
            int maxJoyTicks)
        {
            this.joyOccupants.Add(new ShuttleHabitatJoyOccupantRecord(
                pawn,
                joyKind,
                joyStartTick,
                joyGainRate,
                maxJoyTicks));
        }

        internal void RemoveJoyRecordAt(int index)
        {
            this.joyOccupants.RemoveAt(index);
        }

        internal void RemoveJoyRecord(ShuttleHabitatJoyOccupantRecord record)
        {
            this.joyOccupants.Remove(record);
        }

        internal void ReconcileJoyRecordsToContainedPawns()
        {
            if (this.reconcileJoyRecordsToContainedPawns != null)
            {
                this.reconcileJoyRecordsToContainedPawns();
            }
        }

        internal bool TryDropJoyPawn(
            ShuttleHabitatJoyOccupantRecord record,
            Map map,
            out Pawn droppedPawn)
        {
            droppedPawn = null;
            return this.tryDropJoyPawn != null &&
                this.tryDropJoyPawn(record, map, out droppedPawn);
        }

        internal ShuttlePassengerInternalTransferResult TryTransferCompletedPassenger(
            Pawn pawn,
            out string failureReason)
        {
            return HabitatPassengerInternalTransfer.TryTransfer(
                this.host,
                pawn,
                this.habitatHeldThings,
                out failureReason);
        }

        internal bool TryGetJoyKindForPawn(Pawn pawn, out JoyKindDef joyKind)
        {
            joyKind = null;
            if (pawn == null)
            {
                return false;
            }

            this.EnsureInitialized();
            return HabitatOccupancyQueryService.TryGetJoyKindForPawn(
                this.habitatHeldThings,
                this.joyOccupants,
                pawn,
                out joyKind);
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
