using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Evaluates whether an automatic loader can handle the pending core-magazine request.
    /// It performs no request, progress, completion or Pawn Job mutation.
    /// </summary>
    internal sealed class ShuttleWeaponAutomaticReloadEligibility
    {
        private readonly ShuttleWeaponAmmoLoaderAvailability loaderAvailability;
        private readonly ShuttleWeaponAmmoSupplyResolver ammoSupplyResolver;
        private readonly ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions;
        private readonly CoreMagazineDriver coreMagazine;
        private readonly ShuttleWeaponReloadCompletionTransaction completionTransaction;

        internal ShuttleWeaponAutomaticReloadEligibility(
            ShuttleWeaponAmmoLoaderAvailability loaderAvailability,
            ShuttleWeaponAmmoSupplyResolver ammoSupplyResolver,
            ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions,
            CoreMagazineDriver coreMagazine,
            ShuttleWeaponReloadCompletionTransaction completionTransaction)
        {
            this.loaderAvailability = loaderAvailability ??
                new ShuttleWeaponAmmoLoaderAvailability();
            this.ammoSupplyResolver = ammoSupplyResolver ??
                new ShuttleWeaponAmmoSupplyResolver();
            this.ammoDefinitions = ammoDefinitions ??
                new ShuttleWeaponAmmoDefinitionCatalog();
            this.coreMagazine = coreMagazine ?? new CoreMagazineDriver(this.ammoDefinitions);
            this.completionTransaction = completionTransaction ??
                new ShuttleWeaponReloadCompletionTransaction(this.coreMagazine, null);
        }

        internal bool CanHandle(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState,
            out bool logisticsOnly,
            out string failureReason)
        {
            logisticsOnly = false;
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

            if (this.coreMagazine.GetNeededLoadCount(weaponDef, ammoState) <= 0)
            {
                return false;
            }

            if (context == null || !context.InternalBusPowered)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_AutomaticReloadUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            if (!this.loaderAvailability.HasEnabledLoader(context))
            {
                failureReason = "CT_Shuttle_WeaponAmmo_AmmoLoaderUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            bool canUseLogistics = extension.logisticsAutoFeedEligible &&
                ammoState.LogisticsAutoFeedEnabled;
            if (!ammoState.ReloadRequested && !ammoState.AutoReloadEnabled)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_AutomaticReloadUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            logisticsOnly = canUseLogistics;
            IShuttleWeaponAmmoSupply supply =
                this.ammoSupplyResolver.Resolve(context, logisticsOnly);
            if (supply == null || !supply.IsAvailable)
            {
                failureReason = logisticsOnly
                    ? "CT_Shuttle_WeaponAmmo_LogisticsUnavailable".Translate().ToString()
                    : "CT_Shuttle_WeaponAmmo_SourceUnavailable".Translate().ToString();
                return false;
            }

            if (this.completionTransaction.GetLoadableCount(
                    weaponDef,
                    ammoState,
                    supply) <= 0)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_SourceEmpty".Translate().ToString();
                return false;
            }

            return true;
        }
    }
}
