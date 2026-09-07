using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalActionPanel
    {
        private const float PanelContentTopOffset = 44f;

        private readonly V3MedicalText text;
        private readonly V3MedicalPanelDrawer panel;

        internal V3MedicalActionPanel(
            V3MedicalText text)
        {
            this.text = text;
            this.panel = new V3MedicalPanelDrawer(text);
        }

        internal void DrawStatus(
            Rect rect,
            V3MedicalPageModel model,
            V3MedicalPageState state,
            ShuttlePageDrawContext context)
        {
            this.panel.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_Medical_Status"));
            Rect inner = this.panel.GetPanelInnerRect(rect, PanelContentTopOffset);
            V3MedicalPageReadModel medicalModel = model != null ? model.MedicalModel : null;
            Rect footerRect = new Rect(inner.x, inner.yMax - 34f, inner.width, 34f);
            Rect scrollRect = new Rect(
                inner.x,
                inner.y,
                inner.width,
                Mathf.Max(0f, inner.height - 42f));
            Rect viewRect = new Rect(
                0f,
                0f,
                scrollRect.width - 16f,
                Mathf.Max(scrollRect.height, 330f));

            Widgets.BeginScrollView(scrollRect, ref state.StatusScroll, viewRect);
            float y = 0f;
            this.DrawStatusLine(
                ref y,
                viewRect,
                this.text.Tr("CT_Shuttle_Medical_ModuleStatus"),
                this.GetModuleStatusText(medicalModel),
                this.GetModuleStatusColor(medicalModel),
                this.GetModuleStatusTooltip(medicalModel));
            this.DrawStatusLine(
                ref y,
                viewRect,
                this.text.Tr("CT_Shuttle_Medical_PowerStatus"),
                this.GetPowerStatusText(medicalModel),
                this.GetPowerStatusColor(medicalModel),
                this.GetPowerStatusTooltip(medicalModel));
            this.DrawStatusLine(
                ref y,
                viewRect,
                this.text.Tr("CT_Shuttle_Medical_BedStatus"),
                this.GetBedStatusText(medicalModel),
                this.GetBedStatusColor(medicalModel),
                this.GetBedStatusText(medicalModel));
            this.DrawStatusLine(
                ref y,
                viewRect,
                this.text.Tr("CT_Shuttle_Medical_MedicineStatus"),
                medicalModel != null ? this.text.ValueOrDash(medicalModel.MedicineStateText) : this.text.Tr("CT_Shuttle_Medical_Unavailable"),
                this.GetMedicineStatusColor(medicalModel),
                this.GetMedicineStatusTooltip(medicalModel));
            this.DrawStatusLine(
                ref y,
                viewRect,
                this.text.Tr("CT_Shuttle_Medical_Procedure"),
                this.GetProcedureStatusText(medicalModel),
                this.GetProcedureStatusColor(medicalModel),
                this.GetProcedureStatusTooltip(medicalModel));

            if (medicalModel != null && medicalModel.HasActiveProcedure)
            {
                this.DrawStatusLine(
                    ref y,
                    viewRect,
                    this.text.Tr("CT_Shuttle_Medical_Doctor"),
                    this.text.ValueOrDash(medicalModel.ActiveProcedureDoctorLabel),
                    this.GetProcedureStatusColor(medicalModel),
                    this.GetProcedureStatusTooltip(medicalModel));
                this.DrawStatusLine(
                    ref y,
                    viewRect,
                    this.text.Tr("CT_Shuttle_Medical_Progress"),
                    this.GetProcedureProgressText(medicalModel),
                    this.GetProcedureStatusColor(medicalModel),
                    this.GetProcedureStatusTooltip(medicalModel));
                this.DrawCancelProcedureButton(
                    ref y,
                    viewRect,
                    medicalModel,
                    context);
            }

            this.DrawStatusLine(
                ref y,
                viewRect,
                this.text.Tr("CT_Shuttle_Medical_Messages"),
                this.GetMedicalNoticeText(medicalModel),
                this.GetNoticeColor(medicalModel),
                this.GetMedicalNoticeText(medicalModel));
            Rect noteRect = new Rect(0f, y + 8f, viewRect.width, 58f);
            this.panel.DrawCardBackground(noteRect, false, true, ShuttleUIStyle.DisabledColor);
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(noteRect.x + 8f, noteRect.y + 7f, noteRect.width - 16f, noteRect.height - 14f),
                this.text.Tr("CT_Shuttle_Medical_StatusPanelHint"));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Widgets.EndScrollView();

            this.DrawEjectAllPatientsButton(footerRect, medicalModel, context);
        }

        private void DrawStatusLine(
            ref float y,
            Rect viewRect,
            string label,
            string value,
            Color valueColor,
            string tooltip)
        {
            Rect rowRect = new Rect(0f, y, viewRect.width, 32f);
            this.panel.DrawCardBackground(
                rowRect,
                false,
                false,
                new Color(0.078f, 0.094f, 0.118f, 0.92f));
            Text.Font = GameFont.Tiny;
            float labelWidth = 82f;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rowRect.x + 8f, rowRect.y + 7f, labelWidth, 18f),
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                tooltip,
                TextAnchor.MiddleLeft);
            GUI.color = valueColor;
            float valueX = rowRect.x + labelWidth + 18f;
            float valueWidth = rowRect.xMax - valueX - 8f;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(valueX, rowRect.y + 7f, valueWidth, 18f),
                value,
                GameFont.Tiny,
                GameFont.Tiny,
                valueColor,
                tooltip,
                TextAnchor.MiddleLeft);
            GUI.color = Color.white;
            this.text.AddTooltip(rowRect, tooltip);
            y += 38f;
        }

        private void DrawCancelProcedureButton(
            ref float y,
            Rect viewRect,
            V3MedicalPageReadModel medicalModel,
            ShuttlePageDrawContext context)
        {
            Rect buttonRect = new Rect(0f, y + 2f, viewRect.width, 30f);
            bool recovery = medicalModel != null &&
                medicalModel.ActiveProcedureStatus == "RecoveryRequired";
            IShuttleMedicalProcedureUIActions actions =
                this.GetProcedureActions(context);
            ShuttleMedicalProcedureActionTarget target =
                V3MedicalActionTargetFactory.CreateProcedure(medicalModel);
            bool enabled = actions != null &&
                actions.CanCancelMedicalProcedure(target);
            string label = recovery
                ? this.text.Tr("CT_Shuttle_MedicalProcedure_Recover")
                : this.text.Tr("CT_Shuttle_MedicalProcedure_Cancel");
            string tooltip = actions != null
                ? actions.GetCancelMedicalProcedureTooltip(target)
                : this.GetActionUnavailableTooltip();
            if (this.panel.DrawButton(
                buttonRect,
                label,
                enabled,
                recovery ? V3MedicalText.RedColor : V3MedicalText.YellowColor,
                tooltip))
            {
                if (enabled)
                {
                    actions.CancelMedicalProcedure(target);
                }
                else
                {
                    ShuttleUICommandFeedback.ShowReject(tooltip, false);
                }
            }

            y += 38f;
        }

        private void DrawEjectAllPatientsButton(
            Rect rect,
            V3MedicalPageReadModel medicalModel,
            ShuttlePageDrawContext context)
        {
            IShuttleMedicalOccupantUIActions actions =
                this.GetOccupantActions(context);
            ShuttleMedicalPageActionContext pageContext =
                V3MedicalActionTargetFactory.CreatePageContext(medicalModel);
            bool enabled = actions != null &&
                actions.CanEjectAllPatients(pageContext);
            string tooltip = actions != null
                ? actions.GetAllPatientsEjectTooltip(pageContext)
                : this.GetActionUnavailableTooltip();
            if (this.panel.DrawButton(
                rect,
                this.text.Tr("CT_Shuttle_Medical_EjectAll"),
                enabled,
                V3MedicalText.RedColor,
                tooltip))
            {
                if (enabled)
                {
                    actions.EjectAllPatients(pageContext);
                }
                else
                {
                    ShuttleUICommandFeedback.ShowReject(tooltip, false);
                }
            }
        }

        private IShuttleMedicalOccupantUIActions GetOccupantActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MedicalPageContext != null
                ? context.MedicalPageContext.OccupantActions
                : null;
        }

        private IShuttleMedicalProcedureUIActions GetProcedureActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MedicalPageContext != null
                ? context.MedicalPageContext.ProcedureActions
                : null;
        }

        private string GetActionUnavailableTooltip()
        {
            return ShuttleUIText.Tr("CT_Shuttle_UI_ActionUnavailableYet");
        }

        private string GetModuleStatusText(V3MedicalPageReadModel model)
        {
            if (model == null || !model.MedicalBayInstalled)
            {
                return this.text.Tr("CT_Shuttle_Common_Missing");
            }

            return model.MedicalBayEnabled
                ? this.text.Tr("CT_Shuttle_Main_Enabled")
                : this.text.Tr("CT_Shuttle_Common_Disabled");
        }

        private Color GetModuleStatusColor(V3MedicalPageReadModel model)
        {
            if (model == null || !model.MedicalBayInstalled)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            return model.MedicalBayEnabled
                ? V3MedicalText.GreenColor
                : V3MedicalText.RedColor;
        }

        private string GetModuleStatusTooltip(V3MedicalPageReadModel model)
        {
            return model == null || !model.MedicalBayInstalled
                ? this.text.Tr("CT_Shuttle_Medical_NotInstalled")
                : this.text.ValueOrDash(model.MedicalBayStateText);
        }

        private string GetPowerStatusText(V3MedicalPageReadModel model)
        {
            if (model == null || !model.MedicalBayInstalled)
            {
                return this.text.Tr("CT_Shuttle_Common_Missing");
            }

            if (!model.MedicalBayEnabled)
            {
                return this.text.Tr("CT_Shuttle_Common_Disabled");
            }

            if (model.PowerStatusPlaceholder)
            {
                return this.text.Tr("CT_Shuttle_Medical_PowerUnavailable");
            }

            return model.MedicalBayPowered
                ? this.text.Tr("CT_Shuttle_Common_Normal")
                : this.text.Tr("CT_Shuttle_Medical_Unpowered");
        }

        private Color GetPowerStatusColor(V3MedicalPageReadModel model)
        {
            if (model == null || !model.MedicalBayInstalled)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            if (model.PowerStatusPlaceholder)
            {
                return V3MedicalText.YellowColor;
            }

            return model.MedicalBayEnabled && model.MedicalBayPowered
                ? V3MedicalText.GreenColor
                : V3MedicalText.RedColor;
        }

        private string GetPowerStatusTooltip(V3MedicalPageReadModel model)
        {
            return this.GetPowerStatusText(model);
        }

        private string GetBedStatusText(V3MedicalPageReadModel model)
        {
            if (model == null || !model.MedicalBayInstalled)
            {
                return this.text.Tr("CT_Shuttle_Common_Missing");
            }

            return Mathf.Max(0, model.OccupiedBeds).ToString() + " / " +
                Mathf.Max(0, model.TotalBeds).ToString();
        }

        private Color GetBedStatusColor(V3MedicalPageReadModel model)
        {
            if (model == null || !model.MedicalBayInstalled)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            return model.TotalBeds > 0 && model.OccupiedBeds >= model.TotalBeds
                ? V3MedicalText.RedColor
                : V3MedicalText.GreenColor;
        }

        private Color GetMedicineStatusColor(V3MedicalPageReadModel model)
        {
            if (model == null || !model.MedicalBayInstalled)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            return this.text.GetSeverityColor(this.GetMedicineSeverityKey(model));
        }

        private string GetMedicineStatusTooltip(V3MedicalPageReadModel model)
        {
            if (model == null || !model.MedicalBayInstalled)
            {
                return this.text.Tr("CT_Shuttle_Medical_Unavailable");
            }

            return this.text.ValueOrDash(model.MedicineStateText) + "\n" +
                model.AvailableMedicineCount.ToString() + " / " +
                model.EstimatedMedicineNeed.ToString();
        }

        private string GetMedicineSeverityKey(V3MedicalPageReadModel model)
        {
            if (model == null || !model.MedicalBayInstalled)
            {
                return "Moderate";
            }

            if (model.EstimatedMedicineNeed <= 0)
            {
                return "Stable";
            }

            if (model.AvailableMedicineCount <= 0)
            {
                return "Critical";
            }

            if (model.AvailableMedicineCount < model.EstimatedMedicineNeed)
            {
                return "Severe";
            }

            if (model.AvailableMedicineCount < model.EstimatedMedicineNeed * 2)
            {
                return "Moderate";
            }

            return "Stable";
        }

        private string GetProcedureStatusText(V3MedicalPageReadModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.ActiveProcedureStatusLabel))
            {
                return this.text.Tr("CT_Shuttle_MedicalProcedure_Status_None");
            }

            return model.ActiveProcedureStatusLabel;
        }

        private Color GetProcedureStatusColor(V3MedicalPageReadModel model)
        {
            if (model == null || !model.HasActiveProcedure)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            return model.ActiveProcedureStatus == "RecoveryRequired"
                ? V3MedicalText.RedColor
                : V3MedicalText.YellowColor;
        }

        private string GetProcedureStatusTooltip(V3MedicalPageReadModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.ActiveProcedureTooltip))
            {
                return this.text.Tr("CT_Shuttle_MedicalProcedure_Status_None");
            }

            return model.ActiveProcedureTooltip;
        }

        private string GetProcedureProgressText(V3MedicalPageReadModel model)
        {
            if (model == null || !model.HasActiveProcedure)
            {
                return "-";
            }

            int percent = Mathf.RoundToInt(Mathf.Clamp01(model.ActiveProcedureProgress) * 100f);
            return model.ActiveProcedureWorkTicksDone.ToString() + " / " +
                model.ActiveProcedureWorkTicksTotal.ToString() + " (" +
                percent.ToString() + "%)";
        }

        private string GetMedicalNoticeText(V3MedicalPageReadModel model)
        {
            if (model == null || !model.MedicalBayInstalled)
            {
                return this.text.Tr("CT_Shuttle_Medical_NotInstalled");
            }

            if (!model.MedicalBayEnabled)
            {
                return this.text.Tr("CT_Shuttle_Medical_Disabled");
            }

            if (model.PowerStatusPlaceholder)
            {
                return this.text.Tr("CT_Shuttle_Medical_PowerUnavailable");
            }

            if (!model.MedicalBayPowered)
            {
                return this.text.Tr("CT_Shuttle_Medical_Unpowered");
            }

            if (model.PatientCount <= 0)
            {
                return this.text.Tr("CT_Shuttle_Medical_NoPatients");
            }

            if (model.PendingTreatmentCount > 0)
            {
                return this.text.Tr("CT_Shuttle_Medical_TreatmentItemsPending");
            }

            return this.text.Tr("CT_Shuttle_Medical_SystemsNominal");
        }

        private Color GetNoticeColor(V3MedicalPageReadModel model)
        {
            if (model == null || !model.MedicalBayInstalled)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            if (!model.MedicalBayEnabled || !model.MedicalBayPowered)
            {
                return V3MedicalText.RedColor;
            }

            if (model.PendingTreatmentCount > 0)
            {
                return V3MedicalText.YellowColor;
            }

            return V3MedicalText.GreenColor;
        }
    }
}
