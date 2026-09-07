using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Detached facts from one core-magazine completion attempt. Retry, continuation and
    /// diagnostics consume this result without entering the synchronous transfer transaction.
    /// </summary>
    internal sealed class ShuttleWeaponReloadCompletionResult
    {
        private ShuttleWeaponReloadCompletionResult(
            bool succeeded,
            bool transferCommitted,
            bool fullReload,
            bool sourceStillHasAmmo,
            ShuttleWeaponAmmoDef selectedAmmo,
            ShuttleWeaponReloadExecutorKind completedExecutorKind,
            int loadedBefore,
            int requestedCount,
            int committedCount,
            string failureReason)
        {
            this.Succeeded = succeeded;
            this.TransferCommitted = transferCommitted;
            this.FullReload = fullReload;
            this.SourceStillHasAmmo = sourceStillHasAmmo;
            this.SelectedAmmo = selectedAmmo;
            this.CompletedExecutorKind = completedExecutorKind;
            this.LoadedBefore = loadedBefore;
            this.RequestedCount = requestedCount;
            this.CommittedCount = committedCount;
            this.FailureReason = failureReason;
        }

        internal bool Succeeded { get; private set; }

        internal bool TransferCommitted { get; private set; }

        internal bool FullReload { get; private set; }

        internal bool PartialReload
        {
            get { return this.TransferCommitted && !this.FullReload; }
        }

        internal bool SourceStillHasAmmo { get; private set; }

        internal ShuttleWeaponAmmoDef SelectedAmmo { get; private set; }

        internal ShuttleWeaponReloadExecutorKind CompletedExecutorKind { get; private set; }

        internal int LoadedBefore { get; private set; }

        internal int RequestedCount { get; private set; }

        internal int CommittedCount { get; private set; }

        internal string FailureReason { get; private set; }

        internal static ShuttleWeaponReloadCompletionResult Failed(string failureReason)
        {
            return new ShuttleWeaponReloadCompletionResult(
                false,
                false,
                false,
                false,
                null,
                ShuttleWeaponReloadExecutorKind.None,
                0,
                0,
                0,
                failureReason);
        }

        internal static ShuttleWeaponReloadCompletionResult CompletedWithoutTransfer()
        {
            return new ShuttleWeaponReloadCompletionResult(
                true,
                false,
                true,
                false,
                null,
                ShuttleWeaponReloadExecutorKind.None,
                0,
                0,
                0,
                null);
        }

        internal static ShuttleWeaponReloadCompletionResult CompletedTransfer(
            bool fullReload,
            bool sourceStillHasAmmo,
            ShuttleWeaponAmmoDef selectedAmmo,
            ShuttleWeaponReloadExecutorKind completedExecutorKind,
            int loadedBefore,
            int requestedCount,
            int committedCount,
            string failureReason)
        {
            return new ShuttleWeaponReloadCompletionResult(
                true,
                true,
                fullReload,
                sourceStillHasAmmo,
                selectedAmmo,
                completedExecutorKind,
                loadedBefore,
                requestedCount,
                committedCount,
                failureReason);
        }
    }
}
