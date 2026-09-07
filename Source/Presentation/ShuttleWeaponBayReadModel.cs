using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    /// <summary>
    /// Detached Weapon Bay UI projection. It combines static weapon profile summaries with
    /// shield backend status without exposing AssemblyState, RuntimeState, or the interceptor comp.
    /// </summary>
    public sealed class ShuttleWeaponBayReadModel
    {
        private static readonly ShuttleWeaponBayReadModel empty =
            new ShuttleWeaponBayReadModel(
                false,
                null,
                new ShuttleShieldStatusReadModel[0],
                new ShuttleWeaponSlotReadModel[0],
                new ShuttleWeaponControlReadModel[0]);

        private readonly IReadOnlyList<ShuttleShieldStatusReadModel> shields;
        private readonly IReadOnlyList<ShuttleWeaponSlotReadModel> weaponSlots;
        private readonly IReadOnlyList<ShuttleWeaponControlReadModel> weaponControls;

        public ShuttleWeaponBayReadModel(
            bool hasWeaponBay,
            WeaponProfile weaponProfile,
            IReadOnlyList<ShuttleShieldStatusReadModel> shields,
            IReadOnlyList<ShuttleWeaponSlotReadModel> weaponSlots,
            IReadOnlyList<ShuttleWeaponControlReadModel> weaponControls)
        {
            this.HasWeaponBay = hasWeaponBay;
            this.ApplyWeaponProfile(weaponProfile);
            this.shields = shields ?? new ShuttleShieldStatusReadModel[0];
            this.weaponSlots = weaponSlots ?? new ShuttleWeaponSlotReadModel[0];
            this.weaponControls = weaponControls ?? new ShuttleWeaponControlReadModel[0];
        }

        public static ShuttleWeaponBayReadModel Empty
        {
            get
            {
                return empty;
            }
        }

        public bool HasWeaponBay { get; private set; }
        public int TotalWeaponModules { get; private set; }
        public int TotalWeaponMounts { get; private set; }
        public int PointDefenseModules { get; private set; }
        public int PointDefenseMounts { get; private set; }
        public int CloseInWeaponModules { get; private set; }
        public int CloseInWeaponMounts { get; private set; }
        public int RocketLauncherModules { get; private set; }
        public int RocketLauncherSlots { get; private set; }
        public int AutoFireCapableModules { get; private set; }
        public int ForcedTargetCapableModules { get; private set; }
        public int ScannerRequiredModules { get; private set; }
        public int NavigationRequiredModules { get; private set; }
        public int TotalAmmoCapacity { get; private set; }
        public float TotalStandbyPowerDrawWatts { get; private set; }
        public float MaxActiveFiringPowerDrawWatts { get; private set; }
        public bool HasWeapons { get; private set; }
        public bool HasPointDefense { get; private set; }
        public bool HasCloseInWeapons { get; private set; }
        public bool HasRocketLaunchers { get; private set; }

        public IReadOnlyList<ShuttleShieldStatusReadModel> Shields
        {
            get
            {
                return this.shields;
            }
        }

        public IReadOnlyList<ShuttleWeaponSlotReadModel> WeaponSlots
        {
            get
            {
                return this.weaponSlots;
            }
        }

        public IReadOnlyList<ShuttleWeaponControlReadModel> WeaponControls
        {
            get
            {
                return this.weaponControls;
            }
        }

        private void ApplyWeaponProfile(WeaponProfile weaponProfile)
        {
            if (weaponProfile == null)
            {
                return;
            }

            this.TotalWeaponModules = weaponProfile.TotalWeaponModules;
            this.TotalWeaponMounts = weaponProfile.TotalWeaponMounts;
            this.PointDefenseModules = weaponProfile.PointDefenseModules;
            this.PointDefenseMounts = weaponProfile.PointDefenseMounts;
            this.CloseInWeaponModules = weaponProfile.CloseInWeaponModules;
            this.CloseInWeaponMounts = weaponProfile.CloseInWeaponMounts;
            this.RocketLauncherModules = weaponProfile.RocketLauncherModules;
            this.RocketLauncherSlots = weaponProfile.RocketLauncherSlots;
            this.AutoFireCapableModules = weaponProfile.AutoFireCapableModules;
            this.ForcedTargetCapableModules = weaponProfile.ForcedTargetCapableModules;
            this.ScannerRequiredModules = weaponProfile.ScannerRequiredModules;
            this.NavigationRequiredModules = weaponProfile.NavigationRequiredModules;
            this.TotalAmmoCapacity = weaponProfile.TotalAmmoCapacity;
            this.TotalStandbyPowerDrawWatts = weaponProfile.TotalStandbyPowerDrawWatts;
            this.MaxActiveFiringPowerDrawWatts = weaponProfile.MaxActiveFiringPowerDrawWatts;
            this.HasWeapons = weaponProfile.HasWeapons;
            this.HasPointDefense = weaponProfile.HasPointDefense;
            this.HasCloseInWeapons = weaponProfile.HasCloseInWeapons;
            this.HasRocketLaunchers = weaponProfile.HasRocketLaunchers;
        }
    }

    public sealed class ShuttleShieldStatusReadModel
    {
        public ShuttleShieldStatusReadModel(
            string slotID,
            string moduleInstanceID,
            string moduleLabel,
            string moduleDefName,
            string backendKind,
            string statusKey,
            bool hasModule,
            bool isEnabled,
            bool hasShield,
            bool isSurfaceShield,
            bool isVanillaInterceptorShield,
            bool online,
            bool broken,
            bool rechargingBlocked,
            bool rechargeStalledForNoEnergy,
            bool canSetRadius,
            string status,
            int currentHitPoints,
            int maxHitPoints,
            float hitPointsPercent,
            float selectedRadius,
            float minRadius,
            float maxRadius,
            float defaultRadius,
            int rechargeHitPoints,
            int rechargeIntervalTicks,
            float rechargeEnergyCostWd,
            float rechargeEnergyPerHitPointWd,
            bool supportsRechargeSpeedControl,
            float rechargeSpeedMultiplier,
            float minRechargeSpeedMultiplier,
            float maxRechargeSpeedMultiplier,
            int effectiveRechargeHitPointsPerInterval,
            float effectiveRechargeEnergyPerIntervalWd,
            string interceptMode,
            bool interceptsNonHostile,
            int chargingTicksLeft,
            int cooldownTicksLeft,
            int brokenTicksLeft,
            int rechargeBlockedTicksLeft)
        {
            this.SlotID = slotID;
            this.ModuleInstanceID = moduleInstanceID;
            this.ModuleLabel = moduleLabel;
            this.ModuleDefName = moduleDefName;
            this.BackendKind = string.IsNullOrEmpty(backendKind) ? "None" : backendKind;
            this.StatusKey = string.IsNullOrEmpty(statusKey) ? "Missing" : statusKey;
            this.HasModule = hasModule;
            this.IsEnabled = isEnabled;
            this.HasShield = hasShield;
            this.IsSurfaceShield = isSurfaceShield;
            this.IsVanillaInterceptorShield = isVanillaInterceptorShield;
            this.Online = online;
            this.Broken = broken;
            this.RechargingBlocked = rechargingBlocked;
            this.RechargeStalledForNoEnergy = rechargeStalledForNoEnergy;
            this.CanSetRadius = canSetRadius;
            this.Status = status;
            this.CurrentHitPoints = currentHitPoints;
            this.MaxHitPoints = maxHitPoints;
            this.HitPointsPercent = hitPointsPercent;
            this.SelectedRadius = selectedRadius;
            this.MinRadius = minRadius;
            this.MaxRadius = maxRadius;
            this.DefaultRadius = defaultRadius;
            this.RechargeHitPoints = rechargeHitPoints;
            this.RechargeIntervalTicks = rechargeIntervalTicks;
            this.RechargeEnergyCostWd = rechargeEnergyCostWd;
            this.RechargeEnergyPerHitPointWd = rechargeEnergyPerHitPointWd;
            this.SupportsRechargeSpeedControl = supportsRechargeSpeedControl;
            this.RechargeSpeedMultiplier = rechargeSpeedMultiplier;
            this.MinRechargeSpeedMultiplier = minRechargeSpeedMultiplier;
            this.MaxRechargeSpeedMultiplier = maxRechargeSpeedMultiplier;
            this.EffectiveRechargeHitPointsPerInterval = effectiveRechargeHitPointsPerInterval;
            this.EffectiveRechargeEnergyPerIntervalWd = effectiveRechargeEnergyPerIntervalWd;
            this.InterceptMode = interceptMode;
            this.InterceptsNonHostile = interceptsNonHostile;
            this.ChargingTicksLeft = chargingTicksLeft;
            this.CooldownTicksLeft = cooldownTicksLeft;
            this.BrokenTicksLeft = brokenTicksLeft;
            this.RechargeBlockedTicksLeft = rechargeBlockedTicksLeft;
        }

        public string SlotID { get; private set; }
        public string ModuleInstanceID { get; private set; }
        public string ModuleLabel { get; private set; }
        public string ModuleDefName { get; private set; }
        public string BackendKind { get; private set; }
        public string StatusKey { get; private set; }
        public bool HasModule { get; private set; }
        public bool IsEnabled { get; private set; }
        public bool HasShield { get; private set; }
        public bool IsSurfaceShield { get; private set; }
        public bool IsVanillaInterceptorShield { get; private set; }
        public bool Online { get; private set; }
        public bool Broken { get; private set; }
        public bool RechargingBlocked { get; private set; }
        public bool RechargeStalledForNoEnergy { get; private set; }
        public bool CanSetRadius { get; private set; }
        public string Status { get; private set; }
        // Read-only backend telemetry from the active shield comp. UI must not own or persist HP.
        public int CurrentHitPoints { get; private set; }
        public int MaxHitPoints { get; private set; }
        public float HitPointsPercent { get; private set; }
        // Runtime player setting owned by ShuttleShieldRuntimeState and changed through commands.
        public float SelectedRadius { get; private set; }
        public float MinRadius { get; private set; }
        public float MaxRadius { get; private set; }
        public float DefaultRadius { get; private set; }
        public int RechargeHitPoints { get; private set; }
        public int RechargeIntervalTicks { get; private set; }
        public float RechargeEnergyCostWd { get; private set; }
        public float RechargeEnergyPerHitPointWd { get; private set; }
        public bool SupportsRechargeSpeedControl { get; private set; }
        public float RechargeSpeedMultiplier { get; private set; }
        public float MinRechargeSpeedMultiplier { get; private set; }
        public float MaxRechargeSpeedMultiplier { get; private set; }
        public int EffectiveRechargeHitPointsPerInterval { get; private set; }
        public float EffectiveRechargeEnergyPerIntervalWd { get; private set; }
        public string InterceptMode { get; private set; }
        public bool InterceptsNonHostile { get; private set; }
        public int ChargingTicksLeft { get; private set; }
        public int CooldownTicksLeft { get; private set; }
        public int BrokenTicksLeft { get; private set; }
        public int RechargeBlockedTicksLeft { get; private set; }
    }

    public sealed class ShuttleWeaponSlotReadModel
    {
        public ShuttleWeaponSlotReadModel(
            string slotID,
            string slotLabel,
            string slotShortLabel,
            int slotIndex,
            string installedModuleLabel,
            string installedModuleDefName)
        {
            this.SlotID = slotID;
            this.SlotLabel = slotLabel;
            this.SlotShortLabel = slotShortLabel;
            this.SlotIndex = slotIndex;
            this.InstalledModuleLabel = installedModuleLabel;
            this.InstalledModuleDefName = installedModuleDefName;
        }

        public string SlotID { get; private set; }
        public string SlotLabel { get; private set; }
        public string SlotShortLabel { get; private set; }
        public int SlotIndex { get; private set; }
        public string InstalledModuleLabel { get; private set; }
        public string InstalledModuleDefName { get; private set; }
    }

    public sealed class ShuttleWeaponControlReadModel
    {
        public ShuttleWeaponControlReadModel(
            string moduleInstanceID,
            string moduleLabel,
            string moduleDefName,
            string parentSlotID,
            string parentSlotLabel,
            string parentSlotShortLabel,
            int parentSlotIndex,
            bool isEnabled,
            string weaponLabel,
            string weaponRoleLabel,
            bool canSetForcedTarget,
            bool canClearForcedTarget,
            bool canToggleHoldFire,
            bool holdFire,
            bool hasForcedTarget,
            string forcedTargetLabel,
            bool canTargetLocations,
            bool fireControlLinked,
            string fireControlMode,
            string fireControlModeLabel,
            string targetPriority,
            string targetPriorityLabel,
            bool autoFireEnabled,
            bool hasFireControlRadar,
            bool fireControlAvailable,
            bool effectiveFireControlLinked,
            bool autoFireAvailable,
            bool pointDefenseAvailable,
            string fireControlUnavailableReason,
            string fireControlStatusLabel,
            string fireControlStatusTooltip,
            bool canToggleFireControlLink,
            bool canSetFireControlMode,
            bool canSetTargetPriority,
            bool canSetAutoFire)
            : this(
                moduleInstanceID,
                moduleLabel,
                moduleDefName,
                parentSlotID,
                parentSlotLabel,
                parentSlotShortLabel,
                parentSlotIndex,
                isEnabled,
                weaponLabel,
                weaponRoleLabel,
                canSetForcedTarget,
                canClearForcedTarget,
                canToggleHoldFire,
                holdFire,
                hasForcedTarget,
                forcedTargetLabel,
                canTargetLocations,
                fireControlLinked,
                fireControlMode,
                fireControlModeLabel,
                targetPriority,
                targetPriorityLabel,
                autoFireEnabled,
                hasFireControlRadar,
                fireControlAvailable,
                effectiveFireControlLinked,
                autoFireAvailable,
                pointDefenseAvailable,
                fireControlUnavailableReason,
                fireControlStatusLabel,
                fireControlStatusTooltip,
                canToggleFireControlLink,
                canSetFireControlMode,
                canSetTargetPriority,
                canSetAutoFire,
                false,
                null,
                null,
                0,
                0,
                0,
                false,
                0f,
                false,
                false,
                false,
                0,
                null,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                null,
                null)
        {
        }

        public ShuttleWeaponControlReadModel(
            string moduleInstanceID,
            string moduleLabel,
            string moduleDefName,
            string parentSlotID,
            string parentSlotLabel,
            string parentSlotShortLabel,
            int parentSlotIndex,
            bool isEnabled,
            string weaponLabel,
            string weaponRoleLabel,
            bool canSetForcedTarget,
            bool canClearForcedTarget,
            bool canToggleHoldFire,
            bool holdFire,
            bool hasForcedTarget,
            string forcedTargetLabel,
            bool canTargetLocations,
            bool fireControlLinked,
            string fireControlMode,
            string fireControlModeLabel,
            string targetPriority,
            string targetPriorityLabel,
            bool autoFireEnabled,
            bool hasFireControlRadar,
            bool fireControlAvailable,
            bool effectiveFireControlLinked,
            bool autoFireAvailable,
            bool pointDefenseAvailable,
            string fireControlUnavailableReason,
            string fireControlStatusLabel,
            string fireControlStatusTooltip,
            bool canToggleFireControlLink,
            bool canSetFireControlMode,
            bool canSetTargetPriority,
            bool canSetAutoFire,
            bool hasAmmoSystem,
            string selectedAmmoLabel,
            string selectedAmmoDefName,
            int loadedAmmoCount,
            int magazineCapacity,
            int ammoPerShot,
            bool reloadInProgress,
            float reloadProgress01,
            bool autoReloadEnabled,
            bool logisticsAutoFeedEnabled,
            bool logisticsCoreAvailable,
            int ammoStockCount,
            string lastAmmoFailureReason,
            bool ceAmmoModeActive,
            bool canReload,
            bool canCancelReload,
            bool canSelectAmmo,
            bool canToggleAutoReload,
            bool canToggleLogisticsAutoFeed,
            bool reloadRequested,
            bool manualReloadAllowed,
            bool canToggleManualReloadAllowed,
            bool manualReloadJobActive,
            string lastReloadBlockerReason,
            IReadOnlyList<ShuttleWeaponAmmoOptionReadModel> ammoOptions)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.ModuleLabel = moduleLabel;
            this.ModuleDefName = moduleDefName;
            this.ParentSlotID = parentSlotID;
            this.ParentSlotLabel = parentSlotLabel;
            this.ParentSlotShortLabel = parentSlotShortLabel;
            this.ParentSlotIndex = parentSlotIndex;
            this.IsEnabled = isEnabled;
            this.WeaponLabel = weaponLabel;
            this.WeaponRoleLabel = weaponRoleLabel;
            this.CanSetForcedTarget = canSetForcedTarget;
            this.CanClearForcedTarget = canClearForcedTarget;
            this.CanToggleHoldFire = canToggleHoldFire;
            this.HoldFire = holdFire;
            this.HasForcedTarget = hasForcedTarget;
            this.ForcedTargetLabel = forcedTargetLabel;
            this.CanTargetLocations = canTargetLocations;
            this.FireControlLinked = fireControlLinked;
            this.FireControlMode = fireControlMode;
            this.FireControlModeLabel = fireControlModeLabel;
            this.TargetPriority = targetPriority;
            this.TargetPriorityLabel = targetPriorityLabel;
            this.AutoFireEnabled = autoFireEnabled;
            this.HasFireControlRadar = hasFireControlRadar;
            this.FireControlAvailable = fireControlAvailable;
            this.EffectiveFireControlLinked = effectiveFireControlLinked;
            this.AutoFireAvailable = autoFireAvailable;
            this.PointDefenseAvailable = pointDefenseAvailable;
            this.FireControlUnavailableReason = fireControlUnavailableReason;
            this.FireControlStatusLabel = fireControlStatusLabel;
            this.FireControlStatusTooltip = fireControlStatusTooltip;
            this.CanToggleFireControlLink = canToggleFireControlLink;
            this.CanSetFireControlMode = canSetFireControlMode;
            this.CanSetTargetPriority = canSetTargetPriority;
            this.CanSetAutoFire = canSetAutoFire;
            this.HasAmmoSystem = hasAmmoSystem;
            this.SelectedAmmoLabel = selectedAmmoLabel;
            this.SelectedAmmoDefName = selectedAmmoDefName;
            this.LoadedAmmoCount = loadedAmmoCount;
            this.MagazineCapacity = magazineCapacity;
            this.AmmoPerShot = ammoPerShot;
            this.ReloadInProgress = reloadInProgress;
            this.ReloadProgress01 = reloadProgress01;
            this.AutoReloadEnabled = autoReloadEnabled;
            this.LogisticsAutoFeedEnabled = logisticsAutoFeedEnabled;
            this.LogisticsCoreAvailable = logisticsCoreAvailable;
            this.AmmoStockCount = ammoStockCount;
            this.LastAmmoFailureReason = lastAmmoFailureReason;
            this.CeAmmoModeActive = ceAmmoModeActive;
            this.CanReload = canReload;
            this.CanCancelReload = canCancelReload;
            this.CanSelectAmmo = canSelectAmmo;
            this.CanToggleAutoReload = canToggleAutoReload;
            this.CanToggleLogisticsAutoFeed = canToggleLogisticsAutoFeed;
            this.ReloadRequested = reloadRequested;
            this.ManualReloadAllowed = manualReloadAllowed;
            this.CanToggleManualReloadAllowed = canToggleManualReloadAllowed;
            this.ManualReloadJobActive = manualReloadJobActive;
            this.LastReloadBlockerReason = lastReloadBlockerReason;
            this.ammoOptions = ammoOptions ?? new ShuttleWeaponAmmoOptionReadModel[0];
        }

        private readonly IReadOnlyList<ShuttleWeaponAmmoOptionReadModel> ammoOptions;

        public string ModuleInstanceID { get; private set; }
        public string ModuleLabel { get; private set; }
        public string ModuleDefName { get; private set; }
        public string ParentSlotID { get; private set; }
        public string ParentSlotLabel { get; private set; }
        public string ParentSlotShortLabel { get; private set; }
        public int ParentSlotIndex { get; private set; }
        public bool IsEnabled { get; private set; }
        public string WeaponLabel { get; private set; }
        public string WeaponRoleLabel { get; private set; }
        public bool CanSetForcedTarget { get; private set; }
        public bool CanClearForcedTarget { get; private set; }
        public bool CanToggleHoldFire { get; private set; }
        public bool HoldFire { get; private set; }
        public bool HasForcedTarget { get; private set; }
        public string ForcedTargetLabel { get; private set; }
        public string LastForcedTargetFailureReason { get; internal set; }
        public bool CanTargetLocations { get; private set; }
        public bool FireControlLinked { get; private set; }
        public string FireControlMode { get; private set; }
        public string FireControlModeLabel { get; private set; }
        public string TargetPriority { get; private set; }
        public string TargetPriorityLabel { get; private set; }
        public bool AutoFireEnabled { get; private set; }
        public bool HasFireControlRadar { get; private set; }
        public bool FireControlAvailable { get; private set; }
        public bool EffectiveFireControlLinked { get; private set; }
        public bool AutoFireAvailable { get; private set; }
        public bool PointDefenseAvailable { get; private set; }
        public string FireControlUnavailableReason { get; private set; }
        public string FireControlStatusLabel { get; private set; }
        public string FireControlStatusTooltip { get; private set; }
        public bool CanToggleFireControlLink { get; private set; }
        public bool CanSetFireControlMode { get; private set; }
        public bool CanSetTargetPriority { get; private set; }
        public bool CanSetAutoFire { get; private set; }
        public bool HasAmmoSystem { get; private set; }
        public string SelectedAmmoLabel { get; private set; }
        public string SelectedAmmoDefName { get; private set; }
        public int LoadedAmmoCount { get; private set; }
        public int MagazineCapacity { get; private set; }
        public int AmmoPerShot { get; private set; }
        public bool ReloadInProgress { get; private set; }
        public float ReloadProgress01 { get; private set; }
        public bool AutoReloadEnabled { get; private set; }
        public bool LogisticsAutoFeedEnabled { get; private set; }
        public bool LogisticsCoreAvailable { get; private set; }
        public int AmmoStockCount { get; private set; }
        public string LastAmmoFailureReason { get; private set; }
        public bool CeAmmoModeActive { get; private set; }
        public bool CanReload { get; private set; }
        public bool CanCancelReload { get; private set; }
        public bool CanSelectAmmo { get; private set; }
        public bool CanToggleAutoReload { get; private set; }
        public bool CanToggleLogisticsAutoFeed { get; private set; }
        public bool ReloadRequested { get; private set; }
        public bool ManualReloadAllowed { get; private set; }
        public bool CanToggleManualReloadAllowed { get; private set; }
        public bool ManualReloadJobActive { get; private set; }
        public string LastReloadBlockerReason { get; private set; }

        public IReadOnlyList<ShuttleWeaponAmmoOptionReadModel> AmmoOptions
        {
            get { return this.ammoOptions; }
        }
    }

    public sealed class ShuttleWeaponAmmoOptionReadModel
    {
        public ShuttleWeaponAmmoOptionReadModel(
            string ammoDefName,
            string label,
            int stockCount,
            bool selected)
        {
            this.AmmoDefName = ammoDefName;
            this.Label = label;
            this.StockCount = stockCount;
            this.Selected = selected;
        }

        public string AmmoDefName { get; private set; }
        public string Label { get; private set; }
        public int StockCount { get; private set; }
        public bool Selected { get; private set; }
    }
}
