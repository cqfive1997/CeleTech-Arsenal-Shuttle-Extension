using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class ShuttleHolderManifestQueryUtility
    {
        internal static List<int> GetMedicalBayPatientManifestThingIDs(ShuttleHolderLaunchManifest manifest)
        {
            List<int> result = new List<int>();
            if (manifest == null)
            {
                return result;
            }

            List<ShuttleHolderLaunchManifestEntry> entries =
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind);
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry != null && entry.ThingID > 0 && !result.Contains(entry.ThingID))
                {
                    result.Add(entry.ThingID);
                }
            }

            return result;
        }

        internal static bool MechChargerManifestEntriesAreQuarantined(ShuttleHolderLaunchManifest manifest)
        {
            if (manifest == null)
            {
                return false;
            }

            List<ShuttleHolderLaunchManifestEntry> entries =
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MechChargerHolderKind);
            if (entries.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry == null ||
                    entry.TransferPhase != ShuttleHolderLaunchManifestConstants.PhaseQuarantined)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool PrisonCellManifestEntriesAreQuarantined(ShuttleHolderLaunchManifest manifest)
        {
            if (manifest == null)
            {
                return false;
            }

            List<ShuttleHolderLaunchManifestEntry> entries =
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind);
            if (entries.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry == null ||
                    entry.TransferPhase != ShuttleHolderLaunchManifestConstants.PhaseQuarantined)
                {
                    return false;
                }
            }

            return true;
        }

        internal static List<ShuttleHolderLaunchManifestEntry> GetHabitatManifestEntries(
            ShuttleHolderLaunchManifest manifest)
        {
            List<ShuttleHolderLaunchManifestEntry> entries = new List<ShuttleHolderLaunchManifestEntry>();
            if (manifest == null || manifest.Entries == null)
            {
                return entries;
            }

            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = manifest.Entries[i];
                if (entry != null && ShuttleHolderManifestClassificationUtility.IsHabitatManifestHolderKind(entry.HolderKind))
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        internal static string AppendNote(string current, string note)
        {
            if (string.IsNullOrEmpty(note))
            {
                return current;
            }

            return string.IsNullOrEmpty(current) ? note : current + " | " + note;
        }
    }
}
