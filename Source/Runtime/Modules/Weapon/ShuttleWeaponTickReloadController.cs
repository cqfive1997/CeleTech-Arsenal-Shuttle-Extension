using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Advances only the reload work that belongs at the weapon tick boundary.
    /// </summary>
    internal sealed class ShuttleWeaponTickReloadController
    {
        private readonly ShuttleWeaponAutomaticReloadExecutor automaticReloadExecutor;
        private readonly ShuttleWeaponReloadRequestProcessor reloadRequestProcessor;
        private readonly CoreMagazineDriver coreMagazine;
        private readonly ShuttleWeaponManualReloadClaimReconciler manualReloadClaimReconciler;

        internal ShuttleWeaponTickReloadController(
            ShuttleWeaponAutomaticReloadExecutor automaticReloadExecutor,
            ShuttleWeaponReloadRequestProcessor reloadRequestProcessor,
            CoreMagazineDriver coreMagazine,
            ShuttleWeaponManualReloadClaimReconciler manualReloadClaimReconciler)
        {
            this.automaticReloadExecutor = automaticReloadExecutor;
            this.reloadRequestProcessor = reloadRequestProcessor;
            this.coreMagazine = coreMagazine;
            this.manualReloadClaimReconciler = manualReloadClaimReconciler;
        }

        internal ShuttleWeaponAmmoState AdvancePendingWork(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state)
        {
            ShuttleWeaponAmmoState ammoState = this.coreMagazine.GetOrCreate(
                context.ModuleInstanceID,
                weaponDef,
                state);
            this.manualReloadClaimReconciler.Reconcile(context, ammoState);
            if (ammoState != null && ammoState.ReloadInProgress)
            {
                this.automaticReloadExecutor.Tick(context, weaponDef, ammoState);
            }

            if (ammoState != null &&
                ammoState.ReloadRequested &&
                !ammoState.ReloadInProgress &&
                !this.IsAutomaticTopOffRequest(weaponDef, ammoState))
            {
                this.reloadRequestProcessor.TryProcess(context, weaponDef, ammoState);
            }

            return ammoState;
        }

        internal void ProcessIdleTopOff(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState)
        {
            if (this.ShouldCheckForAutomaticTopOff(context))
            {
                this.reloadRequestProcessor.TryRequestAutomaticTopOff(
                    context,
                    weaponDef,
                    ammoState);
            }

            if (ammoState != null &&
                !ammoState.ReloadInProgress &&
                this.IsAutomaticTopOffRequest(weaponDef, ammoState))
            {
                this.reloadRequestProcessor.TryProcess(context, weaponDef, ammoState);
            }
        }

        internal void RecordDiagnostics(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState,
            ShuttleWeaponTickDiagnostics diagnostics)
        {
            if (!diagnostics.CollectsDetailedCounters)
            {
                return;
            }

            ShuttleWeaponReloadRequestKind requestKind =
                this.GetEffectiveRequestKind(weaponDef, ammoState);
            bool automaticRetryAllowed = true;
            if (ammoState != null &&
                (requestKind == ShuttleWeaponReloadRequestKind.RequiredForFire ||
                 requestKind == ShuttleWeaponReloadRequestKind.AutoTopOff))
            {
                automaticRetryAllowed =
                    ammoState.CanAttemptAutomaticReloadCheck(context.TicksGame);
            }

            diagnostics.RecordReloadState(
                ammoState,
                this.coreMagazine.CanFire(weaponDef, ammoState),
                requestKind,
                automaticRetryAllowed,
                this.coreMagazine.GetNeededLoadCount(weaponDef, ammoState) > 0);
        }

        internal ShuttleWeaponReloadRequestKind GetEffectiveRequestKind(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState)
        {
            return ShuttleWeaponReloadPolicy.ResolveEffectiveRequestKind(
                ammoState != null && ammoState.ReloadRequested,
                ammoState != null
                    ? ammoState.ReloadRequestKind
                    : ShuttleWeaponReloadRequestKind.None,
                this.coreMagazine.CanFire(weaponDef, ammoState));
        }

        private bool IsAutomaticTopOffRequest(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState)
        {
            return ammoState != null &&
                ammoState.ReloadRequested &&
                this.GetEffectiveRequestKind(weaponDef, ammoState) ==
                    ShuttleWeaponReloadRequestKind.AutoTopOff;
        }

        private bool ShouldCheckForAutomaticTopOff(ShuttleModuleRuntimeContext context)
        {
            return context == null ||
                context.TicksGame < 0 ||
                context.TicksGame % 60 == 0;
        }
    }
}
