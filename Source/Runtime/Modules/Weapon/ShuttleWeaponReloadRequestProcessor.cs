using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Routes pending, required-for-fire and automatic top-off requests into the automatic executor.
    /// It owns no supply commit, progress tick, Pawn claim or completion transaction.
    /// </summary>
    internal sealed class ShuttleWeaponReloadRequestProcessor
    {
        private readonly ShuttleWeaponAmmoLoaderAvailability loaderAvailability;
        private readonly ShuttleWeaponAutomaticReloadEligibility automaticEligibility;
        private readonly ShuttleWeaponAutomaticReloadExecutor automaticExecutor;
        private readonly ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions;
        private readonly CoreMagazineDriver coreMagazine;
        private readonly ShuttleWeaponReloadCoordinator reloadCoordinator;

        internal ShuttleWeaponReloadRequestProcessor(
            ShuttleWeaponAmmoLoaderAvailability loaderAvailability,
            ShuttleWeaponAutomaticReloadEligibility automaticEligibility,
            ShuttleWeaponAutomaticReloadExecutor automaticExecutor,
            ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions,
            CoreMagazineDriver coreMagazine,
            ShuttleWeaponReloadCoordinator reloadCoordinator)
        {
            this.loaderAvailability = loaderAvailability ??
                new ShuttleWeaponAmmoLoaderAvailability();
            this.ammoDefinitions = ammoDefinitions ??
                new ShuttleWeaponAmmoDefinitionCatalog();
            this.coreMagazine = coreMagazine ?? new CoreMagazineDriver(this.ammoDefinitions);
            this.reloadCoordinator = reloadCoordinator ??
                new ShuttleWeaponReloadCoordinator();
            this.automaticEligibility = automaticEligibility ??
                new ShuttleWeaponAutomaticReloadEligibility(
                    this.loaderAvailability,
                    null,
                    this.ammoDefinitions,
                    this.coreMagazine,
                    null);
            this.automaticExecutor = automaticExecutor ??
                new ShuttleWeaponAutomaticReloadExecutor(
                    null,
                    null,
                    this.ammoDefinitions,
                    this.coreMagazine,
                    this.reloadCoordinator,
                    null,
                    null);
        }

        internal bool TryProcess(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState)
        {
            if (ammoState == null ||
                !ammoState.ReloadRequested ||
                ammoState.ReloadInProgress ||
                ammoState.ManualReloadJobActive ||
                ammoState.ReloadExecutorKind == ShuttleWeaponReloadExecutorKind.Pawn)
            {
                return false;
            }

            ShuttleWeaponModuleAmmoExtension extension =
                this.ammoDefinitions.GetExtension(weaponDef);
            if (extension == null)
            {
                string reason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                ammoState.SetLastReloadBlockerReason(reason);
                this.reloadCoordinator.ApplyFailure(
                    ammoState,
                    this.reloadCoordinator.ResolveAndRememberEffectiveRequestKind(
                        ammoState,
                        this.coreMagazine.CanFire(weaponDef, ammoState)),
                    reason,
                    GetTicksGame(context));
                return false;
            }

            if (this.coreMagazine.GetNeededLoadCount(weaponDef, ammoState) <= 0)
            {
                this.reloadCoordinator.ClearRequest(ammoState);
                return false;
            }

            int ticksGame = GetTicksGame(context);
            ShuttleWeaponReloadRequestKind requestKind =
                this.reloadCoordinator.ResolveAndRememberEffectiveRequestKind(
                    ammoState,
                    this.coreMagazine.CanFire(weaponDef, ammoState));
            if (!ammoState.CanAttemptAutomaticReloadCheck(ticksGame))
            {
                return false;
            }

            if (requestKind == ShuttleWeaponReloadRequestKind.Manual &&
                ShuttleWeaponReloadExecutorPolicy.ShouldAwaitPawn(
                    requestKind,
                    ammoState.ManualReloadAllowed,
                    this.loaderAvailability.HasEnabledLoader(context)))
            {
                ammoState.SetLastReloadBlockerReason(null);
                ammoState.SetLastFailureReason(null);
                ammoState.ClearLastAutomaticReloadBlockerReason();
                this.reloadCoordinator.ScheduleRetry(
                    ammoState,
                    ticksGame,
                    ShuttleWeaponReloadPolicy.PawnManualReloadRecheckDelayTicks);
                return false;
            }

            bool logisticsOnly;
            string failureReason;
            if (this.automaticEligibility.CanHandle(
                    context,
                    weaponDef,
                    ammoState,
                    out logisticsOnly,
                    out failureReason) &&
                this.automaticExecutor.TryStart(
                    context,
                    weaponDef,
                    ammoState,
                    logisticsOnly,
                    out failureReason))
            {
                return true;
            }

            this.reloadCoordinator.ApplyFailure(
                ammoState,
                requestKind,
                failureReason,
                ticksGame);
            return false;
        }

        internal bool TryPrepareToFireOrStartReload(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState)
        {
            if (this.coreMagazine.CanFire(weaponDef, ammoState))
            {
                return true;
            }

            if (ammoState == null || ammoState.ReloadInProgress)
            {
                return false;
            }

            ShuttleWeaponModuleAmmoExtension extension =
                this.ammoDefinitions.GetExtension(weaponDef);
            if (extension == null || !ammoState.AutoReloadEnabled)
            {
                ammoState.SetLastFailureReason(
                    "CT_Shuttle_WeaponAmmo_NotEnoughAmmo".Translate().ToString());
                return false;
            }

            string failureReason;
            this.reloadCoordinator.TryRequest(
                ammoState,
                true,
                this.coreMagazine.GetNeededLoadCount(weaponDef, ammoState) > 0,
                ShuttleWeaponReloadRequestKind.RequiredForFire,
                "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString(),
                out failureReason);
            this.TryProcess(context, weaponDef, ammoState);
            return false;
        }

        internal bool TryRequestAutomaticTopOff(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState)
        {
            ShuttleWeaponModuleAmmoExtension extension =
                this.ammoDefinitions.GetExtension(weaponDef);
            int threshold = CeleTechShuttleMod.EffectiveCombatTuning
                .WeaponAutomaticTopOffThreshold;
            if (extension == null || ammoState == null ||
                !ShuttleWeaponReloadPolicy.ShouldRequestAutomaticTopOff(
                    ammoState.AutoReloadEnabled,
                    ammoState.ReloadRequested,
                    ammoState.ReloadInProgress,
                    ammoState.ManualReloadJobActive,
                    ammoState.ReloadExecutorKind,
                    extension.ammoPerShot,
                    threshold,
                    ammoState.MagazineCapacity,
                    ammoState.LoadedAmmoCount) ||
                context == null ||
                !this.loaderAvailability.HasEnabledLoader(context))
            {
                return false;
            }

            string failureReason;
            return this.reloadCoordinator.TryRequest(
                ammoState,
                true,
                this.coreMagazine.GetNeededLoadCount(weaponDef, ammoState) > 0,
                ShuttleWeaponReloadRequestKind.AutoTopOff,
                "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString(),
                out failureReason);
        }

        private static int GetTicksGame(ShuttleModuleRuntimeContext context)
        {
            if (context != null)
            {
                return context.TicksGame;
            }

            return Find.TickManager != null ? Find.TickManager.TicksGame : -1;
        }
    }
}
