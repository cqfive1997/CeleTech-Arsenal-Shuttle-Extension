using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefenseMetricModelBuilder
    {
        private const float ShieldCriticalStrengthPct = 0.20f;
        private const float ShieldModerateStrengthPct = 0.60f;

        internal void BuildMetrics(V3DefensePageReadModel model)
        {
            if (model == null)
            {
                return;
            }

            this.AddMetric(
                model,
                ShuttleUIText.Tr("CT_Shuttle_Defense_Metric_OnlineWeapons_Label"),
                ShuttleUIText.Tr("CT_Shuttle_Defense_Metric_OnlineWeapons_Short"),
                model.OnlineWeaponCount.ToString(),
                model.OnlineWeaponCount > 0
                    ? ShuttleUIText.SeverityStable
                    : ShuttleUIText.SeverityModerate,
                ShuttleUIText.Tr("CT_Shuttle_Defense_Metric_OnlineWeapons_Tooltip"));
            this.AddMetric(
                model,
                ShuttleUIText.Tr("CT_Shuttle_Defense_Metric_LinkedFireControl_Label"),
                ShuttleUIText.Tr("CT_Shuttle_Defense_Metric_LinkedFireControl_Short"),
                model.OnlineFireControlCount.ToString(),
                model.OnlineFireControlCount > 0
                    ? ShuttleUIText.SeverityStable
                    : ShuttleUIText.SeverityModerate,
                ShuttleUIText.Tr("CT_Shuttle_Defense_Metric_LinkedFireControl_Tooltip"));
            this.AddShieldMetric(model);
            this.AddMetric(
                model,
                ShuttleUIText.Tr("CT_Shuttle_Defense_Metric_AutoLoader_Label"),
                ShuttleUIText.Tr("CT_Shuttle_Defense_Metric_AutoLoader_Short"),
                model.AutoLoaderInstalled
                    ? ShuttleUIText.Tr("CT_Shuttle_Common_Installed")
                    : ShuttleUIText.Tr("CT_Shuttle_Common_Missing"),
                model.AutoLoaderInstalled
                    ? ShuttleUIText.SeverityStable
                    : ShuttleUIText.SeverityCritical,
                ShuttleUIText.Tr("CT_Shuttle_Defense_Metric_AutoLoader_Tooltip"));
            this.AddWeaponDrawMetric(model);
            this.AddWeaponBayStatusMetric(model);
        }

        private void AddShieldMetric(V3DefensePageReadModel model)
        {
            this.AddMetric(
                model,
                ShuttleUIText.Tr("CT_Shuttle_Defense_ShieldStrength"),
                ShuttleUIText.Tr("CT_Shuttle_Defense_Metric_Shield_Short"),
                model.Shield != null && model.Shield.StrengthPct >= 0f
                    ? Mathf.RoundToInt(model.Shield.StrengthPct * 100f).ToString() + "%"
                    : "0%",
                this.GetShieldMetricSeverity(model.Shield),
                model.Shield != null ? model.Shield.Tooltip : string.Empty);
        }

        private void AddWeaponDrawMetric(V3DefensePageReadModel model)
        {
            this.AddMetric(
                model,
                ShuttleUIText.Tr("CT_Shuttle_Defense_Metric_WeaponDraw_Label"),
                ShuttleUIText.Tr("CT_Shuttle_Defense_Metric_WeaponDraw_Short"),
                ShuttleUIMetricFormatter.FormatWatts(model.WeaponPowerWatts),
                model.WeaponPowerWatts > 0f
                    ? ShuttleUIText.SeverityModerate
                    : ShuttleUIText.SeverityStable,
                ShuttleUIText.Tr("CT_Shuttle_Defense_Metric_WeaponDraw_Tooltip"));
        }

        private void AddWeaponBayStatusMetric(V3DefensePageReadModel model)
        {
            this.AddMetric(
                model,
                ShuttleUIText.Tr("CT_Shuttle_Defense_Metric_WeaponBayStatus_Label"),
                ShuttleUIText.Tr("CT_Shuttle_Defense_Status"),
                model.WeaponBayStatusText,
                this.GetWeaponBayStatusSeverity(model.WeaponBayStatusKey),
                ShuttleUIText.Tr("CT_Shuttle_Defense_Metric_WeaponBayStatus_Tooltip"));
        }

        private string GetWeaponBayStatusSeverity(string statusKey)
        {
            if (statusKey == ShuttleUIText.SeverityCritical)
            {
                return ShuttleUIText.SeverityCritical;
            }

            return statusKey == ShuttleUIText.SeverityWarning
                ? ShuttleUIText.SeverityModerate
                : ShuttleUIText.SeverityStable;
        }

        private string GetShieldMetricSeverity(V3DefenseShieldPanelModel shield)
        {
            if (shield == null || !shield.HasShield)
            {
                return ShuttleUIText.SeverityMissing;
            }

            if (this.IsShieldFailureStatus(shield.StatusKey))
            {
                return ShuttleUIText.SeverityCritical;
            }

            if (shield.StrengthPct < 0f)
            {
                return ShuttleUIText.SeverityMissing;
            }

            if (shield.StrengthPct < ShieldCriticalStrengthPct)
            {
                return ShuttleUIText.SeverityCritical;
            }

            if (shield.StrengthPct <= ShieldModerateStrengthPct ||
                shield.StatusKey == ShuttleUIText.StatusOverload ||
                shield.StatusKey == ShuttleUIText.StatusRechargeBlocked ||
                shield.StatusKey == ShuttleUIText.StatusNoEnergy ||
                shield.StatusKey == ShuttleUIText.StatusDisabled)
            {
                return ShuttleUIText.SeverityModerate;
            }

            return ShuttleUIText.SeverityStable;
        }

        private bool IsShieldFailureStatus(string statusKey)
        {
            return statusKey == ShuttleUIText.StatusDown ||
                statusKey == ShuttleUIText.StatusUnpowered ||
                statusKey == ShuttleUIText.StatusBroken ||
                statusKey == ShuttleUIText.StatusOffline ||
                statusKey == ShuttleUIText.StatusMissing;
        }

        private void AddMetric(
            V3DefensePageReadModel model,
            string label,
            string shortLabel,
            string value,
            string severityKey,
            string tooltip)
        {
            V3DefenseMetricModel metric = new V3DefenseMetricModel();
            metric.Label = label;
            metric.ShortLabel = string.IsNullOrEmpty(shortLabel) ? label : shortLabel;
            metric.Value = value;
            metric.SeverityKey = severityKey;
            metric.Tooltip = string.IsNullOrEmpty(tooltip)
                ? label + ": " + value
                : tooltip;
            model.Metrics.Add(metric);
        }
    }
}
