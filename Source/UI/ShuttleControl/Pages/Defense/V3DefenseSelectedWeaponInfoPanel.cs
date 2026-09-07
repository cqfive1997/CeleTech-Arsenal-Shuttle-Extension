using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Draws selected-weapon details without duplicating card commands.
    /// </summary>
    internal sealed class V3DefenseSelectedWeaponInfoPanel
    {
        private readonly V3DefenseText text;
        private readonly V3DefensePanelDrawer panel;
        private readonly V3DefenseWeaponCardText cardText;

        internal V3DefenseSelectedWeaponInfoPanel(
            V3DefenseText text,
            V3DefensePanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
            this.cardText = new V3DefenseWeaponCardText(text);
        }

        internal void Draw(
            Rect rect,
            V3DefensePageModel model,
            ShuttlePageDrawContext context)
        {
            this.panel.DrawPanelTitle(
                rect,
                this.text.Tr("CT_Shuttle_Defense_SelectedWeaponInformation"));
            Rect inner = this.panel.GetPanelInnerRect(rect, 44f);
            V3DefenseWeaponEntryModel weapon =
                model != null && model.DefenseModel != null
                    ? model.DefenseModel.SelectedWeapon
                    : null;
            if (weapon == null)
            {
                this.panel.DrawEmptyPanelMessage(
                    inner,
                    this.text.Tr("CT_Shuttle_Defense_NoSelectedWeapon"));
                return;
            }

            this.panel.DrawCardBackground(
                inner,
                false,
                false,
                V3DefenseText.StrongCardColor);
            Rect iconRect = new Rect(inner.x + 10f, inner.y + 10f, 44f, 44f);
            this.panel.DrawIcon(
                iconRect,
                context,
                weapon.IconKey,
                this.text.Tr("CT_Shuttle_Defense_WeaponIconFallback"),
                1.1f);
            Rect statusRect = new Rect(inner.xMax - 82f, inner.y + 10f, 70f, 20f);
            this.panel.DrawStatusBadge(
                statusRect,
                weapon.StatusLabel,
                this.text.GetWeaponStatusColor(weapon.StatusKey));
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(
                    iconRect.xMax + 10f,
                    inner.y + 8f,
                    Mathf.Max(0f, statusRect.x - iconRect.xMax - 18f),
                    22f),
                weapon.Label,
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                weapon.Tooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(
                    iconRect.xMax + 10f,
                    inner.y + 31f,
                    Mathf.Max(0f, inner.xMax - iconRect.xMax - 22f),
                    18f),
                ShuttleUIText.Tr(
                    "CT_Shuttle_Defense_SelectedWeaponIdentityFormat",
                    this.cardText.GetSlotLine(weapon),
                    this.text.ValueOrDash(weapon.WeaponTypeLabel)),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                weapon.Tooltip,
                TextAnchor.MiddleLeft);

            float y = inner.y + 64f;
            y = this.DrawLine(inner, y, this.cardText.GetPerformanceLine(weapon), weapon.Tooltip);
            y = this.DrawLine(inner, y, this.cardText.GetFireControlLine(weapon), weapon.FireControlStatusTooltip);
            if (weapon.HasAmmoSystem)
            {
                Rect ammoRect = new Rect(inner.x + 11f, y, inner.width - 22f, 18f);
                this.RegisterTutorialTarget(
                    context,
                    ShuttleTutorialTargetIds.DefenseAmmoPanel,
                    ammoRect);
                y = this.DrawLine(inner, y, this.cardText.GetAmmoLine(weapon), weapon.Tooltip);
                y = this.DrawLine(inner, y, this.cardText.GetReloadLine(weapon), this.cardText.GetReloadCommandTooltip(weapon));
                string policy = this.cardText.GetReloadPolicyLine(weapon);
                if (!string.IsNullOrEmpty(policy))
                {
                    y = this.DrawLine(inner, y, policy, weapon.Tooltip);
                }
            }

            y = this.DrawField(
                inner,
                y,
                "CT_Shuttle_Defense_ForcedTarget",
                this.text.ValueOrDash(weapon.ForcedTargetLabel),
                weapon.Tooltip);
            string diagnostic = this.GetDiagnostic(weapon);
            if (!string.IsNullOrEmpty(diagnostic))
            {
                this.DrawField(
                    inner,
                    y,
                    "CT_Shuttle_Defense_Diagnostic",
                    diagnostic,
                    diagnostic);
            }

            this.text.AddTooltip(inner, weapon.Tooltip);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private float DrawField(
            Rect inner,
            float y,
            string labelKey,
            string value,
            string tooltip)
        {
            return this.DrawLine(
                inner,
                y,
                ShuttleUIText.Tr(
                    "CT_Shuttle_Defense_InfoFieldFormat",
                    this.text.Tr(labelKey),
                    value),
                tooltip);
        }

        private float DrawLine(
            Rect inner,
            float y,
            string value,
            string tooltip)
        {
            if (y + 18f > inner.yMax - 8f)
            {
                return y;
            }

            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(inner.x + 11f, y, inner.width - 22f, 18f),
                value,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                tooltip,
                TextAnchor.MiddleLeft);
            return y + 20f;
        }

        private string GetDiagnostic(V3DefenseWeaponEntryModel weapon)
        {
            if (!string.IsNullOrEmpty(weapon.LastForcedTargetFailureReason))
            {
                return weapon.LastForcedTargetFailureReason;
            }

            if (!string.IsNullOrEmpty(weapon.LastReloadBlockerReason))
            {
                return weapon.LastReloadBlockerReason;
            }

            return weapon.LastAmmoFailureReason;
        }

        private void RegisterTutorialTarget(
            ShuttlePageDrawContext context,
            string id,
            Rect rect)
        {
            if (context != null &&
                context.DefensePageContext != null &&
                context.DefensePageContext.TutorialTargets != null)
            {
                context.DefensePageContext.TutorialTargets.Register(id, rect);
            }
        }
    }
}
