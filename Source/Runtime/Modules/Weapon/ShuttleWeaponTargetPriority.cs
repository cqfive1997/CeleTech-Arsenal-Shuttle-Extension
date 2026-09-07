namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal enum ShuttleWeaponTargetPriority
    {
        // Prefer the closest hostile target.
        ClosestHostile = 0,

        // Prefer hostile human raiders before falling back to distance.
        RaidersFirst = 1,

        // Prefer mechanoids before falling back to distance.
        MechanoidsFirst = 2,

        // Prefer manhunter animals before falling back to distance.
        ManhuntersFirst = 3,

        // Prefer targets scored as high threat.
        HighThreatFirst = 4,

        // Future target selection may use this to reject automatic target acquisition.
        ForcedTargetOnly = 5
    }
}
