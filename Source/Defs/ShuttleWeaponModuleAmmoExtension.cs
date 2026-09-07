using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    public sealed class ShuttleWeaponModuleAmmoExtension : DefModExtension
    {
        public ShuttleWeaponAmmoSetDef ammoSet;
        public int magazineCapacity;
        // Ammo consumed when a fire cycle starts. For burst verbs this should be authored to
        // match the burst projectile count so high-rate weapons still pay ammo per projectile.
        public int ammoPerShot = 1;
        public int reloadWorkTicks;
        public bool autoReloadEnabledByDefault;
        public bool allowManualReload = true;
        public bool allowUnload;
        public bool logisticsAutoFeedEligible;
        public float reloadPowerDemandWatts;
        public float ammoMassCountsTowardCargo = 1f;

        public override IEnumerable<string> ConfigErrors()
        {
            if (this.magazineCapacity <= 0)
            {
                yield return "ShuttleWeaponModuleAmmoExtension has magazineCapacity <= 0.";
            }

            if (this.ammoPerShot <= 0)
            {
                yield return "ShuttleWeaponModuleAmmoExtension has ammoPerShot <= 0.";
            }

            if (this.reloadWorkTicks < 0)
            {
                yield return "ShuttleWeaponModuleAmmoExtension has negative reloadWorkTicks.";
            }

            if (this.ammoSet == null)
            {
                yield return "ShuttleWeaponModuleAmmoExtension has no ammoSet.";
            }
            else if (this.ammoSet.defaultAmmo == null)
            {
                yield return "ShuttleWeaponModuleAmmoExtension ammoSet has no defaultAmmo.";
            }

            if (this.logisticsAutoFeedEligible && this.ammoSet == null)
            {
                yield return "ShuttleWeaponModuleAmmoExtension enables logistics auto-feed without ammoSet.";
            }

            if (this.ammoMassCountsTowardCargo < 0f ||
                float.IsNaN(this.ammoMassCountsTowardCargo) ||
                float.IsInfinity(this.ammoMassCountsTowardCargo))
            {
                yield return "ShuttleWeaponModuleAmmoExtension has invalid ammoMassCountsTowardCargo.";
            }
        }
    }
}
