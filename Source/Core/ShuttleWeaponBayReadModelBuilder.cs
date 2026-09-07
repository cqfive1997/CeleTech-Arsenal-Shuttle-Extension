using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Builds detached Weapon Bay UI projections from assembly/profile/runtime reads.
    /// It does not mutate shuttle state or expose runtime payloads to the UI.
    /// </summary>
    internal sealed class ShuttleWeaponBayReadModelBuilder
    {
        private static readonly ShuttleSurfaceShieldRuntimeService SharedSurfaceShieldRuntimeService =
            new ShuttleSurfaceShieldRuntimeService();

        internal ShuttleWeaponBayReadModel Build(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ThingWithComps shuttleHost,
            ShuttleProfile currentProfile,
            IShuttleCargoResourceBroker cargoResourceBroker = null)
        {
            if (assemblyState == null)
            {
                return ShuttleWeaponBayReadModel.Empty;
            }

            assemblyState.EnsureInitialized();

            List<ShuttleShieldStatusReadModel> shields =
                new List<ShuttleShieldStatusReadModel>();
            List<ShuttleWeaponSlotReadModel> weaponSlots =
                new List<ShuttleWeaponSlotReadModel>();
            List<ShuttleWeaponControlReadModel> weaponControls =
                new List<ShuttleWeaponControlReadModel>();
            ShuttleProjectileInterceptorBackendStatusSnapshot backendStatus =
                this.BuildProjectileInterceptorBackendStatusSnapshot(shuttleHost);
            ShuttleSurfaceShieldStatusSnapshot surfaceShieldStatus =
                this.BuildSurfaceShieldStatusSnapshot(assemblyState, runtimeState, shuttleHost);
            bool hasWeaponBay = false;
            int weaponSlotOrdinal = 0;

            IReadOnlyList<ShuttleSegment> segments = assemblyState.Segments;
            for (int i = 0; i < segments.Count; i++)
            {
                ShuttleSegment segment = segments[i];
                if (segment == null || segment.ModuleSlots == null)
                {
                    continue;
                }

                if (segment.SegmentDef != null &&
                    segment.SegmentDef.SegmentType == ShuttleSegmentType.Weapon)
                {
                    hasWeaponBay = true;
                }

                for (int j = 0; j < segment.ModuleSlots.Count; j++)
                {
                    ShuttleModuleSlot slot = segment.ModuleSlots[j];
                    if (slot == null)
                    {
                        continue;
                    }

                    ShuttleModuleType slotType =
                        ShuttleModuleTypeCatalog.ParseSlotType(slot.SlotTypeID);
                    if (slotType == ShuttleModuleType.Shield)
                    {
                        hasWeaponBay = true;
                        shields.Add(this.BuildShieldStatusReadModel(
                            assemblyState,
                            runtimeState,
                            slot,
                            backendStatus,
                            surfaceShieldStatus));
                    }
                    else if (slotType == ShuttleModuleType.Weapon)
                    {
                        hasWeaponBay = true;
                        weaponSlotOrdinal++;
                        weaponSlots.Add(this.BuildWeaponSlotReadModel(
                            assemblyState,
                            slot,
                            weaponSlotOrdinal));
                        ShuttleWeaponControlReadModel control =
                            this.BuildWeaponControlReadModel(
                                assemblyState,
                                runtimeState,
                                currentProfile,
                                shuttleHost,
                                cargoResourceBroker,
                                slot,
                                weaponSlotOrdinal);
                        if (control != null)
                        {
                            weaponControls.Add(control);
                        }
                    }
                }
            }

            return new ShuttleWeaponBayReadModel(
                hasWeaponBay,
                currentProfile != null ? currentProfile.Weapon : null,
                shields,
                weaponSlots,
                weaponControls);
        }

        internal bool TryGetShieldSettings(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            out ShuttleShieldSettingsSnapshot snapshot)
        {
            snapshot = null;

            if (string.IsNullOrEmpty(moduleInstanceID) || assemblyState == null)
            {
                return false;
            }

            ShuttleModule module = assemblyState.GetModule(moduleInstanceID);
            ShuttleShieldModuleDef shieldDef = module != null
                ? module.ModuleDef as ShuttleShieldModuleDef
                : null;
            if (shieldDef == null || !shieldDef.supportsRadiusControl)
            {
                return false;
            }

            IShuttleModuleRuntimeState runtimePayload;
            ShuttleShieldRuntimeState shieldState = runtimeState != null &&
                runtimeState.Modules.TryGetState(
                    module.ModuleInstanceID,
                    ShuttleShieldRuntimeSystem.ShieldRuntimeSystemKey,
                    out runtimePayload)
                ? runtimePayload as ShuttleShieldRuntimeState
                : null;

            float selectedRadius = shieldState != null
                ? shieldState.SelectedRadius
                : ShuttleShieldRuntimeState.MissingSelectedRadius;

            snapshot = new ShuttleShieldSettingsSnapshot();
            snapshot.ModuleInstanceID = module.ModuleInstanceID;
            snapshot.SupportsRadiusControl = shieldDef.supportsRadiusControl;
            snapshot.MinRadius = shieldDef.minRadius;
            snapshot.MaxRadius = shieldDef.maxRadius;
            snapshot.DefaultRadius = ShuttleShieldRuntimeUtility.ClampRadiusOrDefault(
                ShuttleShieldRuntimeState.MissingSelectedRadius,
                shieldDef);
            snapshot.SelectedRadius = ShuttleShieldRuntimeUtility.ClampRadiusOrDefault(
                selectedRadius,
                shieldDef);
            return true;
        }

        internal bool TryGetActiveProjectileInterceptorShield(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            out ShuttleProjectileInterceptorShieldSnapshot snapshot)
        {
            snapshot = null;
            if (assemblyState == null)
            {
                return false;
            }

            assemblyState.EnsureInitialized();

            IReadOnlyList<ShuttleModule> modules = assemblyState.Modules;
            if (modules == null)
            {
                return false;
            }

            for (int i = 0; i < modules.Count; i++)
            {
                ShuttleModule module = modules[i];
                if (module == null || !module.IsEnabled)
                {
                    continue;
                }

                ShuttleVanillaInterceptorShieldModuleDef shieldDef =
                    module.ModuleDef as ShuttleVanillaInterceptorShieldModuleDef;
                if (shieldDef == null || shieldDef.shieldHitPoints <= 0)
                {
                    continue;
                }

                // Current design exposes one active vanilla-interceptor shield to the host comp.
                // Multi-shield arbitration should be explicit rather than order-accidental.
                snapshot = this.BuildProjectileInterceptorShieldSnapshot(
                    runtimeState,
                    module,
                    shieldDef);
                return snapshot != null;
            }

            return false;
        }

        private ShuttleShieldStatusReadModel BuildShieldStatusReadModel(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ShuttleModuleSlot slot,
            ShuttleProjectileInterceptorBackendStatusSnapshot backendStatus,
            ShuttleSurfaceShieldStatusSnapshot surfaceShieldStatus)
        {
            ShuttleModule module = slot != null && !string.IsNullOrEmpty(slot.InstalledModuleInstanceID)
                ? assemblyState.GetModule(slot.InstalledModuleInstanceID)
                : null;
            ShuttleShieldModuleDef shieldDef = module != null
                ? module.ModuleDef as ShuttleShieldModuleDef
                : null;
            ShuttleVanillaInterceptorShieldModuleDef vanillaDef = module != null
                ? module.ModuleDef as ShuttleVanillaInterceptorShieldModuleDef
                : null;
            ShuttleSurfaceShieldModuleDef surfaceDef = module != null
                ? module.ModuleDef as ShuttleSurfaceShieldModuleDef
                : null;

            ShuttleShieldSettingsSnapshot settings = null;
            bool hasSettings = module != null &&
                this.TryGetShieldSettings(
                    assemblyState,
                    runtimeState,
                    module.ModuleInstanceID,
                    out settings);
            if (surfaceDef != null)
            {
                return this.BuildSurfaceShieldStatusReadModel(
                    slot,
                    module,
                    surfaceDef,
                    settings,
                    hasSettings,
                    surfaceShieldStatus);
            }

            bool backendMatches = module != null &&
                backendStatus != null &&
                backendStatus.HasActiveShield &&
                backendStatus.ModuleInstanceID == module.ModuleInstanceID;
            bool moduleEnabled = module != null && module.IsEnabled;

            string statusKey = this.GetVanillaShieldStatusKey(
                module,
                shieldDef,
                vanillaDef,
                backendStatus,
                backendMatches);
            string status = this.GetVanillaShieldStatusLabel(statusKey);
            int currentHitPoints = moduleEnabled && backendMatches ? backendStatus.CurrentHitPoints : 0;
            int maxHitPoints = moduleEnabled
                ? backendMatches
                    ? backendStatus.MaxHitPoints
                    : vanillaDef != null ? this.GetEffectiveShieldHitPoints(vanillaDef.shieldHitPoints) : 0
                : 0;
            int chargingTicksLeft = moduleEnabled && backendMatches ? backendStatus.ChargingTicksLeft : 0;
            int cooldownTicksLeft = moduleEnabled && backendMatches ? backendStatus.CooldownTicksLeft : 0;
            int rechargeHitPoints = vanillaDef != null
                ? this.GetEffectiveShieldRechargeHitPoints(vanillaDef.hitPointsPerRechargeInterval)
                : 0;
            int rechargeIntervalTicks = vanillaDef != null
                ? this.GetEffectiveShieldRechargeIntervalTicks(vanillaDef.rechargeIntervalTicks)
                : 0;
            float rechargeEnergyPerHitPointWd = vanillaDef != null
                ? this.GetEffectiveShieldRechargeEnergyPerHitPointWd(vanillaDef.rechargeEnergyPerHitPointWd)
                : 0f;
            float rechargeEnergyPerIntervalWd = rechargeHitPoints * rechargeEnergyPerHitPointWd;

            return new ShuttleShieldStatusReadModel(
                slot != null ? slot.SlotID : null,
                module != null ? module.ModuleInstanceID : null,
                this.GetModuleDisplayName(module),
                module != null ? module.moduleDefName : null,
                vanillaDef != null ? "VanillaInterceptor" : "None",
                statusKey,
                module != null,
                moduleEnabled,
                vanillaDef != null,
                false,
                vanillaDef != null,
                moduleEnabled && backendMatches && backendStatus.Active,
                false,
                moduleEnabled && backendMatches && backendStatus.Charging,
                false,
                moduleEnabled && hasSettings && settings.SupportsRadiusControl,
                status,
                currentHitPoints,
                maxHitPoints,
                maxHitPoints > 0 ? (float)currentHitPoints / maxHitPoints : -1f,
                hasSettings ? this.GetEffectiveShieldRadius(settings.SelectedRadius) : 0f,
                hasSettings ? this.GetEffectiveShieldRadius(settings.MinRadius) : 0f,
                hasSettings ? this.GetEffectiveShieldRadius(settings.MaxRadius) : 0f,
                hasSettings ? this.GetEffectiveShieldRadius(settings.DefaultRadius) : 0f,
                rechargeHitPoints,
                rechargeIntervalTicks,
                rechargeEnergyPerIntervalWd,
                rechargeEnergyPerHitPointWd,
                false,
                1f,
                1f,
                1f,
                rechargeHitPoints,
                rechargeEnergyPerIntervalWd,
                vanillaDef != null
                    ? this.GetInterceptMode(vanillaDef.interceptGroundProjectiles, vanillaDef.interceptAirProjectiles)
                    : "CT_Shuttle_ShieldInterceptNone".Translate().ToString(),
                vanillaDef != null && vanillaDef.interceptNonHostileProjectiles,
                chargingTicksLeft,
                cooldownTicksLeft,
                cooldownTicksLeft,
                chargingTicksLeft);
        }

        private ShuttleShieldStatusReadModel BuildSurfaceShieldStatusReadModel(
            ShuttleModuleSlot slot,
            ShuttleModule module,
            ShuttleSurfaceShieldModuleDef surfaceDef,
            ShuttleShieldSettingsSnapshot settings,
            bool hasSettings,
            ShuttleSurfaceShieldStatusSnapshot surfaceShieldStatus)
        {
            bool moduleEnabled = module != null && module.IsEnabled;
            bool snapshotMatches = module != null &&
                surfaceShieldStatus != null &&
                surfaceShieldStatus.ModuleInstanceID == module.ModuleInstanceID;
            string statusKey = snapshotMatches
                ? surfaceShieldStatus.StatusKey
                : moduleEnabled ? "Missing" : "Disabled";
            int maxHitPoints = snapshotMatches
                ? surfaceShieldStatus.MaxHitPoints
                : surfaceDef != null && surfaceDef.maxHitPoints > 0
                    ? this.GetEffectiveShieldHitPoints(surfaceDef.maxHitPoints)
                    : 0;
            int currentHitPoints = snapshotMatches ? surfaceShieldStatus.CurrentHitPoints : 0;
            float hitPointsPercent = snapshotMatches
                ? surfaceShieldStatus.HitPointsPercent
                : maxHitPoints > 0 ? 0f : -1f;
            int rechargeHitPoints = snapshotMatches
                ? surfaceShieldStatus.RechargeHitPointsPerInterval
                : surfaceDef != null && surfaceDef.rechargeHitPointsPerInterval > 0
                    ? this.GetEffectiveShieldRechargeHitPoints(surfaceDef.rechargeHitPointsPerInterval)
                    : 0;
            int rechargeIntervalTicks = snapshotMatches
                ? surfaceShieldStatus.RechargeIntervalTicks
                : surfaceDef != null && surfaceDef.rechargeIntervalTicks > 0
                    ? this.GetEffectiveShieldRechargeIntervalTicks(surfaceDef.rechargeIntervalTicks)
                    : 0;
            float rechargeEnergyPerHitPointWd = snapshotMatches
                ? surfaceShieldStatus.RechargeEnergyPerHitPointWd
                : surfaceDef != null && surfaceDef.rechargeEnergyPerHitPointWd > 0f
                    ? this.GetEffectiveShieldRechargeEnergyPerHitPointWd(surfaceDef.rechargeEnergyPerHitPointWd)
                    : 0f;
            float rechargeSpeedMultiplier = snapshotMatches
                ? surfaceShieldStatus.RechargeSpeedMultiplier
                : ShuttleSurfaceShieldRuntimeState.DefaultRechargeSpeedMultiplier;
            float minRechargeSpeedMultiplier = snapshotMatches
                ? surfaceShieldStatus.MinRechargeSpeedMultiplier
                : ShuttleSurfaceShieldRuntimeState.MinRechargeSpeedMultiplier;
            float maxRechargeSpeedMultiplier = snapshotMatches
                ? surfaceShieldStatus.MaxRechargeSpeedMultiplier
                : ShuttleSurfaceShieldRuntimeState.MaxRechargeSpeedMultiplier;
            int effectiveRechargeHitPoints = snapshotMatches
                ? surfaceShieldStatus.EffectiveRechargeHitPointsPerInterval
                : this.CalculateEffectiveRechargeHitPoints(rechargeHitPoints, rechargeSpeedMultiplier);
            float effectiveRechargeEnergyWd = snapshotMatches
                ? surfaceShieldStatus.EffectiveRechargeEnergyPerIntervalWd
                : effectiveRechargeHitPoints * rechargeEnergyPerHitPointWd;

            return new ShuttleShieldStatusReadModel(
                slot != null ? slot.SlotID : null,
                module != null ? module.ModuleInstanceID : null,
                this.GetModuleDisplayName(module),
                module != null ? module.moduleDefName : null,
                "SurfaceShield",
                statusKey,
                module != null,
                moduleEnabled,
                surfaceDef != null,
                true,
                false,
                snapshotMatches && surfaceShieldStatus.Online,
                snapshotMatches && surfaceShieldStatus.Broken,
                snapshotMatches && surfaceShieldStatus.RechargingBlocked,
                snapshotMatches && surfaceShieldStatus.RechargeStalledForNoEnergy,
                moduleEnabled && hasSettings && settings.SupportsRadiusControl,
                this.GetSurfaceShieldStatusLabel(statusKey),
                currentHitPoints,
                maxHitPoints,
                hitPointsPercent,
                snapshotMatches ? surfaceShieldStatus.SelectedRadius : hasSettings ? this.GetEffectiveShieldRadius(settings.SelectedRadius) : 0f,
                snapshotMatches ? surfaceShieldStatus.MinRadius : hasSettings ? this.GetEffectiveShieldRadius(settings.MinRadius) : 0f,
                snapshotMatches ? surfaceShieldStatus.MaxRadius : hasSettings ? this.GetEffectiveShieldRadius(settings.MaxRadius) : 0f,
                hasSettings ? this.GetEffectiveShieldRadius(settings.DefaultRadius) : 0f,
                rechargeHitPoints,
                rechargeIntervalTicks,
                effectiveRechargeEnergyWd,
                rechargeEnergyPerHitPointWd,
                true,
                rechargeSpeedMultiplier,
                minRechargeSpeedMultiplier,
                maxRechargeSpeedMultiplier,
                effectiveRechargeHitPoints,
                effectiveRechargeEnergyWd,
                "CT_Shuttle_ShieldInterceptNone".Translate().ToString(),
                false,
                0,
                snapshotMatches ? surfaceShieldStatus.BrokenTicksLeft : 0,
                snapshotMatches ? surfaceShieldStatus.BrokenTicksLeft : 0,
                snapshotMatches ? surfaceShieldStatus.RechargeBlockedTicksLeft : 0);
        }

        private ShuttleWeaponSlotReadModel BuildWeaponSlotReadModel(
            ShuttleAssemblyState assemblyState,
            ShuttleModuleSlot slot,
            int weaponSlotOrdinal)
        {
            ShuttleModule module = slot != null && !string.IsNullOrEmpty(slot.InstalledModuleInstanceID)
                ? assemblyState.GetModule(slot.InstalledModuleInstanceID)
                : null;

            return new ShuttleWeaponSlotReadModel(
                slot != null ? slot.SlotID : null,
                this.GetWeaponSlotDisplayLabel(slot, weaponSlotOrdinal, false),
                this.GetWeaponSlotDisplayLabel(slot, weaponSlotOrdinal, true),
                slot != null ? slot.SlotIndex : -1,
                this.GetModuleDisplayName(module),
                module != null ? module.moduleDefName : null);
        }

        private ShuttleWeaponControlReadModel BuildWeaponControlReadModel(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ShuttleProfile currentProfile,
            ThingWithComps shuttleHost,
            IShuttleCargoResourceBroker cargoResourceBroker,
            ShuttleModuleSlot slot,
            int weaponSlotOrdinal)
        {
            ShuttleModule module = slot != null && !string.IsNullOrEmpty(slot.InstalledModuleInstanceID)
                ? assemblyState.GetModule(slot.InstalledModuleInstanceID)
                : null;
            ShuttleWeaponModuleDef weaponDef = module != null
                ? module.ModuleDef as ShuttleWeaponModuleDef
                : null;
            if (module == null || weaponDef == null)
            {
                return null;
            }

            ShuttleWeaponRuntimeState weaponState =
                this.GetWeaponRuntimeStateForRead(runtimeState, module.ModuleInstanceID);
            bool moduleEnabled = module.IsEnabled;
            bool hasState = weaponState != null;
            bool hasForcedTarget = hasState && weaponState.HasForcedTargetForRuntimeOnly();
            LocalTargetInfo forcedTarget = hasForcedTarget
                ? weaponState.GetForcedTargetForRuntimeOnly()
                : LocalTargetInfo.Invalid;
            bool hasFireControlRadar = currentProfile != null &&
                currentProfile.FireControl != null &&
                currentProfile.FireControl.HasFireControlRadar;
            bool fireControlAvailable = hasFireControlRadar && weaponDef.canAutoFire;
            bool fireControlLinked = !hasState || weaponState.FireControlLinked;
            bool effectiveFireControlLinked = hasState && fireControlLinked && fireControlAvailable;
            bool autoFireAvailable = fireControlAvailable &&
                currentProfile.FireControl.SupportsAutoDefense;
            bool pointDefenseAvailable = fireControlAvailable &&
                currentProfile.FireControl.SupportsPointDefense &&
                weaponDef.canInterceptProjectiles;
            bool canUseFireControlCommands = moduleEnabled &&
                hasState &&
                weaponDef.canAutoFire;
            ShuttleWeaponFireControlMode fireControlMode = hasState
                ? weaponState.FireControlMode
                : ShuttleWeaponFireControlMode.AutoDefense;
            ShuttleWeaponTargetPriority targetPriority = hasState
                ? weaponState.TargetPriority
                : ShuttleWeaponTargetPriority.ClosestHostile;
            string fireControlStatusLabel = this.GetFireControlStatusLabel(
                weaponDef,
                hasFireControlRadar,
                fireControlLinked);
            string fireControlStatusTooltip = this.GetFireControlStatusTooltip(
                weaponDef,
                hasFireControlRadar,
                fireControlLinked);
            ShuttleWeaponAmmoReadProjection ammoProjection = this.BuildAmmoProjection(
                module,
                weaponDef,
                weaponState,
                currentProfile,
                runtimeState,
                shuttleHost,
                cargoResourceBroker);

            ShuttleWeaponControlReadModel control = new ShuttleWeaponControlReadModel(
                module.ModuleInstanceID,
                this.GetModuleDisplayName(module),
                module.moduleDefName,
                slot != null ? slot.SlotID : null,
                this.GetWeaponSlotDisplayLabel(slot, weaponSlotOrdinal, false),
                this.GetWeaponSlotDisplayLabel(slot, weaponSlotOrdinal, true),
                slot != null ? slot.SlotIndex : -1,
                moduleEnabled,
                this.GetWeaponDisplayName(weaponDef),
                this.GetWeaponRoleLabel(weaponDef.weaponRole),
                moduleEnabled && hasState && weaponDef.canSetForcedTarget,
                moduleEnabled && hasState && hasForcedTarget,
                moduleEnabled && hasState,
                hasState && weaponState.GetHoldFireForRuntimeOnly(),
                hasForcedTarget,
                this.GetForcedTargetDisplayName(forcedTarget),
                moduleEnabled && this.WeaponRoleAllowsLocationTarget(weaponDef.weaponRole),
                fireControlLinked,
                fireControlMode.ToString(),
                this.GetFireControlModeLabel(fireControlMode),
                targetPriority.ToString(),
                this.GetTargetPriorityLabel(targetPriority),
                !hasState || weaponState.AutoFireEnabled,
                hasFireControlRadar,
                fireControlAvailable,
                effectiveFireControlLinked,
                autoFireAvailable,
                pointDefenseAvailable,
                this.GetFireControlUnavailableReason(moduleEnabled, weaponDef, hasFireControlRadar),
                fireControlStatusLabel,
                fireControlStatusTooltip,
                canUseFireControlCommands,
                canUseFireControlCommands,
                canUseFireControlCommands,
                canUseFireControlCommands,
                ammoProjection.HasAmmoSystem,
                ammoProjection.SelectedAmmoLabel,
                ammoProjection.SelectedAmmoDefName,
                ammoProjection.LoadedAmmoCount,
                ammoProjection.MagazineCapacity,
                ammoProjection.AmmoPerShot,
                ammoProjection.ReloadInProgress,
                ammoProjection.ReloadProgress01,
                ammoProjection.AutoReloadEnabled,
                ammoProjection.LogisticsAutoFeedEnabled,
                ammoProjection.LogisticsCoreAvailable,
                ammoProjection.AmmoStockCount,
                ammoProjection.LastAmmoFailureReason,
                ammoProjection.CeAmmoModeActive,
                ammoProjection.CanReload,
                ammoProjection.CanCancelReload,
                ammoProjection.CanSelectAmmo,
                ammoProjection.CanToggleAutoReload,
                ammoProjection.CanToggleLogisticsAutoFeed,
                ammoProjection.ReloadRequested,
                ammoProjection.ManualReloadAllowed,
                ammoProjection.CanToggleManualReloadAllowed,
                ammoProjection.ManualReloadJobActive,
                ammoProjection.LastReloadBlockerReason,
                ammoProjection.AmmoOptions);
            control.LastForcedTargetFailureReason = hasState
                ? ShuttleWeaponEngagementFailureText.TranslateReasonCode(
                    weaponState.GetLastForcedTargetFailureForRuntimeOnly())
                : null;
            return control;
        }

        private string GetWeaponSlotDisplayLabel(
            ShuttleModuleSlot slot,
            int weaponSlotOrdinal,
            bool shortLabel)
        {
            string key = slot != null
                ? shortLabel ? slot.ShortLabelKey : slot.LabelKey
                : null;
            if (!this.IsGenericWeaponSlotLabelKey(key))
            {
                string translated = this.TranslateKeyOrNull(key);
                if (!string.IsNullOrEmpty(translated))
                {
                    return translated;
                }
            }

            if (weaponSlotOrdinal > 0)
            {
                string indexedKey = shortLabel
                    ? "CT_Shuttle_ModuleSlot_WeaponStationIndexedShort"
                    : "CT_Shuttle_ModuleSlot_WeaponStationIndexed";
                return indexedKey.Translate(weaponSlotOrdinal).ToString();
            }

            string fallbackKey = shortLabel
                ? "CT_Shuttle_ModuleSlot_WeaponShort"
                : "CT_Shuttle_ModuleSlot_Weapon";
            string fallback = this.TranslateKeyOrNull(fallbackKey);
            return !string.IsNullOrEmpty(fallback) ? fallback : fallbackKey;
        }

        private bool IsGenericWeaponSlotLabelKey(string key)
        {
            return key == "CT_Shuttle_ModuleSlot_Weapon" ||
                key == "CT_Shuttle_ModuleSlot_WeaponShort";
        }

        private string TranslateKeyOrNull(string key)
        {
            if (string.IsNullOrEmpty(key) || !Translator.CanTranslate(key))
            {
                return null;
            }

            string translated = key.Translate().ToString();
            return string.IsNullOrEmpty(translated) || translated == key
                ? null
                : translated;
        }

        private ShuttleWeaponAmmoReadProjection BuildAmmoProjection(
            ShuttleModule module,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            ShuttleProfile currentProfile,
            ShuttleRuntimeState runtimeState,
            ThingWithComps shuttleHost,
            IShuttleCargoResourceBroker cargoResourceBroker)
        {
            ShuttleWeaponAmmoReadProjection projection = new ShuttleWeaponAmmoReadProjection();
            if (module == null || weaponState == null)
            {
                return projection;
            }

            ShuttleModuleRuntimeContext context = new ShuttleModuleRuntimeContext(
                shuttleHost,
                currentProfile,
                runtimeState,
                module,
                null,
                weaponState,
                null,
                null,
                cargoResourceBroker,
                ShuttleTickUtility.TicksGameOrMinusOne());
            ShuttleWeaponAmmoSnapshot snapshot;
            if (!ShuttleWeaponRuntimeSystem.Instance.TryBuildAmmoSnapshot(
                    context,
                    out snapshot) ||
                snapshot == null)
            {
                return projection;
            }

            projection.HasAmmoSystem = snapshot.HasAmmoSystem;
            projection.SelectedAmmoDefName = snapshot.SelectedAmmoDefName;
            projection.SelectedAmmoLabel = snapshot.SelectedAmmoLabel;
            projection.LoadedAmmoCount = snapshot.LoadedAmmoCount;
            projection.MagazineCapacity = snapshot.MagazineCapacity;
            projection.AmmoPerShot = snapshot.AmmoPerShot;
            projection.ReloadInProgress = snapshot.ReloadInProgress;
            projection.ReloadRequested = snapshot.ReloadRequested;
            projection.ReloadProgress01 = snapshot.ReloadProgress01;
            projection.AutoReloadEnabled = snapshot.AutoReloadEnabled;
            projection.LogisticsAutoFeedEnabled = snapshot.LogisticsAutoFeedEnabled;
            projection.ManualReloadAllowed = snapshot.ManualReloadAllowed;
            projection.ManualReloadJobActive = snapshot.ManualReloadJobActive;
            projection.LogisticsCoreAvailable = snapshot.LogisticsCoreAvailable;
            projection.AmmoStockCount = snapshot.AmmoStockCount;
            projection.LastAmmoFailureReason = snapshot.LastAmmoFailureReason;
            projection.LastReloadBlockerReason = snapshot.LastReloadBlockerReason;
            projection.CeAmmoModeActive = snapshot.CeAmmoModeActive;
            projection.CanReload = snapshot.CanReload;
            projection.CanCancelReload = snapshot.CanCancelReload;
            projection.CanSelectAmmo = snapshot.CanSelectAmmo;
            projection.CanToggleAutoReload = snapshot.CanToggleAutoReload;
            projection.CanToggleLogisticsAutoFeed = snapshot.CanToggleLogisticsAutoFeed;
            projection.CanToggleManualReloadAllowed = snapshot.CanToggleManualReloadAllowed;

            for (int i = 0; i < snapshot.AmmoOptions.Count; i++)
            {
                ShuttleWeaponAmmoOptionSnapshot option = snapshot.AmmoOptions[i];
                projection.AmmoOptions.Add(new ShuttleWeaponAmmoOptionReadModel(
                    option.AmmoDefName,
                    option.Label,
                    option.AvailableCount,
                    option.Selected));
            }

            return projection;
        }

        private ShuttleWeaponRuntimeState GetWeaponRuntimeStateForRead(
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID)
        {
            if (runtimeState == null || string.IsNullOrEmpty(moduleInstanceID))
            {
                return null;
            }

            IShuttleModuleRuntimeState runtimePayload;
            if (!runtimeState.Modules.TryGetState(
                moduleInstanceID,
                ShuttleWeaponRuntimeSystem.WeaponRuntimeSystemKey,
                out runtimePayload))
            {
                return null;
            }

            return runtimePayload as ShuttleWeaponRuntimeState;
        }

        private ShuttleProjectileInterceptorBackendStatusSnapshot BuildProjectileInterceptorBackendStatusSnapshot(
            ThingWithComps shuttleHost)
        {
            CompModularShuttleProjectileInterceptor interceptor = shuttleHost != null
                ? shuttleHost.TryGetComp<CompModularShuttleProjectileInterceptor>()
                : null;
            return interceptor != null ? interceptor.BuildStatusSnapshot() : null;
        }

        private ShuttleSurfaceShieldStatusSnapshot BuildSurfaceShieldStatusSnapshot(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ThingWithComps shuttleHost)
        {
            ShuttleSurfaceShieldStatusSnapshot snapshot;
            int ticksGame = ShuttleTickUtility.TicksGameOrZero();
            return SharedSurfaceShieldRuntimeService.TryGetActiveSurfaceShieldStatus(
                shuttleHost,
                assemblyState,
                runtimeState,
                ticksGame,
                out snapshot)
                ? snapshot
                : null;
        }

        private string GetVanillaShieldStatusKey(
            ShuttleModule module,
            ShuttleShieldModuleDef shieldDef,
            ShuttleVanillaInterceptorShieldModuleDef vanillaDef,
            ShuttleProjectileInterceptorBackendStatusSnapshot backendStatus,
            bool backendMatches)
        {
            if (module == null)
            {
                return "Missing";
            }

            if (!module.IsEnabled)
            {
                return "Disabled";
            }

            if (shieldDef == null || vanillaDef == null || !backendMatches || backendStatus == null)
            {
                return "Offline";
            }

            if (backendStatus.Charging)
            {
                return "Charging";
            }

            if (backendStatus.OnCooldown)
            {
                return "Cooldown";
            }

            return backendStatus.Active ? "Active" : "Offline";
        }

        private string GetVanillaShieldStatusLabel(string statusKey)
        {
            if (statusKey == "Missing")
            {
                return "CT_Shuttle_ShieldStatusNoModule".Translate().ToString();
            }

            if (statusKey == "Disabled")
            {
                return "CT_Shuttle_ShieldStatusDisabled".Translate().ToString();
            }

            if (statusKey == "Charging")
            {
                return "CT_Shuttle_ShieldStatusCharging".Translate().ToString();
            }

            if (statusKey == "Cooldown")
            {
                return "CT_Shuttle_ShieldStatusCooldown".Translate().ToString();
            }

            if (statusKey == "Active")
            {
                return "CT_Shuttle_ShieldStatusActive".Translate().ToString();
            }

            return "CT_Shuttle_ShieldStatusOffline".Translate().ToString();
        }

        private string GetSurfaceShieldStatusLabel(string statusKey)
        {
            string safeKey = string.IsNullOrEmpty(statusKey) ? "Missing" : statusKey;
            return ("CT_Shuttle_SurfaceShield_Status_" + safeKey).Translate().ToString();
        }

        private string GetInterceptMode(bool ground, bool air)
        {
            if (ground && air)
            {
                return "CT_Shuttle_ShieldInterceptGroundAir".Translate().ToString();
            }

            if (ground)
            {
                return "CT_Shuttle_ShieldInterceptGround".Translate().ToString();
            }

            return air
                ? "CT_Shuttle_ShieldInterceptAir".Translate().ToString()
                : "CT_Shuttle_ShieldInterceptNone".Translate().ToString();
        }

        private string GetModuleDisplayName(ShuttleModule module)
        {
            if (module == null)
            {
                return null;
            }

            if (module.ModuleDef != null && !string.IsNullOrEmpty(module.ModuleDef.label))
            {
                return module.ModuleDef.LabelCap.ToString();
            }

            return module.moduleDefName;
        }

        private string GetWeaponDisplayName(ShuttleWeaponModuleDef weaponDef)
        {
            if (weaponDef == null)
            {
                return null;
            }

            if (weaponDef.weaponDef != null && !string.IsNullOrEmpty(weaponDef.weaponDef.label))
            {
                return weaponDef.weaponDef.LabelCap.ToString();
            }

            return weaponDef.weaponDef != null ? weaponDef.weaponDef.defName : weaponDef.defName;
        }

        private string GetWeaponRoleLabel(ShuttleWeaponRole role)
        {
            switch (role)
            {
                case ShuttleWeaponRole.PointDefense:
                    return "CT_Shuttle_WeaponRole_PointDefense".Translate().ToString();
                case ShuttleWeaponRole.CloseInWeapon:
                    return "CT_Shuttle_WeaponRole_CloseInWeapon".Translate().ToString();
                case ShuttleWeaponRole.DirectFire:
                    return "CT_Shuttle_WeaponRole_DirectFire".Translate().ToString();
                case ShuttleWeaponRole.Rocket:
                    return "CT_Shuttle_WeaponRole_Rocket".Translate().ToString();
                case ShuttleWeaponRole.Missile:
                    return "CT_Shuttle_WeaponRole_Missile".Translate().ToString();
                case ShuttleWeaponRole.Artillery:
                    return "CT_Shuttle_WeaponRole_Artillery".Translate().ToString();
                default:
                    return "CT_Shuttle_WeaponRole_Unknown".Translate().ToString();
            }
        }

        private string GetForcedTargetDisplayName(LocalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return "CT_Shuttle_WeaponForcedTargetNone".Translate().ToString();
            }

            if (target.HasThing && target.Thing != null)
            {
                return target.Thing.LabelCap.ToString();
            }

            return "CT_Shuttle_WeaponForcedTargetCell".Translate(target.Cell).ToString();
        }

        private string GetFireControlModeLabel(ShuttleWeaponFireControlMode mode)
        {
            switch (mode)
            {
                case ShuttleWeaponFireControlMode.Offline:
                    return "CT_Shuttle_FireControl_Mode_Offline".Translate().ToString();
                case ShuttleWeaponFireControlMode.AutoDefense:
                    return "CT_Shuttle_FireControl_Mode_AutoDefense".Translate().ToString();
                case ShuttleWeaponFireControlMode.PointDefense:
                    return "CT_Shuttle_FireControl_Mode_PointDefense".Translate().ToString();
                default:
                    return "CT_Shuttle_FireControl_Mode_ManualOnly".Translate().ToString();
            }
        }

        private string GetTargetPriorityLabel(ShuttleWeaponTargetPriority priority)
        {
            switch (priority)
            {
                case ShuttleWeaponTargetPriority.RaidersFirst:
                    return "CT_Shuttle_FireControl_TargetPriority_RaidersFirst".Translate().ToString();
                case ShuttleWeaponTargetPriority.MechanoidsFirst:
                    return "CT_Shuttle_FireControl_TargetPriority_MechanoidsFirst".Translate().ToString();
                case ShuttleWeaponTargetPriority.ManhuntersFirst:
                    return "CT_Shuttle_FireControl_TargetPriority_ManhuntersFirst".Translate().ToString();
                case ShuttleWeaponTargetPriority.HighThreatFirst:
                    return "CT_Shuttle_FireControl_TargetPriority_HighThreatFirst".Translate().ToString();
                case ShuttleWeaponTargetPriority.ForcedTargetOnly:
                    return "CT_Shuttle_FireControl_TargetPriority_ForcedTargetOnly".Translate().ToString();
                default:
                    return "CT_Shuttle_FireControl_TargetPriority_ClosestHostile".Translate().ToString();
            }
        }

        private string GetFireControlUnavailableReason(
            bool moduleEnabled,
            ShuttleWeaponModuleDef weaponDef,
            bool hasFireControlRadar)
        {
            if (!moduleEnabled)
            {
                return "CT_Shuttle_FireControl_Unavailable_ModuleDisabled".Translate().ToString();
            }

            if (weaponDef == null || !weaponDef.canAutoFire)
            {
                return "CT_Shuttle_FireControl_Unavailable_WeaponNoAutoFire".Translate().ToString();
            }

            if (!hasFireControlRadar)
            {
                return "CT_Shuttle_FireControl_Unavailable_NoRadar".Translate().ToString();
            }

            return string.Empty;
        }

        private string GetFireControlStatusLabel(
            ShuttleWeaponModuleDef weaponDef,
            bool hasFireControlRadar,
            bool fireControlLinked)
        {
            if (weaponDef == null || !weaponDef.canAutoFire)
            {
                return "CT_Shuttle_FireControl_Status_Unsupported".Translate().ToString();
            }

            if (!hasFireControlRadar && fireControlLinked)
            {
                return "CT_Shuttle_FireControl_Status_WaitingForRadar".Translate().ToString();
            }

            if (!hasFireControlRadar)
            {
                return "CT_Shuttle_FireControl_Status_ManuallyDisabledNoRadar".Translate().ToString();
            }

            return fireControlLinked
                ? "CT_Shuttle_FireControl_Status_Linked".Translate().ToString()
                : "CT_Shuttle_FireControl_Status_ManuallyDisabled".Translate().ToString();
        }

        private string GetFireControlStatusTooltip(
            ShuttleWeaponModuleDef weaponDef,
            bool hasFireControlRadar,
            bool fireControlLinked)
        {
            if (weaponDef == null || !weaponDef.canAutoFire)
            {
                return "CT_Shuttle_FireControl_Unavailable_WeaponNoAutoFire".Translate().ToString();
            }

            if (!hasFireControlRadar && fireControlLinked)
            {
                return "CT_Shuttle_FireControl_EnableTooltip_NoRadar".Translate().ToString();
            }

            if (!hasFireControlRadar)
            {
                return "CT_Shuttle_FireControl_DisableTooltip_NoRadar".Translate().ToString();
            }

            return fireControlLinked
                ? "CT_Shuttle_FireControl_EnableTooltip_WithRadar".Translate().ToString()
                : "CT_Shuttle_FireControl_DisableTooltip_WithRadar".Translate().ToString();
        }

        private bool WeaponRoleAllowsLocationTarget(ShuttleWeaponRole role)
        {
            return role == ShuttleWeaponRole.Rocket ||
                role == ShuttleWeaponRole.Missile ||
                role == ShuttleWeaponRole.Artillery;
        }

        private int CalculateEffectiveRechargeHitPoints(int baseRechargeHitPoints, float multiplier)
        {
            if (baseRechargeHitPoints <= 0)
            {
                return 0;
            }

            float sanitizedMultiplier =
                ShuttleSurfaceShieldRuntimeState.SanitizeRechargeSpeedMultiplier(multiplier);
            double effective = System.Math.Ceiling(baseRechargeHitPoints * (double)sanitizedMultiplier);
            if (effective <= 0d)
            {
                return 0;
            }

            return effective > int.MaxValue ? int.MaxValue : (int)effective;
        }

        private ShuttleProjectileInterceptorShieldSnapshot BuildProjectileInterceptorShieldSnapshot(
            ShuttleRuntimeState runtimeState,
            ShuttleModule module,
            ShuttleVanillaInterceptorShieldModuleDef shieldDef)
        {
            if (runtimeState == null || module == null || shieldDef == null)
            {
                return null;
            }

            IShuttleModuleRuntimeState runtimePayload;
            ShuttleShieldRuntimeState shieldState = runtimeState.Modules.TryGetState(
                module.ModuleInstanceID,
                ShuttleShieldRuntimeSystem.ShieldRuntimeSystemKey,
                out runtimePayload)
                ? runtimePayload as ShuttleShieldRuntimeState
                : null;

            float selectedRadius = shieldState != null
                ? shieldState.SelectedRadius
                : ShuttleShieldRuntimeState.MissingSelectedRadius;

            ShuttleProjectileInterceptorShieldSnapshot snapshot =
                new ShuttleProjectileInterceptorShieldSnapshot();
            snapshot.ModuleInstanceID = module.ModuleInstanceID;
            snapshot.SelectedRadius = ShuttleShieldRuntimeUtility.ClampRadiusOrDefault(
                selectedRadius,
                shieldDef);
            snapshot.SelectedRadius = this.GetEffectiveShieldRadius(snapshot.SelectedRadius);
            snapshot.ShieldHitPoints = this.GetEffectiveShieldHitPoints(shieldDef.shieldHitPoints);
            snapshot.HitPointsPerRechargeInterval = this.GetEffectiveShieldRechargeHitPoints(shieldDef.hitPointsPerRechargeInterval);
            snapshot.RechargeIntervalTicks = this.GetEffectiveShieldRechargeIntervalTicks(shieldDef.rechargeIntervalTicks);
            snapshot.RechargeEnergyPerHitPointWd = this.GetEffectiveShieldRechargeEnergyPerHitPointWd(shieldDef.rechargeEnergyPerHitPointWd);
            snapshot.InterceptGroundProjectiles = shieldDef.interceptGroundProjectiles;
            snapshot.InterceptAirProjectiles = shieldDef.interceptAirProjectiles;
            snapshot.InterceptNonHostileProjectiles = shieldDef.interceptNonHostileProjectiles;
            snapshot.CooldownTicks = this.GetEffectiveShieldBrokenTicks(shieldDef.cooldownTicks);
            snapshot.InterceptEffect = shieldDef.interceptEffect;
            snapshot.Color = shieldDef.color;
            snapshot.ActiveSound = shieldDef.activeSound;
            snapshot.ReactivateEffect = shieldDef.reactivateEffect;
            return snapshot;
        }

        private int GetEffectiveShieldHitPoints(int baseHitPoints)
        {
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyIntMultiplier(
                baseHitPoints > 0 ? baseHitPoints : 0,
                CeleTechShuttleMod.EffectiveCombatTuning.ShieldHitPointsMultiplier,
                baseHitPoints > 0 ? 1 : 0,
                int.MaxValue);
        }

        private int GetEffectiveShieldRechargeHitPoints(int baseHitPoints)
        {
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyIntMultiplier(
                baseHitPoints > 0 ? baseHitPoints : 0,
                CeleTechShuttleMod.EffectiveCombatTuning.ShieldRechargeRateMultiplier,
                baseHitPoints > 0 ? 1 : 0,
                int.MaxValue);
        }

        private int GetEffectiveShieldRechargeIntervalTicks(int baseTicks)
        {
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyTicksMultiplier(
                baseTicks > 0 ? baseTicks : 0,
                CeleTechShuttleMod.EffectiveCombatTuning.ShieldRechargeIntervalMultiplier,
                baseTicks > 0 ? 1 : 0,
                int.MaxValue);
        }

        private float GetEffectiveShieldRechargeEnergyPerHitPointWd(float baseCost)
        {
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyFloatMultiplier(
                baseCost > 0f ? baseCost : 0f,
                CeleTechShuttleMod.EffectiveCombatTuning.ShieldRechargeEnergyCostMultiplier,
                0f,
                float.MaxValue);
        }

        private int GetEffectiveShieldBrokenTicks(int baseTicks)
        {
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyTicksMultiplier(
                baseTicks > 0 ? baseTicks : 0,
                CeleTechShuttleMod.EffectiveCombatTuning.ShieldBrokenDowntimeMultiplier,
                baseTicks > 0 ? 1 : 0,
                int.MaxValue);
        }

        private float GetEffectiveShieldRadius(float baseRadius)
        {
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyFloatMultiplier(
                baseRadius > 0f ? baseRadius : 0f,
                CeleTechShuttleMod.EffectiveCombatTuning.ShieldRadiusMultiplier,
                0f,
                float.MaxValue);
        }

        private int GetEffectiveWeaponAmmoCapacity(int baseCapacity)
        {
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyIntMultiplier(
                baseCapacity > 0 ? baseCapacity : 0,
                CeleTechShuttleMod.EffectiveCombatTuning.WeaponAmmoCapacityMultiplier,
                baseCapacity > 0 ? 1 : 0,
                int.MaxValue);
        }

        private sealed class ShuttleWeaponAmmoReadProjection
        {
            internal bool HasAmmoSystem;
            internal string SelectedAmmoLabel;
            internal string SelectedAmmoDefName;
            internal int LoadedAmmoCount;
            internal int MagazineCapacity;
            internal int AmmoPerShot;
            internal bool ReloadInProgress;
            internal bool ReloadRequested;
            internal float ReloadProgress01;
            internal bool AutoReloadEnabled;
            internal bool LogisticsAutoFeedEnabled;
            internal bool ManualReloadAllowed;
            internal bool ManualReloadJobActive;
            internal bool LogisticsCoreAvailable;
            internal int AmmoStockCount;
            internal string LastAmmoFailureReason;
            internal string LastReloadBlockerReason;
            internal bool CeAmmoModeActive;
            internal bool CanReload;
            internal bool CanCancelReload;
            internal bool CanSelectAmmo;
            internal bool CanToggleAutoReload;
            internal bool CanToggleLogisticsAutoFeed;
            internal bool CanToggleManualReloadAllowed;
            internal readonly List<ShuttleWeaponAmmoOptionReadModel> AmmoOptions =
                new List<ShuttleWeaponAmmoOptionReadModel>();
        }
    }
}
