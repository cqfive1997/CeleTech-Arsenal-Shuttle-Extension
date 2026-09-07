using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static partial class ShuttleHolderLaunchTransferService
    {
        internal static bool TryClearManifest(ThingWithComps shuttleHost)
        {
            string failureReason;
            return TryClearManifest(shuttleHost, out failureReason);
        }

        internal static bool TryClearManifest(ThingWithComps shuttleHost, out string failureReason)
        {
            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] Holder transfer manifest clear failed: state comp unavailable.";
                return false;
            }

            string clearReason;
            if (!state.HasOnlyClearableDevTestManifestEntries(out clearReason))
            {
                failureReason = "[CeleTech Shuttle] Refusing to clear holder transfer manifest because only DevTest manifests are clearable: " +
                    (clearReason ?? "No reason provided.") +
                    " " +
                    state.DescribeActiveOrRecoveryTransferBlocker();
                Log.Warning(failureReason);
                return false;
            }

            state.ClearManifest();
            failureReason = null;
            return true;
        }

        internal static bool HasOnlyActiveHabitatLaunchTransferManifest(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] Holder transfer state is unavailable.";
                return false;
            }

            if (!state.HasActiveManifest)
            {
                failureReason = "[CeleTech Shuttle] No active holder transfer manifest is present.";
                return false;
            }

            return ShuttleHolderManifestClassificationUtility.ManifestContainsOnlyHabitatEntries(state.Manifest, out failureReason);
        }

        internal static bool HasOnlyActiveMedicalBayLaunchTransferManifest(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] Holder transfer state is unavailable.";
                return false;
            }

            if (!state.HasActiveManifest)
            {
                failureReason = "[CeleTech Shuttle] No active holder transfer manifest is present.";
                return false;
            }

            return ShuttleHolderManifestClassificationUtility.ManifestContainsOnlyMedicalBayPatientEntries(state.Manifest, out failureReason);
        }

        internal static bool HasOnlyActiveHabitatAndMedicalBayLaunchTransferManifest(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] Holder transfer state is unavailable.";
                return false;
            }

            if (!state.HasActiveManifest)
            {
                failureReason = "[CeleTech Shuttle] No active holder transfer manifest is present.";
                return false;
            }

            return ShuttleHolderManifestClassificationUtility.ManifestContainsOnlyHabitatAndMedicalBayEntries(state.Manifest, out failureReason);
        }

        internal static string DumpManifest(ThingWithComps shuttleHost)
        {
            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            return state != null ? state.DumpManifestForDebug() : "Holder transfer state comp unavailable.";
        }

    }
}
