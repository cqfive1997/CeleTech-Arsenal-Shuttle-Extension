using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewDetailPanel
    {
        private const float PanelContentTopOffset = 44f;
        private const float DetailRowHeight = 42f;

        private readonly V3CrewText text;
        private readonly V3CrewPanelDrawer panelDrawer;

        internal V3CrewDetailPanel(V3CrewText text)
        {
            this.text = text;
            this.panelDrawer = new V3CrewPanelDrawer(text);
        }

        internal void Draw(
            Rect rect,
            V3CrewCardModel card,
            V3CrewPageState state,
            Action onClose,
            Action onOpenVanillaInfo,
            Action onRemove,
            bool canRemove,
            string removeTooltip)
        {
            this.DrawTitleWithClose(rect, onClose);
            Rect inner = this.panelDrawer.GetPanelInnerRect(rect, PanelContentTopOffset);
            if (card == null)
            {
                this.panelDrawer.DrawEmptyPanelMessage(
                    inner,
                    this.text.Tr("CT_ShuttleCrew_SelectMemberForDetails"));
                return;
            }

            const float actionsHeight = 116f;
            Rect actionsRect = new Rect(inner.x, inner.yMax - actionsHeight, inner.width, actionsHeight);
            Rect scrollRect = new Rect(
                inner.x,
                inner.y,
                inner.width,
                Mathf.Max(0f, inner.height - actionsHeight - 8f));
            float issuesHeight = Mathf.Max(82f, 34f + (this.GetIssueCount(card) * DetailRowHeight));
            float contentHeight = 140f + 102f + issuesHeight + 132f + 122f;
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(0f, scrollRect.width - 16f), Mathf.Max(scrollRect.height, contentHeight));
            Widgets.BeginScrollView(scrollRect, ref state.DetailScroll, viewRect);
            try
            {
                float y = 0f;
                this.DrawSummary(new Rect(0f, y, viewRect.width, 130f), card);
                y += 140f;
                this.DrawLocationSection(new Rect(0f, y, viewRect.width, 92f), card);
                y += 102f;
                this.DrawIssuesSection(new Rect(0f, y, viewRect.width, issuesHeight), card);
                y += issuesHeight + 10f;
                this.DrawNeedsSection(new Rect(0f, y, viewRect.width, 122f), card);
                y += 132f;
                this.DrawImpactSection(new Rect(0f, y, viewRect.width, 112f), card);
            }
            finally
            {
                Widgets.EndScrollView();
            }

            this.DrawActions(actionsRect, card, onOpenVanillaInfo, onRemove, canRemove, removeTooltip);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private void DrawTitleWithClose(Rect rect, Action onClose)
        {
            ShuttleUILayout.DrawPanelBackground(rect);
            Rect titleRect = new Rect(rect.x + 10f, rect.y + 5f, rect.width - 88f, 24f);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                titleRect,
                this.text.FitLabelText(this.text.Tr("CT_ShuttleCrew_DetailsTitle"), titleRect.width));
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 8f, rect.y + 29f, rect.width - 16f, 1f),
                V3CrewVisuals.WithAlpha(ShuttleUIStyle.MutedTextColor, 0.45f));
            Rect closeRect = new Rect(rect.xMax - 70f, rect.y + 5f, 60f, 22f);
            if (ShuttleUIActionButtonDrawer.DrawNormalButton(
                    closeRect,
                    this.text.Tr("CT_ShuttleCrew_DetailsClose"),
                    true,
                    this.text.Tr("CT_ShuttleCrew_DetailsClose")) &&
                onClose != null)
            {
                onClose();
            }
        }

        private void DrawSummary(Rect rect, V3CrewCardModel card)
        {
            this.panelDrawer.DrawCardBackground(rect, false, false, V3CrewText.CardColor);
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 6f, rect.y + 8f, 4f, rect.height - 16f),
                V3CrewVisuals.GetRiskColor(card.RiskLevel));
            Rect portraitRect = new Rect(rect.x + 16f, rect.y + 14f, 58f, 58f);
            this.text.DrawThingIcon(portraitRect, card.DisplayThing, card.FallbackIconText, card.AppearanceTooltip);

            Rect infoRect = new Rect(portraitRect.xMax + 12f, rect.y + 12f, rect.width - portraitRect.width - 42f, 84f);
            this.DrawFittedLine(new Rect(infoRect.x, infoRect.y, infoRect.width, 24f), this.BuildNameLine(card), Color.white);
            this.DrawFittedLine(new Rect(infoRect.x, infoRect.y + 28f, infoRect.width, 20f), this.text.ValueOrDash(card.IdentityLabel), card.CategoryTextColor);
            this.DrawFittedLine(new Rect(infoRect.x, infoRect.y + 50f, infoRect.width, 20f), this.text.ValueOrDash(card.CurrentActivityLabel), card.ActivityTextColor);

            Rect pillRect = new Rect(rect.x + 16f, rect.yMax - 34f, rect.width - 32f, 22f);
            this.DrawPill(
                pillRect,
                this.text.ValueOrDash(card.CompartmentLabel),
                V3CrewVisuals.GetCompartmentColor(card.SourceKind),
                card.CompartmentTooltip);
        }

        private void DrawLocationSection(Rect rect, V3CrewCardModel card)
        {
            this.DrawSectionCard(rect, this.text.Tr("CT_ShuttleCrew_PositionSection"));
            float y = rect.y + 32f;
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_ShuttleCrew_CurrentCompartment"), this.text.ValueOrDash(card.CompartmentLabel));
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_ShuttleCrew_CurrentAction"), this.text.ValueOrDash(card.CurrentActivityLabel));
            if (!string.IsNullOrEmpty(card.Note))
            {
                this.DrawDetailLine(rect, ref y, this.text.Tr("CT_ShuttleCrew_Note"), card.Note);
            }
        }

        private void DrawIssuesSection(Rect rect, V3CrewCardModel card)
        {
            this.DrawSectionCard(rect, this.text.Tr("CT_ShuttleCrew_StatusSection"));
            Rect listRect = new Rect(rect.x + 8f, rect.y + 32f, rect.width - 16f, rect.height - 40f);
            if (card.Issues == null || card.Issues.Count == 0)
            {
                GUI.color = ShuttleUIStyle.MutedTextColor;
                Text.Font = GameFont.Tiny;
                ShuttleUILayout.SafeLabel(listRect, this.text.Tr("CT_ShuttleCrew_NoMajorIssues"));
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                return;
            }

            for (int i = 0; i < card.Issues.Count; i++)
            {
                Rect rowRect = new Rect(listRect.x, listRect.y + (i * DetailRowHeight), listRect.width, DetailRowHeight - 5f);
                this.DrawIssueRow(rowRect, card.Issues[i]);
            }
        }

        private void DrawNeedsSection(Rect rect, V3CrewCardModel card)
        {
            this.DrawSectionCard(rect, this.text.Tr("CT_ShuttleCrew_NeedsSection"));
            Rect listRect = new Rect(rect.x + 10f, rect.y + 34f, rect.width - 20f, rect.height - 44f);
            if (card.Group == V3CrewGroupKind.Mech)
            {
                this.DrawNeedBar(new Rect(listRect.x, listRect.y, listRect.width, 18f), this.text.Tr("CT_Shuttle_Crew_Need_Energy"), card.EnergyPct, V3CrewText.BlueColor);
                return;
            }

            this.DrawNeedBar(new Rect(listRect.x, listRect.y, listRect.width, 18f), this.text.Tr("CT_Shuttle_Crew_Need_Hunger"), card.FoodPct, V3CrewText.YellowColor);
            this.DrawNeedBar(new Rect(listRect.x, listRect.y + 22f, listRect.width, 18f), this.text.Tr("CT_Shuttle_Crew_Need_Rest"), card.RestPct, V3CrewText.BlueColor);
            this.DrawNeedBar(new Rect(listRect.x, listRect.y + 44f, listRect.width, 18f), this.text.Tr("CT_Shuttle_Crew_Need_Mood"), card.MoodPct, V3CrewText.GreenColor);
            this.DrawNeedBar(new Rect(listRect.x, listRect.y + 66f, listRect.width, 18f), this.text.Tr("CT_Shuttle_Crew_Need_Joy"), card.JoyPct, V3CrewText.BlueColor);
        }

        private void DrawImpactSection(Rect rect, V3CrewCardModel card)
        {
            this.DrawSectionCard(rect, this.text.Tr("CT_ShuttleCrew_ImpactSection"));
            float y = rect.y + 32f;
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_ShuttleCrew_PopulationCapacity"), "1");
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_ShuttleCrew_BedSlot"), card.SourceKind == V3CrewCardSourceKind.Habitat ? this.text.Tr("CT_ShuttleCrew_Known") : this.text.Tr("CT_Shuttle_Crew_NoData"));
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_ShuttleCrew_NeedsFoodSupply"), card.FoodPct >= 0f ? this.text.Tr("CT_ShuttleCrew_Yes") : this.text.Tr("CT_ShuttleCrew_No"));
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_ShuttleCrew_NeedsRecreation"), card.JoyPct >= 0f ? this.text.Tr("CT_ShuttleCrew_Yes") : this.text.Tr("CT_ShuttleCrew_No"));
        }

        private void DrawActions(
            Rect rect,
            V3CrewCardModel card,
            Action onOpenVanillaInfo,
            Action onRemove,
            bool canRemove,
            string removeTooltip)
        {
            this.DrawSectionCard(rect, this.text.Tr("CT_ShuttleCrew_ActionsSection"));
            Rect buttonRect = new Rect(rect.x + 8f, rect.y + 34f, rect.width - 16f, 24f);
            this.DrawActionRow(buttonRect, this.text.Tr("CT_ShuttleCrew_ViewVanillaInfo"), card.DisplayThing != null && !card.DisplayThing.Destroyed, false, this.text.Tr("CT_ShuttleCrew_ViewVanillaInfoTip"), onOpenVanillaInfo);
            buttonRect.y += 28f;
            this.DrawActionRow(buttonRect, this.text.Tr("CT_ShuttleCrew_ReassignCompartment"), false, false, this.text.Tr("CT_ShuttleCrew_ReassignCompartmentDisabledTip"), null);
            buttonRect.y += 28f;
            this.DrawActionRow(buttonRect, this.text.Tr("CT_ShuttleCrew_EjectMember"), canRemove, true, removeTooltip, onRemove);
        }

        private void DrawActionRow(
            Rect rect,
            string label,
            bool enabled,
            bool danger,
            string tooltip,
            Action action)
        {
            if (!this.panelDrawer.DrawButton(rect, label, enabled, danger, tooltip))
            {
                return;
            }

            if (enabled && action != null)
            {
                action();
            }
            else
            {
                ShuttleUICommandFeedback.ShowReject(
                    string.IsNullOrEmpty(tooltip)
                        ? this.text.Tr("CT_Shuttle_UI_ActionUnavailableYet")
                        : tooltip,
                    false);
            }
        }

        private void DrawIssueRow(Rect rect, V3CrewIssueModel issue)
        {
            if (issue == null)
            {
                return;
            }

            Color color = V3CrewVisuals.GetIssueColor(issue.Severity);
            this.panelDrawer.DrawCardBackground(rect, false, false, V3CrewText.StrongCardColor);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 3f, rect.height), color);
            this.DrawFittedLine(new Rect(rect.x + 8f, rect.y + 3f, rect.width - 16f, 16f), issue.Label, color);
            this.DrawFittedLine(new Rect(rect.x + 8f, rect.y + 20f, rect.width - 16f, 16f), issue.Summary, ShuttleUIStyle.MutedTextColor);
        }

        private void DrawNeedBar(Rect rect, string label, float value01, Color color)
        {
            float labelWidth = this.text.IsEnglishLanguage() ? 72f : 52f;
            float pctWidth = 42f;
            Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            Rect pctRect = new Rect(rect.xMax - pctWidth, rect.y, pctWidth, rect.height);
            Rect meterRect = new Rect(labelRect.xMax + 6f, rect.y + 5f, pctRect.x - labelRect.xMax - 12f, 8f);
            Color textColor = value01 >= 0f && value01 < 0.30f ? V3CrewText.YellowColor : ShuttleUIStyle.MutedTextColor;
            this.DrawFittedLine(labelRect, label, textColor);
            Widgets.DrawBoxSolid(meterRect, V3CrewVisuals.WithAlpha(ShuttleUIStyle.MutedTextColor, 0.25f));
            Widgets.DrawBoxSolid(new Rect(meterRect.x, meterRect.y, meterRect.width * (value01 >= 0f ? Mathf.Clamp01(value01) : 0f), meterRect.height), value01 >= 0f ? color : ShuttleUIStyle.MutedTextColor);
            ShuttleUILayout.DrawRectBorder(
                meterRect,
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);
            Text.Anchor = TextAnchor.MiddleRight;
            this.DrawFittedLine(pctRect, this.text.FormatPercent(value01), textColor);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawDetailLine(Rect container, ref float y, string label, string value)
        {
            Rect rect = new Rect(container.x + 10f, y, container.width - 20f, 18f);
            float labelWidth = this.text.IsEnglishLanguage() ? 108f : 84f;
            this.DrawFittedLine(new Rect(rect.x, rect.y, labelWidth, rect.height), label, ShuttleUIStyle.MutedTextColor);
            this.DrawFittedLine(new Rect(rect.x + labelWidth + 6f, rect.y, rect.width - labelWidth - 6f, rect.height), this.text.ValueOrDash(value), Color.white);
            y += 20f;
        }

        private void DrawSectionCard(Rect rect, string title)
        {
            this.panelDrawer.DrawCardBackground(rect, false, false, V3CrewText.CardColor);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 20f),
                this.text.FitLabelText(title, rect.width - 16f));
        }

        private void DrawPill(Rect rect, string label, Color color, string tooltip)
        {
            Widgets.DrawBoxSolid(rect, V3CrewVisuals.WithAlpha(color, 0.18f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                V3CrewVisuals.WithAlpha(color, 0.78f),
                ShuttleUIStyle.ThinBorder);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 6f, rect.y + 1f, rect.width - 12f, rect.height - 2f),
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                color,
                tooltip,
                TextAnchor.MiddleCenter);
            this.text.AddTooltip(rect, tooltip);
        }

        private void DrawFittedLine(Rect rect, string label, Color color)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = color;
            ShuttleUILayout.SafeLabel(rect, this.text.FitLabelText(this.text.ValueOrDash(label), rect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private int GetIssueCount(V3CrewCardModel card)
        {
            return card != null && card.Issues != null ? card.Issues.Count : 0;
        }

        private string BuildNameLine(V3CrewCardModel card)
        {
            string label = card != null && !string.IsNullOrEmpty(card.Label) ? card.Label : "-";
            if (card != null && !string.IsNullOrEmpty(card.TitleLabel))
            {
                return label + ", " + card.TitleLabel;
            }

            return label;
        }
    }
}
