using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Draws shield state only. Configuration is hosted by the Settings hub.
    /// </summary>
    internal sealed class V3DefenseShieldInfoPanel
    {
        private readonly V3DefenseText text;
        private readonly V3DefensePanelDrawer panel;

        internal V3DefenseShieldInfoPanel(
            V3DefenseText text,
            V3DefensePanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
        }

        internal void Draw(Rect rect, V3DefensePageModel model)
        {
            this.panel.DrawPanelTitle(
                rect,
                this.text.Tr("CT_Shuttle_Defense_ShieldInformation"));
            Rect inner = this.panel.GetPanelInnerRect(rect, 44f);
            V3DefenseShieldPanelModel shield =
                model != null && model.DefenseModel != null
                    ? model.DefenseModel.Shield
                    : null;
            if (shield == null || !shield.HasShield)
            {
                this.panel.DrawEmptyPanelMessage(
                    inner,
                    this.text.Tr("CT_Shuttle_Defense_NoShieldModule"));
                return;
            }

            this.panel.DrawCardBackground(
                inner,
                false,
                false,
                V3DefenseText.StrongCardColor);
            Rect statusRect = new Rect(inner.xMax - 82f, inner.y + 9f, 70f, 20f);
            this.panel.DrawStatusBadge(
                statusRect,
                shield.StatusLabel,
                this.text.GetShieldStatusColor(shield.StatusKey));
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(
                    inner.x + 11f,
                    inner.y + 7f,
                    Mathf.Max(0f, statusRect.x - inner.x - 20f),
                    23f),
                shield.Label,
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                shield.Tooltip,
                TextAnchor.MiddleLeft);

            float y = inner.y + 36f;
            this.DrawInfoLine(
                new Rect(inner.x + 11f, y, inner.width - 22f, 18f),
                "CT_Shuttle_Defense_Backend",
                this.text.ValueOrDash(shield.BackendLabel),
                shield.Tooltip);
            y += 20f;
            this.DrawInfoLine(
                new Rect(inner.x + 11f, y, inner.width - 22f, 18f),
                "CT_Shuttle_Defense_ShieldStrength",
                this.FormatStrength(shield),
                shield.Tooltip);
            y += 21f;
            this.panel.DrawMeter(
                new Rect(inner.x + 11f, y, inner.width - 22f, 6f),
                shield.StrengthPct >= 0f ? shield.StrengthPct : 0f,
                this.text.GetShieldStrengthColor(shield));
            y += 14f;
            this.DrawInfoLine(
                new Rect(inner.x + 11f, y, inner.width - 22f, 18f),
                "CT_Shuttle_Defense_ShieldRange",
                this.text.ValueOrDash(shield.RangeLabel),
                shield.Tooltip);
            y += 20f;
            this.DrawRechargeLine(
                new Rect(inner.x + 11f, y, inner.width - 22f, 18f),
                shield);
            y += 20f;
            if (shield.IsSurfaceShield && shield.RechargeHitPointsPerInterval > 0)
            {
                ShuttleUILayout.DrawFittedSingleLineLabel(
                    new Rect(inner.x + 11f, y, inner.width - 22f, 18f),
                    ShuttleUIText.Tr(
                        "CT_Shuttle_SurfaceShield_RechargeEffectiveFormat",
                        shield.EffectiveRechargeHitPointsPerInterval,
                        shield.RechargeIntervalTicks,
                        shield.EffectiveRechargeEnergyPerIntervalWd.ToString("0.##")),
                    GameFont.Tiny,
                    GameFont.Tiny,
                    ShuttleUIStyle.MutedTextColor,
                    shield.RechargeSpeedTooltip,
                    TextAnchor.MiddleLeft);
            }

            this.text.AddTooltip(inner, shield.Tooltip);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private void DrawRechargeLine(
            Rect rect,
            V3DefenseShieldPanelModel shield)
        {
            string value = shield.IsSurfaceShield &&
                !string.IsNullOrEmpty(shield.RechargeSpeedLabel)
                    ? shield.RechargeSpeedLabel
                    : this.text.Tr("CT_Shuttle_Defense_Unavailable");
            this.DrawInfoLine(
                rect,
                "CT_Shuttle_SurfaceShield_RechargeSpeed",
                value,
                shield.RechargeSpeedTooltip);
        }

        private void DrawInfoLine(
            Rect rect,
            string labelKey,
            string value,
            string tooltip)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect,
                ShuttleUIText.Tr(
                    "CT_Shuttle_Defense_InfoFieldFormat",
                    this.text.Tr(labelKey),
                    value),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                tooltip,
                TextAnchor.MiddleLeft);
        }

        private string FormatStrength(V3DefenseShieldPanelModel shield)
        {
            return ShuttleUIText.Tr(
                "CT_Shuttle_ShuttleDefenseSettings_StrengthFormat",
                shield.StrengthPct >= 0f
                    ? ShuttleUIMetricFormatter.FormatPercent(shield.StrengthPct)
                    : this.text.Tr("CT_Shuttle_Defense_Unavailable"),
                shield.CurrentHitPoints,
                shield.MaxHitPoints);
        }
    }
}
