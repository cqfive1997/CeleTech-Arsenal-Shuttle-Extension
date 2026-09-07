using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalAdmissionPanel
    {
        private const float PanelContentTopOffset = 44f;

        private readonly V3MedicalText text;
        private readonly V3MedicalPanelDrawer panel;

        internal V3MedicalAdmissionPanel(
            V3MedicalText text)
        {
            this.text = text;
            this.panel = new V3MedicalPanelDrawer(text);
        }

        internal void Draw(
            Rect rect,
            V3MedicalPatientCardModel selectedPatient,
            V3MedicalPageModel model,
            ShuttlePageDrawContext context,
            Func<Rect, Rect> tutorialTargetMapper)
        {
            this.panel.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_Medical_TreatmentActions"));
            Rect inner = this.panel.GetPanelInnerRect(rect, PanelContentTopOffset);
            V3MedicalPageReadModel medicalModel = model != null ? model.MedicalModel : null;
            float y = 0f;
            this.DrawAdmissionButton(
                ref y,
                inner,
                medicalModel,
                context,
                tutorialTargetMapper);

            if (selectedPatient == null)
            {
                this.panel.DrawEmptyPanelMessage(
                    new Rect(inner.x, inner.y + y, inner.width, Mathf.Max(0f, inner.height - y)),
                    this.text.Tr("CT_Shuttle_Medical_SelectPatientForActions"));
                return;
            }

            this.DrawTreatmentButtons(
                ref y,
                inner,
                selectedPatient,
                medicalModel,
                context,
                tutorialTargetMapper);

            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(inner.x, inner.yMax - 34f, inner.width, 30f),
                this.text.Tr("CT_Shuttle_Medical_ActionAvailabilityNotice"));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawAdmissionButton(
            ref float y,
            Rect inner,
            V3MedicalPageReadModel medicalModel,
            ShuttlePageDrawContext context,
            Func<Rect, Rect> tutorialTargetMapper)
        {
            Rect buttonRect = new Rect(inner.x, inner.y + y, inner.width, 34f);
            this.RegisterMappedTutorialTarget(
                context,
                ShuttleTutorialTargetIds.MedicalAdmissionPanel,
                buttonRect,
                tutorialTargetMapper);
            IShuttleMedicalAdmissionUIActions actions =
                this.GetAdmissionActions(context);
            ShuttleMedicalPageActionContext pageContext =
                V3MedicalActionTargetFactory.CreatePageContext(medicalModel);
            bool canOpen = actions != null &&
                actions.CanOpenAdmissionDialog(pageContext);
            string tooltip = actions != null
                ? actions.GetAdmissionDialogTooltip(pageContext)
                : this.GetActionUnavailableTooltip();
            if (this.panel.DrawButton(
                buttonRect,
                this.text.Tr("CT_Shuttle_Medical_AdmitPatient"),
                canOpen,
                V3MedicalText.BlueColor,
                tooltip))
            {
                if (canOpen)
                {
                    actions.OpenAdmissionDialog(pageContext);
                }
                else
                {
                    ShuttleUICommandFeedback.ShowReject(tooltip, false);
                }
            }

            y += 42f;
        }

        private void DrawTreatmentButtons(
            ref float y,
            Rect inner,
            V3MedicalPatientCardModel patient,
            V3MedicalPageReadModel medicalModel,
            ShuttlePageDrawContext context,
            Func<Rect, Rect> tutorialTargetMapper)
        {
            if (patient.Kind == V3MedicalPatientKind.Human)
            {
                this.DrawTreatButton(
                    ref y,
                    inner,
                    patient,
                    medicalModel,
                    context,
                    this.text.Tr("CT_Shuttle_Medical_TreatNow"),
                    null,
                    tutorialTargetMapper);
                if (patient.NeedsTreatment)
                {
                    this.DrawTreatButton(
                        ref y,
                        inner,
                        patient,
                        medicalModel,
                        context,
                        this.text.Tr("CT_Shuttle_Medical_TendWithMedicine"),
                        true,
                        tutorialTargetMapper);
                    this.DrawTreatButton(
                        ref y,
                        inner,
                        patient,
                        medicalModel,
                        context,
                        this.text.Tr("CT_Shuttle_Medical_TendWithoutMedicine"),
                        false,
                        tutorialTargetMapper);
                }

                this.DrawSurgeryButton(
                    ref y,
                    inner,
                    patient,
                    medicalModel,
                    context,
                    tutorialTargetMapper);
            }

            this.DrawEjectButton(ref y, inner, patient, medicalModel, context);
        }

        private void DrawTreatButton(
            ref float y,
            Rect inner,
            V3MedicalPatientCardModel patient,
            V3MedicalPageReadModel medicalModel,
            ShuttlePageDrawContext context,
            string label,
            bool? useAvailableMedicine,
            Func<Rect, Rect> tutorialTargetMapper)
        {
            Rect buttonRect = new Rect(inner.x, inner.y + y, inner.width, 28f);
            this.RegisterMappedTutorialTarget(
                context,
                ShuttleTutorialTargetIds.MedicalTreatmentPanel,
                buttonRect,
                tutorialTargetMapper);
            bool blockedByProcedure = medicalModel != null && medicalModel.HasActiveProcedure;
            IShuttleMedicalTreatmentUIActions actions =
                this.GetTreatmentActions(context);
            ShuttleMedicalPatientActionTarget target =
                V3MedicalActionTargetFactory.CreatePatient(patient, medicalModel);
            bool enabled = !blockedByProcedure &&
                actions != null &&
                actions.CanTreatPatient(target);
            string tooltip = blockedByProcedure
                ? this.text.Tr("CT_Shuttle_MedicalProcedure_TreatmentBlocked")
                : actions != null
                    ? actions.GetTreatPatientTooltip(target)
                    : this.GetActionUnavailableTooltip();
            if (this.panel.DrawButton(buttonRect, label, enabled, false, tooltip))
            {
                if (enabled)
                {
                    actions.OpenDoctorMenu(target, useAvailableMedicine);
                }
                else
                {
                    ShuttleUICommandFeedback.ShowReject(tooltip, false);
                }
            }

            y += 34f;
        }

        private void DrawSurgeryButton(
            ref float y,
            Rect inner,
            V3MedicalPatientCardModel patient,
            V3MedicalPageReadModel medicalModel,
            ShuttlePageDrawContext context,
            Func<Rect, Rect> tutorialTargetMapper)
        {
            Rect buttonRect = new Rect(inner.x, inner.y + y, inner.width, 28f);
            this.RegisterMappedTutorialTarget(
                context,
                ShuttleTutorialTargetIds.MedicalSurgeryPanel,
                buttonRect,
                tutorialTargetMapper);
            bool blockedByProcedure = medicalModel != null && medicalModel.HasActiveProcedure;
            IShuttleMedicalSurgeryUIActions actions =
                this.GetSurgeryActions(context);
            ShuttleMedicalPatientActionTarget target =
                V3MedicalActionTargetFactory.CreatePatient(patient, medicalModel);
            bool enabled = !blockedByProcedure &&
                actions != null &&
                actions.CanScheduleSurgery(target);
            string tooltip = blockedByProcedure
                ? this.text.Tr("CT_Shuttle_MedicalProcedure_TreatmentBlocked")
                : actions != null
                    ? actions.GetScheduleSurgeryTooltip(target)
                    : this.GetActionUnavailableTooltip();
            if (this.panel.DrawButton(
                buttonRect,
                this.text.Tr("CT_Shuttle_MedicalSurgery_Schedule"),
                enabled,
                V3MedicalText.YellowColor,
                tooltip))
            {
                if (enabled)
                {
                    actions.OpenSurgeryMenu(target);
                }
                else
                {
                    ShuttleUICommandFeedback.ShowReject(tooltip, false);
                }
            }

            y += 34f;
        }

        private void DrawEjectButton(
            ref float y,
            Rect inner,
            V3MedicalPatientCardModel patient,
            V3MedicalPageReadModel medicalModel,
            ShuttlePageDrawContext context)
        {
            Rect buttonRect = new Rect(inner.x, inner.y + y, inner.width, 28f);
            bool blockedByProcedure = patient != null && patient.IsActiveProcedurePatient;
            IShuttleMedicalOccupantUIActions actions =
                this.GetOccupantActions(context);
            ShuttleMedicalPatientActionTarget target =
                V3MedicalActionTargetFactory.CreatePatient(patient, medicalModel);
            bool enabled = !blockedByProcedure &&
                actions != null &&
                actions.CanEjectPatient(target);
            string tooltip = blockedByProcedure
                ? this.text.Tr("CT_Shuttle_MedicalProcedure_PatientActive")
                : actions != null
                    ? actions.GetEjectPatientTooltip(target)
                    : this.GetActionUnavailableTooltip();
            if (this.panel.DrawButton(
                buttonRect,
                this.text.Tr("CT_Shuttle_Medical_RemoveFromBay"),
                enabled,
                V3MedicalText.RedColor,
                tooltip))
            {
                if (enabled)
                {
                    actions.EjectPatient(target);
                }
                else
                {
                    ShuttleUICommandFeedback.ShowReject(tooltip, false);
                }
            }

            y += 34f;
        }

        private void RegisterMappedTutorialTarget(
            ShuttlePageDrawContext context,
            string targetId,
            Rect rect,
            Func<Rect, Rect> targetMapper)
        {
            IShuttleTutorialTargetService tutorialTargets =
                context != null && context.MedicalPageContext != null
                    ? context.MedicalPageContext.TutorialTargets
                    : null;
            if (tutorialTargets == null)
            {
                return;
            }

            tutorialTargets.Register(
                targetId,
                targetMapper != null ? targetMapper(rect) : rect);
        }

        private IShuttleMedicalAdmissionUIActions GetAdmissionActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MedicalPageContext != null
                ? context.MedicalPageContext.AdmissionActions
                : null;
        }

        private IShuttleMedicalOccupantUIActions GetOccupantActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MedicalPageContext != null
                ? context.MedicalPageContext.OccupantActions
                : null;
        }

        private IShuttleMedicalTreatmentUIActions GetTreatmentActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MedicalPageContext != null
                ? context.MedicalPageContext.TreatmentActions
                : null;
        }

        private IShuttleMedicalSurgeryUIActions GetSurgeryActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MedicalPageContext != null
                ? context.MedicalPageContext.SurgeryActions
                : null;
        }

        private string GetActionUnavailableTooltip()
        {
            return ShuttleUIText.Tr("CT_Shuttle_UI_ActionUnavailableYet");
        }
    }
}
