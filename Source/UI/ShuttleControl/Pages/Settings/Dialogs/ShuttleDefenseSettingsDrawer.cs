using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    /// <summary>
    /// Draws the current-shuttle shield working values without issuing commands.
    /// </summary>
    internal sealed class ShuttleDefenseSettingsDrawer
    {
        internal void Draw(Rect rect, ShuttleDefenseSettingsModel model)
        {
            if (model == null)
            {
                return;
            }

            model.SanitizeWorkingValues();
            if (!model.HasShield)
            {
                ShuttleV3DialogLayout.DrawCardBackground(rect, false, true);
                this.DrawCenteredHint(
                    rect,
                    ShuttleUIText.Tr("CT_Shuttle_Defense_NoShieldModule"));
                return;
            }

            const float summaryHeight = 88f;
            const float gap = 10f;
            Rect summaryRect = new Rect(rect.x, rect.y, rect.width, summaryHeight);
            this.DrawSummary(summaryRect, model);

            Rect controlsRect = new Rect(
                rect.x,
                summaryRect.yMax + gap,
                rect.width,
                Mathf.Max(0f, rect.yMax - summaryRect.yMax - gap));
            float cardWidth = Mathf.Max(0f, (controlsRect.width - gap) * 0.5f);
            Rect rangeRect = new Rect(
                controlsRect.x,
                controlsRect.y,
                cardWidth,
                controlsRect.height);
            Rect rechargeRect = new Rect(
                rangeRect.xMax + gap,
                controlsRect.y,
                Mathf.Max(0f, controlsRect.xMax - rangeRect.xMax - gap),
                controlsRect.height);
            this.DrawRangeCard(rangeRect, model);
            this.DrawRechargeCard(rechargeRect, model);
        }

        private void DrawSummary(Rect rect, ShuttleDefenseSettingsModel model)
        {
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false);
            string tooltip = model.Shield.Tooltip;
            ShuttleV3DialogLayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 22f),
                model.Shield.Label,
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                tooltip,
                TextAnchor.MiddleLeft);
            string strength = ShuttleUIText.Tr(
                "CT_Shuttle_ShuttleDefenseSettings_StrengthFormat",
                model.Shield.StrengthPct >= 0f
                    ? ShuttleUIMetricFormatter.FormatPercent(model.Shield.StrengthPct)
                    : ShuttleUIText.Tr("CT_Shuttle_Defense_Unavailable"),
                model.Shield.CurrentHitPoints,
                model.Shield.MaxHitPoints);
            ShuttleV3DialogLayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 12f, rect.y + 34f, rect.width - 24f, 18f),
                ShuttleUIText.Tr(
                    "CT_Shuttle_ShuttleDefenseSettings_SummaryFormat",
                    model.Shield.StatusLabel,
                    model.Shield.BackendLabel,
                    strength),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleV3DialogStyle.MutedTextColor,
                tooltip,
                TextAnchor.MiddleLeft);

            Rect meterRect = new Rect(rect.x + 12f, rect.yMax - 20f, rect.width - 24f, 7f);
            Widgets.DrawBoxSolid(meterRect, ShuttleV3DialogStyle.DisabledColor);
            Widgets.DrawBoxSolid(
                new Rect(
                    meterRect.x,
                    meterRect.y,
                    meterRect.width * Mathf.Clamp01(model.Shield.StrengthPct),
                    meterRect.height),
                ShuttleV3DialogStyle.GreenStatusColor);
            ShuttleUITooltip.Tip(rect, tooltip);
        }

        private void DrawRangeCard(Rect rect, ShuttleDefenseSettingsModel model)
        {
            ShuttleV3DialogLayout.DrawCardBackground(
                rect,
                model.HasPendingRange,
                !model.SupportsRange);
            ShuttleV3DialogLayout.DrawSectionHeader(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Defense_ShieldRange"));
            Rect inner = new Rect(
                rect.x + 12f,
                rect.y + 42f,
                rect.width - 24f,
                Mathf.Max(0f, rect.height - 54f));
            if (!model.SupportsRange)
            {
                this.DrawCenteredHint(
                    inner,
                    ShuttleUIText.Tr(
                        "CT_Shuttle_ShuttleDefenseSettings_RangeUnsupported"));
                ShuttleUITooltip.Tip(
                    rect,
                    ShuttleUIText.Tr("CT_Shuttle_Defense_ShieldRangeUnsupportedTooltip"));
                return;
            }

            string value = ShuttleUIText.Tr(
                "CT_Shuttle_ShuttleDefenseSettings_RangeValueFormat",
                model.WorkingRange.ToString("0.#"));
            this.DrawWorkingValue(
                new Rect(inner.x, inner.y, inner.width, 24f),
                value,
                model.HasPendingRange,
                ShuttleUIText.Tr("CT_Shuttle_Defense_ShieldRangeAdjustTooltip"));
            Rect sliderRect = new Rect(inner.x, inner.y + 36f, inner.width, 24f);
            model.WorkingRange = Widgets.HorizontalSlider(
                sliderRect,
                model.WorkingRange,
                model.Shield.MinRange,
                model.Shield.MaxRange,
                false,
                null,
                null,
                null,
                1f);
            this.DrawSliderEndpoints(
                new Rect(inner.x, sliderRect.yMax, inner.width, 18f),
                ShuttleUIText.Tr(
                    "CT_Shuttle_ShuttleDefenseSettings_RangeValueFormat",
                    model.Shield.MinRange.ToString("0.#")),
                ShuttleUIText.Tr(
                    "CT_Shuttle_ShuttleDefenseSettings_RangeValueFormat",
                    model.Shield.MaxRange.ToString("0.#")));
            this.DrawDescription(
                new Rect(inner.x, sliderRect.yMax + 28f, inner.width, inner.yMax - sliderRect.yMax - 28f),
                ShuttleUIText.Tr("CT_Shuttle_Defense_ShieldRangeAdjustTooltip"));
            ShuttleUITooltip.Tip(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Defense_ShieldRangeAdjustTooltip"));
        }

        private void DrawRechargeCard(Rect rect, ShuttleDefenseSettingsModel model)
        {
            ShuttleV3DialogLayout.DrawCardBackground(
                rect,
                model.HasPendingRechargeSpeed,
                !model.SupportsRechargeSpeed);
            ShuttleV3DialogLayout.DrawSectionHeader(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_SurfaceShield_RechargeSpeed"));
            Rect inner = new Rect(
                rect.x + 12f,
                rect.y + 42f,
                rect.width - 24f,
                Mathf.Max(0f, rect.height - 54f));
            if (!model.SupportsRechargeSpeed)
            {
                this.DrawCenteredHint(
                    inner,
                    ShuttleUIText.Tr(
                        "CT_Shuttle_ShuttleDefenseSettings_RechargeUnsupported"));
                ShuttleUITooltip.Tip(
                    rect,
                    ShuttleUIText.Tr("CT_Shuttle_SurfaceShield_RechargeSpeedTooltip"));
                return;
            }

            string value = ShuttleUIText.Tr(
                "CT_Shuttle_SurfaceShield_RechargeSpeedFormat",
                model.WorkingRechargeSpeed.ToString("0.##"));
            this.DrawWorkingValue(
                new Rect(inner.x, inner.y, inner.width, 24f),
                value,
                model.HasPendingRechargeSpeed,
                ShuttleUIText.Tr("CT_Shuttle_SurfaceShield_RechargeSpeedTooltip"));
            Rect sliderRect = new Rect(inner.x, inner.y + 36f, inner.width, 24f);
            model.WorkingRechargeSpeed = Widgets.HorizontalSlider(
                sliderRect,
                model.WorkingRechargeSpeed,
                model.Shield.MinRechargeSpeedMultiplier,
                model.Shield.MaxRechargeSpeedMultiplier,
                false,
                null,
                null,
                null,
                0.05f);
            this.DrawSliderEndpoints(
                new Rect(inner.x, sliderRect.yMax, inner.width, 18f),
                ShuttleUIText.Tr(
                    "CT_Shuttle_SurfaceShield_RechargeSpeedFormat",
                    model.Shield.MinRechargeSpeedMultiplier.ToString("0.##")),
                ShuttleUIText.Tr(
                    "CT_Shuttle_SurfaceShield_RechargeSpeedFormat",
                    model.Shield.MaxRechargeSpeedMultiplier.ToString("0.##")));

            int effectiveHitPoints = this.CalculateRechargeHitPoints(
                model.Shield.RechargeHitPointsPerInterval,
                model.WorkingRechargeSpeed);
            float energy = effectiveHitPoints *
                Mathf.Max(0f, model.Shield.RechargeEnergyPerHitPointWd);
            this.DrawDescription(
                new Rect(inner.x, sliderRect.yMax + 28f, inner.width, inner.yMax - sliderRect.yMax - 28f),
                ShuttleUIText.Tr(
                    "CT_Shuttle_SurfaceShield_RechargeEffectiveFormat",
                    effectiveHitPoints,
                    model.Shield.RechargeIntervalTicks,
                    energy.ToString("0.##")));
            ShuttleUITooltip.Tip(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_SurfaceShield_RechargeSpeedTooltip"));
        }

        private void DrawWorkingValue(
            Rect rect,
            string value,
            bool pending,
            string tooltip)
        {
            ShuttleV3DialogLayout.DrawFittedSingleLineLabel(
                rect,
                pending
                    ? ShuttleUIText.Tr(
                        "CT_Shuttle_ShuttleDefenseSettings_PendingValueFormat",
                        value)
                    : value,
                GameFont.Small,
                GameFont.Tiny,
                pending
                    ? ShuttleV3DialogStyle.YellowStatusColor
                    : ShuttleV3DialogStyle.ButtonTextColor,
                tooltip,
                TextAnchor.MiddleLeft);
        }

        private void DrawSliderEndpoints(Rect rect, string left, string right)
        {
            float half = Mathf.Max(0f, (rect.width - 8f) * 0.5f);
            ShuttleV3DialogLayout.DrawFittedSingleLineLabel(
                new Rect(rect.x, rect.y, half, rect.height),
                left,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleV3DialogStyle.MutedTextColor,
                left,
                TextAnchor.MiddleLeft);
            ShuttleV3DialogLayout.DrawFittedSingleLineLabel(
                new Rect(rect.xMax - half, rect.y, half, rect.height),
                right,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleV3DialogStyle.MutedTextColor,
                right,
                TextAnchor.MiddleRight);
        }

        private void DrawDescription(Rect rect, string description)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(rect, description);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawCenteredHint(Rect rect, string text)
        {
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            TextAnchor oldAnchor = Text.Anchor;
            try
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = ShuttleV3DialogStyle.MutedTextColor;
                ShuttleV3DialogLayout.SafeLabel(rect, text);
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                GUI.color = oldColor;
            }
        }

        private int CalculateRechargeHitPoints(int baseHitPoints, float multiplier)
        {
            if (baseHitPoints <= 0)
            {
                return 0;
            }

            double value = Math.Ceiling(baseHitPoints * (double)multiplier);
            return value > int.MaxValue ? int.MaxValue : Math.Max(0, (int)value);
        }
    }
}
