using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatRestoreService
    {
        internal static bool TryRestoreDevJoyFromLocalStaging(
            HabitatRestoreAccess access,
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string failureReason)
        {
            return HabitatJoyRestoreService.TryRestoreDevJoyFromLocalStaging(
                access,
                manifest,
                source,
                out failureReason);
        }

        internal static bool TryRestoreMixedFromLaunchStaging(
            HabitatRestoreAccess access,
            ShuttleHolderLaunchManifest manifest,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            Map map,
            IntVec3 fallbackCell,
            out string failureReason)
        {
            // Incoming mixed restore is the production safety path. It must remain
            // all-or-nothing so base.Impact never sees Habitat contents as cargo.
            failureReason = null;
            access.EnsureInitialized();
            if (manifest == null)
            {
                failureReason = "No Habitat mixed manifest exists for launch restore.";
                return false;
            }

            manifest.EnsureInitialized();
            bool hasLivingEntries =
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind).Count > 0 ||
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind).Count > 0 ||
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind).Count > 0;
            bool hasJoyEntries =
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind).Count > 0;
            if (!hasLivingEntries || !hasJoyEntries)
            {
                failureReason = "Habitat mixed launch restore requires both living and joy manifest entries.";
                return false;
            }

            HabitatMixedRestorePlan plan;
            if (!TryBuildHabitatMixedRestorePlan(
                access,
                manifest,
                primarySource,
                secondarySource,
                out plan,
                out failureReason))
            {
                return false;
            }

            if (plan == null || !plan.HasEntries)
            {
                failureReason = "Habitat mixed launch restore plan is empty.";
                return false;
            }

            HabitatLivingRestoreTransaction transaction;
            if (!TryApplyHabitatMixedRestorePlan(
                access,
                plan,
                map,
                fallbackCell,
                true,
                out transaction,
                out failureReason))
            {
                return false;
            }

            CommitHabitatMixedRestorePlan(access, plan);
            manifest.RemoveHabitatLivingEntries();
            manifest.RemoveHabitatJoyEntries();
            failureReason = null;
            return true;
        }

        internal static bool TryRestoreJoyFromLaunchStaging(
            HabitatRestoreAccess access,
            ShuttleHolderLaunchManifest manifest,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            Map map,
            IntVec3 fallbackCell,
            out string failureReason)
        {
            return HabitatJoyRestoreService.TryRestoreJoyFromLaunchStaging(
                access,
                manifest,
                primarySource,
                secondarySource,
                map,
                fallbackCell,
                out failureReason);
        }

        internal static bool TryRestoreLivingFromLaunchStaging(
            HabitatRestoreAccess access,
            ShuttleHolderLaunchManifest manifest,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            Map map,
            IntVec3 fallbackCell,
            out string failureReason)
        {
            failureReason = null;

            access.EnsureInitialized();
            if (manifest == null)
            {
                failureReason = "No Habitat living manifest exists for launch restore.";
                return false;
            }

            manifest.EnsureInitialized();
            List<ShuttleHolderLaunchManifestEntry> sleepEntries =
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind);
            List<ShuttleHolderLaunchManifestEntry> diningPawnEntries =
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind);
            List<ShuttleHolderLaunchManifestEntry> diningFoodEntries =
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind);
            if (sleepEntries.Count == 0 && diningPawnEntries.Count == 0 && diningFoodEntries.Count == 0)
            {
                failureReason = "No Habitat living entries exist in the active manifest.";
                return true;
            }

            HabitatLivingRestorePlan plan;
            if (!TryBuildHabitatLivingRestorePlan(
                access,
                sleepEntries,
                diningPawnEntries,
                diningFoodEntries,
                primarySource,
                secondarySource,
                out plan,
                out failureReason))
            {
                return false;
            }

            HabitatLivingRestoreTransaction transaction;
            if (!TryApplyHabitatLivingRestorePlan(
                access,
                plan,
                map,
                fallbackCell,
                out transaction,
                out failureReason))
            {
                return false;
            }

            CommitHabitatLivingRestorePlan(access, plan);
            manifest.RemoveHabitatLivingEntries();
            failureReason = null;
            return true;
        }

        internal static bool TryBuildHabitatMixedRestorePlan(
            HabitatRestoreAccess access,
            ShuttleHolderLaunchManifest manifest,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            out HabitatMixedRestorePlan plan,
            out string failureReason)
        {
            return HabitatRestorePlanBuilder.TryBuildHabitatMixedRestorePlan(
                access,
                manifest,
                primarySource,
                secondarySource,
                out plan,
                out failureReason);
        }

        internal static bool TryBuildHabitatLivingRestorePlan(
            HabitatRestoreAccess access,
            List<ShuttleHolderLaunchManifestEntry> sleepEntries,
            List<ShuttleHolderLaunchManifestEntry> diningPawnEntries,
            List<ShuttleHolderLaunchManifestEntry> diningFoodEntries,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            out HabitatLivingRestorePlan plan,
            out string failureReason)
        {
            return HabitatRestorePlanBuilder.TryBuildHabitatLivingRestorePlan(
                access,
                sleepEntries,
                diningPawnEntries,
                diningFoodEntries,
                primarySource,
                secondarySource,
                out plan,
                out failureReason);
        }

        internal static bool TryApplyHabitatLivingRestorePlan(
            HabitatRestoreAccess access,
            HabitatLivingRestorePlan plan,
            Map map,
            IntVec3 fallbackCell,
            out HabitatLivingRestoreTransaction transaction,
            out string failureReason)
        {
            return HabitatRestoreTransactionRunner.TryApplyHabitatLivingRestorePlan(
                access,
                plan,
                map,
                fallbackCell,
                out transaction,
                out failureReason);
        }

        internal static void CommitHabitatLivingRestorePlan(
            HabitatRestoreAccess access,
            HabitatLivingRestorePlan plan)
        {
            HabitatRestoreCommitter.CommitHabitatLivingRestorePlan(access, plan);
        }

        internal static bool TryApplyHabitatMixedRestorePlan(
            HabitatRestoreAccess access,
            HabitatMixedRestorePlan plan,
            Map map,
            IntVec3 fallbackCell,
            bool allowFoodSplits,
            out HabitatLivingRestoreTransaction transaction,
            out string failureReason)
        {
            return HabitatRestoreTransactionRunner.TryApplyHabitatMixedRestorePlan(
                access,
                plan,
                map,
                fallbackCell,
                allowFoodSplits,
                out transaction,
                out failureReason);
        }

        internal static void CommitHabitatMixedRestorePlan(
            HabitatRestoreAccess access,
            HabitatMixedRestorePlan plan)
        {
            HabitatRestoreCommitter.CommitHabitatMixedRestorePlan(access, plan);
        }
    }
}
