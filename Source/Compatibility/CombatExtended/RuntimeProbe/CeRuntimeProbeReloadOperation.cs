namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal sealed class CeRuntimeProbeReloadOperation
    {
        internal int RequestedCount { get; set; }

        internal int OfferedCount { get; set; }

        internal int StagedBefore { get; set; }

        internal int StagedAfter { get; set; }

        internal int LoadedBefore { get; set; }

        internal int LoadedAfter { get; set; }

        internal int CommittedCount { get; set; }

        internal int RolledBackCount { get; set; }

        internal bool MagazineMutationRolledBack { get; set; }

        internal string SourceModel { get; set; }

        internal int CargoStoredBefore { get; set; }

        internal int CargoStoredAfter { get; set; }

        internal int CargoWithdrawn { get; set; }

        internal int CargoSourceThingID { get; set; }

        internal int CargoSourceStackBefore { get; set; }

        internal int CargoSourceStackAfterWithdrawal { get; set; }

        internal bool CargoConsumed { get; set; }

        internal bool CargoRolledBack { get; set; }

        internal string CargoRecoveryMode { get; set; }

        internal bool MagazineCommitAccepted { get; set; }

        internal bool MagazineRollbackRequested { get; set; }

        internal bool MagazineRollbackAccepted { get; set; }
    }
}
