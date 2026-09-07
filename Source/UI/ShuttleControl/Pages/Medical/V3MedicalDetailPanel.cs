using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalDetailPanel
    {
        private const float DetailRowHeight = 48f;
        private const float PanelContentTopOffset = 44f;

        private readonly V3MedicalText text;
        private readonly V3MedicalPanelDrawer panel;

        internal V3MedicalDetailPanel(V3MedicalText text)
        {
            this.text = text;
            this.panel = new V3MedicalPanelDrawer(text);
        }

        internal void Draw(
            Rect rect,
            V3MedicalPatientCardModel selectedPatient,
            V3MedicalPageState state)
        {
            this.panel.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_Medical_Overview"));
            Rect inner = this.panel.GetPanelInnerRect(rect, PanelContentTopOffset);
            if (selectedPatient == null)
            {
                this.panel.DrawEmptyPanelMessage(inner, this.text.Tr("CT_Shuttle_Medical_NoPatientSelected"));
                return;
            }

            Rect summaryRect = new Rect(inner.x, inner.y, inner.width, 156f);
            this.DrawSummary(summaryRect, selectedPatient);
            Rect listRect = new Rect(
                inner.x,
                summaryRect.yMax + 8f,
                inner.width,
                Mathf.Max(0f, inner.yMax - summaryRect.yMax - 8f));
            this.DrawDetailList(listRect, selectedPatient, state);
        }

        private void DrawSummary(Rect rect, V3MedicalPatientCardModel patient)
        {
            this.panel.DrawCardBackground(rect, false, false, ShuttleUIStyle.LeftColumnCardColor);
            Rect portraitRect = new Rect(rect.x + 12f, rect.y + 10f, 56f, 56f);
            this.text.DrawThingIcon(
                portraitRect,
                patient.DisplayThing,
                this.text.GetPatientFallbackIconText(patient),
                patient.Label);

            float statusWidth = Mathf.Min(118f, Mathf.Max(92f, rect.width * 0.30f));
            Rect statusRect = new Rect(rect.xMax - statusWidth - 12f, rect.y + 8f, statusWidth, 48f);
            float infoX = portraitRect.xMax + 12f;
            Rect infoRect = new Rect(infoX, rect.y + 8f, statusRect.x - infoX - 8f, 76f);
            Text.Font = GameFont.Small;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(infoRect.x, infoRect.y, infoRect.width, 22f),
                patient.Label,
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                patient.Label,
                TextAnchor.MiddleLeft);
            Text.Font = GameFont.Tiny;
            GUI.color = this.text.GetPatientKindTextColor(patient);
            ShuttleUILayout.SafeLabel(new Rect(infoRect.x, infoRect.y + 25f, infoRect.width, 18f), this.text.GetKindLabel(patient));
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(infoRect.x, infoRect.y + 43f, infoRect.width, 18f),
                patient.LocationText,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                patient.LocationText,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.SafeLabel(new Rect(infoRect.x, infoRect.y + 61f, infoRect.width, 18f), this.text.GetStateLabel(patient));
            GUI.color = Color.white;

            this.panel.DrawStatusBadge(
                new Rect(statusRect.x, statusRect.y, statusRect.width, 22f),
                this.text.GetTriageLabel(patient),
                this.text.GetSeverityColor(patient.TriageKey));
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(statusRect.x, statusRect.y + 26f, statusRect.width, 18f),
                this.text.FitLabelText(patient.MedicineNeedText, statusRect.width));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            this.DrawVitalGrid(
                new Rect(rect.x + 10f, rect.y + 100f, rect.width - 20f, 48f),
                patient.Vitals);
        }

        private void DrawVitalGrid(Rect rect, List<V3MedicalVitalLineModel> vitals)
        {
            if (vitals == null || vitals.Count == 0)
            {
                return;
            }

            int count = vitals.Count;
            int columns = rect.width >= 260f ? 2 : 1;
            int rows = Mathf.CeilToInt(count / (float)columns);
            float gap = 8f;
            float rowHeight = Mathf.Max(15f, rect.height / Mathf.Max(1, rows));
            float columnWidth = columns > 1 ? (rect.width - gap) * 0.5f : rect.width;
            for (int i = 0; i < count; i++)
            {
                int column = columns > 1 ? i % columns : 0;
                int row = columns > 1 ? i / columns : i;
                Rect rowRect = new Rect(
                    rect.x + (column * (columnWidth + gap)),
                    rect.y + (row * rowHeight),
                    columnWidth,
                    rowHeight);
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

        private void DrawDetailList(
            Rect rect,
            V3MedicalPatientCardModel patient,
            V3MedicalPageState state)
        {
            this.panel.DrawCardBackground(rect, false, false, ShuttleUIStyle.RightBottomCardColor);
            Text.Font = GameFont.Small;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 20f),
                this.text.Tr("CT_Shuttle_Medical_HediffDetails"));
            Rect listRect = new Rect(rect.x + 8f, rect.y + 32f, rect.width - 16f, Mathf.Max(0f, rect.height - 40f));
            List<V3MedicalDetailLineModel> details = patient != null ? patient.Details : null;
            if (details == null || details.Count == 0)
            {
                GUI.color = ShuttleUIStyle.MutedTextColor;
                Text.Font = GameFont.Tiny;
                ShuttleUILayout.SafeLabel(listRect, this.text.Tr("CT_Shuttle_Medical_Unavailable"));
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                listRect.width - 16f,
                Mathf.Max(listRect.height, details.Count * DetailRowHeight));
            Widgets.BeginScrollView(listRect, ref state.DetailScroll, viewRect);
            for (int i = 0; i < details.Count; i++)
            {
                Rect rowRect = new Rect(0f, i * DetailRowHeight, viewRect.width, DetailRowHeight - 5f);
                this.DrawDetailRow(rowRect, details[i]);
            }

            Widgets.EndScrollView();
            Text.Font = GameFont.Small;
        }

        private void DrawDetailRow(Rect rect, V3MedicalDetailLineModel detail)
        {
            if (detail == null)
            {
                return;
            }

            Color color = this.text.GetSeverityColor(detail.SeverityKey);
            this.panel.DrawCardBackground(
                rect,
                false,
                false,
                new Color(0.078f, 0.094f, 0.118f, 0.92f));
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 3f, rect.height), color);
            Text.Font = GameFont.Tiny;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 8f, rect.y + 4f, rect.width - 92f, 20f),
                detail.Label,
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                detail.Label,
                TextAnchor.MiddleLeft);
            GUI.color = detail.IsPlaceholder
                ? ShuttleUIStyle.MutedTextColor
                : detail.PendingTreatment ? V3MedicalText.YellowColor : ShuttleUIStyle.MutedTextColor;
            string statusText = detail.IsPlaceholder
                ? this.text.Tr("CT_Shuttle_Medical_Unavailable")
                : detail.PendingTreatment
                    ? this.text.Tr("CT_Shuttle_UI_Status_Pending")
                    : this.text.Tr("CT_Shuttle_UI_Status_Logged");
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.xMax - 82f, rect.y + 4f, 76f, 20f),
                statusText,
                GameFont.Tiny,
                GameFont.Tiny,
                GUI.color,
                statusText,
                TextAnchor.MiddleRight);
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 8f, rect.y + 26f, rect.width - 16f, 18f),
                detail.Summary,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                detail.Summary,
                TextAnchor.MiddleLeft);
            GUI.color = Color.white;
            this.text.AddTooltip(
                rect,
                this.text.ValueOrDash(detail.Label) + "\n" +
                this.text.ValueOrDash(detail.Summary));
        }
    }
}
