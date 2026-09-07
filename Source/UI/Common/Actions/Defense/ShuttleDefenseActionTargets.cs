using System.Collections.Generic;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense
{
    internal sealed class ShuttleDefenseWeaponActionTarget
    {
        internal string Id;
        internal string ModuleInstanceID;
        internal string Label;
        internal string ModuleLabel;
        internal string ModuleDefName;
        internal string SlotID;
        internal string SlotLabel;
        internal string SlotShortLabel;
        internal int SlotIndex = -1;
        internal bool IsEnabled = true;
        internal string WeaponTypeLabel;
        internal string DamageLabel;
        internal string CooldownLabel;
        internal string FireRateLabel;
        internal string IconKey;
        internal bool IsOnline;
        internal bool IsDormant;
        internal bool HoldFire;
        internal bool HasFireControl;
        internal bool HasForcedTarget;
        internal bool FireControlLinked;
        internal string FireControlMode;
        internal string FireControlModeLabel;
        internal string TargetPriority;
        internal string TargetPriorityLabel;
        internal bool AutoFireEnabled;
        internal bool HasFireControlRadar;
        internal bool FireControlAvailable;
        internal bool EffectiveFireControlLinked;
        internal bool AutoFireAvailable;
        internal bool PointDefenseAvailable;
        internal string FireControlUnavailableReason;
        internal string FireControlStatusLabel;
        internal string FireControlStatusTooltip;
        internal bool CanToggleHoldFire;
        internal bool CanSetForcedTarget;
        internal bool CanClearForcedTarget;
        internal bool CanTargetLocations;
        internal bool CanToggleFireControlLink;
        internal bool CanSetFireControlMode;
        internal bool CanSetTargetPriority;
        internal bool CanSetAutoFire;
        internal string ForcedTargetLabel;
        internal string LastForcedTargetFailureReason;
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
        internal readonly List<ShuttleDefenseAmmoOptionActionTarget> AmmoOptions =
            new List<ShuttleDefenseAmmoOptionActionTarget>();
        internal string StatusLabel;
        internal string StatusKey;
        internal Vector2 UIAnchor;
        internal string Tooltip;
    }

    internal sealed class ShuttleDefenseAmmoOptionActionTarget
    {
        internal string AmmoDefName;
        internal string Label;
        internal int StockCount;
        internal bool Selected;
    }

    internal sealed class ShuttleDefenseShieldActionTarget
    {
        internal bool HasShield;
        internal bool CanSetRange;
        internal string ModuleInstanceID;
        internal string BackendKind;
        internal bool IsSurfaceShield;
        internal bool IsVanillaInterceptorShield;
        internal bool IsEnabled;
        internal bool Online;
        internal bool Broken;
        internal bool RechargingBlocked;
        internal bool RechargeStalledForNoEnergy;
        internal string Label;
        internal string BackendLabel;
        internal string StatusLabel;
        internal string StatusKey;
        internal float StrengthPct = -1f;
        internal int CurrentHitPoints;
        internal int MaxHitPoints;
        internal int BrokenTicksLeft;
        internal int RechargeBlockedTicksLeft;
        internal int RechargeHitPointsPerInterval;
        internal int RechargeIntervalTicks;
        internal float RechargeEnergyPerHitPointWd;
        internal bool SupportsRechargeSpeedControl;
        internal float RechargeSpeedMultiplier = 1f;
        internal float MinRechargeSpeedMultiplier = 0.25f;
        internal float MaxRechargeSpeedMultiplier = 2f;
        internal int EffectiveRechargeHitPointsPerInterval;
        internal float EffectiveRechargeEnergyPerIntervalWd;
        internal string RechargeSpeedLabel;
        internal string RechargeSpeedTooltip;
        internal float RangeValue = -1f;
        internal float MinRange;
        internal float MaxRange;
        internal float DefaultRange;
        internal string RangeLabel;
        internal string Tooltip;
    }
}
