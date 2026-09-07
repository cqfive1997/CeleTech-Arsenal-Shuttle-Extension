using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Starts and advances automatic-loader work after admission has been decided.
    /// Request routing, eligibility and Pawn execution remain separate.
    /// </summary>
    internal sealed class ShuttleWeaponAutomaticReloadExecutor
    {
        private readonly ShuttleWeaponReloadPowerCoordinator reloadPowerCoordinator;
        private readonly ShuttleWeaponAmmoSupplyResolver ammoSupplyResolver;
        private readonly ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions;
        private readonly CoreMagazineDriver coreMagazine;
        private readonly ShuttleWeaponReloadCoordinator reloadCoordinator;
        private readonly ShuttleWeaponReloadCompletionTransaction completionTransaction;
        private readonly ShuttleWeaponReloadCompletionOrchestrator completionOrchestrator;

        internal ShuttleWeaponAutomaticReloadExecutor(
            ShuttleWeaponReloadPowerCoordinator reloadPowerCoordinator,
            ShuttleWeaponAmmoSupplyResolver ammoSupplyResolver,
            ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions,
            CoreMagazineDriver coreMagazine,
            ShuttleWeaponReloadCoordinator reloadCoordinator,
            ShuttleWeaponReloadCompletionTransaction completionTransaction,
            ShuttleWeaponReloadCompletionOrchestrator completionOrchestrator)
        {
            this.reloadPowerCoordinator = reloadPowerCoordinator ??
                new ShuttleWeaponReloadPowerCoordinator();
            this.ammoSupplyResolver = ammoSupplyResolver ??
                new ShuttleWeaponAmmoSupplyResolver();
            this.ammoDefinitions = ammoDefinitions ??
                new ShuttleWeaponAmmoDefinitionCatalog();
            this.coreMagazine = coreMagazine ?? new CoreMagazineDriver(this.ammoDefinitions);
            this.reloadCoordinator = reloadCoordinator ??
                new ShuttleWeaponReloadCoordinator();
            this.completionTransaction = completionTransaction ??
                new ShuttleWeaponReloadCompletionTransaction(this.coreMagazine, null);
            this.completionOrchestrator = completionOrchestrator ??
                new ShuttleWeaponReloadCompletionOrchestrator(
                    this.ammoDefinitions,
                    this.coreMagazine,
                    this.reloadCoordinator,
                    this.completionTransaction);
        }

        internal bool TryStart(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState,
            bool logisticsOnly,
            out string failureReason)
        {
            failureReason = null;
            ShuttleWeaponModuleAmmoExtension extension =
                this.ammoDefinitions.GetExtension(weaponDef);
            if (extension == null || ammoState == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                return false;
            }

            if (ammoState.ManualReloadJobActive ||
                ammoState.ReloadExecutorKind == ShuttleWeaponReloadExecutorKind.Pawn)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_AutomaticReloadUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            if (!logisticsOnly && !extension.allowManualReload)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_ReloadFailed".Translate().ToString();
                return false;
            }

            if (ammoState.ReloadInProgress)
            {
                return true;
            }

            if (this.coreMagazine.GetNeededLoadCount(weaponDef, ammoState) <= 0)
            {
                return true;
            }

            IShuttleWeaponAmmoSupply supply =
                this.ammoSupplyResolver.Resolve(context, logisticsOnly);
            if (supply == null || !supply.IsAvailable)
            {
                failureReason = logisticsOnly
                    ? "CT_Shuttle_WeaponAmmo_LogisticsUnavailable".Translate().ToString()
                    : "CT_Shuttle_WeaponAmmo_SourceUnavailable".Translate().ToString();
                ammoState.SetLastFailureReason(failureReason);
                return false;
            }

            if (this.completionTransaction.GetLoadableCount(
                    weaponDef,
                    ammoState,
                    supply) <= 0)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_SourceEmpty".Translate().ToString();
                ammoState.SetLastFailureReason(failureReason);
                return false;
            }

            ShuttleWeaponReloadRequestKind requestKind =
                this.reloadCoordinator.ResolveAndRememberEffectiveRequestKind(
                    ammoState,
                    this.coreMagazine.CanFire(weaponDef, ammoState));
            if (extension.reloadWorkTicks <= 0)
            {
                return this.completionOrchestrator.TryComplete(
                    context,
                    weaponDef,
                    ammoState,
                    supply,
                    logisticsOnly
                        ? "CT_Shuttle_WeaponAmmo_LogisticsUnavailable".Translate().ToString()
                        : "CT_Shuttle_WeaponAmmo_SourceUnavailable".Translate().ToString(),
                    out failureReason);
            }

            if (!this.reloadCoordinator.TryStartAutomatic(
                    ammoState,
                    extension.reloadWorkTicks,
                    requestKind))
            {
                failureReason = "CT_Shuttle_WeaponAmmo_AutomaticReloadUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            return true;
        }

        internal bool Tick(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState)
        {
            if (ammoState == null || !ammoState.ReloadInProgress)
            {
                return true;
            }

            ShuttleWeaponModuleAmmoExtension extension =
                this.ammoDefinitions.GetExtension(weaponDef);
            ShuttleWeaponReloadProgressAction progressAction =
                this.reloadCoordinator.AdvanceProgress(
                    ammoState,
                    this.reloadPowerCoordinator.CanAdvance(context, ammoState, extension));
            if (progressAction != ShuttleWeaponReloadProgressAction.ReadyToCommit)
            {
                return true;
            }

            bool useLogistics = ammoState.LogisticsAutoFeedEnabled &&
                extension != null &&
                extension.logisticsAutoFeedEligible;
            IShuttleWeaponAmmoSupply supply =
                this.ammoSupplyResolver.Resolve(context, useLogistics);
            string failureReason;
            return this.completionOrchestrator.TryComplete(
                context,
                weaponDef,
                ammoState,
                supply,
                useLogistics
                    ? "CT_Shuttle_WeaponAmmo_LogisticsUnavailable".Translate().ToString()
                    : "CT_Shuttle_WeaponAmmo_SourceUnavailable".Translate().ToString(),
                out failureReason);
        }
    }
}
