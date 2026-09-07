using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Quotes core Pawn reload work and completes an exact claimed trip. Job creation and claim
    /// mutation stay in the backend adapter; automatic progress stays in its own executor.
    /// </summary>
    internal sealed class ShuttleWeaponManualReloadHandler
    {
        private readonly ShuttleWeaponAmmoLoaderAvailability loaderAvailability;
        private readonly ShuttleWeaponAutomaticReloadEligibility automaticEligibility;
        private readonly ShuttleWeaponAmmoSupplyResolver ammoSupplyResolver;
        private readonly ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions;
        private readonly CoreMagazineDriver coreMagazine;
        private readonly ShuttleWeaponReloadCoordinator reloadCoordinator;
        private readonly ShuttleWeaponReloadCompletionOrchestrator completionOrchestrator;

        internal ShuttleWeaponManualReloadHandler(
            ShuttleWeaponAmmoLoaderAvailability loaderAvailability,
            ShuttleWeaponAutomaticReloadEligibility automaticEligibility,
            ShuttleWeaponAmmoSupplyResolver ammoSupplyResolver,
            ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions,
            CoreMagazineDriver coreMagazine,
            ShuttleWeaponReloadCoordinator reloadCoordinator,
            ShuttleWeaponReloadCompletionOrchestrator completionOrchestrator)
        {
            this.loaderAvailability = loaderAvailability ??
                new ShuttleWeaponAmmoLoaderAvailability();
            this.ammoSupplyResolver = ammoSupplyResolver ??
                new ShuttleWeaponAmmoSupplyResolver();
            this.ammoDefinitions = ammoDefinitions ??
                new ShuttleWeaponAmmoDefinitionCatalog();
            this.coreMagazine = coreMagazine ?? new CoreMagazineDriver(this.ammoDefinitions);
            this.reloadCoordinator = reloadCoordinator ??
                new ShuttleWeaponReloadCoordinator();
            this.automaticEligibility = automaticEligibility ??
                new ShuttleWeaponAutomaticReloadEligibility(
                    this.loaderAvailability,
                    this.ammoSupplyResolver,
                    this.ammoDefinitions,
                    this.coreMagazine,
                    null);
            this.completionOrchestrator = completionOrchestrator ??
                new ShuttleWeaponReloadCompletionOrchestrator(
                    this.ammoDefinitions,
                    this.coreMagazine,
                    this.reloadCoordinator,
                    null);
        }

        internal bool CanHandle(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState,
            out int workTicks,
            out ThingDef ammoThingDef,
            out int requestedAmmoCount,
            out string failureReason)
        {
            workTicks = 0;
            ammoThingDef = null;
            requestedAmmoCount = 0;
            failureReason = null;
            ShuttleWeaponModuleAmmoExtension extension =
                this.ammoDefinitions.GetExtension(weaponDef);
            if (extension == null || ammoState == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                return false;
            }

            if (!ammoState.ReloadRequested ||
                ammoState.ReloadInProgress ||
                ammoState.ManualReloadJobActive ||
                ammoState.ReloadExecutorKind != ShuttleWeaponReloadExecutorKind.None)
            {
                return false;
            }

            if (!ammoState.ManualReloadAllowed)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_ManualReloadDisabled"
                    .Translate()
                    .ToString();
                return false;
            }

            ShuttleWeaponReloadRequestKind requestKind =
                this.reloadCoordinator.ResolveAndRememberEffectiveRequestKind(
                    ammoState,
                    this.coreMagazine.CanFire(weaponDef, ammoState));
            if (requestKind == ShuttleWeaponReloadRequestKind.AutoTopOff)
            {
                return false;
            }

            bool logisticsOnly;
            bool shouldAwaitPawn = requestKind == ShuttleWeaponReloadRequestKind.Manual &&
                ShuttleWeaponReloadExecutorPolicy.ShouldAwaitPawn(
                    requestKind,
                    ammoState.ManualReloadAllowed,
                    this.loaderAvailability.HasEnabledLoader(context));
            if (!shouldAwaitPawn &&
                this.automaticEligibility.CanHandle(
                    context,
                    weaponDef,
                    ammoState,
                    out logisticsOnly,
                    out failureReason))
            {
                return false;
            }

            ShuttleWeaponAmmoDef selectedAmmo =
                this.coreMagazine.GetSelectedAmmo(weaponDef, ammoState);
            ammoThingDef = selectedAmmo != null ? selectedAmmo.AmmoThingDef : null;
            if (ammoThingDef == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_Incompatible".Translate().ToString();
                return false;
            }

            requestedAmmoCount = this.coreMagazine.GetNeededLoadCount(weaponDef, ammoState);
            if (requestedAmmoCount <= 0)
            {
                return false;
            }

            workTicks = extension.reloadWorkTicks > 0 ? extension.reloadWorkTicks : 1;
            failureReason = null;
            return true;
        }

        internal bool TryComplete(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState,
            Pawn pawn,
            int pawnThingID,
            int jobLoadID,
            out string failureReason)
        {
            failureReason = null;
            if (ammoState == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                return false;
            }

            if (!ammoState.IsManualReloadClaim(pawnThingID, jobLoadID))
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoManualReloadJob".Translate().ToString();
                return false;
            }

            bool result = this.completionOrchestrator.TryComplete(
                context,
                weaponDef,
                ammoState,
                this.ammoSupplyResolver.ResolvePawn(pawn),
                "CT_Shuttle_WeaponAmmo_SourceUnavailable".Translate().ToString(),
                out failureReason);
            if (!result)
            {
                this.reloadCoordinator.ClearRequest(ammoState);
                if (!string.IsNullOrEmpty(failureReason))
                {
                    ammoState.SetLastReloadBlockerReason(failureReason);
                }
            }

            return result;
        }
    }
}
