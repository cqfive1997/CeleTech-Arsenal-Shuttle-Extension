using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleHabitatOccupancy
    {
        // EntryPoints

        internal bool TryExportDevLivingForLaunch(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner destination,
            out string failureReason)
        {
            if (!Prefs.DevMode)
            {
                failureReason = "Dev Habitat living export test is DevMode-only.";
                return false;
            }

            return this.TryExportLivingForLaunchCore(
                manifest,
                destination,
                "Dev Habitat living sleep export test",
                "Dev Habitat living dining pawn export test",
                "Dev Habitat living dining food export test",
                "No Habitat sleep or dining occupants are eligible for the Dev Habitat living export test.",
                out failureReason);
        }

        internal bool TryExportLivingForLaunch(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner destination,
            out string failureReason)
        {
            return this.TryExportLivingForLaunchCore(
                manifest,
                destination,
                "Formal Living Habitat sleep launch transfer.",
                "Formal Living Habitat dining pawn launch transfer.",
                "Formal Living Habitat dining food launch transfer.",
                "No Habitat sleep or dining occupants are eligible for Living Habitat launch transfer.",
                out failureReason);
        }

        internal bool TryExportDevMixedForLaunch(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner destination,
            out string failureReason)
        {
            if (!Prefs.DevMode)
            {
                failureReason = "Dev Habitat mixed export test is DevMode-only.";
                return false;
            }

            return this.TryExportMixedForLaunchCore(
                manifest,
                destination,
                "Dev Habitat mixed sleep export test",
                "Dev Habitat mixed dining pawn export test",
                "Dev Habitat mixed dining food export test",
                "Dev Habitat mixed joy export test",
                "local staging",
                out failureReason);
        }

        // Formal mixed export path. It snapshots every supported Habitat activity
        // before moving Things into the launch handoff, and it rolls back the whole
        // batch if any sleep/dining/joy move fails.
        internal bool TryExportMixedForLaunch(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner destination,
            out string failureReason)
        {
            return this.TryExportMixedForLaunchCore(
                manifest,
                destination,
                "Formal Habitat mixed sleep launch transfer.",
                "Formal Habitat mixed dining pawn launch transfer.",
                "Formal Habitat mixed dining food launch transfer.",
                "Formal Habitat mixed joy launch transfer.",
                "ActiveTransporterInfo.innerContainer",
                out failureReason);
        }

        internal bool TryExportDevJoyForLaunch(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner destination,
            out string failureReason)
        {
            if (!Prefs.DevMode)
            {
                failureReason = "Dev Habitat joy export test is DevMode-only.";
                return false;
            }

            return this.TryExportJoyForLaunchCore(
                manifest,
                destination,
                "Dev Habitat joy export test",
                "local staging holder",
                "No Habitat joy occupants are eligible for the Dev Habitat joy export test.",
                out failureReason);
        }

        internal bool TryExportJoyForLaunch(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner destination,
            out string failureReason)
        {
            return this.TryExportJoyForLaunchCore(
                manifest,
                destination,
                "Formal Habitat joy launch transfer.",
                "ActiveTransporterInfo.innerContainer",
                "No Habitat joy occupants are eligible for Habitat joy launch transfer.",
                out failureReason);
        }

        // Validation

        internal bool CanTransferLivingForLaunch(out string failureReason)
        {
            this.EnsureInitialized();
            return HabitatLaunchTransferValidationService.CanTransferLivingForLaunch(
                this.CreateLaunchTransferValidationAccess(),
                out failureReason);
        }

        // Joy-only launch gate. Mixed holders are routed through CanTransferMixedForLaunch.
        internal bool CanTransferJoyOnlyForLaunch(out string failureReason)
        {
            this.EnsureInitialized();
            return HabitatLaunchTransferValidationService.CanTransferJoyOnlyForLaunch(
                this.CreateLaunchTransferValidationAccess(),
                out failureReason);
        }

        internal bool CanTransferMixedForLaunch(out string failureReason)
        {
            this.EnsureInitialized();
            return HabitatLaunchTransferValidationService.CanTransferMixedForLaunch(
                this.CreateLaunchTransferValidationAccess(),
                out failureReason);
        }

        // Formal mixed gate. This rejects partial/ambiguous holder states before
        // launch validation is allowed to proceed.
        private bool TryValidateMixedExportState(out string failureReason)
        {
            return HabitatLaunchTransferValidationService.TryValidateMixedExportState(
                this.CreateLaunchTransferValidationAccess(),
                out failureReason);
        }

        private bool TryValidateLivingExportState(out string failureReason)
        {
            return HabitatLaunchTransferValidationService.TryValidateLivingExportState(
                this.CreateLaunchTransferValidationAccess(),
                out failureReason);
        }

        private bool TryValidateHabitatMixedRestoreHolderConflicts(
            HashSet<int> plannedPawnIDs,
            HashSet<int> plannedFoodSourceThingIDs,
            out string failureReason)
        {
            return HabitatLaunchTransferValidationService.TryValidateHabitatMixedRestoreHolderConflicts(
                this.CreateLaunchTransferValidationAccess(),
                plannedPawnIDs,
                plannedFoodSourceThingIDs,
                out failureReason);
        }

        private bool TryValidateHabitatMixedLocalStagingRestorePlan(
            HabitatMixedRestorePlan plan,
            out string failureReason)
        {
            return HabitatLaunchTransferValidationService.TryValidateHabitatMixedLocalStagingRestorePlan(
                plan,
                out failureReason);
        }

        private HabitatLaunchTransferValidationAccess CreateLaunchTransferValidationAccess()
        {
            return new HabitatLaunchTransferValidationAccess(
                this.habitatHeldThings,
                this.sleepingOccupants,
                this.diningOccupants,
                this.joyOccupants);
        }

        // Manifest / compatibility wrappers

        private Thing FindThingForHabitatRollback(int thingID, ThingOwner source)
        {
            Thing heldThing = ShuttleHolderTransferLookupUtility.FindThingInOwner(this.habitatHeldThings, thingID);
            if (heldThing != null)
            {
                return heldThing;
            }

            return ShuttleHolderTransferLookupUtility.FindThingInOwner(source, thingID);
        }

        private Thing FindThingForHabitatTransfer(int thingID, ThingOwner primarySource, ThingOwner secondarySource)
        {
            Thing heldThing = ShuttleHolderTransferLookupUtility.FindThingInOwner(this.habitatHeldThings, thingID);
            if (heldThing != null)
            {
                return heldThing;
            }

            Thing primaryThing = ShuttleHolderTransferLookupUtility.FindThingInOwner(primarySource, thingID);
            if (primaryThing != null)
            {
                return primaryThing;
            }

            return ShuttleHolderTransferLookupUtility.FindThingInOwner(secondarySource, thingID);
        }

        private ThingOwner FindThingOwnerForHabitatTransfer(Thing thing, ThingOwner primarySource, ThingOwner secondarySource)
        {
            if (thing == null)
            {
                return null;
            }

            if (this.habitatHeldThings != null && this.habitatHeldThings.Contains(thing))
            {
                return this.habitatHeldThings;
            }

            if (primarySource != null && primarySource.Contains(thing))
            {
                return primarySource;
            }

            if (secondarySource != null && secondarySource.Contains(thing))
            {
                return secondarySource;
            }

            return null;
        }

        private ThingOwner FindOwnerForHabitatTransferThing(
            Thing thing,
            ThingOwner primarySource,
            ThingOwner secondarySource)
        {
            if (this.OwnerContainsThing(this.habitatHeldThings, thing))
            {
                return this.habitatHeldThings;
            }

            if (this.OwnerContainsThing(primarySource, thing))
            {
                return primarySource;
            }

            if (this.OwnerContainsThing(secondarySource, thing))
            {
                return secondarySource;
            }

            return null;
        }

        private bool OwnerContainsThing(ThingOwner owner, Thing thing)
        {
            return ShuttleHolderTransferLookupUtility.OwnerContainsThing(owner, thing);
        }

        private int GetDiningFoodStackCount(ShuttleHolderLaunchManifestEntry foodEntry)
        {
            return HabitatLaunchTransferManifestHelper.GetDiningFoodStackCount(foodEntry);
        }

        private ShuttleHolderLaunchManifestEntry FindDiningFoodEntryForActivity(
            List<ShuttleHolderLaunchManifestEntry> foodEntries,
            string activityID)
        {
            return HabitatLaunchTransferManifestHelper.FindDiningFoodEntryForActivity(
                foodEntries,
                activityID);
        }

        private CompShuttleHolderLaunchTransferState GetHolderTransferState()
        {
            return HabitatLaunchTransferManifestHelper.GetHolderTransferState(this.parent);
        }

        private HabitatLaunchExportAccess CreateLaunchExportAccess(ThingOwner destination)
        {
            return new HabitatLaunchExportAccess(
                this.habitatHeldThings,
                this.sleepingOccupants,
                this.diningOccupants,
                this.joyOccupants,
                destination,
                this.EnsureInitialized,
                this.TryValidateLivingExportState,
                this.TryValidateMixedExportState,
                this.CanTransferJoyOnlyForLaunch,
                delegate(ShuttleHolderLaunchManifest rollbackManifest, out string notice)
                {
                    return this.TryRollbackLivingExport(rollbackManifest, destination, out notice);
                },
                delegate(ShuttleHolderLaunchManifest rollbackManifest, out string notice)
                {
                    return this.TryRollbackJoyExport(rollbackManifest, destination, out notice);
                },
                delegate(ShuttleHolderLaunchManifest rollbackManifest, out string notice)
                {
                    return this.TryRollbackMixedExport(rollbackManifest, destination, out notice);
                });
        }

        // Export

        private bool TryExportLivingForLaunchCore(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner destination,
            string sleepDebugNote,
            string diningPawnDebugNote,
            string diningFoodDebugNote,
            string noEligibleReason,
            out string failureReason)
        {
            return HabitatLaunchExportService.TryExportLivingForLaunchCore(
                this.CreateLaunchExportAccess(destination),
                manifest,
                sleepDebugNote,
                diningPawnDebugNote,
                diningFoodDebugNote,
                noEligibleReason,
                out failureReason);
        }

        private bool TryExportJoyForLaunchCore(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner destination,
            string debugNote,
            string destinationLabel,
            string noEligibleReason,
            out string failureReason)
        {
            return HabitatLaunchExportService.TryExportJoyForLaunchCore(
                this.CreateLaunchExportAccess(destination),
                manifest,
                debugNote,
                destinationLabel,
                noEligibleReason,
                out failureReason);
        }

        // Shared mixed export transaction. Live records are removed only after all
        // associated Things have reached the destination owner.
        private bool TryExportMixedForLaunchCore(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner destination,
            string sleepDebugNote,
            string diningPawnDebugNote,
            string diningFoodDebugNote,
            string joyDebugNote,
            string destinationLabel,
            out string failureReason)
        {
            return HabitatLaunchExportService.TryExportMixedForLaunchCore(
                this.CreateLaunchExportAccess(destination),
                manifest,
                sleepDebugNote,
                diningPawnDebugNote,
                diningFoodDebugNote,
                joyDebugNote,
                destinationLabel,
                out failureReason);
        }

        // Restore

        internal bool TryRestoreDevJoyFromLocalStaging(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string failureReason)
        {
            return HabitatRestoreService.TryRestoreDevJoyFromLocalStaging(
                this.CreateRestoreAccess(),
                manifest,
                source,
                out failureReason);
        }

        internal bool TryRestoreDevMixedFromLocalStaging(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string failureReason)
        {
            return HabitatDevMixedLocalRestoreService.TryRestoreDevMixedFromLocalStaging(
                this.CreateDevRestoreAccess(),
                manifest,
                source,
                out failureReason);
        }

        internal bool TryRestoreMixedFromLaunchStaging(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            Map map,
            IntVec3 fallbackCell,
            out string failureReason)
        {
            return HabitatRestoreService.TryRestoreMixedFromLaunchStaging(
                this.CreateRestoreAccess(),
                manifest,
                primarySource,
                secondarySource,
                map,
                fallbackCell,
                out failureReason);
        }

        internal bool TryRestoreJoyFromLaunchStaging(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            Map map,
            IntVec3 fallbackCell,
            out string failureReason)
        {
            return HabitatRestoreService.TryRestoreJoyFromLaunchStaging(
                this.CreateRestoreAccess(),
                manifest,
                primarySource,
                secondarySource,
                map,
                fallbackCell,
                out failureReason);
        }

        internal bool TryRestoreDevLivingFromLocalStaging(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string failureReason)
        {
            return HabitatDevLivingLocalRestoreService.TryRestoreDevLivingFromLocalStaging(
                this.CreateDevRestoreAccess(),
                manifest,
                source,
                out failureReason);
        }

        internal bool TryRestoreLivingFromLaunchStaging(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            Map map,
            IntVec3 fallbackCell,
            out string failureReason)
        {
            return HabitatRestoreService.TryRestoreLivingFromLaunchStaging(
                this.CreateRestoreAccess(),
                manifest,
                primarySource,
                secondarySource,
                map,
                fallbackCell,
                out failureReason);
        }

        internal bool TryBuildHabitatMixedRestorePlan(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            out HabitatMixedRestorePlan plan,
            out string failureReason)
        {
            return HabitatRestoreService.TryBuildHabitatMixedRestorePlan(
                this.CreateRestoreAccess(),
                manifest,
                primarySource,
                secondarySource,
                out plan,
                out failureReason);
        }

        private bool TryApplyHabitatMixedRestorePlan(
            HabitatMixedRestorePlan plan,
            Map map,
            IntVec3 fallbackCell,
            bool allowFoodSplits,
            out HabitatLivingRestoreTransaction transaction,
            out string failureReason)
        {
            return HabitatRestoreService.TryApplyHabitatMixedRestorePlan(
                this.CreateRestoreAccess(),
                plan,
                map,
                fallbackCell,
                allowFoodSplits,
                out transaction,
                out failureReason);
        }

        private void CommitHabitatMixedRestorePlan(HabitatMixedRestorePlan plan)
        {
            HabitatRestoreService.CommitHabitatMixedRestorePlan(this.CreateRestoreAccess(), plan);
        }

        private bool TryReturnThingToHabitatHolder(Thing thing, out string failureReason)
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

        private void LogHabitatRestoreFailure(ShuttleHolderLaunchManifestEntry entry, string failureReason)
        {
            Log.Warning(
                "[CeleTech Shuttle] Habitat living manifest restore failed: " +
                failureReason +
                " entry=" +
                (entry != null ? entry.DumpForDebug() : "null"));
        }

        private HabitatRestoreAccess CreateRestoreAccess()
        {
            this.EnsureInitialized();
            return new HabitatRestoreAccess(
                this.habitatHeldThings,
                this.sleepingOccupants,
                this.diningOccupants,
                this.joyOccupants,
                this.EnsureInitialized,
                this.TryValidateHabitatMixedRestoreHolderConflicts,
                this.TryRollbackHabitatLivingRestoreTransaction,
                this.RollbackMovedJoyRestorePawns,
                this.RollbackMovedJoyRestorePawnsToOriginalOwners);
        }

        private HabitatDevRestoreAccess CreateDevRestoreAccess()
        {
            this.EnsureInitialized();
            return new HabitatDevRestoreAccess(
                this.parent,
                this.habitatHeldThings,
                this.sleepingOccupants,
                this.diningOccupants,
                this.joyOccupants,
                this.EnsureInitialized,
                this.TryValidateHabitatMixedRestoreHolderConflicts,
                this.TryRollbackHabitatLivingRestoreTransaction,
                this.RollbackMovedJoyRestorePawns,
                this.RollbackMovedJoyRestorePawnsToOriginalOwners);
        }

        // Rollback

        internal bool TryRollbackLivingExport(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string notice)
        {
            return HabitatLaunchRollbackService.TryRollbackLivingExport(
                this.CreateLaunchRollbackAccess(),
                manifest,
                source,
                out notice);
        }

        internal bool TryRollbackJoyExport(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string notice)
        {
            return HabitatLaunchRollbackService.TryRollbackJoyExport(
                this.CreateLaunchRollbackAccess(),
                manifest,
                source,
                out notice);
        }

        internal bool TryRollbackDevJoyExport(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string notice)
        {
            return HabitatLaunchRollbackService.TryRollbackDevJoyExport(
                this.CreateLaunchRollbackAccess(),
                manifest,
                source,
                out notice);
        }

        internal bool TryRollbackDevMixedExport(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string notice)
        {
            return HabitatLaunchRollbackService.TryRollbackDevMixedExport(
                this.CreateLaunchRollbackAccess(),
                manifest,
                source,
                out notice);
        }

        internal bool TryRollbackMixedExport(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string notice)
        {
            return HabitatLaunchRollbackService.TryRollbackMixedExport(
                this.CreateLaunchRollbackAccess(),
                manifest,
                source,
                out notice);
        }

        private void TryRollbackHabitatLivingRestoreTransaction(
            HabitatLivingRestoreTransaction transaction,
            Map map,
            IntVec3 fallbackCell,
            ref string failureReason)
        {
            HabitatRestoreRollbackService.TryRollbackHabitatLivingRestoreTransaction(
                this.CreateLaunchRollbackAccess(),
                transaction,
                map,
                fallbackCell,
                ref failureReason);
        }

        private void RollbackMovedJoyRestorePawns(
            List<HabitatJoyRestorePlanEntry> movedEntries,
            ThingOwner source)
        {
            HabitatRestoreRollbackService.RollbackMovedJoyRestorePawns(
                this.CreateLaunchRollbackAccess(),
                movedEntries,
                source);
        }

        private void RollbackMovedJoyRestorePawnsToOriginalOwners(
            List<HabitatJoyRestorePlanEntry> movedEntries,
            Map map,
            IntVec3 fallbackCell,
            ref string failureReason)
        {
            HabitatRestoreRollbackService.RollbackMovedJoyRestorePawnsToOriginalOwners(
                this.CreateLaunchRollbackAccess(),
                movedEntries,
                map,
                fallbackCell,
                ref failureReason);
        }

        private bool TryRollbackThingToOwnerOrRecovery(
            Thing thing,
            ThingOwner preferredOwner,
            Map map,
            IntVec3 fallbackCell,
            string context,
            out string failureReason)
        {
            return HabitatRestoreRollbackService.TryRollbackThingToOwnerOrRecovery(
                this.CreateLaunchRollbackAccess(),
                thing,
                preferredOwner,
                map,
                fallbackCell,
                context,
                out failureReason);
        }

        private void RollbackDiningFoodRestoreAllocations(List<DiningFoodRestoreAllocation> allocations)
        {
            HabitatRestoreRollbackService.RollbackDiningFoodRestoreAllocations(
                this.CreateLaunchRollbackAccess(),
                allocations);
        }

        private void RollbackUnrestoredDiningFoodRestoreAllocations(
            Dictionary<string, DiningFoodRestoreAllocation> allocationsByActivityID,
            HashSet<string> restoredActivityIDs)
        {
            HabitatRestoreRollbackService.RollbackUnrestoredDiningFoodRestoreAllocations(
                this.CreateLaunchRollbackAccess(),
                allocationsByActivityID,
                restoredActivityIDs);
        }

        private HabitatLaunchRollbackAccess CreateLaunchRollbackAccess()
        {
            this.EnsureInitialized();
            return new HabitatLaunchRollbackAccess(
                this.parent,
                this.habitatHeldThings,
                this.sleepingOccupants,
                this.diningOccupants,
                this.joyOccupants,
                this.EnsureInitialized);
        }
    }
}
