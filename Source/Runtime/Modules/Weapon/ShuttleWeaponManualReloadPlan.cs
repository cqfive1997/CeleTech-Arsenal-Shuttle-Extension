using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Detached quote for one Pawn reload attempt. It contains no reservation or runtime-state
    /// ownership and must be revalidated when the Job claims the weapon.
    /// </summary>
    internal sealed class ShuttleWeaponManualReloadPlan
    {
        internal ShuttleWeaponManualReloadPlan(
            string moduleInstanceID,
            ThingDef ammoThingDef,
            int requestedAmmoCount,
            int workTicks)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.AmmoThingDef = ammoThingDef;
            this.RequestedAmmoCount = requestedAmmoCount;
            this.WorkTicks = workTicks;
        }

        internal string ModuleInstanceID { get; private set; }

        internal ThingDef AmmoThingDef { get; private set; }

        internal int RequestedAmmoCount { get; private set; }

        internal int WorkTicks { get; private set; }
    }
}
