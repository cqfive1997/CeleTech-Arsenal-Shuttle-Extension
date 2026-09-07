using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatDevMixedLocalRestoreService
    {
        internal static bool TryRestoreDevMixedFromLocalStaging(
            HabitatDevRestoreAccess owner,
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string failureReason)
        {
            failureReason = null;
            if (!Prefs.DevMode)
            {
                failureReason = "Dev Habitat mixed local restore test is DevMode-only.";
                return false;
            }

            owner.EnsureInitialized();
            if (manifest == null)
            {
                failureReason = "No Habitat mixed manifest exists for restore.";
                return false;
            }

            if (source == null)
            {
                failureReason = "Habitat mixed local staging holder is missing.";
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
                failureReason = "Habitat mixed restore requires both living and joy manifest entries.";
                return false;
            }

            HabitatMixedRestorePlan plan;
            if (!owner.TryBuildHabitatMixedRestorePlan(
                manifest,
                source,
                null,
                out plan,
                out failureReason))
            {
                return false;
            }

            if (plan == null || !plan.HasEntries)
            {
                failureReason = "Habitat mixed restore plan is empty.";
                return false;
            }

            if (!owner.TryValidateHabitatMixedLocalStagingRestorePlan(plan, out failureReason))
            {
                return false;
            }

            HabitatLivingRestoreTransaction transaction;
            if (!owner.TryApplyHabitatMixedRestorePlan(
                plan,
                null,
                IntVec3.Invalid,
                false,
                out transaction,
                out failureReason))
            {
                return false;
            }

            if (source.Count != 0)
            {
                failureReason = "Habitat mixed local staging restore moved manifest Things but staging is not empty. count=" +
                    source.Count;
                owner.TryRollbackHabitatLivingRestoreTransaction(
                    transaction,
                    null,
                    IntVec3.Invalid,
                    ref failureReason);
                return false;
            }

            owner.CommitHabitatMixedRestorePlan(plan);
            manifest.RemoveHabitatLivingEntries();
            manifest.RemoveHabitatJoyEntries();
            failureReason = null;
            return true;
        }
    }
}
