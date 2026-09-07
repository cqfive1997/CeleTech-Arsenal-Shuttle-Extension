using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Narrow reload source over the current Pawn-carried stack. It never searches the map or
    /// reaches into shuttle cargo ownership.
    /// </summary>
    internal sealed class ShuttleWeaponPawnCarriedAmmoSupply : IShuttleWeaponAmmoSupply
    {
        private readonly Pawn pawn;

        internal ShuttleWeaponPawnCarriedAmmoSupply(Pawn pawn)
        {
            this.pawn = pawn;
        }

        public bool IsAvailable
        {
            get
            {
                Thing carried = this.GetCarriedThing();
                return carried != null &&
                    !carried.Destroyed &&
                    carried.def != null &&
                    carried.stackCount > 0;
            }
        }

        public int CountAvailable(ThingDef ammoThingDef)
        {
            Thing carried = this.GetCarriedThing();
            return carried != null &&
                !carried.Destroyed &&
                carried.def == ammoThingDef &&
                carried.stackCount > 0
                    ? carried.stackCount
                    : 0;
        }

        public bool TryConsume(
            ThingDef ammoThingDef,
            int count,
            string reason,
            out int consumedCount,
            out string failureReason)
        {
            consumedCount = 0;
            failureReason = null;
            if (count <= 0)
            {
                return true;
            }

            if (ammoThingDef == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_Incompatible".Translate().ToString();
                return false;
            }

            Thing carried = this.GetCarriedThing();
            if (carried == null || carried.Destroyed || carried.def != ammoThingDef)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_Incompatible".Translate().ToString();
                return false;
            }

            if (carried.stackCount < count)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_SourceEmpty".Translate().ToString();
                return false;
            }

            if (carried.stackCount == count)
            {
                consumedCount = carried.stackCount;
                carried.Destroy(DestroyMode.Vanish);
                return true;
            }

            Thing consumed = carried.SplitOff(count);
            if (consumed == null || consumed.Destroyed || consumed.stackCount <= 0)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_SourceEmpty".Translate().ToString();
                return false;
            }

            consumedCount = consumed.stackCount;
            consumed.Destroy(DestroyMode.Vanish);
            return consumedCount == count;
        }

        private Thing GetCarriedThing()
        {
            return this.pawn != null && this.pawn.carryTracker != null
                ? this.pawn.carryTracker.CarriedThing
                : null;
        }
    }
}
