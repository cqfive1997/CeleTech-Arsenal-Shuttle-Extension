using CombatExtended;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeMagazineCommitter
    {
        internal static bool TryCommit(
            CeRuntimeProbeReloadSession session,
            bool injectRollback,
            out CeRuntimeProbeReloadOperation operation,
            out string failure)
        {
            operation = null;
            if (!CeRuntimeProbeReloadSessionRunner.IsReadyForAction(session, out failure))
            {
                return false;
            }

            CompAmmoUser ammo = session.GetAmmo();
            if (!TryValidate(session, ammo, out failure))
            {
                return false;
            }

            AmmoDef currentBefore = ammo.CurrentAmmo;
            AmmoDef selectedBefore = ammo.SelectedAmmo;
            int loadedBefore = ammo.CurMagCount;
            int stagedBefore = session.StagedCount;
            operation = new CeRuntimeProbeReloadOperation
            {
                RequestedCount = session.RequestedCount,
                OfferedCount = stagedBefore,
                StagedBefore = stagedBefore,
                StagedAfter = stagedBefore,
                LoadedBefore = loadedBefore,
                LoadedAfter = loadedBefore
            };

            ammo.CurrentAmmo = session.StagedAmmo;
            ammo.SelectedAmmo = session.StagedAmmo;
            ammo.CurMagCount = loadedBefore + stagedBefore;

            bool mutationMatches = !injectRollback &&
                ReferenceEquals(ammo.CurrentAmmo, session.StagedAmmo) &&
                ReferenceEquals(ammo.SelectedAmmo, session.StagedAmmo) &&
                ammo.CurMagCount == loadedBefore + stagedBefore;
            if (!mutationMatches)
            {
                ammo.CurrentAmmo = currentBefore;
                ammo.SelectedAmmo = selectedBefore;
                ammo.CurMagCount = loadedBefore;
                operation.MagazineMutationRolledBack = true;
                operation.RolledBackCount = stagedBefore;
                operation.LoadedAfter = ammo.CurMagCount;

                if (!ReferenceEquals(ammo.CurrentAmmo, currentBefore) ||
                    !ReferenceEquals(ammo.SelectedAmmo, selectedBefore) ||
                    ammo.CurMagCount != loadedBefore)
                {
                    session.Active = false;
                    failure = "The CE magazine mutation failed and its previous state could not be restored.";
                    return false;
                }

                session.CaptureExpectedState();
                if (injectRollback)
                {
                    return true;
                }

                failure = "The CE magazine did not accept the staged count exactly; the previous state was restored.";
                return false;
            }

            session.ClearStagedGrant();
            operation.StagedAfter = 0;
            operation.LoadedAfter = ammo.CurMagCount;
            operation.CommittedCount = stagedBefore;
            session.CaptureExpectedState();
            failure = null;
            return true;
        }

        private static bool TryValidate(
            CeRuntimeProbeReloadSession session,
            CompAmmoUser ammo,
            out string failure)
        {
            failure = null;
            if (ammo == null || !ammo.UseAmmo || !ammo.HasMagazine)
            {
                failure = "The CE weapon does not expose an active magazine.";
                return false;
            }

            if (session.StagedAmmo == null || session.StagedCount <= 0)
            {
                failure = "No staged CE ammunition grant is available to commit.";
                return false;
            }

            if (session.StagedAmmo.ammoCount != 1)
            {
                failure = "The first reload probe supports only staged ammunition whose ammoCount is exactly one.";
                return false;
            }

            if (!SupportsAmmo(ammo, session.StagedAmmo))
            {
                failure = "The staged CE ammunition is not part of the weapon's active ammo set.";
                return false;
            }

            if (ammo.CurMagCount < 0 || ammo.CurMagCount > ammo.MagSize)
            {
                failure = "The CE magazine count is outside its valid bounds.";
                return false;
            }

            if (ammo.CurMagCount > 0 &&
                !ReferenceEquals(ammo.CurrentAmmo, session.StagedAmmo))
            {
                failure = "The staged ammo type cannot replace a non-empty CE magazine.";
                return false;
            }

            int missing = ammo.MagSize - ammo.CurMagCount;
            if (session.StagedCount > missing)
            {
                failure = "The staged CE ammunition count exceeds the magazine's missing capacity.";
                return false;
            }

            return true;
        }

        private static bool SupportsAmmo(CompAmmoUser ammo, AmmoDef ammoDef)
        {
            if (ammo.CurAmmoSet == null || ammo.CurAmmoSet.ammoTypes == null)
            {
                return false;
            }

            for (int i = 0; i < ammo.CurAmmoSet.ammoTypes.Count; i++)
            {
                AmmoLink link = ammo.CurAmmoSet.ammoTypes[i];
                if (link != null && ReferenceEquals(link.ammo, ammoDef))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
