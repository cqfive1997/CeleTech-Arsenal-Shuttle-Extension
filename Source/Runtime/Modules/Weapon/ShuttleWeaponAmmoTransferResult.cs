namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Detached result for one synchronous supply-to-magazine commit. A positive committed count
    /// always equals the final magazine delta; partial success is explicit.
    /// </summary>
    internal sealed class ShuttleWeaponAmmoTransferResult
    {
        private ShuttleWeaponAmmoTransferResult(
            bool succeeded,
            int requestedCount,
            int committedCount,
            bool magazineRollbackFailed,
            string failureReason)
        {
            this.Succeeded = succeeded;
            this.RequestedCount = requestedCount;
            this.CommittedCount = committedCount;
            this.MagazineRollbackFailed = magazineRollbackFailed;
            this.FailureReason = failureReason;
        }

        internal bool Succeeded { get; private set; }

        internal int RequestedCount { get; private set; }

        internal int CommittedCount { get; private set; }

        internal bool IsPartial
        {
            get
            {
                return this.Succeeded &&
                    this.CommittedCount > 0 &&
                    this.CommittedCount < this.RequestedCount;
            }
        }

        internal bool MagazineRollbackFailed { get; private set; }

        internal string FailureReason { get; private set; }

        internal static ShuttleWeaponAmmoTransferResult Completed(
            int requestedCount,
            int committedCount,
            string failureReason)
        {
            return new ShuttleWeaponAmmoTransferResult(
                committedCount > 0,
                requestedCount,
                committedCount,
                false,
                failureReason);
        }

        internal static ShuttleWeaponAmmoTransferResult Failed(
            int requestedCount,
            bool magazineRollbackFailed,
            string failureReason)
        {
            return new ShuttleWeaponAmmoTransferResult(
                false,
                requestedCount,
                0,
                magazineRollbackFailed,
                failureReason);
        }
    }
}
