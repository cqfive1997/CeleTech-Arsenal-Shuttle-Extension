namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal sealed class CeWeaponMagazineAuthorityTransferProbeReport
    {
        internal string ModuleInstanceID { get; set; }

        internal string ModuleDefName { get; set; }

        internal string AmmoDefName { get; set; }

        internal int SourceLoadedCount { get; set; }

        internal string AuthorityBefore { get; set; }

        internal string AuthorityAfterCommit { get; set; }

        internal string AuthorityAfterRollback { get; set; }

        internal bool CommitAccepted { get; set; }

        internal bool RollbackAccepted { get; set; }

        internal bool SourceRestored { get; set; }

        internal string Failure { get; set; }

        internal bool Passed
        {
            get
            {
                return this.CommitAccepted &&
                    this.RollbackAccepted &&
                    this.SourceRestored &&
                    string.IsNullOrEmpty(this.Failure);
            }
        }
    }
}
