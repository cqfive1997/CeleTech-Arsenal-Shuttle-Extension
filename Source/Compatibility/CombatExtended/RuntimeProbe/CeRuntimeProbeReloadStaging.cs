using System;
using CombatExtended;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeReloadStaging
    {
        internal static bool TryStageInsufficient(
            CeRuntimeProbeReloadSession session,
            out CeRuntimeProbeReloadOperation operation,
            out string failure)
        {
            return TryStage(session, false, out operation, out failure);
        }

        internal static bool TryStageRemaining(
            CeRuntimeProbeReloadSession session,
            out CeRuntimeProbeReloadOperation operation,
            out string failure)
        {
            return TryStage(session, true, out operation, out failure);
        }

        internal static bool TryCancel(
            CeRuntimeProbeReloadSession session,
            out CeRuntimeProbeReloadOperation operation,
            out string failure)
        {
            operation = null;
            if (!CeRuntimeProbeReloadSessionRunner.IsReadyForAction(session, out failure))
            {
                return false;
            }

            if (session.StagedCount <= 0 || session.StagedAmmo == null)
            {
                failure = "No staged CE ammunition grant is available to cancel.";
                return false;
            }

            CompAmmoUser ammo = session.GetAmmo();
            operation = new CeRuntimeProbeReloadOperation
            {
                RequestedCount = session.RequestedCount,
                OfferedCount = session.StagedCount,
                StagedBefore = session.StagedCount,
                StagedAfter = 0,
                LoadedBefore = ammo.CurMagCount,
                LoadedAfter = ammo.CurMagCount,
                RolledBackCount = session.StagedCount
            };
            session.ClearStagedGrant();
            session.CaptureExpectedState();
            return true;
        }

        private static bool TryStage(
            CeRuntimeProbeReloadSession session,
            bool exactRemaining,
            out CeRuntimeProbeReloadOperation operation,
            out string failure)
        {
            operation = null;
            if (!CeRuntimeProbeReloadSessionRunner.IsReadyForAction(session, out failure))
            {
                return false;
            }

            if (session.StagedCount > 0 || session.StagedAmmo != null)
            {
                failure = "A CE ammunition grant is already staged; commit or cancel it first.";
                return false;
            }

            CompAmmoUser ammo = session.GetAmmo();
            if (ammo == null || !ammo.UseAmmo || !ammo.HasMagazine)
            {
                failure = "The CE weapon does not expose an active magazine.";
                return false;
            }

            if (ammo.SelectedAmmo == null || ammo.SelectedAmmo.ammoCount != 1)
            {
                failure = "The first reload probe supports only selected ammunition whose ammoCount is exactly one.";
                return false;
            }

            int missing = ammo.MagSize - ammo.CurMagCount;
            if (missing <= 0)
            {
                failure = "The CE magazine is already full.";
                return false;
            }

            int offered = exactRemaining
                ? missing
                : Math.Max(1, missing / 3);
            if (!exactRemaining && offered >= missing)
            {
                offered = missing - 1;
            }

            if (offered <= 0)
            {
                failure = "The CE magazine is too small for an insufficient-source scenario.";
                return false;
            }

            session.SetStagedGrant(ammo.SelectedAmmo, missing, offered);
            session.CaptureExpectedState();
            operation = new CeRuntimeProbeReloadOperation
            {
                RequestedCount = missing,
                OfferedCount = offered,
                StagedBefore = 0,
                StagedAfter = offered,
                LoadedBefore = ammo.CurMagCount,
                LoadedAfter = ammo.CurMagCount
            };
            return true;
        }
    }
}
