using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal static class V3DefenseActionTargetFactory
    {
        internal static ShuttleDefenseWeaponActionTarget CreateWeapon(
            V3DefenseWeaponEntryModel source)
        {
            if (source == null)
            {
                return null;
            }

            ShuttleDefenseWeaponActionTarget target =
                new ShuttleDefenseWeaponActionTarget();
            target.Id = source.Id;
            target.ModuleInstanceID = source.ModuleInstanceID;
            target.Label = source.Label;
            target.ModuleLabel = source.ModuleLabel;
            target.ModuleDefName = source.ModuleDefName;
            target.SlotID = source.SlotID;
            target.SlotLabel = source.SlotLabel;
            target.SlotShortLabel = source.SlotShortLabel;
            target.SlotIndex = source.SlotIndex;
            target.IsEnabled = source.IsEnabled;
            target.WeaponTypeLabel = source.WeaponTypeLabel;
            target.DamageLabel = source.DamageLabel;
            target.CooldownLabel = source.CooldownLabel;
            target.FireRateLabel = source.FireRateLabel;
            target.IconKey = source.IconKey;
            target.IsOnline = source.IsOnline;
            target.IsDormant = source.IsDormant;
            target.HoldFire = source.HoldFire;
            target.HasFireControl = source.HasFireControl;
            target.HasForcedTarget = source.HasForcedTarget;
            target.FireControlLinked = source.FireControlLinked;
            target.FireControlMode = source.FireControlMode;
            target.FireControlModeLabel = source.FireControlModeLabel;
            target.TargetPriority = source.TargetPriority;
            target.TargetPriorityLabel = source.TargetPriorityLabel;
            target.AutoFireEnabled = source.AutoFireEnabled;
            target.HasFireControlRadar = source.HasFireControlRadar;
            target.FireControlAvailable = source.FireControlAvailable;
            target.EffectiveFireControlLinked = source.EffectiveFireControlLinked;
            target.AutoFireAvailable = source.AutoFireAvailable;
            target.PointDefenseAvailable = source.PointDefenseAvailable;
            target.FireControlUnavailableReason = source.FireControlUnavailableReason;
            target.FireControlStatusLabel = source.FireControlStatusLabel;
            target.FireControlStatusTooltip = source.FireControlStatusTooltip;
            target.CanToggleHoldFire = source.CanToggleHoldFire;
            target.CanSetForcedTarget = source.CanSetForcedTarget;
            target.CanClearForcedTarget = source.CanClearForcedTarget;
            target.CanTargetLocations = source.CanTargetLocations;
            target.CanToggleFireControlLink = source.CanToggleFireControlLink;
            target.CanSetFireControlMode = source.CanSetFireControlMode;
            target.CanSetTargetPriority = source.CanSetTargetPriority;
            target.CanSetAutoFire = source.CanSetAutoFire;
            target.ForcedTargetLabel = source.ForcedTargetLabel;
            target.LastForcedTargetFailureReason = source.LastForcedTargetFailureReason;
            target.HasAmmoSystem = source.HasAmmoSystem;
            target.SelectedAmmoLabel = source.SelectedAmmoLabel;
            target.SelectedAmmoDefName = source.SelectedAmmoDefName;
            target.LoadedAmmoCount = source.LoadedAmmoCount;
            target.MagazineCapacity = source.MagazineCapacity;
            target.AmmoPerShot = source.AmmoPerShot;
            target.ReloadInProgress = source.ReloadInProgress;
            target.ReloadRequested = source.ReloadRequested;
            target.ReloadProgress01 = source.ReloadProgress01;
            target.AutoReloadEnabled = source.AutoReloadEnabled;
            target.LogisticsAutoFeedEnabled = source.LogisticsAutoFeedEnabled;
            target.ManualReloadAllowed = source.ManualReloadAllowed;
            target.ManualReloadJobActive = source.ManualReloadJobActive;
            target.LogisticsCoreAvailable = source.LogisticsCoreAvailable;
            target.AmmoStockCount = source.AmmoStockCount;
            target.LastAmmoFailureReason = source.LastAmmoFailureReason;
            target.LastReloadBlockerReason = source.LastReloadBlockerReason;
            target.CeAmmoModeActive = source.CeAmmoModeActive;
            target.CanReload = source.CanReload;
            target.CanCancelReload = source.CanCancelReload;
            target.CanSelectAmmo = source.CanSelectAmmo;
            target.CanToggleAutoReload = source.CanToggleAutoReload;
            target.CanToggleLogisticsAutoFeed = source.CanToggleLogisticsAutoFeed;
            target.CanToggleManualReloadAllowed = source.CanToggleManualReloadAllowed;
            target.StatusLabel = source.StatusLabel;
            target.StatusKey = source.StatusKey;
            target.UIAnchor = source.UIAnchor;
            target.Tooltip = source.Tooltip;

            for (int i = 0; source.AmmoOptions != null && i < source.AmmoOptions.Count; i++)
            {
                V3DefenseAmmoOptionModel sourceOption = source.AmmoOptions[i];
                if (sourceOption == null)
                {
                    continue;
                }

                ShuttleDefenseAmmoOptionActionTarget option =
                    new ShuttleDefenseAmmoOptionActionTarget();
                option.AmmoDefName = sourceOption.AmmoDefName;
                option.Label = sourceOption.Label;
                option.StockCount = sourceOption.StockCount;
                option.Selected = sourceOption.Selected;
                target.AmmoOptions.Add(option);
            }

            return target;
        }

        internal static ShuttleDefenseShieldActionTarget CreateShield(
            V3DefenseShieldPanelModel source)
        {
            if (source == null)
            {
                return null;
            }

            ShuttleDefenseShieldActionTarget target =
                new ShuttleDefenseShieldActionTarget();
            target.HasShield = source.HasShield;
            target.CanSetRange = source.CanSetRange;
            target.ModuleInstanceID = source.ModuleInstanceID;
            target.BackendKind = source.BackendKind;
            target.IsSurfaceShield = source.IsSurfaceShield;
            target.IsVanillaInterceptorShield = source.IsVanillaInterceptorShield;
            target.IsEnabled = source.IsEnabled;
            target.Online = source.Online;
            target.Broken = source.Broken;
            target.RechargingBlocked = source.RechargingBlocked;
            target.RechargeStalledForNoEnergy = source.RechargeStalledForNoEnergy;
            target.Label = source.Label;
            target.BackendLabel = source.BackendLabel;
            target.StatusLabel = source.StatusLabel;
            target.StatusKey = source.StatusKey;
            target.StrengthPct = source.StrengthPct;
            target.CurrentHitPoints = source.CurrentHitPoints;
            target.MaxHitPoints = source.MaxHitPoints;
            target.BrokenTicksLeft = source.BrokenTicksLeft;
            target.RechargeBlockedTicksLeft = source.RechargeBlockedTicksLeft;
            target.RechargeHitPointsPerInterval = source.RechargeHitPointsPerInterval;
            target.RechargeIntervalTicks = source.RechargeIntervalTicks;
            target.RechargeEnergyPerHitPointWd = source.RechargeEnergyPerHitPointWd;
            target.SupportsRechargeSpeedControl = source.SupportsRechargeSpeedControl;
            target.RechargeSpeedMultiplier = source.RechargeSpeedMultiplier;
            target.MinRechargeSpeedMultiplier = source.MinRechargeSpeedMultiplier;
            target.MaxRechargeSpeedMultiplier = source.MaxRechargeSpeedMultiplier;
            target.EffectiveRechargeHitPointsPerInterval =
                source.EffectiveRechargeHitPointsPerInterval;
            target.EffectiveRechargeEnergyPerIntervalWd =
                source.EffectiveRechargeEnergyPerIntervalWd;
            target.RechargeSpeedLabel = source.RechargeSpeedLabel;
            target.RechargeSpeedTooltip = source.RechargeSpeedTooltip;
            target.RangeValue = source.RangeValue;
            target.MinRange = source.MinRange;
            target.MaxRange = source.MaxRange;
            target.DefaultRange = source.DefaultRange;
            target.RangeLabel = source.RangeLabel;
            target.Tooltip = source.Tooltip;
            return target;
        }
    }
}
