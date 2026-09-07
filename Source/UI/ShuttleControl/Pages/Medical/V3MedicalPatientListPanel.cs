using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalPatientListPanel
    {
        private const float PatientCardHeight = 156f;
        private const float PanelContentTopOffset = 44f;

        private readonly V3MedicalText text;
        private readonly V3MedicalPanelDrawer panel;

        internal V3MedicalPatientListPanel(V3MedicalText text)
        {
            this.text = text;
            this.panel = new V3MedicalPanelDrawer(text);
        }

        internal void Draw(
            Rect rect,
            V3MedicalPageModel model,
            V3MedicalPatientCardModel selectedPatient,
            V3MedicalPageState state)
        {
            this.panel.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_Medical_PatientList"));
            Rect listRect = this.panel.GetPanelInnerRect(rect, PanelContentTopOffset);
            V3MedicalPageReadModel medicalModel = model != null ? model.MedicalModel : null;
            if (medicalModel == null || !medicalModel.HasMedicalBay)
            {
                this.panel.DrawEmptyPanelMessage(listRect, this.text.Tr("CT_Shuttle_Medical_NotInstalled"));
                return;
            }

            List<V3MedicalPatientCardModel> patients = medicalModel.Patients;
            bool hasOccupants = medicalModel.Occupants != null && medicalModel.Occupants.Count > 0;
            if (!hasOccupants && (patients == null || patients.Count == 0))
            {
                this.panel.DrawEmptyPanelMessage(listRect, this.text.Tr("CT_Shuttle_Medical_NoPatients"));
                return;
            }

            int count = hasOccupants ? medicalModel.Occupants.Count : patients.Count;
            Rect viewRect = new Rect(
                0f,
                0f,
                listRect.width - 16f,
                Mathf.Max(listRect.height, count * PatientCardHeight));
            Widgets.BeginScrollView(listRect, ref state.PatientScroll, viewRect);
            for (int i = 0; i < count; i++)
            {
                Rect rowRect = new Rect(0f, i * PatientCardHeight, viewRect.width, PatientCardHeight - 6f);
                if (hasOccupants)
                {
                    this.DrawOccupantCard(
                        rowRect,
                        medicalModel.Occupants[i],
                        selectedPatient,
                        state);
                }
                else
                {
                    this.DrawPatientCard(
                        rowRect,
                        patients[i],
                        IsSamePatient(patients[i], selectedPatient),
                        state);
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawOccupantCard(
            Rect rect,
            V3MedicalOccupantCardModel occupant,
            V3MedicalPatientCardModel selectedPatient,
            V3MedicalPageState state)
        {
            if (occupant == null)
            {
                return;
            }

            if (occupant.Kind == V3MedicalOccupantKind.Patient &&
                occupant.CanSelectAsPatient &&
                occupant.Patient != null)
            {
                this.DrawPatientCard(
                    rect,
                    occupant.Patient,
                    IsSamePatient(occupant.Patient, selectedPatient),
                    state);
                return;
            }

            this.panel.DrawCardBackground(rect, false, false, ShuttleUIStyle.RightTopCardColor);
            Rect iconRect = new Rect(rect.x + 8f, rect.y + 10f, 46f, 46f);
            this.text.DrawThingIcon(
                iconRect,
                occupant.DisplayThing,
                this.GetOccupantFallbackIconText(occupant),
                occupant.Label);
            Rect statusRect = new Rect(rect.xMax - 112f, rect.y + 8f, 104f, 22f);
            this.panel.DrawStatusBadge(statusRect, occupant.CurrentStatusLabel, V3MedicalText.BlueColor);

            Rect textRect = new Rect(iconRect.xMax + 8f, rect.y + 8f, statusRect.x - iconRect.xMax - 14f, 62f);
            Text.Font = GameFont.Small;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(textRect.x, textRect.y, textRect.width, 22f),
                occupant.Label,
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                this.BuildOccupantTooltip(occupant),
                TextAnchor.MiddleLeft);
            Text.Font = GameFont.Tiny;
            GUI.color = V3MedicalText.BlueColor;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(textRect.x, textRect.y + 24f, textRect.width, 19f),
                occupant.RoleLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                V3MedicalText.BlueColor,
                this.BuildOccupantTooltip(occupant),
                TextAnchor.MiddleLeft);
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(textRect.x, textRect.y + 44f, textRect.width, 19f),
                occupant.CurrentStatusLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                this.BuildOccupantTooltip(occupant),
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 8f, rect.y + 72f, rect.width - 16f, 19f),
                occupant.ActivityLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                this.BuildOccupantTooltip(occupant),
                TextAnchor.MiddleLeft);
            GUI.color = Color.white;
            this.DrawCompactVitals(
                new Rect(rect.x + 8f, rect.y + 96f, rect.width - 16f, 48f),
                occupant.Vitals,
                3);
            this.text.AddTooltip(rect, this.BuildOccupantTooltip(occupant));
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }
        }

        private void DrawPatientCard(
            Rect rect,
            V3MedicalPatientCardModel patient,
            bool selected,
            V3MedicalPageState state)
        {
            if (patient == null)
            {
                return;
            }

            this.panel.DrawCardBackground(rect, selected, false, this.text.GetPatientCardColor(patient));
            Rect iconRect = new Rect(rect.x + 8f, rect.y + 10f, 46f, 46f);
            this.text.DrawThingIcon(
                iconRect,
                patient.DisplayThing,
                this.text.GetPatientFallbackIconText(patient),
                patient.Label);
            Rect badgeRect = new Rect(rect.xMax - 74f, rect.y + 8f, 66f, 22f);
            this.panel.DrawStatusBadge(
                badgeRect,
                this.text.GetTriageLabel(patient),
                this.text.GetSeverityColor(patient.TriageKey));

            Rect textRect = new Rect(iconRect.xMax + 8f, rect.y + 8f, badgeRect.x - iconRect.xMax - 14f, 62f);
            Text.Font = GameFont.Small;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(textRect.x, textRect.y, textRect.width, 22f),
                patient.Label,
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                this.BuildPatientTooltip(patient),
                TextAnchor.MiddleLeft);
            Text.Font = GameFont.Tiny;
            GUI.color = this.text.GetPatientKindTextColor(patient);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(textRect.x, textRect.y + 24f, textRect.width, 19f),
                this.text.GetKindLabel(patient),
                GameFont.Tiny,
                GameFont.Tiny,
                this.text.GetPatientKindTextColor(patient),
                this.BuildPatientTooltip(patient),
                TextAnchor.MiddleLeft);
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(textRect.x, textRect.y + 44f, textRect.width, 19f),
                this.text.GetStateLabel(patient),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                this.BuildPatientTooltip(patient),
                TextAnchor.MiddleLeft);
            GUI.color = Color.white;

            this.DrawCompactVitals(
                new Rect(rect.x + 8f, rect.y + 72f, rect.width - 16f, 72f),
                patient.Vitals,
                4);

            if (Widgets.ButtonInvisible(rect) && state != null)
            {
                state.SelectedPatientThingID = patient.PatientThingID;
                state.SelectedPatientKey = patient.PatientKey;
                state.DetailScroll = Vector2.zero;
            }

            this.text.AddTooltip(rect, this.BuildPatientTooltip(patient));
        }

        private void DrawCompactVitals(
            Rect rect,
            List<V3MedicalVitalLineModel> vitals,
            int maxCount)
        {
            if (vitals == null || vitals.Count == 0)
            {
                return;
            }

            int count = Mathf.Min(Mathf.Max(1, maxCount), vitals.Count);
            float rowHeight = Mathf.Max(18f, rect.height / count);
            for (int i = 0; i < count; i++)
            {
                Rect rowRect = new Rect(rect.x, rect.y + (i * rowHeight), rect.width, rowHeight);
                this.DrawVitalLine(rowRect, vitals[i]);
            }
        }

        private void DrawVitalLine(Rect rect, V3MedicalVitalLineModel vital)
        {
            if (vital == null)
            {
                return;
            }

            Text.Font = GameFont.Tiny;
            Rect labelRect = new Rect(rect.x, rect.y, 46f, rect.height);
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                labelRect,
                vital.Label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                vital.Label,
                TextAnchor.MiddleLeft);
            Rect meterRect = new Rect(labelRect.xMax + 5f, rect.y + Mathf.Max(2f, (rect.height - 8f) * 0.5f), rect.width - 105f, 8f);
            Color color = vital.Value01 >= 0f
                ? this.text.GetSeverityColor(vital.SeverityKey)
                : ShuttleUIStyle.MutedTextColor;
            this.panel.DrawMeter(meterRect, vital.Value01 >= 0f ? vital.Value01 : 0f, color);
            GUI.color = color;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.xMax - 44f, rect.y, 42f, rect.height),
                vital.ValueText,
                GameFont.Tiny,
                GameFont.Tiny,
                color,
                vital.Label + ": " + vital.ValueText,
                TextAnchor.MiddleLeft);
            GUI.color = Color.white;
        }

        private string BuildPatientTooltip(V3MedicalPatientCardModel patient)
        {
            if (patient == null)
            {
                return string.Empty;
            }

            string tooltip = this.text.ValueOrDash(patient.Label) + "\n" +
                this.text.GetKindLabel(patient) + " / " +
                this.text.GetStateLabel(patient) + "\n" +
                this.text.GetTriageLabel(patient);
            if (!string.IsNullOrEmpty(patient.ActiveProcedureTooltip))
            {
                tooltip += "\n" + patient.ActiveProcedureTooltip;
            }

            return tooltip;
        }

        private string BuildOccupantTooltip(V3MedicalOccupantCardModel occupant)
        {
            if (occupant == null)
            {
                return string.Empty;
            }

            string tooltip = this.text.ValueOrDash(occupant.Label) + "\n" +
                this.text.ValueOrDash(occupant.RoleLabel) + " / " +
                this.text.ValueOrDash(occupant.CurrentStatusLabel);
            if (!string.IsNullOrEmpty(occupant.ActivityLabel))
            {
                tooltip += "\n" + occupant.ActivityLabel;
            }

            return tooltip;
        }

        private string GetOccupantFallbackIconText(V3MedicalOccupantCardModel occupant)
        {
            if (occupant == null || string.IsNullOrEmpty(occupant.RoleLabel))
            {
                return "?";
            }

            return occupant.RoleLabel.Substring(0, 1);
        }

        private static bool IsSamePatient(
            V3MedicalPatientCardModel left,
            V3MedicalPatientCardModel right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            if (left.PatientThingID > 0 && right.PatientThingID > 0)
            {
                return left.PatientThingID == right.PatientThingID;
            }

            return !string.IsNullOrEmpty(left.PatientKey) && left.PatientKey == right.PatientKey;
        }
    }
}
