using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewCardDrawer
    {
        private const int VisibleIssueChipCount = 2;
        private const float IssueChipHeight = 22f;
        private const float IssueChipVerticalPadding = 1f;
        private const float NeedBarRowHeight = 19f;
        private const float NeedBarMeterHeight = 8f;

        private readonly V3CrewText text;
        private readonly V3CrewPanelDrawer panelDrawer;

        internal V3CrewCardDrawer(
            V3CrewText text,
            V3CrewPanelDrawer panelDrawer)
        {
            this.text = text;
            this.panelDrawer = panelDrawer;
        }

        internal void DrawCrewCard(
            Rect rect,
            V3CrewCardModel card,
            bool compact,
            V3CrewPageState state,
            Action<Rect, V3CrewCardModel> drawActionButton)
        {
            if (card == null)
            {
                return;
            }

            bool selected = V3CrewSelection.IsSelected(card, state);
            this.panelDrawer.DrawCardBackground(rect, selected, false, card.CardColor);
            this.DrawRiskBar(rect, card);

            Rect portraitRect = new Rect(rect.x + 12f, rect.y + 12f, compact ? 42f : 50f, compact ? 42f : 50f);
            this.text.DrawThingIcon(portraitRect, card.DisplayThing, card.FallbackIconText, card.AppearanceTooltip);

            Rect actionRect = new Rect(rect.xMax - 32f, rect.y + 8f, 24f, 24f);
            if (drawActionButton != null)
            {
                drawActionButton(actionRect, card);
            }

            Rect pillRect = new Rect(rect.xMax - 124f, rect.y + 9f, 84f, 20f);
            this.DrawCompartmentPill(pillRect, card);

            float barsWidth = card.Group == V3CrewGroupKind.Human
                ? Mathf.Clamp(rect.width * 0.36f, 118f, 154f)
                : Mathf.Clamp(rect.width * 0.32f, 92f, 132f);
            Rect barsRect = new Rect(rect.xMax - barsWidth - 10f, rect.y + 36f, barsWidth, rect.height - 44f);
            Rect textRect = new Rect(
                portraitRect.xMax + 10f,
                rect.y + 8f,
                Mathf.Max(64f, barsRect.x - portraitRect.xMax - 18f),
                rect.height - 16f);

            this.DrawMemberText(textRect, card, compact);
            this.DrawStatusChips(
                new Rect(
                    textRect.x,
                    rect.y + (compact ? 70f : 84f),
                    Mathf.Max(0f, barsRect.x - textRect.x - 8f),
                    IssueChipHeight + (IssueChipVerticalPadding * 2f)),
                card);
            this.DrawNeedBars(barsRect, card);
            this.text.AddTooltip(rect, this.BuildCardTooltip(card));

            if (Widgets.ButtonInvisible(rect))
            {
                V3CrewSelection.Select(state, card);
            }
        }

        private void DrawMemberText(Rect rect, V3CrewCardModel card, bool compact)
        {
            string nameLine = this.BuildNameLine(card);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x, rect.y, rect.width, 22f),
                this.text.FitLabelText(nameLine, rect.width));

            Text.Font = GameFont.Tiny;
            GUI.color = this.GetCategoryTextColor(card);
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x, rect.y + 24f, rect.width, 18f),
                this.text.FitLabelText(this.text.ValueOrDash(card.IdentityLabel), rect.width));

            GUI.color = this.GetActivityTextColor(card);
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x, rect.y + 44f, rect.width, 18f),
                this.text.FitLabelText(this.text.ValueOrDash(card.CurrentActivityLabel), rect.width));

            if (!compact && !string.IsNullOrEmpty(card.Note))
            {
                GUI.color = ShuttleUIStyle.MutedTextColor;
                ShuttleUILayout.SafeLabel(
                    new Rect(rect.x, rect.y + 64f, rect.width, 18f),
                    this.text.FitLabelText(card.Note, rect.width));
            }

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawRiskBar(Rect rect, V3CrewCardModel card)
        {
            Color color = V3CrewVisuals.GetRiskColor(
                card != null ? card.RiskLevel : V3CrewRiskLevel.Unknown);
            Widgets.DrawBoxSolid(new Rect(rect.x + 5f, rect.y + 7f, 4f, rect.height - 14f), color);
        }

        private void DrawCompartmentPill(Rect rect, V3CrewCardModel card)
        {
            string label = card != null && !string.IsNullOrEmpty(card.CompartmentLabel)
                ? card.CompartmentLabel
                : this.text.Tr("CT_ShuttleCrew_Unassigned");
            Color color = V3CrewVisuals.GetCompartmentColor(
                card != null ? card.SourceKind : V3CrewCardSourceKind.Unknown);
            Widgets.DrawBoxSolid(rect, V3CrewVisuals.WithAlpha(color, 0.18f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                V3CrewVisuals.WithAlpha(color, 0.78f),
                ShuttleUIStyle.ThinBorder);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 5f, rect.y + 1f, rect.width - 10f, rect.height - 2f),
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                color,
                card != null ? card.CompartmentTooltip : label,
                TextAnchor.MiddleCenter);
            this.text.AddTooltip(rect, card != null ? card.CompartmentTooltip : label);
        }

        private void DrawStatusChips(Rect rect, V3CrewCardModel card)
        {
            if (card == null || card.Issues == null || card.Issues.Count == 0 || rect.width <= 24f)
            {
                return;
            }

            float x = rect.x;
            int visible = Mathf.Min(VisibleIssueChipCount, card.Issues.Count);
            for (int i = 0; i < visible; i++)
            {
                V3CrewIssueModel issue = card.Issues[i];
                if (issue == null)
                {
                    continue;
                }

                float width = Mathf.Min(Mathf.Max(42f, Text.CalcSize(issue.Label).x + 16f), 78f);
                if (x + width > rect.xMax)
                {
                    break;
                }

                Rect chipRect = new Rect(
                    x,
                    rect.y + IssueChipVerticalPadding,
                    width,
                    IssueChipHeight);
                this.DrawIssueChip(
                    chipRect,
                    issue.Label,
                    V3CrewVisuals.GetIssueColor(issue.Severity),
                    issue.Summary);
                x += width + 5f;
            }

            int remaining = card.Issues.Count - visible;
            if (remaining > 0 && x + 34f <= rect.xMax)
            {
                this.DrawIssueChip(
                    new Rect(
                        x,
                        rect.y + IssueChipVerticalPadding,
                        34f,
                        IssueChipHeight),
                    "+" + remaining.ToString(),
                    ShuttleUIStyle.MutedTextColor,
                    card.IssueTooltip);
            }
        }

        private void DrawIssueChip(Rect rect, string label, Color color, string tooltip)
        {
            Widgets.DrawBoxSolid(rect, V3CrewVisuals.WithAlpha(color, 0.18f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                V3CrewVisuals.WithAlpha(color, 0.70f),
                ShuttleUIStyle.ThinBorder);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 4f, rect.y, rect.width - 8f, rect.height),
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                color,
                tooltip,
                TextAnchor.MiddleCenter);
            this.text.AddTooltip(rect, tooltip);
        }

        private void DrawNeedBars(Rect rect, V3CrewCardModel card)
        {
            if (card.Group == V3CrewGroupKind.Human)
            {
                this.DrawNeedBar(new Rect(rect.x, rect.y, rect.width, NeedBarRowHeight), this.text.Tr("CT_Shuttle_Crew_Need_Hunger"), card.FoodPct, V3CrewText.YellowColor);
                this.DrawNeedBar(new Rect(rect.x, rect.y + NeedBarRowHeight, rect.width, NeedBarRowHeight), this.text.Tr("CT_Shuttle_Crew_Need_Rest"), card.RestPct, V3CrewText.BlueColor);
                this.DrawNeedBar(new Rect(rect.x, rect.y + (NeedBarRowHeight * 2f), rect.width, NeedBarRowHeight), this.text.Tr("CT_Shuttle_Crew_Need_Mood"), card.MoodPct, V3CrewText.GreenColor);
                this.DrawNeedBar(new Rect(rect.x, rect.y + (NeedBarRowHeight * 3f), rect.width, NeedBarRowHeight), this.text.Tr("CT_Shuttle_Crew_Need_Joy"), card.JoyPct, V3CrewText.BlueColor);
                return;
            }

            if (card.Group == V3CrewGroupKind.Mech)
            {
                this.DrawNeedBar(new Rect(rect.x, rect.y, rect.width, NeedBarRowHeight), this.text.Tr("CT_Shuttle_Crew_Need_Energy"), card.EnergyPct, V3CrewText.BlueColor);
                return;
            }

            this.DrawNeedBar(new Rect(rect.x, rect.y, rect.width, NeedBarRowHeight), this.text.Tr("CT_Shuttle_Crew_Need_Hunger"), card.FoodPct, V3CrewText.YellowColor);
            this.DrawNeedBar(
                new Rect(rect.x, rect.y + 22f, rect.width, NeedBarRowHeight),
                card.Group == V3CrewGroupKind.Animal
                    ? this.text.Tr("CT_Shuttle_Crew_Need_Rest")
                    : this.text.Tr("CT_Shuttle_Crew_Need_Mood"),
                card.Group == V3CrewGroupKind.Animal ? card.RestPct : card.MoodPct,
                card.Group == V3CrewGroupKind.Animal ? V3CrewText.BlueColor : V3CrewText.GreenColor);
        }

        private void DrawNeedBar(Rect rect, string label, float value01, Color color)
        {
            float labelWidth = this.text.IsEnglishLanguage() ? 50f : 38f;
            float meterWidth = Mathf.Max(36f, rect.width - labelWidth - 4f);
            Rect meterRect = new Rect(
                rect.xMax - meterWidth,
                rect.y + ((rect.height - NeedBarMeterHeight) * 0.5f),
                meterWidth,
                NeedBarMeterHeight);
            Rect labelRect = new Rect(
                meterRect.x - labelWidth - 4f,
                rect.y,
                labelWidth,
                rect.height);
            Color labelColor = value01 >= 0f && value01 < 0.30f
                ? V3CrewText.YellowColor
                : ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                labelRect,
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                labelColor,
                label,
                TextAnchor.MiddleRight);
            float clamped = value01 >= 0f ? Mathf.Clamp01(value01) : 0f;
            Widgets.DrawBoxSolid(meterRect, V3CrewVisuals.WithAlpha(ShuttleUIStyle.MutedTextColor, 0.25f));
            Widgets.DrawBoxSolid(new Rect(meterRect.x, meterRect.y, meterRect.width * clamped, meterRect.height), value01 >= 0f ? color : ShuttleUIStyle.MutedTextColor);
            ShuttleUILayout.DrawRectBorder(
                meterRect,
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);
            if (value01 < 0f)
            {
                ShuttleUILayout.DrawFittedSingleLineLabel(
                    new Rect(meterRect.x + 4f, meterRect.y - 6f, meterRect.width - 8f, 18f),
                    "--",
                    GameFont.Tiny,
                    GameFont.Tiny,
                    ShuttleUIStyle.MutedTextColor,
                    null,
                    TextAnchor.MiddleLeft);
            }

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            this.text.AddTooltip(rect, label + ": " + this.text.FormatPercent(value01));
        }

        private Color GetCategoryTextColor(V3CrewCardModel card)
        {
            if (card == null)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            if (card.HasCrewKindDisplay)
            {
                return card.CategoryTextColor;
            }

            if (card.Group == V3CrewGroupKind.Mech)
            {
                return V3CrewText.BlueColor;
            }

            if (card.Group == V3CrewGroupKind.Animal)
            {
                return V3CrewText.GreenColor;
            }

            if (card.Group == V3CrewGroupKind.Entity)
            {
                return V3CrewText.PurpleColor;
            }

            return V3CrewText.BlueColor;
        }

        private Color GetActivityTextColor(V3CrewCardModel card)
        {
            if (card != null && card.HasCrewActivityDisplay)
            {
                return card.ActivityTextColor;
            }

            return ShuttleUIStyle.MutedTextColor;
        }

        private string BuildCardTooltip(V3CrewCardModel card)
        {
            if (card == null)
            {
                return null;
            }

            return this.BuildNameLine(card) + "\n" +
                this.text.ValueOrDash(card.CompartmentTooltip) + "\n" +
                this.text.ValueOrDash(card.CurrentActivityLabel) + "\n\n" +
                this.text.ValueOrDash(card.IssueTooltip);
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
