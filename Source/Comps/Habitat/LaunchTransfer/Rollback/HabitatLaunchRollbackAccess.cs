using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class HabitatLaunchRollbackAccess
    {
        private readonly ThingWithComps host;
        private readonly ThingOwner<Thing> habitatHeldThings;
        private readonly List<ShuttleHabitatOccupantRecord> sleepingOccupants;
        private readonly List<ShuttleHabitatDiningOccupantRecord> diningOccupants;
        private readonly List<ShuttleHabitatJoyOccupantRecord> joyOccupants;
        private readonly Action ensureInitialized;

        internal HabitatLaunchRollbackAccess(
            ThingWithComps host,
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatOccupantRecord> sleepingOccupants,
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants,
            List<ShuttleHabitatJoyOccupantRecord> joyOccupants,
            Action ensureInitialized)
        {
            this.host = host;
            this.habitatHeldThings = habitatHeldThings;
            this.sleepingOccupants = sleepingOccupants;
            this.diningOccupants = diningOccupants;
            this.joyOccupants = joyOccupants;
            this.ensureInitialized = ensureInitialized;
        }

        internal void EnsureInitialized()
        {
            if (this.ensureInitialized != null)
            {
                this.ensureInitialized();
            }
        }

        internal Thing FindThingForHabitatRollback(int thingID, ThingOwner source)
        {
            Thing heldThing = ShuttleHolderTransferLookupUtility.FindThingInOwner(this.habitatHeldThings, thingID);
            if (heldThing != null)
            {
                return heldThing;
            }

            return ShuttleHolderTransferLookupUtility.FindThingInOwner(source, thingID);
        }

        internal bool TryReturnThingToHabitatHolder(Thing thing, out string failureReason)
        {
            failureReason = null;
            if (thing == null || thing.Destroyed)
            {
                failureReason = "Cannot return missing or destroyed Habitat export thing.";
                return false;
            }

            if (this.IsHeldThing(thing))
            {
                return true;
            }

            if (!this.habitatHeldThings.TryAddOrTransfer(thing, false) || !this.IsHeldThing(thing))
            {
                failureReason = "Failed to return Habitat export thing to habitatHeldThings. thingID=" +
                    thing.thingIDNumber;
                return false;
            }

            return true;
        }

        internal bool HasRecordFor(Pawn pawn)
        {
            return HabitatOccupancyQueryService.HasRecordFor(this.sleepingOccupants, pawn);
        }

        internal bool HasDiningRecordFor(Pawn pawn)
        {
            return HabitatOccupancyQueryService.HasDiningRecordFor(this.diningOccupants, pawn);
        }

        internal bool HasJoyRecordFor(Pawn pawn)
        {
            return HabitatOccupancyQueryService.HasJoyRecordFor(this.joyOccupants, pawn);
        }

        internal void AddSleepingRecord(
            Pawn pawn,
            int restStartTick,
            int restedTicks,
            bool isSleeping)
        {
            this.sleepingOccupants.Add(new ShuttleHabitatOccupantRecord(
                pawn,
                restStartTick,
                restedTicks,
                isSleeping));
        }

        internal void AddDiningRecord(
            Pawn pawn,
            Thing food,
            int diningStartTick,
            int chewTicksLeft,
            int chewTicksTotal,
            bool isDining,
            bool finalized)
        {
            this.diningOccupants.Add(new ShuttleHabitatDiningOccupantRecord(
                pawn,
                food,
                diningStartTick,
                chewTicksLeft,
                chewTicksTotal,
                isDining,
                finalized));
        }

        internal void AddJoyRecord(
            Pawn pawn,
            RimWorld.JoyKindDef joyKind,
            int joyStartTick,
            int joyTicks,
            float joyGainRate,
            int maxJoyTicks,
            bool isJoying)
        {
            this.joyOccupants.Add(new ShuttleHabitatJoyOccupantRecord(
                pawn,
                joyKind,
                joyStartTick,
                joyTicks,
                joyGainRate,
                maxJoyTicks,
                isJoying));
        }

        internal ShuttleHolderLaunchManifestEntry FindDiningFoodEntryForActivity(
            List<ShuttleHolderLaunchManifestEntry> foodEntries,
            string activityID)
        {
            return HabitatLaunchTransferManifestHelper.FindDiningFoodEntryForActivity(
                foodEntries,
                activityID);
        }

        internal bool IsHeldThing(Thing thing)
        {
            return HabitatOccupancyQueryService.IsHeldThing(this.habitatHeldThings, thing);
        }

        internal bool OwnerContainsThing(ThingOwner owner, Thing thing)
        {
            return ShuttleHolderTransferLookupUtility.OwnerContainsThing(owner, thing);
        }

        internal Map GetParentMap()
        {
            return this.host != null ? this.host.Map : null;
        }

        internal IntVec3 GetEjectCell(Map map)
        {
            return ShuttleHabitatEjectDropUtility.GetEjectCell(this.host, map);
        }

        internal bool TrySafeEjectRestoreThing(Thing thing, Map map, IntVec3 fallbackCell)
        {
            if (thing == null || thing.Destroyed || map == null || !fallbackCell.IsValid)
            {
                return false;
            }

            if (thing.Spawned)
            {
                return true;
            }

            Thing resultingThing;
            if (this.IsHeldThing(thing))
            {
                return this.habitatHeldThings.TryDrop(
                    thing,
                    fallbackCell,
                    map,
                    ThingPlaceMode.Near,
                    out resultingThing,
                    null,
                    null);
            }

            return ShuttleHabitatEjectDropUtility.TryPlaceThingNear(thing, fallbackCell, map);
        }

        internal bool TryRecoverTransferThing(
            Thing thing,
            string context,
            ThingOwner preferredOwner,
            Map map,
            IntVec3 fallbackCell,
            out string recoveryFailureReason)
        {
            recoveryFailureReason = null;
            CompShuttleHolderLaunchTransferState transferState =
                HabitatLaunchTransferManifestHelper.GetHolderTransferState(this.host);
            if (transferState == null)
            {
                recoveryFailureReason = "holder transfer state unavailable";
                return false;
            }

            ShuttleTransferRecoveryStatus recoveryStatus;
            return transferState.TryRecoverTransferThing(
                thing,
                context,
                preferredOwner,
                null,
                map,
                fallbackCell,
                out recoveryStatus,
                out recoveryFailureReason);
        }
    }
}
