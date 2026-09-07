using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Keeps the existing Pawn hauling shell behind the selected backend instead of letting the
    /// controller assume that every weapon still uses the core magazine.
    /// </summary>
    internal sealed class CoreWeaponManualReloadDriver : IShuttleWeaponManualReloadDriver
    {
        private readonly ShuttleWeaponManualReloadHandler manualReloadHandler;
        private readonly CoreMagazineDriver coreMagazine;

        internal CoreWeaponManualReloadDriver(
            ShuttleWeaponManualReloadHandler manualReloadHandler,
            CoreMagazineDriver coreMagazine)
        {
            this.manualReloadHandler = manualReloadHandler ??
                new ShuttleWeaponManualReloadHandler(
                    null,
                    null,
                    null,
                    null,
                    coreMagazine,
                    null,
                    null);
            this.coreMagazine = coreMagazine ?? new CoreMagazineDriver(
                new ShuttleWeaponAmmoDefinitionCatalog());
        }

        public bool TryGetPlan(
            ShuttleModuleRuntimeContext context,
            out ShuttleWeaponManualReloadPlan plan,
            out string failureReason)
        {
            return this.TryBuildPlan(
                context,
                false,
                null,
                -1,
                null,
                out plan,
                out failureReason);
        }

        public bool TryClaim(
            ShuttleModuleRuntimeContext context,
            Pawn pawn,
            int jobLoadID,
            ThingDef requiredAmmoThingDef,
            out ShuttleWeaponManualReloadPlan plan,
            out string failureReason)
        {
            return this.TryBuildPlan(
                context,
                true,
                pawn,
                jobLoadID,
                requiredAmmoThingDef,
                out plan,
                out failureReason);
        }

        public bool TryComplete(
            ShuttleModuleRuntimeContext context,
            Pawn pawn,
            int jobLoadID,
            out string failureReason)
        {
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            ShuttleWeaponAmmoState ammoState = state != null
                ? this.coreMagazine.GetOrCreate(
                    context.ModuleInstanceID,
                    weaponDef,
                    state)
                : null;
            return this.manualReloadHandler.TryComplete(
                context,
                weaponDef,
                ammoState,
                pawn,
                pawn != null ? pawn.thingIDNumber : -1,
                jobLoadID,
                out failureReason);
        }

        private bool TryBuildPlan(
            ShuttleModuleRuntimeContext context,
            bool claim,
            Pawn pawn,
            int jobLoadID,
            ThingDef requiredAmmoThingDef,
            out ShuttleWeaponManualReloadPlan plan,
            out string failureReason)
        {
            plan = null;
            failureReason = null;
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            ShuttleWeaponAmmoState ammoState = state != null
                ? this.coreMagazine.GetOrCreate(
                    context.ModuleInstanceID,
                    weaponDef,
                    state)
                : null;
            int workTicks;
            ThingDef ammoThingDef;
            int requestedAmmoCount;
            if (!this.manualReloadHandler.CanHandle(
                    context,
                    weaponDef,
                    ammoState,
                    out workTicks,
                    out ammoThingDef,
                    out requestedAmmoCount,
                    out failureReason) ||
                (requiredAmmoThingDef != null && ammoThingDef != requiredAmmoThingDef))
            {
                return false;
            }

            if (claim &&
                (pawn == null ||
                 !ammoState.TryMarkManualReloadJobActive(
                    context.TicksGame,
                    pawn.thingIDNumber,
                    jobLoadID)))
            {
                return false;
            }

            plan = new ShuttleWeaponManualReloadPlan(
                context.ModuleInstanceID,
                ammoThingDef,
                requestedAmmoCount,
                workTicks);
            return true;
        }
    }
}
