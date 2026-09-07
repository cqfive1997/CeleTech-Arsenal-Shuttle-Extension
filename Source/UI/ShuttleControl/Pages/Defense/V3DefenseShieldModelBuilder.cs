using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefenseShieldModelBuilder
    {
        private readonly V3DefenseTooltipBuilder tooltipBuilder;

        internal V3DefenseShieldModelBuilder(V3DefenseTooltipBuilder tooltipBuilder)
        {
            this.tooltipBuilder = tooltipBuilder ?? new V3DefenseTooltipBuilder();
        }

        internal V3DefenseShieldPanelModel BuildShield(
            ShuttleWeaponBayReadModel weaponBayModel,
            ShuttleControlReadModel controlModel)
        {
            if (weaponBayModel == null ||
                weaponBayModel.Shields == null ||
                weaponBayModel.Shields.Count == 0)
            {
                return this.BuildMissingShield(true);
            }

            ShuttleShieldStatusReadModel selected = this.SelectShield(weaponBayModel);
            if (selected == null)
            {
                return this.BuildMissingShield(false);
            }

            V3DefenseShieldPanelModel shield = new V3DefenseShieldPanelModel();
            this.ApplyShieldIdentity(shield, selected);
            this.ApplyShieldStrength(shield, selected);
            this.ApplyRecharge(shield, selected);
            this.ApplyRangeAndStatus(shield, selected, controlModel);
            return shield;
        }

        private V3DefenseShieldPanelModel BuildMissingShield(bool includeTooltip)
        {
            V3DefenseShieldPanelModel shield = new V3DefenseShieldPanelModel();
            shield.HasShield = false;
            shield.Label = ShuttleUIText.Tr("CT_Shuttle_Defense_NoShieldModule");
            shield.StatusKey = ShuttleUIText.StatusMissing;
            shield.StatusLabel = ShuttleUIText.Tr("CT_Shuttle_Common_Missing");
            shield.RangeLabel = ShuttleUIText.Tr("CT_Shuttle_Defense_RangeUnavailable");
            if (includeTooltip)
            {
                shield.Tooltip =
                    ShuttleUIText.Tr("CT_Shuttle_Defense_NoShieldModuleTooltip");
            }

            return shield;
        }

        private ShuttleShieldStatusReadModel SelectShield(
            ShuttleWeaponBayReadModel weaponBayModel)
        {
            ShuttleShieldStatusReadModel selected = null;
            for (int i = 0; i < weaponBayModel.Shields.Count; i++)
            {
                ShuttleShieldStatusReadModel entry = weaponBayModel.Shields[i];
                if (entry == null || !entry.HasModule)
                {
                    continue;
                }

                if (this.IsBetterShieldSelection(entry, selected))
                {
                    selected = entry;
                }
            }

            return selected;
        }

        private void ApplyShieldIdentity(
            V3DefenseShieldPanelModel shield,
            ShuttleShieldStatusReadModel selected)
        {
            shield.HasShield = true;
            shield.CanSetRange =
                selected.IsEnabled && selected.CanSetRadius && !selected.IsSurfaceShield;
            shield.ModuleInstanceID = selected.ModuleInstanceID;
            shield.BackendKind = selected.BackendKind;
            shield.IsSurfaceShield = selected.IsSurfaceShield;
            shield.IsVanillaInterceptorShield = selected.IsVanillaInterceptorShield;
            shield.IsEnabled = selected.IsEnabled;
            shield.Online = selected.Online;
            shield.Broken = selected.Broken;
            shield.RechargingBlocked = selected.RechargingBlocked;
            shield.RechargeStalledForNoEnergy =
                selected.RechargeStalledForNoEnergy;
            shield.Label = ShuttleAssemblyDisplayTextResolver.ResolveModuleDisplayName(
                selected.ModuleDefName,
                selected.ModuleLabel,
                ShuttleUIText.Tr("CT_Shuttle_Defense_ShieldModule"));
            shield.BackendLabel = this.GetShieldBackendLabel(selected);
        }

        private void ApplyShieldStrength(
            V3DefenseShieldPanelModel shield,
            ShuttleShieldStatusReadModel selected)
        {
            shield.CurrentHitPoints = Math.Max(0, selected.CurrentHitPoints);
            shield.MaxHitPoints = Math.Max(0, selected.MaxHitPoints);
            shield.StrengthPct = selected.HitPointsPercent >= 0f
                ? Mathf.Clamp01(selected.HitPointsPercent)
                : shield.MaxHitPoints > 0
                    ? Mathf.Clamp01((float)shield.CurrentHitPoints / shield.MaxHitPoints)
                    : -1f;
            shield.BrokenTicksLeft = Math.Max(0, selected.BrokenTicksLeft);
            shield.RechargeBlockedTicksLeft =
                Math.Max(0, selected.RechargeBlockedTicksLeft);
        }

        private void ApplyRecharge(
            V3DefenseShieldPanelModel shield,
            ShuttleShieldStatusReadModel selected)
        {
            shield.RechargeHitPointsPerInterval =
                Math.Max(0, selected.RechargeHitPoints);
            shield.RechargeIntervalTicks =
                Math.Max(0, selected.RechargeIntervalTicks);
            shield.RechargeEnergyPerHitPointWd =
                Mathf.Max(0f, selected.RechargeEnergyPerHitPointWd);
            shield.SupportsRechargeSpeedControl =
                selected.IsSurfaceShield && selected.SupportsRechargeSpeedControl;
            shield.RechargeSpeedMultiplier =
                ShuttleSurfaceShieldRuntimeState.SanitizeRechargeSpeedMultiplier(
                    selected.RechargeSpeedMultiplier);
            shield.MinRechargeSpeedMultiplier =
                Mathf.Max(0.01f, selected.MinRechargeSpeedMultiplier);
            shield.MaxRechargeSpeedMultiplier =
                Mathf.Max(shield.MinRechargeSpeedMultiplier, selected.MaxRechargeSpeedMultiplier);
            shield.EffectiveRechargeHitPointsPerInterval =
                Math.Max(0, selected.EffectiveRechargeHitPointsPerInterval);
            shield.EffectiveRechargeEnergyPerIntervalWd =
                Mathf.Max(0f, selected.EffectiveRechargeEnergyPerIntervalWd);
            shield.RechargeSpeedLabel = ShuttleUIText.Tr(
                "CT_Shuttle_SurfaceShield_RechargeSpeedFormat",
                shield.RechargeSpeedMultiplier.ToString("0.##"));
            shield.RechargeSpeedTooltip =
                ShuttleUIText.Tr("CT_Shuttle_SurfaceShield_RechargeSpeedTooltip") +
                "\n" +
                ShuttleUIText.Tr(
                    "CT_Shuttle_SurfaceShield_RechargeEffectiveFormat",
                    shield.EffectiveRechargeHitPointsPerInterval,
                    shield.RechargeIntervalTicks,
                    shield.EffectiveRechargeEnergyPerIntervalWd.ToString("0.##"));
        }

        private void ApplyRangeAndStatus(
            V3DefenseShieldPanelModel shield,
            ShuttleShieldStatusReadModel selected,
            ShuttleControlReadModel controlModel)
        {
            shield.RangeValue = selected.CanSetRadius ? selected.SelectedRadius : -1f;
            shield.MinRange = selected.MinRadius;
            shield.MaxRange = selected.MaxRadius;
            shield.DefaultRange = selected.DefaultRadius;
            shield.RangeLabel = selected.CanSetRadius
                ? this.FormatRange(selected.SelectedRadius)
                : ShuttleUIText.Tr("CT_Shuttle_Defense_RangeUnavailable");
            shield.StatusKey = this.GetShieldStatusKey(selected, controlModel);
            shield.StatusLabel = selected.IsSurfaceShield
                ? ShuttleUISurfaceShieldText.GetSurfaceShieldStatusLabel(shield.StatusKey)
                : this.GetShieldStatusLabel(shield.StatusKey);
            shield.Tooltip = selected.IsSurfaceShield ||
                selected.IsVanillaInterceptorShield
                ? this.tooltipBuilder.BuildShieldTooltip(shield)
                : this.BuildBasicShieldTooltip(shield);
        }

        private bool IsBetterShieldSelection(
            ShuttleShieldStatusReadModel candidate,
            ShuttleShieldStatusReadModel current)
        {
            if (candidate == null)
            {
                return false;
            }

            if (current == null)
            {
                return true;
            }

            if (candidate.IsSurfaceShield != current.IsSurfaceShield)
            {
                return candidate.IsSurfaceShield;
            }

            if (candidate.IsEnabled != current.IsEnabled)
            {
                return candidate.IsEnabled;
            }

            if (candidate.Online != current.Online)
            {
                return candidate.Online;
            }

            if (candidate.MaxHitPoints != current.MaxHitPoints)
            {
                return candidate.MaxHitPoints > current.MaxHitPoints;
            }

            return candidate.CanSetRadius && !current.CanSetRadius;
        }

        private string GetShieldStatusKey(
            ShuttleShieldStatusReadModel shield,
            ShuttleControlReadModel controlModel)
        {
            if (shield == null || !shield.HasModule)
            {
                return ShuttleUIText.StatusMissing;
            }

            if (!shield.IsEnabled)
            {
                return ShuttleUIText.StatusDisabled;
            }

            if (shield.IsSurfaceShield)
            {
                return string.IsNullOrEmpty(shield.StatusKey)
                    ? ShuttleUIText.StatusMissing
                    : shield.StatusKey;
            }

            if (controlModel != null && !controlModel.InternalBusPowered)
            {
                return ShuttleUIText.StatusUnpowered;
            }

            if (shield.MaxHitPoints > 0 && shield.CurrentHitPoints <= 0)
            {
                return ShuttleUIText.StatusDown;
            }

            string status = shield.Status ?? string.Empty;
            if (status.ToLowerInvariant().Contains("cooldown") ||
                status.ToLowerInvariant().Contains("charging"))
            {
                return ShuttleUIText.StatusOverload;
            }

            return ShuttleUIText.StatusNormal;
        }

        private string GetShieldStatusLabel(string statusKey)
        {
            if (ShuttleUISurfaceShieldText.IsSurfaceShieldStatusKey(statusKey))
            {
                return ShuttleUISurfaceShieldText.GetSurfaceShieldStatusLabel(statusKey);
            }

            if (statusKey == ShuttleUIText.StatusMissing)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Common_Missing");
            }

            if (statusKey == ShuttleUIText.StatusDisabled)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Common_Disabled");
            }

            if (statusKey == ShuttleUIText.StatusUnpowered)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Common_Unpowered");
            }

            if (statusKey == ShuttleUIText.StatusDown)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Defense_ShieldDown");
            }

            return statusKey == ShuttleUIText.StatusOverload
                ? ShuttleUIText.Tr("CT_Shuttle_Common_Overload")
                : ShuttleUIText.Tr("CT_Shuttle_Common_Normal");
        }

        private string GetShieldBackendLabel(ShuttleShieldStatusReadModel shield)
        {
            if (shield != null && shield.IsSurfaceShield)
            {
                return ShuttleUIText.Tr("CT_Shuttle_SurfaceShield_BackendLabel");
            }

            if (shield != null && shield.IsVanillaInterceptorShield)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Defense_Backend_BasicInterceptor");
            }

            return ShuttleUIText.Tr("CT_Shuttle_Common_None");
        }

        private string BuildBasicShieldTooltip(V3DefenseShieldPanelModel shield)
        {
            return shield.Label + "\n" +
                ShuttleUIText.Tr("CT_Shuttle_Defense_ShieldStrength") + ": " +
                (shield.StrengthPct >= 0f
                    ? Mathf.RoundToInt(shield.StrengthPct * 100f).ToString() + "%"
                    : "-") +
                "\n" + ShuttleUIText.Tr("CT_Shuttle_Main_Range") + ": " +
                shield.RangeLabel;
        }

        private string FormatRange(float range)
        {
            return range >= 0f
                ? range.ToString("0.#") + " " +
                    ShuttleUIText.Tr("CT_Shuttle_Defense_Tiles")
                : ShuttleUIText.Tr("CT_Shuttle_Defense_Unavailable");
        }
    }
}
