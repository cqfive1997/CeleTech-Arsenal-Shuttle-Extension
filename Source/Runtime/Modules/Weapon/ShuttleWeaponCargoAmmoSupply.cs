using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal sealed class ShuttleWeaponCargoAmmoSupply : IShuttleWeaponAmmoSupply
    {
        private readonly IShuttleCargoResourceBroker cargoBroker;

        internal ShuttleWeaponCargoAmmoSupply(IShuttleCargoResourceBroker cargoBroker)
        {
            this.cargoBroker = cargoBroker;
        }

        public bool IsAvailable
        {
            get { return this.cargoBroker != null && this.cargoBroker.IsAvailable; }
        }

        public int CountAvailable(ThingDef ammoThingDef)
        {
            return ammoThingDef != null && this.cargoBroker != null
                ? this.cargoBroker.CountAvailable(ammoThingDef)
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

            if (!this.IsAvailable)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_SourceUnavailable".Translate().ToString();
                return false;
            }

            if (ammoThingDef == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_Incompatible".Translate().ToString();
                return false;
            }

            if (!this.cargoBroker.TryConsumeImmediatelyAndDestroy(
                ammoThingDef,
                count,
                reason,
                out consumedCount))
            {
                failureReason = "CT_Shuttle_WeaponAmmo_SourceEmpty".Translate().ToString();
                return false;
            }

            return consumedCount >= count;
        }
    }
}
