using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal sealed class V3PrisonCellListPanel
    {
        private const float PanelContentTopOffset = 44f;
        private const float PrisonerCardHeight = 118f;
        private const float CandidateCardHeight = 92f;

        private readonly V3PrisonCellText text;
        private readonly V3PrisonCellPanelDrawer panelDrawer;

        internal V3PrisonCellListPanel(V3PrisonCellText text)
        {
            this.text = text;
            this.panelDrawer = new V3PrisonCellPanelDrawer(text);
        }

        internal void DrawCandidates(
            Rect rect,
            V3PrisonCellPageModel model,
            V3PrisonCellPageState state)
        {
            this.panelDrawer.DrawPanelTitle(rect, ShuttleUIText.Tr("CT_Shuttle_PrisonCell_Candidates"));
            Rect listRect = this.panelDrawer.GetPanelInnerRect(rect, PanelContentTopOffset);
            ShuttlePrisonCellReadModel prisonModel = model != null ? model.PrisonCellModel : null;
            if (prisonModel == null || !prisonModel.HasPrisonCell)
            {
                this.panelDrawer.DrawEmptyPanelMessage(listRect, ShuttleUIText.Tr("CT_Shuttle_PrisonCell_NotInstalled"));
                return;
            }

            IReadOnlyList<ShuttlePrisonerCandidateReadModel> candidates = prisonModel.Candidates;
            if (candidates == null || candidates.Count == 0)
            {
                this.panelDrawer.DrawEmptyPanelMessage(listRect, ShuttleUIText.Tr("CT_Shuttle_PrisonCell_NoCandidates"));
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, listRect.width - 16f),
                Mathf.Max(listRect.height, candidates.Count * CandidateCardHeight));
            Widgets.BeginScrollView(listRect, ref state.CandidateScroll, viewRect);
            try
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    ShuttlePrisonerCandidateReadModel candidate = candidates[i];
                    Rect rowRect = new Rect(0f, i * CandidateCardHeight, viewRect.width, CandidateCardHeight - 7f);
                    bool selected = candidate != null &&
                        candidate.ThingIDNumber == state.SelectedCandidateThingID;
                    this.DrawCandidateCard(rowRect, candidate, selected, state);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        internal void DrawHeldPrisoners(
            Rect rect,
            V3PrisonCellPageModel model,
            V3PrisonCellPageState state)
        {
            this.panelDrawer.DrawPanelTitle(rect, ShuttleUIText.Tr("CT_Shuttle_PrisonCell_HeldPrisoners"));
            Rect listRect = this.panelDrawer.GetPanelInnerRect(rect, PanelContentTopOffset);
            ShuttlePrisonCellReadModel prisonModel = model != null ? model.PrisonCellModel : null;
            if (prisonModel == null || !prisonModel.HasPrisonCell)
            {
                this.panelDrawer.DrawEmptyPanelMessage(listRect, ShuttleUIText.Tr("CT_Shuttle_PrisonCell_NotInstalled"));
                return;
            }

            IReadOnlyList<ShuttleHeldPrisonerReadModel> prisoners = prisonModel.Prisoners;
            if (prisoners == null || prisoners.Count == 0)
            {
                this.panelDrawer.DrawEmptyPanelMessage(listRect, ShuttleUIText.Tr("CT_Shuttle_PrisonCell_NoHeldPrisoners"));
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, listRect.width - 16f),
                Mathf.Max(listRect.height, prisoners.Count * PrisonerCardHeight));
            Widgets.BeginScrollView(listRect, ref state.HeldScroll, viewRect);
            try
            {
                for (int i = 0; i < prisoners.Count; i++)
                {
                    ShuttleHeldPrisonerReadModel prisoner = prisoners[i];
                    Rect rowRect = new Rect(0f, i * PrisonerCardHeight, viewRect.width, PrisonerCardHeight - 7f);
                    bool selected = prisoner != null &&
                        prisoner.ThingIDNumber == state.SelectedHeldPrisonerThingID;
                    this.DrawHeldPrisonerCard(rowRect, prisoner, selected, state);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private void DrawHeldPrisonerCard(
            Rect rect,
            ShuttleHeldPrisonerReadModel prisoner,
            bool selected,
            V3PrisonCellPageState state)
        {
            if (prisoner == null)
            {
                return;
            }

            this.panelDrawer.DrawCardBackground(
                rect,
                selected,
                false,
                ShuttleUIStyle.LeftColumnCardColor);
            Rect iconRect = new Rect(rect.x + 8f, rect.y + 10f, 42f, 42f);
            this.text.DrawThingIcon(iconRect, prisoner.DisplayThing, "P");

            Text.Font = GameFont.Small;
            ShuttleUILayout.SafeLabel(
                new Rect(iconRect.xMax + 8f, rect.y + 8f, rect.width - iconRect.width - 26f, 22f),
                this.text.FitLabelText(prisoner.Label, rect.width - iconRect.width - 28f));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(iconRect.xMax + 8f, rect.y + 32f, rect.width - iconRect.width - 26f, 18f),
                ShuttleUIText.Tr("CT_Shuttle_PrisonCell_Faction") + ": " + prisoner.FactionLabel);
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 8f, rect.y + 60f, rect.width - 16f, 18f),
                ShuttleUIText.Tr("CT_Shuttle_PrisonCell_HostFaction") + ": " + prisoner.HostFactionLabel);
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 8f, rect.y + 80f, rect.width - 16f, 18f),
                ShuttleUIText.Tr("CT_Shuttle_PrisonCell_Food") + ": " + prisoner.FoodLabel +
                " / " + ShuttleUIText.Tr("CT_Shuttle_PrisonCell_Health") + ": " + prisoner.HealthLabel +
                " / " + prisoner.TendStatusLabel);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            if (Widgets.ButtonInvisible(rect) && state != null)
            {
                state.SelectedHeldPrisonerThingID = prisoner.ThingIDNumber;
            }

            this.text.AddTooltip(rect, this.BuildHeldPrisonerTooltip(prisoner));
        }

        private void DrawCandidateCard(
            Rect rect,
            ShuttlePrisonerCandidateReadModel candidate,
            bool selected,
            V3PrisonCellPageState state)
        {
            if (candidate == null)
            {
                return;
            }

            this.panelDrawer.DrawCardBackground(rect, selected, !candidate.CanAdmit);
            Rect iconRect = new Rect(rect.x + 8f, rect.y + 10f, 38f, 38f);
            this.text.DrawThingIcon(iconRect, candidate.DisplayThing, "P");
            Text.Font = GameFont.Small;
            ShuttleUILayout.SafeLabel(
                new Rect(iconRect.xMax + 8f, rect.y + 8f, rect.width - 60f, 22f),
                this.text.FitLabelText(candidate.Label, rect.width - 70f));
            Text.Font = GameFont.Tiny;
            GUI.color = candidate.CanAdmit ? V3PrisonCellText.GreenColor : V3PrisonCellText.YellowColor;
            ShuttleUILayout.SafeLabel(
                new Rect(iconRect.xMax + 8f, rect.y + 31f, rect.width - 60f, 18f),
                candidate.CanAdmit
                    ? candidate.ReasonLabel
                    : this.text.ValueOrFallback(
                        candidate.CannotAdmitReason,
                        ShuttleUIText.Tr("CT_Shuttle_PrisonCell_CannotAdmit")));
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 8f, rect.y + 58f, rect.width - 16f, 18f),
                ShuttleUIText.Tr("CT_Shuttle_PrisonCell_Faction") + ": " + candidate.FactionLabel);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            if (Widgets.ButtonInvisible(rect) && state != null)
            {
                state.SelectedCandidateThingID = candidate.ThingIDNumber;
            }

            this.text.AddTooltip(rect, this.BuildCandidateTooltip(candidate));
        }

        private string BuildHeldPrisonerTooltip(ShuttleHeldPrisonerReadModel prisoner)
        {
            string tooltip = this.text.ValueOrDash(prisoner.Label) + "\n" +
                ShuttleUIText.Tr("CT_Shuttle_PrisonCell_InteractionMode") + ": " +
                this.text.ValueOrDash(prisoner.InteractionModeLabel);
            if (!string.IsNullOrEmpty(prisoner.FeedTooltip))
            {
                tooltip += "\n" + prisoner.FeedTooltip;
            }

            if (!string.IsNullOrEmpty(prisoner.TendTooltip))
            {
                tooltip += "\n" + prisoner.TendTooltip;
            }

            if (prisoner.IsDead)
            {
                tooltip += "\n" + ShuttleUIText.Tr("CT_Shuttle_PrisonCell_Dead");
            }
            else if (prisoner.IsDowned)
            {
                tooltip += "\n" + ShuttleUIText.Tr("CT_Shuttle_PrisonCell_Downed");
            }

            return tooltip;
        }

        private string BuildCandidateTooltip(ShuttlePrisonerCandidateReadModel candidate)
        {
            string tooltip = this.text.ValueOrDash(candidate.Label) + "\n" +
                this.text.ValueOrDash(candidate.ReasonLabel);
            if (!candidate.CanAdmit && !string.IsNullOrEmpty(candidate.CannotAdmitReason))
            {
                tooltip += "\n" + candidate.CannotAdmitReason;
            }

            return tooltip;
        }
    }
}
