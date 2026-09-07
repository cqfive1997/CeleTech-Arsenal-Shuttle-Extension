using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal sealed class CeWeaponBackendPowerDemandDriver : IShuttleWeaponPowerDemandDriver
    {
        private readonly CeWeaponFiringPowerProjection projection;
        private readonly CeWeaponReloadDriver reloadDriver;

        internal CeWeaponBackendPowerDemandDriver(
            CeWeaponFiringPowerProjection projection,
            CeWeaponReloadDriver reloadDriver)
        {
            this.projection = projection;
            this.reloadDriver = reloadDriver;
        }

        public void CollectPowerDemand(ShuttleModuleRuntimeContext context)
        {
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            if (state == null ||
                state.MagazineAuthorityBackendIdForRuntimeOnly != CeWeaponBackendFactory.Id)
            {
                return;
            }

            ShuttleWeaponModuleDef weaponDef = context.ModuleDef as ShuttleWeaponModuleDef;
            if (this.reloadDriver != null)
            {
                this.reloadDriver.CollectPowerDemand(context, weaponDef, state);
            }

            CeWeaponMuzzleVerb verb = CeWeaponRuntimeGunAccess.GetMuzzleVerb(
                state.GunForRuntimeOnly);
            if (this.projection != null && verb != null)
            {
                this.projection.Collect(context, verb);
            }
        }
    }
}
