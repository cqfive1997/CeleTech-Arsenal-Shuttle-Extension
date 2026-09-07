using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.PrisonCell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal sealed class V3PrisonCellActionPanel
    {
        private const float PanelContentTopOffset = 44f;

        private readonly V3PrisonCellText text;
        private readonly V3PrisonCellPanelDrawer panelDrawer;
        private readonly V3PrisonCellWorkerPanel workerPanel;

        internal V3PrisonCellActionPanel(V3PrisonCellText text)
        {
            this.text = text;
            this.panelDrawer = new V3PrisonCellPanelDrawer(text);
            this.workerPanel = new V3PrisonCellWorkerPanel(text, this.panelDrawer);
        }

        internal void Draw(
            Rect rect,
            V3PrisonCellPageModel model,
            V3PrisonCellPageState state,
            ShuttlePageDrawContext context)
        {
            this.panelDrawer.DrawPanelTitle(
                rect,
                this.text.Tr("CT_Shuttle_PrisonCell_SelectOperation"));
            Rect inner = this.panelDrawer.GetPanelInnerRect(rect, PanelContentTopOffset);
            ShuttlePrisonCellReadModel prisonModel = model != null ? model.PrisonCellModel : null;
            if (prisonModel == null || !prisonModel.HasPrisonCell)
            {
                this.panelDrawer.DrawEmptyPanelMessage(
                    inner,
                    this.text.Tr("CT_Shuttle_PrisonCell_NotInstalled"));
                return;
            }

            float gap = 8f;
            Rect modeTabsRect = new Rect(inner.x, inner.y, inner.width, 34f);
            Rect summaryRect = new Rect(inner.x, modeTabsRect.yMax + gap, inner.width, 82f);
            Rect hintRect = new Rect(inner.x, inner.yMax - 32f, inner.width, 32f);
            Rect buttonRect = new Rect(inner.x, hintRect.y - gap - 30f, inner.width, 30f);
            Rect workerRect = new Rect(
                inner.x,
                summaryRect.yMax + gap,
                inner.width,
                Mathf.Max(48f, buttonRect.y - summaryRect.yMax - (gap * 2f)));

            IShuttlePrisonCellUIActions actions = GetActions(context);
            this.DrawOperationTabs(modeTabsRect, state);
            this.DrawOperationSummary(summaryRect, prisonModel, state);
            this.workerPanel.Draw(workerRect, prisonModel, state);
            this.DrawPrimaryOperationButton(buttonRect, prisonModel, state, actions);
            this.DrawOperationHint(hintRect, prisonModel, state, actions);
        }

        private void DrawOperationTabs(Rect rect, V3PrisonCellPageState state)
        {
            if (state == null)
            {
                return;
            }

            float gap = 5f;
            float tabWidth = (rect.width - (gap * 3f)) / 4f;
            float x = rect.x;
            this.DrawOperationModeTab(
                new Rect(x, rect.y, tabWidth, rect.height),
                this.text.Tr("CT_Shuttle_PrisonCell_Mode_Carry"),
                V3PrisonCellOperationMode.Carry,
                state);
            x += tabWidth + gap;
            this.DrawOperationModeTab(
                new Rect(x, rect.y, tabWidth, rect.height),
                this.text.Tr("CT_Shuttle_PrisonCell_Mode_Feed"),
                V3PrisonCellOperationMode.Feed,
                state);
            x += tabWidth + gap;
            this.DrawOperationModeTab(
                new Rect(x, rect.y, tabWidth, rect.height),
                this.text.Tr("CT_Shuttle_PrisonCell_Mode_Tend"),
                V3PrisonCellOperationMode.Tend,
                state);
            x += tabWidth + gap;
            this.DrawOperationModeTab(
                new Rect(x, rect.y, tabWidth, rect.height),
                this.text.Tr("CT_Shuttle_PrisonCell_Mode_Eject"),
                V3PrisonCellOperationMode.Eject,
                state);
        }

        private void DrawOperationModeTab(
            Rect rect,
            string label,
            V3PrisonCellOperationMode mode,
            V3PrisonCellPageState state)
        {
            bool selected = state != null && state.SelectedOperationMode == mode;
            bool clicked = selected
                ? ShuttleUIActionButtonDrawer.DrawAccentButton(
                    rect,
                    this.text.FitLabelText(label, rect.width - 4f),
                    state != null,
                    V3PrisonCellText.AccentColor,
                    label)
                : ShuttleUIActionButtonDrawer.DrawNormalButton(
                    rect,
                    this.text.FitLabelText(label, rect.width - 4f),
                    state != null,
                    label);
            if (clicked && state != null)
            {
                state.SelectedOperationMode = mode;
            }
        }

        private void DrawOperationSummary(
            Rect rect,
            ShuttlePrisonCellReadModel prisonModel,
            V3PrisonCellPageState state)
        {
            ShuttleUILayout.DrawCardBackground(rect, false, false, ShuttleUIStyle.RightBottomCardColor);
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 18f),
                this.text.Tr("CT_Shuttle_PrisonCell_OperationTarget"));

            V3PrisonCellOperationMode mode = state != null
                ? state.SelectedOperationMode
                : V3PrisonCellOperationMode.Carry;
            float y = rect.y + 30f;
            if (mode == V3PrisonCellOperationMode.Carry)
            {
                ShuttlePrisonerCandidateReadModel candidate =
                    V3PrisonCellSelection.GetSelectedCandidate(prisonModel, state);
                ShuttlePrisonerCarrierReadModel carrier =
                    V3PrisonCellSelection.GetSelectedCarrier(prisonModel, state);
                this.DrawSummaryLine(
                    rect,
                    y,
                    this.text.Tr("CT_Shuttle_PrisonCell_SelectedCandidate"),
                    candidate != null ? candidate.Label : this.text.Tr("CT_Shuttle_PrisonCell_NoSelection"));
                this.DrawSummaryLine(
                    rect,
                    y + 22f,
                    this.text.Tr("CT_Shuttle_PrisonCell_SelectedCarrier"),
                    carrier != null ? carrier.Label : this.text.Tr("CT_Shuttle_PrisonCell_NoSelection"));
            }
            else if (mode == V3PrisonCellOperationMode.Feed)
            {
                ShuttleHeldPrisonerReadModel prisoner =
                    V3PrisonCellSelection.GetSelectedHeldPrisoner(prisonModel, state);
                ShuttlePrisonerFeederReadModel feeder =
                    V3PrisonCellSelection.GetSelectedFeeder(prisonModel, state);
                this.DrawSummaryLine(
                    rect,
                    y,
                    this.text.Tr("CT_Shuttle_PrisonCell_SelectedHeldPrisoner"),
                    prisoner != null ? prisoner.Label : this.text.Tr("CT_Shuttle_PrisonCell_NoHeldPrisonerSelected"));
                this.DrawSummaryLine(
                    rect,
                    y + 22f,
                    this.text.Tr("CT_Shuttle_PrisonCell_SelectedFeeder"),
                    feeder != null ? feeder.Label : this.text.Tr("CT_Shuttle_PrisonCell_NoSelection"));
            }
            else if (mode == V3PrisonCellOperationMode.Tend)
            {
                ShuttleHeldPrisonerReadModel prisoner =
                    V3PrisonCellSelection.GetSelectedHeldPrisoner(prisonModel, state);
                ShuttlePrisonerDoctorReadModel doctor =
                    V3PrisonCellSelection.GetSelectedDoctor(prisonModel, state);
                this.DrawSummaryLine(
                    rect,
                    y,
                    this.text.Tr("CT_Shuttle_PrisonCell_SelectedHeldPrisoner"),
                    prisoner != null ? prisoner.Label : this.text.Tr("CT_Shuttle_PrisonCell_NoHeldPrisonerSelected"));
                this.DrawSummaryLine(
                    rect,
                    y + 22f,
                    this.text.Tr("CT_Shuttle_PrisonCell_SelectedDoctor"),
                    doctor != null ? doctor.Label : this.text.Tr("CT_Shuttle_PrisonCell_NoSelection"));
            }
            else
            {
                ShuttleHeldPrisonerReadModel prisoner =
                    V3PrisonCellSelection.GetSelectedHeldPrisoner(prisonModel, state);
                this.DrawSummaryLine(
                    rect,
                    y,
                    this.text.Tr("CT_Shuttle_PrisonCell_SelectedHeldPrisoner"),
                    prisoner != null ? prisoner.Label : this.text.Tr("CT_Shuttle_PrisonCell_NoHeldPrisonerSelected"));
            }

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawSummaryLine(Rect rect, float y, string label, string value)
        {
            string line = label + ": " + this.text.ValueOrDash(value);
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 10f, y, rect.width - 20f, 18f),
                this.text.FitLabelText(line, rect.width - 20f));
        }

        private void DrawPrimaryOperationButton(
            Rect rect,
            ShuttlePrisonCellReadModel prisonModel,
            V3PrisonCellPageState state,
            IShuttlePrisonCellUIActions actions)
        {
            string tooltip = this.GetOperationTooltip(prisonModel, state, actions);
            bool enabled = this.CanExecuteOperation(prisonModel, state, actions);
            bool clicked = this.panelDrawer.DrawButton(
                rect,
                this.GetOperationButtonLabel(state),
                enabled,
                this.GetSelectedMode(state) == V3PrisonCellOperationMode.Eject,
                tooltip);
            if (!clicked)
            {
                return;
            }

            if (enabled)
            {
                this.ExecuteOperation(prisonModel, state, actions);
            }
            else
            {
                ShuttleUICommandFeedback.ShowReject(
                    !string.IsNullOrEmpty(tooltip)
                        ? tooltip
                        : this.text.Tr("CT_Shuttle_PrisonCell_ActionUnavailable"));
            }
        }

        private void DrawOperationHint(
            Rect rect,
            ShuttlePrisonCellReadModel prisonModel,
            V3PrisonCellPageState state,
            IShuttlePrisonCellUIActions actions)
        {
            string tooltip = this.GetOperationTooltip(prisonModel, state, actions);
            bool enabled = this.CanExecuteOperation(prisonModel, state, actions);
            ShuttleUILayout.DrawCardBackground(rect, false, false, ShuttleUIStyle.RightBottomCardColor);
            Text.Font = GameFont.Tiny;
            GUI.color = enabled
                ? ShuttleUIStyle.MutedTextColor
                : V3PrisonCellText.YellowColor;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 8f, rect.y + 7f, rect.width - 16f, 18f),
                this.text.FitLabelText(
                    this.text.ValueOrDash(
                        !string.IsNullOrEmpty(tooltip)
                            ? tooltip
                            : this.text.Tr("CT_Shuttle_PrisonCell_ActionUnavailable")),
                    rect.width - 16f));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            this.text.AddTooltip(rect, tooltip);
        }

        private bool CanExecuteOperation(
            ShuttlePrisonCellReadModel prisonModel,
            V3PrisonCellPageState state,
            IShuttlePrisonCellUIActions actions)
        {
            if (actions == null)
            {
                return false;
            }

            V3PrisonCellOperationMode mode = this.GetSelectedMode(state);
            if (mode == V3PrisonCellOperationMode.Carry)
            {
                return actions.CanCarryToCell(
                    V3PrisonCellSelection.GetSelectedCandidate(prisonModel, state),
                    V3PrisonCellSelection.GetSelectedCarrier(prisonModel, state));
            }

            if (mode == V3PrisonCellOperationMode.Feed)
            {
                return actions.CanFeed(
                    V3PrisonCellSelection.GetSelectedHeldPrisoner(prisonModel, state),
                    V3PrisonCellSelection.GetSelectedFeeder(prisonModel, state));
            }

            if (mode == V3PrisonCellOperationMode.Tend)
            {
                return actions.CanTend(
                    V3PrisonCellSelection.GetSelectedHeldPrisoner(prisonModel, state),
                    V3PrisonCellSelection.GetSelectedDoctor(prisonModel, state));
            }

            return actions.CanEject(
                V3PrisonCellSelection.GetSelectedHeldPrisoner(prisonModel, state));
        }

        private bool ExecuteOperation(
            ShuttlePrisonCellReadModel prisonModel,
            V3PrisonCellPageState state,
            IShuttlePrisonCellUIActions actions)
        {
            if (actions == null)
            {
                return false;
            }

            V3PrisonCellOperationMode mode = this.GetSelectedMode(state);
            if (mode == V3PrisonCellOperationMode.Carry)
            {
                return actions.CarryToCell(
                    V3PrisonCellSelection.GetSelectedCandidate(prisonModel, state),
                    V3PrisonCellSelection.GetSelectedCarrier(prisonModel, state));
            }

            if (mode == V3PrisonCellOperationMode.Feed)
            {
                return actions.Feed(
                    V3PrisonCellSelection.GetSelectedHeldPrisoner(prisonModel, state),
                    V3PrisonCellSelection.GetSelectedFeeder(prisonModel, state));
            }

            if (mode == V3PrisonCellOperationMode.Tend)
            {
                return actions.Tend(
                    V3PrisonCellSelection.GetSelectedHeldPrisoner(prisonModel, state),
                    V3PrisonCellSelection.GetSelectedDoctor(prisonModel, state));
            }

            return actions.Eject(
                V3PrisonCellSelection.GetSelectedHeldPrisoner(prisonModel, state));
        }

        private string GetOperationTooltip(
            ShuttlePrisonCellReadModel prisonModel,
            V3PrisonCellPageState state,
            IShuttlePrisonCellUIActions actions)
        {
            if (actions == null)
            {
                return this.text.Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            V3PrisonCellOperationMode mode = this.GetSelectedMode(state);
            if (mode == V3PrisonCellOperationMode.Carry)
            {
                return actions.GetCarryTooltip(
                    V3PrisonCellSelection.GetSelectedCandidate(prisonModel, state),
                    V3PrisonCellSelection.GetSelectedCarrier(prisonModel, state));
            }

            if (mode == V3PrisonCellOperationMode.Feed)
            {
                return actions.GetFeedTooltip(
                    V3PrisonCellSelection.GetSelectedHeldPrisoner(prisonModel, state),
                    V3PrisonCellSelection.GetSelectedFeeder(prisonModel, state));
            }

            if (mode == V3PrisonCellOperationMode.Tend)
            {
                return actions.GetTendTooltip(
                    V3PrisonCellSelection.GetSelectedHeldPrisoner(prisonModel, state),
                    V3PrisonCellSelection.GetSelectedDoctor(prisonModel, state));
            }

            return actions.GetEjectTooltip(
                V3PrisonCellSelection.GetSelectedHeldPrisoner(prisonModel, state));
        }

        private string GetOperationButtonLabel(V3PrisonCellPageState state)
        {
            V3PrisonCellOperationMode mode = this.GetSelectedMode(state);
            if (mode == V3PrisonCellOperationMode.Feed)
            {
                return this.text.Tr("CT_Shuttle_PrisonCell_Feed");
            }

            if (mode == V3PrisonCellOperationMode.Tend)
            {
                return this.text.Tr("CT_Shuttle_PrisonCell_Tend");
            }

            if (mode == V3PrisonCellOperationMode.Eject)
            {
                return this.text.Tr("CT_Shuttle_PrisonCell_Eject");
            }

            return this.text.Tr("CT_Shuttle_PrisonCell_CarryToCell");
        }

        private V3PrisonCellOperationMode GetSelectedMode(V3PrisonCellPageState state)
        {
            return state != null
                ? state.SelectedOperationMode
                : V3PrisonCellOperationMode.Carry;
        }

        private static IShuttlePrisonCellUIActions GetActions(ShuttlePageDrawContext context)
        {
            if (context != null && context.PrisonCellPageContext != null)
            {
                return context.PrisonCellPageContext.PrisonCellActions;
            }

            return null;
        }
    }
}
