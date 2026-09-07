using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal sealed class V3PrisonCellWorkerPanel
    {
        private const float WorkerCardHeight = 58f;

        private readonly V3PrisonCellText text;
        private readonly V3PrisonCellPanelDrawer panelDrawer;

        internal V3PrisonCellWorkerPanel(
            V3PrisonCellText text,
            V3PrisonCellPanelDrawer panelDrawer)
        {
            this.text = text;
            this.panelDrawer = panelDrawer;
        }

        internal void Draw(
            Rect rect,
            ShuttlePrisonCellReadModel prisonModel,
            V3PrisonCellPageState state)
        {
            V3PrisonCellOperationMode mode = state != null
                ? state.SelectedOperationMode
                : V3PrisonCellOperationMode.Carry;
            if (mode == V3PrisonCellOperationMode.Carry)
            {
                this.DrawCarrierList(rect, prisonModel, state);
                return;
            }

            if (mode == V3PrisonCellOperationMode.Feed)
            {
                this.DrawFeederList(rect, prisonModel, state);
                return;
            }

            if (mode == V3PrisonCellOperationMode.Tend)
            {
                this.DrawDoctorList(rect, prisonModel, state);
                return;
            }

            this.panelDrawer.DrawEmptyPanelMessage(
                rect,
                this.text.Tr("CT_Shuttle_PrisonCell_NoWorkerRequired"));
        }

        private void DrawCarrierList(
            Rect rect,
            ShuttlePrisonCellReadModel prisonModel,
            V3PrisonCellPageState state)
        {
            this.DrawListTitle(rect, this.text.Tr("CT_Shuttle_PrisonCell_Carriers"));
            Rect listRect = this.GetWorkerListRect(rect);
            IReadOnlyList<ShuttlePrisonerCarrierReadModel> carriers =
                prisonModel != null ? prisonModel.Carriers : null;
            if (carriers == null || carriers.Count == 0)
            {
                this.panelDrawer.DrawEmptyPanelMessage(
                    listRect,
                    this.text.Tr("CT_Shuttle_PrisonCell_NoCarriers"));
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, listRect.width - 16f),
                Mathf.Max(listRect.height, carriers.Count * WorkerCardHeight));
            Widgets.BeginScrollView(listRect, ref state.CarrierScroll, viewRect);
            try
            {
                for (int i = 0; i < carriers.Count; i++)
                {
                    ShuttlePrisonerCarrierReadModel carrier = carriers[i];
                    Rect rowRect = new Rect(0f, i * WorkerCardHeight, viewRect.width, WorkerCardHeight - 6f);
                    bool selected = carrier != null &&
                        carrier.ThingIDNumber == state.SelectedCarrierThingID;
                    this.DrawCarrierCard(rowRect, carrier, selected, state);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private void DrawFeederList(
            Rect rect,
            ShuttlePrisonCellReadModel prisonModel,
            V3PrisonCellPageState state)
        {
            this.DrawListTitle(rect, this.text.Tr("CT_Shuttle_PrisonCell_Feeders"));
            Rect listRect = this.GetWorkerListRect(rect);
            IReadOnlyList<ShuttlePrisonerFeederReadModel> feeders =
                prisonModel != null ? prisonModel.Feeders : null;
            if (feeders == null || feeders.Count == 0)
            {
                this.panelDrawer.DrawEmptyPanelMessage(
                    listRect,
                    this.text.Tr("CT_Shuttle_PrisonCell_NoFeeders"));
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, listRect.width - 16f),
                Mathf.Max(listRect.height, feeders.Count * WorkerCardHeight));
            Widgets.BeginScrollView(listRect, ref state.FeederScroll, viewRect);
            try
            {
                for (int i = 0; i < feeders.Count; i++)
                {
                    ShuttlePrisonerFeederReadModel feeder = feeders[i];
                    Rect rowRect = new Rect(0f, i * WorkerCardHeight, viewRect.width, WorkerCardHeight - 6f);
                    bool selected = feeder != null &&
                        feeder.ThingIDNumber == state.SelectedFeederThingID;
                    this.DrawFeederCard(rowRect, feeder, selected, state);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private void DrawDoctorList(
            Rect rect,
            ShuttlePrisonCellReadModel prisonModel,
            V3PrisonCellPageState state)
        {
            this.DrawListTitle(rect, this.text.Tr("CT_Shuttle_PrisonCell_Doctors"));
            Rect listRect = this.GetWorkerListRect(rect);
            IReadOnlyList<ShuttlePrisonerDoctorReadModel> doctors =
                prisonModel != null ? prisonModel.Doctors : null;
            if (doctors == null || doctors.Count == 0)
            {
                this.panelDrawer.DrawEmptyPanelMessage(
                    listRect,
                    this.text.Tr("CT_Shuttle_PrisonCell_NoDoctor"));
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, listRect.width - 16f),
                Mathf.Max(listRect.height, doctors.Count * WorkerCardHeight));
            Widgets.BeginScrollView(listRect, ref state.DoctorScroll, viewRect);
            try
            {
                for (int i = 0; i < doctors.Count; i++)
                {
                    ShuttlePrisonerDoctorReadModel doctor = doctors[i];
                    Rect rowRect = new Rect(0f, i * WorkerCardHeight, viewRect.width, WorkerCardHeight - 6f);
                    bool selected = doctor != null &&
                        doctor.ThingIDNumber == state.SelectedDoctorThingID;
                    this.DrawDoctorCard(rowRect, doctor, selected, state);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private void DrawCarrierCard(
            Rect rect,
            ShuttlePrisonerCarrierReadModel carrier,
            bool selected,
            V3PrisonCellPageState state)
        {
            if (carrier == null)
            {
                return;
            }

            this.DrawWorkerCard(
                rect,
                carrier.Label,
                carrier.CanCarry,
                this.text.Tr("CT_Shuttle_PrisonCell_CanCarry"),
                this.text.Tr("CT_Shuttle_PrisonCell_ActionUnavailable"),
                carrier.CanCarry
                    ? this.text.Tr("CT_Shuttle_PrisonCell_CarryTooltip")
                    : this.text.ValueOrFallback(
                        carrier.CannotCarryReason,
                        this.text.Tr("CT_Shuttle_PrisonCell_CarrierUnavailable")),
                selected);
            if (Widgets.ButtonInvisible(rect) && state != null)
            {
                state.SelectedCarrierThingID = carrier.ThingIDNumber;
            }
        }

        private void DrawFeederCard(
            Rect rect,
            ShuttlePrisonerFeederReadModel feeder,
            bool selected,
            V3PrisonCellPageState state)
        {
            if (feeder == null)
            {
                return;
            }

            this.DrawWorkerCard(
                rect,
                feeder.Label,
                feeder.CanFeed,
                this.text.Tr("CT_Shuttle_PrisonCell_AvailableFood"),
                this.text.Tr("CT_Shuttle_PrisonCell_ActionUnavailable"),
                feeder.CanFeed
                    ? this.text.Tr("CT_Shuttle_PrisonCell_FeedTooltip")
                    : this.text.ValueOrFallback(
                        feeder.CannotFeedReason,
                        this.text.Tr("CT_Shuttle_PrisonCell_FeederUnavailable")),
                selected);
            if (Widgets.ButtonInvisible(rect) && state != null)
            {
                state.SelectedFeederThingID = feeder.ThingIDNumber;
            }
        }

        private void DrawDoctorCard(
            Rect rect,
            ShuttlePrisonerDoctorReadModel doctor,
            bool selected,
            V3PrisonCellPageState state)
        {
            if (doctor == null)
            {
                return;
            }

            this.DrawWorkerCard(
                rect,
                doctor.Label,
                doctor.CanTend,
                this.text.Tr("CT_Shuttle_PrisonCell_CanTend"),
                this.text.Tr("CT_Shuttle_PrisonCell_ActionUnavailable"),
                doctor.CanTend
                    ? this.text.Tr("CT_Shuttle_PrisonCell_TendTooltip")
                    : this.text.ValueOrFallback(
                        doctor.CannotTendReason,
                        this.text.Tr("CT_Shuttle_PrisonCell_DoctorUnavailable")),
                selected);
            if (Widgets.ButtonInvisible(rect) && state != null)
            {
                state.SelectedDoctorThingID = doctor.ThingIDNumber;
            }
        }

        private void DrawWorkerCard(
            Rect rect,
            string label,
            bool available,
            string availableLabel,
            string unavailableLabel,
            string tooltip,
            bool selected)
        {
            this.panelDrawer.DrawCardBackground(
                rect,
                selected,
                !available,
                ShuttleUIStyle.RightBottomCardColor);
            Text.Font = GameFont.Small;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 22f),
                this.text.FitLabelText(label, rect.width - 16f));
            Text.Font = GameFont.Tiny;
            GUI.color = available ? V3PrisonCellText.GreenColor : V3PrisonCellText.YellowColor;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 8f, rect.y + 31f, rect.width - 16f, 18f),
                available ? availableLabel : unavailableLabel);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            this.text.AddTooltip(rect, tooltip);
        }

        private void DrawListTitle(Rect rect, string title)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(new Rect(rect.x, rect.y, rect.width, 18f), title);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private Rect GetWorkerListRect(Rect rect)
        {
            return new Rect(rect.x, rect.y + 22f, rect.width, rect.height - 22f);
        }
    }
}
