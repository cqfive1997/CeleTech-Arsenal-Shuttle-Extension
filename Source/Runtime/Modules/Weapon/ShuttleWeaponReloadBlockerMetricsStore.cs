using System;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Classifies reload request kinds and blocker reasons for diagnostic summaries.
    /// </summary>
    internal static class ShuttleWeaponReloadBlockerMetricsStore
    {
        private static long blockerNoAmmoSystemTicks;
        private static long blockerAmmoLoaderUnavailableTicks;
        private static long blockerLogisticsUnavailableTicks;
        private static long blockerSourceUnavailableTicks;
        private static long blockerSourceEmptyTicks;
        private static long blockerAutomaticReloadUnavailableTicks;
        private static long blockerManualReloadDisabledTicks;
        private static long blockerIncompatibleAmmoTicks;
        private static long blockerNoReloadNeededTicks;
        private static long blockerUnknownEmptyReasonTicks;
        private static long blockerRetryThrottledNoReasonTicks;
        private static long autoTopOffRequestedTicks;
        private static long requiredForFireRequestedTicks;
        private static long manualRequestedTicks;
        private static long automaticRetryThrottledTicks;
        private static long automaticRetryAllowedTicks;
        private static string blockerReasonNoAmmoSystem;
        private static string blockerReasonAmmoLoaderUnavailable;
        private static string blockerReasonLogisticsUnavailable;
        private static string blockerReasonSourceUnavailable;
        private static string blockerReasonSourceEmpty;
        private static string blockerReasonAutomaticReloadUnavailable;
        private static string blockerReasonManualReloadDisabled;
        private static string blockerReasonIncompatibleAmmo;
        private static string blockerReasonNoCompatibleAmmo;

        internal static void Record(
            ShuttleWeaponAmmoState ammoState,
            bool canFireLoadedAmmo,
            bool reloadNeeded,
            ShuttleWeaponReloadRequestKind requestKind,
            bool automaticRetryAllowed)
        {
            RecordRequestKind(requestKind, automaticRetryAllowed);
            string reason = ammoState.LastReloadBlockerReason;
            if (string.IsNullOrEmpty(reason))
            {
                reason = ammoState.LastAutomaticReloadBlockerReason;
            }

            if (string.IsNullOrEmpty(reason))
            {
                reason = ammoState.LastFailureReason;
            }

            if (string.IsNullOrEmpty(reason))
            {
                RecordMissingReason(
                    canFireLoadedAmmo,
                    reloadNeeded,
                    requestKind,
                    automaticRetryAllowed);
                return;
            }

            EnsureReasonLabels();
            if (!IncrementKnownBlocker(reason))
            {
                blockerUnknownEmptyReasonTicks++;
            }
        }

        internal static void Log(int ticksGame)
        {
            if (!ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns)
            {
                return;
            }

            Verse.Log.Message(
                "[CeleTech Shuttle] PerfSummary weapon reload blocker profile" +
                " ticksGame=" + ticksGame +
                "\n - no ammo system: " + blockerNoAmmoSystemTicks +
                "\n - ammo loader unavailable: " + blockerAmmoLoaderUnavailableTicks +
                "\n - logistics unavailable: " + blockerLogisticsUnavailableTicks +
                "\n - source unavailable: " + blockerSourceUnavailableTicks +
                "\n - source empty: " + blockerSourceEmptyTicks +
                "\n - automatic reload unavailable: " + blockerAutomaticReloadUnavailableTicks +
                "\n - manual reload disabled: " + blockerManualReloadDisabledTicks +
                "\n - incompatible ammo: " + blockerIncompatibleAmmoTicks +
                "\n - no reload needed: " + blockerNoReloadNeededTicks +
                "\n - retry throttled no blocker reason: " + blockerRetryThrottledNoReasonTicks +
                "\n - unknown/empty reason: " + blockerUnknownEmptyReasonTicks +
                "\n - auto top-off requested ticks: " + autoTopOffRequestedTicks +
                "\n - required-for-fire requested ticks: " + requiredForFireRequestedTicks +
                "\n - manual requested ticks: " + manualRequestedTicks +
                "\n - automatic retry throttled ticks: " + automaticRetryThrottledTicks +
                "\n - automatic retry allowed ticks: " + automaticRetryAllowedTicks);
        }

        internal static void Clear()
        {
            blockerNoAmmoSystemTicks = 0L;
            blockerAmmoLoaderUnavailableTicks = 0L;
            blockerLogisticsUnavailableTicks = 0L;
            blockerSourceUnavailableTicks = 0L;
            blockerSourceEmptyTicks = 0L;
            blockerAutomaticReloadUnavailableTicks = 0L;
            blockerManualReloadDisabledTicks = 0L;
            blockerIncompatibleAmmoTicks = 0L;
            blockerNoReloadNeededTicks = 0L;
            blockerUnknownEmptyReasonTicks = 0L;
            blockerRetryThrottledNoReasonTicks = 0L;
            autoTopOffRequestedTicks = 0L;
            requiredForFireRequestedTicks = 0L;
            manualRequestedTicks = 0L;
            automaticRetryThrottledTicks = 0L;
            automaticRetryAllowedTicks = 0L;
        }

        private static void RecordRequestKind(
            ShuttleWeaponReloadRequestKind requestKind,
            bool automaticRetryAllowed)
        {
            if (requestKind == ShuttleWeaponReloadRequestKind.Manual)
            {
                manualRequestedTicks++;
                return;
            }

            if (requestKind == ShuttleWeaponReloadRequestKind.AutoTopOff)
            {
                autoTopOffRequestedTicks++;
            }
            else if (requestKind == ShuttleWeaponReloadRequestKind.RequiredForFire)
            {
                requiredForFireRequestedTicks++;
            }
            else
            {
                return;
            }

            if (automaticRetryAllowed)
            {
                automaticRetryAllowedTicks++;
            }
            else
            {
                automaticRetryThrottledTicks++;
            }
        }

        private static void RecordMissingReason(
            bool canFireLoadedAmmo,
            bool reloadNeeded,
            ShuttleWeaponReloadRequestKind requestKind,
            bool automaticRetryAllowed)
        {
            if (!reloadNeeded)
            {
                blockerNoReloadNeededTicks++;
            }
            else if (!canFireLoadedAmmo &&
                ShuttleWeaponReloadPolicy.IsAutomaticRequest(requestKind) &&
                !automaticRetryAllowed)
            {
                blockerRetryThrottledNoReasonTicks++;
            }
            else if (!canFireLoadedAmmo)
            {
                blockerUnknownEmptyReasonTicks++;
            }
        }

        private static bool IncrementKnownBlocker(string reason)
        {
            if (string.Equals(reason, blockerReasonNoAmmoSystem, StringComparison.Ordinal))
            {
                blockerNoAmmoSystemTicks++;
            }
            else if (string.Equals(reason, blockerReasonAmmoLoaderUnavailable, StringComparison.Ordinal))
            {
                blockerAmmoLoaderUnavailableTicks++;
            }
            else if (string.Equals(reason, blockerReasonLogisticsUnavailable, StringComparison.Ordinal))
            {
                blockerLogisticsUnavailableTicks++;
            }
            else if (string.Equals(reason, blockerReasonSourceUnavailable, StringComparison.Ordinal))
            {
                blockerSourceUnavailableTicks++;
            }
            else if (string.Equals(reason, blockerReasonSourceEmpty, StringComparison.Ordinal))
            {
                blockerSourceEmptyTicks++;
            }
            else if (string.Equals(reason, blockerReasonAutomaticReloadUnavailable, StringComparison.Ordinal))
            {
                blockerAutomaticReloadUnavailableTicks++;
            }
            else if (string.Equals(reason, blockerReasonManualReloadDisabled, StringComparison.Ordinal))
            {
                blockerManualReloadDisabledTicks++;
            }
            else if (string.Equals(reason, blockerReasonIncompatibleAmmo, StringComparison.Ordinal) ||
                string.Equals(reason, blockerReasonNoCompatibleAmmo, StringComparison.Ordinal))
            {
                blockerIncompatibleAmmoTicks++;
            }
            else
            {
                return false;
            }

            return true;
        }

        private static void EnsureReasonLabels()
        {
            if (blockerReasonNoAmmoSystem != null)
            {
                return;
            }

            blockerReasonNoAmmoSystem = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
            blockerReasonAmmoLoaderUnavailable = "CT_Shuttle_WeaponAmmo_AmmoLoaderUnavailable".Translate().ToString();
            blockerReasonLogisticsUnavailable = "CT_Shuttle_WeaponAmmo_LogisticsUnavailable".Translate().ToString();
            blockerReasonSourceUnavailable = "CT_Shuttle_WeaponAmmo_SourceUnavailable".Translate().ToString();
            blockerReasonSourceEmpty = "CT_Shuttle_WeaponAmmo_SourceEmpty".Translate().ToString();
            blockerReasonAutomaticReloadUnavailable = "CT_Shuttle_WeaponAmmo_AutomaticReloadUnavailable".Translate().ToString();
            blockerReasonManualReloadDisabled = "CT_Shuttle_WeaponAmmo_ManualReloadDisabled".Translate().ToString();
            blockerReasonIncompatibleAmmo = "CT_Shuttle_WeaponAmmo_Incompatible".Translate().ToString();
            blockerReasonNoCompatibleAmmo = "CT_Shuttle_WeaponAmmo_NoCompatibleAmmo".Translate().ToString();
        }
    }
}
