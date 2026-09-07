using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal sealed class ShuttleWeaponLogisticsAmmoSupply : IShuttleWeaponAmmoSupply
    {
        private readonly ShuttleModuleRuntimeContext context;
        private readonly ShuttleWeaponCargoAmmoSupply cargoSupply;

        internal ShuttleWeaponLogisticsAmmoSupply(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponCargoAmmoSupply cargoSupply)
        {
            this.context = context;
            this.cargoSupply = cargoSupply;
        }

        public bool IsAvailable
        {
            get
            {
                return this.HasLogisticsCore() &&
                    this.cargoSupply != null &&
                    this.cargoSupply.IsAvailable;
            }
        }

        public int CountAvailable(ThingDef ammoThingDef)
        {
            return this.IsAvailable && this.cargoSupply != null
                ? this.cargoSupply.CountAvailable(ammoThingDef)
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
            if (!this.IsAvailable || this.cargoSupply == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_LogisticsUnavailable".Translate().ToString();
                return false;
            }

            return this.cargoSupply.TryConsume(
                ammoThingDef,
                count,
                reason,
                out consumedCount,
                out failureReason);
        }

        internal bool HasLogisticsCore()
        {
            return this.context != null &&
                this.context.Profile != null &&
                this.context.Profile.CargoLogistics != null &&
                this.context.Profile.CargoLogistics.HasCargoLogistics &&
                this.context.Profile.CargoLogistics.SupportsItemConsumption;
        }
    }
}
