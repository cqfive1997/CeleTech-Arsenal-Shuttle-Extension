using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Projects reload and firing demand from the Native-selected cycle without ticking or firing.
    /// </summary>
    internal sealed class NativeVerbWeaponPowerDemandDriver : IShuttleWeaponPowerDemandDriver
    {
        private readonly ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions;
        private readonly CoreMagazineDriver coreMagazine;
        private readonly ShuttleWeaponReloadPowerCoordinator reloadPowerCoordinator;
        private readonly IShuttleWeaponCycleHost cycleHost;
        private readonly ShuttleWeaponCyclePolicy cyclePolicy;

        internal NativeVerbWeaponPowerDemandDriver(
            ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions,
            CoreMagazineDriver coreMagazine,
            ShuttleWeaponReloadPowerCoordinator reloadPowerCoordinator,
            IShuttleWeaponCycleHost cycleHost,
            ShuttleWeaponCyclePolicy cyclePolicy)
        {
            this.ammoDefinitions = ammoDefinitions;
            this.coreMagazine = coreMagazine;
            this.reloadPowerCoordinator = reloadPowerCoordinator;
            this.cycleHost = cycleHost;
            this.cyclePolicy = cyclePolicy;
        }

        public void CollectPowerDemand(ShuttleModuleRuntimeContext context)
        {
            if (context == null || !context.IsEnabled ||
                this.ammoDefinitions == null || this.coreMagazine == null ||
                this.reloadPowerCoordinator == null || this.cycleHost == null ||
                this.cyclePolicy == null)
            {
                return;
            }

            ShuttleWeaponModuleDef weaponDef = context.ModuleDef as ShuttleWeaponModuleDef;
            ShuttleWeaponRuntimeState state = context.State as ShuttleWeaponRuntimeState;
            if (weaponDef == null || state == null)
            {
                return;
            }

            ShuttleWeaponAmmoState ammoState = this.coreMagazine.GetOrCreate(
                context.ModuleInstanceID,
                weaponDef,
                state);
            ShuttleWeaponModuleAmmoExtension ammoExtension =
                this.ammoDefinitions.GetExtension(weaponDef);
            this.reloadPowerCoordinator.CollectDemand(context, ammoState, ammoExtension);

            if (weaponDef.firingPowerDrawWatts <= 0f)
            {
                return;
            }

            Thing gun = state.GunForRuntimeOnly;
            Verb attackVerb = this.cycleHost.GetAttackVerb(context);
            if (gun == null || gun.Destroyed ||
                !this.cyclePolicy.ShouldReportFiringPowerDemand(state, attackVerb))
            {
                return;
            }

            context.AddInternalPowerDemandWatts(
                this.cyclePolicy.GetFiringPowerDrawWatts(weaponDef));
        }
    }
}
