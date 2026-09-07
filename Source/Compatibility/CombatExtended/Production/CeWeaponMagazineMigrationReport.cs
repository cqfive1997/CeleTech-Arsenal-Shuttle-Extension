namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal sealed class CeWeaponMagazineMigrationReport
    {
        internal string ModuleInstanceID { get; set; }

        internal string ModuleDefName { get; set; }

        internal string RuntimeGunDefName { get; set; }

        internal string AmmoDefName { get; set; }

        internal int SourceLoadedCount { get; set; }

        internal int SourceCapacity { get; set; }

        internal int StagedLoadedCount { get; set; }

        internal int StagedCapacity { get; set; }

        internal bool SourceUnchanged { get; set; }

        internal bool Ready { get; set; }

        internal string Failure { get; set; }
    }
}
