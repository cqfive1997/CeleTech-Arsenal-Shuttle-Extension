using CeleTech.ShuttleExtension.ModularShuttle.Comps;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class ShuttleHolderManifestClassificationUtility
    {
        internal static bool ManifestContainsOnlyKnownIncomingRestoreHolderKinds(
            ShuttleHolderLaunchManifest manifest,
            out string failureReason)
        {
            failureReason = null;
            if (manifest == null ||
                manifest.Entries == null ||
                manifest.Entries.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = manifest.Entries[i];
                if (entry == null)
                {
                    failureReason = "null manifest entry at index=" + i + ".";
                    return false;
                }

                if (entry.HolderKind == ShuttleHolderLaunchManifestConstants.DevTestHolderKind ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.MechChargerHolderKind ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind)
                {
                    continue;
                }

                failureReason =
                    "unknown manifest entry at index=" +
                    i +
                    " holderKind=" +
                    (entry.HolderKind ?? "null") +
                    " thingID=" +
                    entry.ThingID +
                    " phase=" +
                    (entry.TransferPhase ?? "null") +
                    ".";
                return false;
            }

            return true;
        }

        internal static bool ManifestContainsOnlyHabitatEntries(
            ShuttleHolderLaunchManifest manifest,
            out string failureReason)
        {
            failureReason = null;
            if (manifest == null ||
                manifest.Entries == null ||
                manifest.Entries.Count == 0)
            {
                failureReason = "No Habitat manifest entries are active for this holder transfer transaction.";
                return false;
            }

            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = manifest.Entries[i];
                if (entry == null)
                {
                    failureReason = "Null manifest entry at index=" + i + ".";
                    return false;
                }

                if (entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind)
                {
                    if (entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseFailed ||
                        entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseRolledBack ||
                        entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseRestored ||
                        entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseEjected ||
                        entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseQuarantined)
                    {
                        failureReason = "Active Habitat manifest entry is not export-active. phase=" +
                            (entry.TransferPhase ?? "null") +
                            " index=" +
                            i +
                            ".";
                        return false;
                    }

                    continue;
                }

                failureReason = "Active manifest contains non-Habitat holder kind " +
                    (entry.HolderKind ?? "null") +
                    " at index=" +
                    i +
                    ".";
                return false;
            }

            return true;
        }

        internal static bool ManifestContainsOnlyMedicalBayPatientEntries(
            ShuttleHolderLaunchManifest manifest,
            out string failureReason)
        {
            failureReason = null;
            if (manifest == null ||
                manifest.Entries == null ||
                manifest.Entries.Count == 0)
            {
                failureReason = "No MedicalBay patient manifest entries are active for this holder transfer transaction.";
                return false;
            }

            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = manifest.Entries[i];
                if (entry == null)
                {
                    failureReason = "Null manifest entry at index=" + i + ".";
                    return false;
                }

                if (entry.HolderKind != ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind)
                {
                    failureReason = "Active manifest contains non-MedicalBay holder kind " +
                        (entry.HolderKind ?? "null") +
                        " at index=" +
                        i +
                        ".";
                    return false;
                }

                if (entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseFailed ||
                    entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseRolledBack ||
                    entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseRestored ||
                    entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseEjected ||
                    entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseQuarantined)
                {
                    failureReason = "Active MedicalBay manifest entry is not export-active. phase=" +
                        (entry.TransferPhase ?? "null") +
                        " index=" +
                        i +
                        ".";
                    return false;
                }
            }

            return true;
        }

        internal static bool ManifestContainsOnlyHabitatAndMedicalBayEntries(
            ShuttleHolderLaunchManifest manifest,
            out string failureReason)
        {
            failureReason = null;
            if (manifest == null ||
                manifest.Entries == null ||
                manifest.Entries.Count == 0)
            {
                failureReason = "No Habitat or MedicalBay manifest entries are active for this holder transfer transaction.";
                return false;
            }

            bool hasHabitatEntry = false;
            bool hasMedicalBayEntry = false;
            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = manifest.Entries[i];
                if (entry == null)
                {
                    failureReason = "Null manifest entry at index=" + i + ".";
                    return false;
                }

                bool isHabitatEntry =
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind;
                bool isMedicalBayEntry =
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind;
                if (!isHabitatEntry && !isMedicalBayEntry)
                {
                    failureReason = "Active manifest contains holder kind outside Habitat + MedicalBay transaction " +
                        (entry.HolderKind ?? "null") +
                        " at index=" +
                        i +
                        ".";
                    return false;
                }

                if (entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseFailed ||
                    entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseRolledBack ||
                    entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseRestored ||
                    entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseEjected ||
                    entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseQuarantined)
                {
                    failureReason = "Active Habitat + MedicalBay manifest entry is not export-active. phase=" +
                        (entry.TransferPhase ?? "null") +
                        " index=" +
                        i +
                        ".";
                    return false;
                }

                hasHabitatEntry = hasHabitatEntry || isHabitatEntry;
                hasMedicalBayEntry = hasMedicalBayEntry || isMedicalBayEntry;
            }

            if (!hasHabitatEntry || !hasMedicalBayEntry)
            {
                failureReason = "Active manifest is not a Habitat + MedicalBay transaction. hasHabitat=" +
                    hasHabitatEntry +
                    " hasMedicalBay=" +
                    hasMedicalBayEntry +
                    ".";
                return false;
            }

            return true;
        }

        internal static bool IsHabitatManifestHolderKind(string holderKind)
        {
            return holderKind == ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind ||
                holderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind ||
                holderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind ||
                holderKind == ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind;
        }

        internal static bool ManifestContainsOnlyExportActiveKnownHolderEntries(
            ShuttleHolderLaunchManifest manifest,
            out string failureReason)
        {
            failureReason = null;
            if (manifest == null ||
                manifest.Entries == null ||
                manifest.Entries.Count == 0)
            {
                failureReason = "No holder manifest entries are active for this transaction.";
                return false;
            }

            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = manifest.Entries[i];
                if (entry == null)
                {
                    failureReason = "Null manifest entry at index=" + i + ".";
                    return false;
                }

                if (!IsKnownHolderKind(entry.HolderKind))
                {
                    failureReason = "Active manifest contains unknown holder kind " +
                        (entry.HolderKind ?? "null") +
                        " at index=" +
                        i +
                        ".";
                    return false;
                }

                if (entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseFailed ||
                    entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseRolledBack ||
                    entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseRestored ||
                    entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseEjected ||
                    entry.TransferPhase == ShuttleHolderLaunchManifestConstants.PhaseQuarantined)
                {
                    failureReason = "Active holder manifest entry is not export-active. phase=" +
                        (entry.TransferPhase ?? "null") +
                        " index=" +
                        i +
                        ".";
                    return false;
                }
            }

            return true;
        }

        private static bool IsKnownHolderKind(string holderKind)
        {
            return holderKind == ShuttleHolderLaunchManifestConstants.DevTestHolderKind ||
                holderKind == ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind ||
                holderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind ||
                holderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind ||
                holderKind == ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind ||
                holderKind == ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind ||
                holderKind == ShuttleHolderLaunchManifestConstants.MechChargerHolderKind ||
                holderKind == ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind;
        }
    }
}
