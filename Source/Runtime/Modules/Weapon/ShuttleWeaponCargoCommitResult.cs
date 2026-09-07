namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Detached evidence from one synchronous cargo-to-magazine transaction. It owns no Thing,
    /// holder, controller, or backend reference.
    /// </summary>
    internal sealed class ShuttleWeaponCargoCommitResult
    {
        internal string AmmoDefName { get; set; }

        internal int RequestedCount { get; set; }

        internal int OfferedCount { get; set; }

        internal int StoredBefore { get; set; }

        internal int StoredAfter { get; set; }

        internal int SourceThingID { get; set; }

        internal int SourceStackBefore { get; set; }

        internal int SourceStackAfterWithdrawal { get; set; }

        internal bool MagazineCommitAccepted { get; set; }

        internal bool MagazineRollbackRequested { get; set; }

        internal bool MagazineRollbackAccepted { get; set; }

        internal bool CargoConsumed { get; set; }

        internal bool CargoRolledBack { get; set; }

        internal string CargoRecoveryMode { get; set; }
    }
}
