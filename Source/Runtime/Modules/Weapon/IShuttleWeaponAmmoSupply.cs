using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Narrow ammunition supply boundary. Callers identify ordinary cargo ammunition by ThingDef;
    /// the supply does not know shuttle weapon/module/ammo Defs or magazine policy.
    /// A failed consume must report consumedCount=0. A successful partial consume is explicit
    /// through consumedCount and is reconciled by the transfer committer.
    /// </summary>
    internal interface IShuttleWeaponAmmoSupply
    {
        bool IsAvailable { get; }

        int CountAvailable(ThingDef ammoThingDef);

        bool TryConsume(
            ThingDef ammoThingDef,
            int count,
            string reason,
            out int consumedCount,
            out string failureReason);
    }
}
