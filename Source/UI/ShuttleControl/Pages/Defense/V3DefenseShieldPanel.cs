using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefenseShieldPanel
    {
        private readonly V3DefenseText text;
        private readonly V3DefensePanelDrawer panel;

        internal V3DefenseShieldPanel(
            V3DefenseText text,
            V3DefensePanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
        }

        internal void Draw(
            Rect rect,
            V3DefensePageModel model,
            V3DefensePageState state,
            ShuttlePageDrawContext context)
        {
            this.panel.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_Defense_ShieldControl"));
            Rect inner = this.panel.GetPanelInnerRect(rect, 44f);
            V3DefenseShieldPanelModel shield =
                model != null && model.DefenseModel != null
                    ? model.DefenseModel.Shield
                    : null;
            if (shield == null || !shield.HasShield)
            {
                this.panel.DrawEmptyPanelMessage(inner, this.text.Tr("CT_Shuttle_Defense_NoShieldModule"));
                return;
            }

            this.SyncShieldRangePreview(state, shield);
            this.SyncSurfaceShieldRechargeSpeedPreview(state, shield);

            Rect statusCard = new Rect(inner.x, inner.y, inner.width, 72f);
            this.DrawStatusCard(statusCard, shield);

            Rect controlCard = new Rect(
                inner.x,
                statusCard.yMax + 6f,
                inner.width,
                Mathf.Max(0f, inner.yMax - statusCard.yMax - 6f));
            this.DrawShieldControlFrame(controlCard);
            if (shield.IsSurfaceShield && shield.SupportsRechargeSpeedControl)
            {
                this.DrawSurfaceShieldRechargeSpeedControl(controlCard, shield, state, context);
            }
            else
            {
                this.DrawRangeControl(controlCard, shield, state, context);
            }
        }

        private void DrawStatusCard(Rect rect, V3DefenseShieldPanelModel shield)
        {
            this.DrawShieldSummaryFrame(rect);
            bool drawStatusChip = this.ShouldDrawShieldStatusChip(shield);
            Rect badgeRect = new Rect(rect.xMax - 72f, rect.y + 8f, 62f, 18f);
            if (drawStatusChip)
            {
                this.DrawLightStatusChip(
                    badgeRect,
                    shield.StatusLabel,
                    this.text.GetShieldStatusColor(shield.StatusKey),
                    shield.Tooltip);
            }

            float titleRight = drawStatusChip
                ? badgeRect.x - 8f
                : rect.xMax - 9f;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 9f, rect.y + 6f, Mathf.Max(0f, titleRight - rect.x - 9f), 20f),
                shield.Label,
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                shield.Tooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 9f, rect.y + 29f, (rect.width - 24f) * 0.50f, 15f),
                this.text.Tr("CT_Shuttle_Defense_Backend") + ": " +
                this.text.ValueOrDash(shield.BackendLabel),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                shield.Tooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 11f + ((rect.width - 24f) * 0.50f), rect.y + 29f, (rect.width - 24f) * 0.50f, 15f),
                this.text.Tr("CT_Shuttle_Defense_ShieldStrength") + ": " +
                this.FormatShieldStrength(shield),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.WithAlpha(Color.white, 0.76f),
                shield.Tooltip,
                TextAnchor.MiddleLeft);
            this.panel.DrawMeter(
                new Rect(rect.x + 9f, rect.yMax - 14f, rect.width - 18f, 5f),
                shield.StrengthPct >= 0f ? shield.StrengthPct : 0f,
                this.text.GetShieldStrengthColor(shield));
            Text.Font = GameFont.Small;
            this.text.AddTooltip(rect, shield.Tooltip);
        }

        private bool ShouldDrawShieldStatusChip(V3DefenseShieldPanelModel shield)
        {
            if (shield == null || string.IsNullOrEmpty(shield.StatusKey))
            {
                return false;
            }

            return shield.StatusKey != ShuttleUIText.StatusNormal &&
                shield.StatusKey != ShuttleUIText.StatusOnline &&
                shield.StatusKey != ShuttleUIText.StatusFull;
        }

        private void DrawRangeControl(
            Rect rect,
            V3DefenseShieldPanelModel shield,
            V3DefensePageState state,
            ShuttlePageDrawContext context)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 9f, rect.y + 7f, rect.width - 18f, 16f),
                this.text.Tr("CT_Shuttle_Defense_ShieldRange"),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                this.text.Tr("CT_Shuttle_Defense_ShieldRangeAdjustTooltip"),
                TextAnchor.MiddleLeft);
            GUI.color = Color.white;

            if (!shield.CanSetRange || shield.MaxRange <= shield.MinRange)
            {
                this.panel.DrawEmptyPanelMessage(
                    new Rect(rect.x + 9f, rect.y + 28f, rect.width - 18f, rect.height - 36f),
                    this.text.Tr("CT_Shuttle_Defense_RangeControlUnavailable"));
                this.text.AddTooltip(rect, this.text.Tr("CT_Shuttle_Defense_ShieldRangeUnsupportedTooltip"));
                return;
            }

            float displayedRange = this.GetDisplayedShieldRange(state, shield);
            GUI.color = Color.white;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 9f, rect.y + 26f, rect.width - 18f, 18f),
                this.text.FormatRange(displayedRange) +
                (this.IsShieldRangePreviewActive(state)
                    ? " " + this.text.Tr("CT_Shuttle_Defense_Preview")
                    : string.Empty),
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                this.text.Tr("CT_Shuttle_Defense_ShieldRangeAdjustTooltip"),
                TextAnchor.MiddleLeft);
            Rect sliderRect = new Rect(rect.x + 10f, rect.y + 52f, rect.width - 20f, 22f);
            float nextRange = Widgets.HorizontalSlider(
                sliderRect,
                displayedRange,
                shield.MinRange,
                shield.MaxRange,
                false,
                null,
                null,
                null,
                1f);
            this.DrawSliderEndpoints(
                new Rect(sliderRect.x, sliderRect.yMax - 1f, sliderRect.width, 12f),
                this.text.FormatRange(shield.MinRange),
                this.text.FormatRange(shield.MaxRange));
            if (!Mathf.Approximately(nextRange, displayedRange) && state != null)
            {
                state.ShieldRangePreview = nextRange;
            }

            float buttonWidth = (rect.width - 25f) * 0.5f;
            Rect applyRect = new Rect(rect.x + 9f, rect.yMax - 28f, buttonWidth, 22f);
            Rect resetRect = new Rect(applyRect.xMax + 7f, applyRect.y, buttonWidth, 22f);
            bool canApply = this.CanApplyShieldRange(context, shield, displayedRange);
            if (this.panel.DrawButton(
                applyRect,
                this.text.Tr("CT_Shuttle_Defense_ApplyRange"),
                canApply,
                V3DefenseText.YellowColor,
                this.GetShieldRangeTooltip(context, shield)))
            {
                if (this.ApplyShieldRange(context, shield, displayedRange) && state != null)
                {
                    state.ShieldRangePreview = -1f;
                }
            }

            if (this.panel.DrawButton(
                resetRect,
                this.text.Tr("CT_Shuttle_Defense_ResetPreview"),
                this.IsShieldRangePreviewActive(state),
                ShuttleUIStyle.MutedTextColor,
                this.text.Tr("CT_Shuttle_Defense_ResetPreviewTooltip")) &&
                state != null)
            {
                state.ShieldRangePreview = -1f;
            }

            this.text.AddTooltip(rect, this.text.Tr("CT_Shuttle_Defense_ShieldRangeAdjustTooltip"));
        }

        private void DrawSurfaceShieldRechargeSpeedControl(
            Rect rect,
            V3DefenseShieldPanelModel shield,
            V3DefensePageState state,
            ShuttlePageDrawContext context)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 9f, rect.y + 7f, rect.width - 18f, 16f),
                this.text.Tr("CT_Shuttle_SurfaceShield_RechargeSpeed"),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                shield.RechargeSpeedTooltip,
                TextAnchor.MiddleLeft);
            GUI.color = Color.white;

            float displayedSpeed = this.GetDisplayedSurfaceShieldRechargeSpeed(state, shield);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 9f, rect.y + 26f, rect.width - 18f, 18f),
                ShuttleUIText.Tr("CT_Shuttle_SurfaceShield_RechargeSpeedFormat", displayedSpeed.ToString("0.##")) +
                (this.IsSurfaceShieldRechargeSpeedPreviewActive(state)
                    ? " " + this.text.Tr("CT_Shuttle_SurfaceShield_RechargePreview")
                    : string.Empty),
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                shield.RechargeSpeedTooltip,
                TextAnchor.MiddleLeft);

            Rect sliderRect = new Rect(rect.x + 10f, rect.y + 51f, rect.width - 20f, 22f);
            string minSpeedLabel = ShuttleUIText.Tr(
                "CT_Shuttle_SurfaceShield_RechargeSpeedFormat",
                shield.MinRechargeSpeedMultiplier.ToString("0.##"));
            string maxSpeedLabel = ShuttleUIText.Tr(
                "CT_Shuttle_SurfaceShield_RechargeSpeedFormat",
                shield.MaxRechargeSpeedMultiplier.ToString("0.##"));
            float nextSpeed = Widgets.HorizontalSlider(
                sliderRect,
                displayedSpeed,
                shield.MinRechargeSpeedMultiplier,
                shield.MaxRechargeSpeedMultiplier,
                false,
                null,
                null,
                null,
                0.05f);
            this.DrawSliderEndpoints(
                new Rect(sliderRect.x, sliderRect.yMax - 1f, sliderRect.width, 12f),
                minSpeedLabel,
                maxSpeedLabel);
            if (!Mathf.Approximately(nextSpeed, displayedSpeed) && state != null)
            {
                state.SurfaceShieldRechargeSpeedPreview = nextSpeed;
            }

            GUI.color = ShuttleUIStyle.MutedTextColor;
            int displayedEffectiveHp = this.CalculateDisplayedSurfaceShieldRechargeHp(
                shield,
                displayedSpeed);
            float displayedEffectiveEnergyWd =
                displayedEffectiveHp * Mathf.Max(0f, shield.RechargeEnergyPerHitPointWd);
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 9f, rect.y + 88f, rect.width - 18f, 18f),
                ShuttleUIText.Tr(
                    "CT_Shuttle_SurfaceShield_RechargeEffectiveFormat",
                    displayedEffectiveHp,
                    shield.RechargeIntervalTicks,
                    displayedEffectiveEnergyWd.ToString("0.##")));
            GUI.color = Color.white;

            float buttonWidth = (rect.width - 25f) * 0.5f;
            Rect applyRect = new Rect(rect.x + 9f, rect.yMax - 28f, buttonWidth, 22f);
            Rect resetRect = new Rect(applyRect.xMax + 7f, applyRect.y, buttonWidth, 22f);
            bool canApply = this.CanApplySurfaceShieldRechargeSpeed(
                context,
                shield,
                displayedSpeed);
            if (this.panel.DrawButton(
                applyRect,
                this.text.Tr("CT_Shuttle_SurfaceShield_RechargeApply"),
                canApply,
                V3DefenseText.BlueColor,
                this.GetSurfaceShieldRechargeSpeedTooltip(context, shield)))
            {
                if (this.ApplySurfaceShieldRechargeSpeed(context, shield, displayedSpeed) &&
                    state != null)
                {
                    state.SurfaceShieldRechargeSpeedPreview = -1f;
                }
            }

            if (this.panel.DrawButton(
                resetRect,
                this.text.Tr("CT_Shuttle_SurfaceShield_RechargeReset"),
                this.IsSurfaceShieldRechargeSpeedPreviewActive(state),
                ShuttleUIStyle.MutedTextColor,
                this.text.Tr("CT_Shuttle_SurfaceShield_RechargeResetTooltip")) &&
                state != null)
            {
                state.SurfaceShieldRechargeSpeedPreview = -1f;
            }

            this.text.AddTooltip(rect, shield.RechargeSpeedTooltip);
        }

        private void DrawShieldSummaryFrame(Rect rect)
        {
            Widgets.DrawBoxSolid(
                rect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.RightTopCardColor, 0.54f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.SubtleBorderColor, 0.30f),
                ShuttleUIStyle.ThinBorder);
        }

        private void DrawShieldControlFrame(Rect rect)
        {
            Widgets.DrawBoxSolid(
                rect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.RightBottomCardColor, 0.34f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.SubtleBorderColor, 0.24f),
                ShuttleUIStyle.ThinBorder);
        }

        private void DrawLightStatusChip(Rect rect, string label, Color color, string tooltip)
        {
            Widgets.DrawBoxSolid(rect, ShuttleUIStyle.WithAlpha(color, 0.05f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(color, 0.32f),
                ShuttleUIStyle.ThinBorder);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(
                    rect.x + 4f,
                    rect.y + 1f,
                    Mathf.Max(0f, rect.width - 8f),
                    Mathf.Max(0f, rect.height - 2f)),
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.WithAlpha(color, 0.76f),
                tooltip,
                TextAnchor.MiddleCenter);
        }

        private void DrawSliderEndpoints(Rect rect, string leftLabel, string rightLabel)
        {
            float halfWidth = Mathf.Max(0f, (rect.width - 8f) * 0.5f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x, rect.y, halfWidth, rect.height),
                leftLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedTextColor, 0.54f),
                leftLabel,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.xMax - halfWidth, rect.y, halfWidth, rect.height),
                rightLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedTextColor, 0.54f),
                rightLabel,
                TextAnchor.MiddleRight);
        }

        private bool CanApplyShieldRange(
            ShuttlePageDrawContext context,
            V3DefenseShieldPanelModel shield,
            float range)
        {
            IShuttleDefenseShieldUIActions actions = GetShieldActions(context);
            ShuttleDefenseShieldActionTarget target =
                V3DefenseActionTargetFactory.CreateShield(shield);
            return actions != null &&
                target != null &&
                actions.CanApplyShieldRange(target, range);
        }

        private bool ApplyShieldRange(
            ShuttlePageDrawContext context,
            V3DefenseShieldPanelModel shield,
            float range)
        {
            IShuttleDefenseShieldUIActions actions = GetShieldActions(context);
            ShuttleDefenseShieldActionTarget target =
                V3DefenseActionTargetFactory.CreateShield(shield);
            return actions != null &&
                target != null &&
                actions.ApplyShieldRange(target, range);
        }

        private string GetShieldRangeTooltip(
            ShuttlePageDrawContext context,
            V3DefenseShieldPanelModel shield)
        {
            IShuttleDefenseShieldUIActions actions = GetShieldActions(context);
            ShuttleDefenseShieldActionTarget target =
                V3DefenseActionTargetFactory.CreateShield(shield);
            return actions != null && target != null
                ? actions.GetShieldRangeTooltip(target)
                : ShuttleUIText.Tr("CT_Shuttle_UI_ActionUnavailableYet");
        }

        private bool CanApplySurfaceShieldRechargeSpeed(
            ShuttlePageDrawContext context,
            V3DefenseShieldPanelModel shield,
            float multiplier)
        {
            IShuttleDefenseShieldUIActions actions = GetShieldActions(context);
            ShuttleDefenseShieldActionTarget target =
                V3DefenseActionTargetFactory.CreateShield(shield);
            return actions != null &&
                target != null &&
                actions.CanApplySurfaceShieldRechargeSpeed(target, multiplier);
        }

        private bool ApplySurfaceShieldRechargeSpeed(
            ShuttlePageDrawContext context,
            V3DefenseShieldPanelModel shield,
            float multiplier)
        {
            IShuttleDefenseShieldUIActions actions = GetShieldActions(context);
            ShuttleDefenseShieldActionTarget target =
                V3DefenseActionTargetFactory.CreateShield(shield);
            return actions != null &&
                target != null &&
                actions.ApplySurfaceShieldRechargeSpeed(target, multiplier);
        }

        private string GetSurfaceShieldRechargeSpeedTooltip(
            ShuttlePageDrawContext context,
            V3DefenseShieldPanelModel shield)
        {
            IShuttleDefenseShieldUIActions actions = GetShieldActions(context);
            ShuttleDefenseShieldActionTarget target =
                V3DefenseActionTargetFactory.CreateShield(shield);
            return actions != null && target != null
                ? actions.GetSurfaceShieldRechargeSpeedTooltip(target)
                : ShuttleUIText.Tr("CT_Shuttle_UI_ActionUnavailableYet");
        }

        private static IShuttleDefenseShieldUIActions GetShieldActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.DefensePageContext != null
                ? context.DefensePageContext.ShieldActions
                : null;
        }

        private void SyncShieldRangePreview(
            V3DefensePageState state,
            V3DefenseShieldPanelModel shield)
        {
            if (state == null || shield == null)
            {
                return;
            }

            string id = shield.ModuleInstanceID ?? string.Empty;
            if (state.ShieldRangePreviewModuleID != id)
            {
                state.ShieldRangePreview = -1f;
                state.ShieldRangePreviewModuleID = id;
            }
        }

        private bool IsShieldRangePreviewActive(V3DefensePageState state)
        {
            return state != null && state.ShieldRangePreview >= 0f;
        }

        private float GetDisplayedShieldRange(
            V3DefensePageState state,
            V3DefenseShieldPanelModel shield)
        {
            if (shield == null)
            {
                return 0f;
            }

            float fallback = shield.RangeValue >= 0f ? shield.RangeValue : shield.DefaultRange;
            if (state != null && state.ShieldRangePreview >= 0f)
            {
                return Mathf.Clamp(state.ShieldRangePreview, shield.MinRange, shield.MaxRange);
            }

            return Mathf.Clamp(fallback, shield.MinRange, shield.MaxRange);
        }

        private void SyncSurfaceShieldRechargeSpeedPreview(
            V3DefensePageState state,
            V3DefenseShieldPanelModel shield)
        {
            if (state == null || shield == null)
            {
                return;
            }

            string id = shield.ModuleInstanceID ?? string.Empty;
            if (state.SurfaceShieldRechargeSpeedPreviewModuleID != id)
            {
                state.SurfaceShieldRechargeSpeedPreview = -1f;
                state.SurfaceShieldRechargeSpeedPreviewModuleID = id;
            }
        }

        private bool IsSurfaceShieldRechargeSpeedPreviewActive(
            V3DefensePageState state)
        {
            return state != null && state.SurfaceShieldRechargeSpeedPreview >= 0f;
        }

        private float GetDisplayedSurfaceShieldRechargeSpeed(
            V3DefensePageState state,
            V3DefenseShieldPanelModel shield)
        {
            if (shield == null)
            {
                return 1f;
            }

            float min = Mathf.Max(0.01f, shield.MinRechargeSpeedMultiplier);
            float max = Mathf.Max(min, shield.MaxRechargeSpeedMultiplier);
            if (state != null && state.SurfaceShieldRechargeSpeedPreview >= 0f)
            {
                return Mathf.Clamp(state.SurfaceShieldRechargeSpeedPreview, min, max);
            }

            return Mathf.Clamp(shield.RechargeSpeedMultiplier, min, max);
        }

        private int CalculateDisplayedSurfaceShieldRechargeHp(
            V3DefenseShieldPanelModel shield,
            float multiplier)
        {
            if (shield == null || shield.RechargeHitPointsPerInterval <= 0)
            {
                return 0;
            }

            double effective = System.Math.Ceiling(shield.RechargeHitPointsPerInterval * (double)multiplier);
            if (effective <= 0d)
            {
                return 0;
            }

            return effective > int.MaxValue ? int.MaxValue : (int)effective;
        }

        private string FormatShieldStrength(V3DefenseShieldPanelModel shield)
        {
            string pct = shield.StrengthPct >= 0f
                ? Mathf.RoundToInt(shield.StrengthPct * 100f).ToString() + "%"
                : "-";
            return pct + "  " +
                shield.CurrentHitPoints.ToString() + "/" +
                shield.MaxHitPoints.ToString();
        }
    }
}
