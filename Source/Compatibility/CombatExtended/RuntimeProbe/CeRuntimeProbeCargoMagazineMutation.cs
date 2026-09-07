using CombatExtended;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    /// <summary>
    /// Owns one reversible CE magazine mutation for the synchronous real-cargo probe.
    /// It neither reads nor mutates shuttle cargo.
    /// </summary>
    internal sealed class CeRuntimeProbeCargoMagazineMutation
    {
        private readonly CeRuntimeProbeReloadSession session;
        private readonly AmmoDef expectedAmmo;
        private readonly bool injectFailure;

        private CompAmmoUser ammo;
        private AmmoDef currentBefore;
        private AmmoDef selectedBefore;
        private int loadedBefore;
        private int committedCount;
        private bool snapshotCaptured;
        private bool applied;

        internal CeRuntimeProbeCargoMagazineMutation(
            CeRuntimeProbeReloadSession session,
            AmmoDef expectedAmmo,
            bool injectFailure)
        {
            this.session = session;
            this.expectedAmmo = expectedAmmo;
            this.injectFailure = injectFailure;
        }

        internal int LoadedBefore
        {
            get { return this.snapshotCaptured ? this.loadedBefore : -1; }
        }

        internal int LoadedAfter
        {
            get { return this.ammo != null ? this.ammo.CurMagCount : -1; }
        }

        internal int CommittedCount
        {
            get { return this.applied ? this.committedCount : 0; }
        }

        internal bool Restored { get; private set; }

        internal string Failure { get; private set; }

        internal bool TryApply(int count)
        {
            this.Failure = null;
            if (!this.TryValidate(count))
            {
                return false;
            }

            this.currentBefore = this.ammo.CurrentAmmo;
            this.selectedBefore = this.ammo.SelectedAmmo;
            this.loadedBefore = this.ammo.CurMagCount;
            this.committedCount = count;
            this.snapshotCaptured = true;

            this.ammo.CurrentAmmo = this.expectedAmmo;
            this.ammo.SelectedAmmo = this.expectedAmmo;
            this.ammo.CurMagCount = this.loadedBefore + count;

            if (this.injectFailure)
            {
                this.Failure = "injected-ce-magazine-rejection";
                this.RestoreSnapshot();
                return false;
            }

            if (!ReferenceEquals(this.ammo.CurrentAmmo, this.expectedAmmo) ||
                !ReferenceEquals(this.ammo.SelectedAmmo, this.expectedAmmo) ||
                this.ammo.CurMagCount != this.loadedBefore + count)
            {
                this.Failure = "ce-magazine-mutation-mismatch";
                this.RestoreSnapshot();
                return false;
            }

            this.applied = true;
            return true;
        }

        internal bool TryRollback()
        {
            this.Failure = null;
            if (!this.snapshotCaptured)
            {
                this.Restored = true;
                return true;
            }

            return this.RestoreSnapshot();
        }

        private bool TryValidate(int count)
        {
            string readyFailure;
            if (!CeRuntimeProbeReloadSessionRunner.IsReadyForAction(
                this.session,
                out readyFailure))
            {
                this.Failure = readyFailure;
                return false;
            }

            this.ammo = this.session.GetAmmo();
            if (this.ammo == null ||
                this.expectedAmmo == null ||
                count <= 0 ||
                this.expectedAmmo.ammoCount != 1)
            {
                this.Failure = "The CE magazine request is invalid.";
                return false;
            }

            if (!SupportsAmmo(this.ammo, this.expectedAmmo))
            {
                this.Failure = "The cargo ammunition is not supported by the active CE ammo set.";
                return false;
            }

            if (this.ammo.CurMagCount < 0 ||
                this.ammo.CurMagCount > this.ammo.MagSize ||
                count > this.ammo.MagSize - this.ammo.CurMagCount)
            {
                this.Failure = "The cargo ammunition count does not fit the CE magazine.";
                return false;
            }

            if (this.ammo.CurMagCount > 0 &&
                !ReferenceEquals(this.ammo.CurrentAmmo, this.expectedAmmo))
            {
                this.Failure = "The cargo ammunition cannot replace a non-empty CE magazine.";
                return false;
            }

            return true;
        }

        private bool RestoreSnapshot()
        {
            this.ammo.CurrentAmmo = this.currentBefore;
            this.ammo.SelectedAmmo = this.selectedBefore;
            this.ammo.CurMagCount = this.loadedBefore;
            this.applied = false;
            this.Restored = ReferenceEquals(this.ammo.CurrentAmmo, this.currentBefore) &&
                ReferenceEquals(this.ammo.SelectedAmmo, this.selectedBefore) &&
                this.ammo.CurMagCount == this.loadedBefore;
            if (!this.Restored)
            {
                this.Failure = "The previous CE magazine state could not be restored.";
            }

            return this.Restored;
        }

        private static bool SupportsAmmo(CompAmmoUser ammo, AmmoDef ammoDef)
        {
            if (ammo == null || ammo.CurAmmoSet == null || ammo.CurAmmoSet.ammoTypes == null)
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
