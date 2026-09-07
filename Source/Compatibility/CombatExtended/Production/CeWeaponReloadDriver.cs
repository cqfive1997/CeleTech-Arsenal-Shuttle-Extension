using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CeleTech.ShuttleExtension.ModularShuttle;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Advances CE-owned reload work and commits one exact cargo stack through the core cargo
    /// transaction. It never mirrors the CE magazine into the common ammo state.
    /// </summary>
    internal sealed class CeWeaponReloadDriver
    {
        private const int RetryDelayTicks = 60;

        private readonly ShuttleWeaponReloadPowerCoordinator powerCoordinator;
        private readonly CeWeaponCargoReloadTransaction cargoTransaction =
            new CeWeaponCargoReloadTransaction();
        private readonly ConditionalWeakTable<ShuttleWeaponModuleDef, CachedAmmoExtension>
            ammoExtensionCache =
                new ConditionalWeakTable<ShuttleWeaponModuleDef, CachedAmmoExtension>();
        private readonly ConditionalWeakTable<ShuttleProfile, Dictionary<string, bool>>
            ammoLoaderAvailabilityByProfile =
                new ConditionalWeakTable<ShuttleProfile, Dictionary<string, bool>>();

        internal CeWeaponReloadDriver(
            ShuttleWeaponReloadPowerCoordinator powerCoordinator)
        {
            this.powerCoordinator = powerCoordinator ??
                new ShuttleWeaponReloadPowerCoordinator();
        }

        internal bool TryRequest(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            ShuttleWeaponReloadRequestKind requestKind,
            out string failureReason)
        {
            failureReason = null;
            ShuttleWeaponModuleAmmoExtension extension = GetExtension(weaponDef);
            CompAmmoUser magazine = GetAuthoritativeMagazine(state);
            ShuttleWeaponAmmoState commonState = state != null
                ? state.AmmoForRuntimeOnly
                : null;
            if (extension == null || magazine == null || commonState == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                return false;
            }

            if (requestKind == ShuttleWeaponReloadRequestKind.None)
            {
                requestKind = ShuttleWeaponReloadRequestKind.Manual;
            }

            if (requestKind == ShuttleWeaponReloadRequestKind.Manual &&
                (!extension.allowManualReload || !commonState.ManualReloadAllowed))
            {
                failureReason = "CT_Shuttle_WeaponAmmo_ManualReloadDisabled"
                    .Translate()
                    .ToString();
                return false;
            }

            if (magazine.CurMagCount >= magazine.MagSize)
            {
                commonState.ClearReloadRequest();
                return true;
            }

            commonState.RequestReload(requestKind);
            return true;
        }

        internal bool Tick(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            CompAmmoUser magazine,
            out string failureReason)
        {
            failureReason = null;
            ShuttleWeaponAmmoState commonState = state != null
                ? state.AmmoForRuntimeOnly
                : null;
            ShuttleWeaponModuleAmmoExtension extension = GetExtension(weaponDef);
            if (context == null || commonState == null || extension == null ||
                magazine == null || GetAuthoritativeMagazine(state) != magazine)
            {
                return false;
            }

            this.TryRequestAutomaticTopOff(context, extension, commonState, magazine);
            if (commonState.ReloadInProgress)
            {
                if (commonState.ReloadExecutorKind !=
                    ShuttleWeaponReloadExecutorKind.AutomaticLoader)
                {
                    return true;
                }

                if (!this.powerCoordinator.CanAdvance(context, commonState, extension))
                {
                    return true;
                }

                commonState.AddReloadWork(1);
                if (commonState.ReloadWorkDone < commonState.ReloadWorkTotal)
                {
                    return true;
                }

                if (!this.cargoTransaction.TryCommit(
                        context,
                        weaponDef,
                        commonState,
                        magazine,
                        out failureReason))
                {
                    commonState.FailReload(ToPlayerFailure(failureReason));
                    commonState.ScheduleAutomaticReloadRetry(
                        context.TicksGame,
                        RetryDelayTicks);
                }

                return true;
            }

            if (!commonState.ReloadRequested)
            {
                return false;
            }

            if (magazine.CurMagCount >= magazine.MagSize)
            {
                commonState.FinishReload();
                return false;
            }

            if (commonState.ManualReloadJobActive ||
                commonState.ReloadExecutorKind == ShuttleWeaponReloadExecutorKind.Pawn)
            {
                return true;
            }

            ShuttleWeaponReloadRequestKind requestKind = commonState.ReloadRequestKind;
            if (requestKind == ShuttleWeaponReloadRequestKind.None)
            {
                requestKind = ShuttleWeaponReloadRequestKind.RequiredForFire;
                commonState.SetReloadRequestKindForRuntimeOnly(requestKind);
            }

            if (!commonState.CanAttemptAutomaticReloadCheck(context.TicksGame))
            {
                return requestKind != ShuttleWeaponReloadRequestKind.AutoTopOff;
            }

            if (!this.CanHandleFromCargo(
                    context,
                    magazine,
                    out failureReason))
            {
                commonState.SetLastReloadBlockerReason(ToPlayerFailure(failureReason));
                commonState.ScheduleAutomaticReloadRetry(
                    context.TicksGame,
                    RetryDelayTicks);
                return requestKind != ShuttleWeaponReloadRequestKind.AutoTopOff;
            }

            int workTicks = Math.Max(1, extension.reloadWorkTicks);
            if (!commonState.TryStartAutomaticReload(workTicks, requestKind))
            {
                failureReason = "automatic-reload-claim-rejected";
                commonState.SetLastReloadBlockerReason(ToPlayerFailure(failureReason));
                return true;
            }

            return true;
        }

        internal void CollectPowerDemand(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state)
        {
            if (state == null ||
                state.MagazineAuthorityBackendIdForRuntimeOnly != CeWeaponBackendFactory.Id)
            {
                return;
            }

            this.powerCoordinator.CollectDemand(
                context,
                state.AmmoForRuntimeOnly,
                GetExtension(weaponDef));
        }

        internal bool CanHandleFromCargo(
            ShuttleModuleRuntimeContext context,
            CompAmmoUser magazine,
            out string failureReason)
        {
            failureReason = null;
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            ShuttleWeaponModuleAmmoExtension extension = GetExtension(weaponDef);
            ShuttleWeaponAmmoState commonState = state != null
                ? state.AmmoForRuntimeOnly
                : null;
            AmmoDef ammoDef = magazine != null
                ? magazine.SelectedAmmo ?? magazine.CurrentAmmo
                : null;
            if (context == null || extension == null || commonState == null ||
                !context.IsEnabled || !context.InternalBusPowered)
            {
                failureReason = "automatic-reload-unpowered";
                return false;
            }

            if (!HasEnabledAmmoLoader(context))
            {
                failureReason = "ammo-loader-unavailable";
                return false;
            }

            if (extension.logisticsAutoFeedEligible &&
                commonState.LogisticsAutoFeedEnabled &&
                !HasLogisticsCore(context))
            {
                failureReason = "logistics-core-unavailable";
                return false;
            }

            if (context.CargoResourceBroker == null ||
                !context.CargoResourceBroker.IsAvailable)
            {
                failureReason = "cargo-unavailable";
                return false;
            }

            if (ammoDef == null || context.CargoResourceBroker.CountStored(ammoDef) <= 0)
            {
                failureReason = "compatible-cargo-ammo-missing";
                return false;
            }

            return true;
        }

        private void TryRequestAutomaticTopOff(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleAmmoExtension extension,
            ShuttleWeaponAmmoState commonState,
            CompAmmoUser magazine)
        {
            if (context != null &&
                context.TicksGame >= 0 &&
                context.TicksGame % RetryDelayTicks != 0)
            {
                return;
            }

            int threshold = CeleTechShuttleMod.EffectiveCombatTuning
                .WeaponAutomaticTopOffThreshold;
            if (context == null || commonState == null || magazine == null ||
                !commonState.AutoReloadEnabled ||
                commonState.ReloadRequested ||
                commonState.ReloadInProgress ||
                commonState.ManualReloadJobActive ||
                commonState.ReloadExecutorKind != ShuttleWeaponReloadExecutorKind.None ||
                extension == null || extension.ammoPerShot <= 0 || threshold < 0 ||
                magazine.CurMagCount < extension.ammoPerShot ||
                magazine.CurMagCount >= magazine.MagSize ||
                magazine.CurMagCount > threshold ||
                !HasEnabledAmmoLoader(context))
            {
                return;
            }

            commonState.RequestReload(ShuttleWeaponReloadRequestKind.AutoTopOff);
        }

        private bool HasEnabledAmmoLoader(ShuttleModuleRuntimeContext context)
        {
            if (context == null || context.Profile == null ||
                context.Profile.Layout == null || context.Profile.Layout.Modules == null)
            {
                return false;
            }

            string segmentInstanceID = context.ParentSegmentInstanceID ?? string.Empty;
            Dictionary<string, bool> availabilityBySegment =
                this.ammoLoaderAvailabilityByProfile.GetOrCreateValue(context.Profile);
            bool cachedAvailability;
            if (availabilityBySegment.TryGetValue(
                    segmentInstanceID,
                    out cachedAvailability))
            {
                return cachedAvailability;
            }

            System.Collections.Generic.IReadOnlyList<ModuleLayoutEntry> modules =
                context.Profile.Layout.Modules;
            for (int i = 0; i < modules.Count; i++)
            {
                ModuleLayoutEntry module = modules[i];
                if (module == null || !module.IsEnabled ||
                    string.IsNullOrEmpty(module.ModuleDefName) ||
                    module.ParentSegmentInstanceID != context.ParentSegmentInstanceID)
                {
                    continue;
                }

                ShuttleModuleBaseDef moduleDef =
                    DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(module.ModuleDefName);
                if (moduleDef != null && moduleDef.ModuleType == ShuttleModuleType.AmmoLoader)
                {
                    availabilityBySegment[segmentInstanceID] = true;
                    return true;
                }
            }

            availabilityBySegment[segmentInstanceID] = false;
            return false;
        }

        private static bool HasLogisticsCore(ShuttleModuleRuntimeContext context)
        {
            return context != null &&
                context.Profile != null &&
                context.Profile.CargoLogistics != null &&
                context.Profile.CargoLogistics.HasCargoLogistics &&
                context.Profile.CargoLogistics.SupportsItemConsumption;
        }

        private static CompAmmoUser GetAuthoritativeMagazine(ShuttleWeaponRuntimeState state)
        {
            return state != null &&
                state.MagazineAuthorityBackendIdForRuntimeOnly == CeWeaponBackendFactory.Id
                    ? CeWeaponRuntimeGunAccess.GetMagazine(state.GunForRuntimeOnly)
                    : null;
        }

        private ShuttleWeaponModuleAmmoExtension GetExtension(
            ShuttleWeaponModuleDef weaponDef)
        {
            if (weaponDef == null)
            {
                return null;
            }

            CachedAmmoExtension cached =
                this.ammoExtensionCache.GetOrCreateValue(weaponDef);
            if (!cached.Resolved)
            {
                cached.Extension =
                    weaponDef.GetModExtension<ShuttleWeaponModuleAmmoExtension>();
                cached.Resolved = true;
            }

            return cached.Extension;
        }

        private sealed class CachedAmmoExtension
        {
            public CachedAmmoExtension()
            {
            }

            internal bool Resolved;
            internal ShuttleWeaponModuleAmmoExtension Extension;
        }

        private static string ToPlayerFailure(string failureReason)
        {
            if (failureReason == "ammo-loader-unavailable")
            {
                return "CT_Shuttle_WeaponAmmo_AmmoLoaderUnavailable".Translate().ToString();
            }

            if (failureReason == "compatible-cargo-ammo-missing")
            {
                return "CT_Shuttle_WeaponAmmo_SourceEmpty".Translate().ToString();
            }

            if (failureReason == "cargo-unavailable")
            {
                return "CT_Shuttle_WeaponAmmo_SourceUnavailable".Translate().ToString();
            }

            if (failureReason == "logistics-core-unavailable")
            {
                return "CT_Shuttle_WeaponAmmo_LogisticsUnavailable".Translate().ToString();
            }

            return "CT_Shuttle_WeaponAmmo_AutomaticReloadUnavailable"
                .Translate()
                .ToString();
        }

    }
}
