using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalPatientDetailModelBuilder
    {
        private readonly V3MedicalReadModelFormatter formatter;

        internal V3MedicalPatientDetailModelBuilder(
            V3MedicalReadModelFormatter formatter)
        {
            this.formatter = formatter;
        }

        internal void BuildDetails(
            V3MedicalPatientCardModel patient,
            ShuttleMedicalPatientReadModel source,
            Pawn pawn)
        {
            if (patient == null || source == null)
            {
                return;
            }

            this.AddReadModelHediffs(patient, source);
            this.AddDirectPawnHediffDetails(patient, pawn);

            if (patient.Details.Count == 0)
            {
                this.AddPlaceholderDetail(patient);
            }
        }

        private void AddReadModelHediffs(
            V3MedicalPatientCardModel patient,
            ShuttleMedicalPatientReadModel source)
        {
            if (source.Hediffs == null)
            {
                return;
            }

            for (int i = 0; i < source.Hediffs.Count; i++)
            {
                ShuttleMedicalHediffReadModel hediff = source.Hediffs[i];
                if (hediff == null)
                {
                    continue;
                }

                V3MedicalDetailLineModel line = new V3MedicalDetailLineModel();
                line.Label = !string.IsNullOrEmpty(hediff.Label)
                    ? hediff.Label
                    : ShuttleUIText.Tr("CT_Shuttle_Medical_Condition_Unnamed");
                line.Summary = !string.IsNullOrEmpty(hediff.SummaryLabel)
                    ? hediff.SummaryLabel
                    : this.formatter.GetHediffFallbackSummary(hediff);
                line.PendingTreatment = hediff.Tendable && !hediff.Tended;
                line.SeverityKey = line.PendingTreatment
                    ? "Severe"
                    : hediff.Bleeding ? "Moderate" : "Stable";
                patient.Details.Add(line);
            }
        }

        private void AddDirectPawnHediffDetails(
            V3MedicalPatientCardModel patient,
            Pawn pawn)
        {
            if (patient == null ||
                pawn == null ||
                pawn.health == null ||
                pawn.health.hediffSet == null ||
                pawn.health.hediffSet.hediffs == null)
            {
                return;
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                this.AddDirectPawnHediffDetail(patient, hediffs[i]);
            }
        }

        private void AddDirectPawnHediffDetail(
            V3MedicalPatientCardModel patient,
            Hediff hediff)
        {
            if (!this.ShouldShowDirectHediff(hediff))
            {
                return;
            }

            string label = this.GetDirectHediffLabel(hediff);
            if (string.IsNullOrEmpty(label) || this.HasDetailLabel(patient, label))
            {
                return;
            }

            bool pendingTreatment =
                hediff.TendableNow(false) && !this.IsDirectHediffTended(hediff);
            V3MedicalDetailLineModel line = new V3MedicalDetailLineModel();
            line.Label = label;
            line.Summary = this.BuildDirectHediffSummary(
                hediff,
                label,
                pendingTreatment);
            line.PendingTreatment = pendingTreatment;
            line.IsPlaceholder = false;
            line.SeverityKey = pendingTreatment || hediff.Bleeding
                ? "Severe"
                : hediff.Severity >= 0.45f ? "Moderate" : "Stable";
            patient.Details.Add(line);
        }

        private bool ShouldShowDirectHediff(Hediff hediff)
        {
            if (hediff == null)
            {
                return false;
            }

            if (hediff.Bleeding ||
                hediff.TendableNow(false) ||
                hediff is Hediff_Injury ||
                this.IsDirectInfectionLikeHediff(hediff) ||
                this.HasDirectHediffDefName(hediff, "Heatstroke") ||
                this.HasDirectHediffDefName(hediff, "BloodLoss"))
            {
                return true;
            }

            return hediff.Severity > 0.01f;
        }

        private string GetDirectHediffLabel(Hediff hediff)
        {
            if (hediff == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(hediff.LabelCap))
            {
                return hediff.LabelCap;
            }

            return !string.IsNullOrEmpty(hediff.Label) ? hediff.Label : string.Empty;
        }

        private string BuildDirectHediffSummary(
            Hediff hediff,
            string label,
            bool pendingTreatment)
        {
            List<string> tags = new List<string>();
            if (hediff != null && hediff.Bleeding)
            {
                tags.Add(ShuttleUIText.Tr("CT_Shuttle_MedicalBay_HediffBleeding"));
            }

            if (hediff != null && this.IsDirectHediffTended(hediff))
            {
                tags.Add(ShuttleUIText.Tr("CT_Shuttle_MedicalBay_HediffTended"));
            }
            else if (pendingTreatment)
            {
                tags.Add(ShuttleUIText.Tr("CT_Shuttle_MedicalBay_HediffUntended"));
            }

            string partLabel = this.GetDirectHediffPartLabel(hediff);
            if (!string.IsNullOrEmpty(partLabel))
            {
                tags.Add(partLabel);
            }

            return tags.Count > 0
                ? label + " (" + string.Join(", ", tags.ToArray()) + ")"
                : label;
        }

        private string GetDirectHediffPartLabel(Hediff hediff)
        {
            if (hediff == null || hediff.Part == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(hediff.Part.LabelCap))
            {
                return hediff.Part.LabelCap;
            }

            return !string.IsNullOrEmpty(hediff.Part.Label) ? hediff.Part.Label : string.Empty;
        }

        private bool IsDirectHediffTended(Hediff hediff)
        {
            HediffWithComps withComps = hediff as HediffWithComps;
            HediffComp_TendDuration tendComp = withComps != null
                ? withComps.GetComp<HediffComp_TendDuration>()
                : null;
            return tendComp != null && tendComp.IsTended;
        }

        private bool IsDirectInfectionLikeHediff(Hediff hediff)
        {
            HediffWithComps withComps = hediff as HediffWithComps;
            return withComps != null &&
                withComps.GetComp<HediffComp_Immunizable>() != null;
        }

        private bool HasDirectHediffDefName(Hediff hediff, string defName)
        {
            return hediff != null &&
                hediff.def != null &&
                hediff.def.defName == defName;
        }

        private bool HasDetailLabel(V3MedicalPatientCardModel patient, string label)
        {
            if (patient == null || patient.Details == null || string.IsNullOrEmpty(label))
            {
                return false;
            }

            for (int i = 0; i < patient.Details.Count; i++)
            {
                V3MedicalDetailLineModel detail = patient.Details[i];
                if (detail != null &&
                    string.Equals(detail.Label, label, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void AddPlaceholderDetail(V3MedicalPatientCardModel patient)
        {
            V3MedicalDetailLineModel line = new V3MedicalDetailLineModel();
            line.Label = ShuttleUIText.Tr("CT_Shuttle_Medical_Detail_NoDetails");
            line.Summary = ShuttleUIText.Tr("CT_Shuttle_Medical_Detail_NoAdditionalDetails");
            line.PendingTreatment = false;
            line.IsPlaceholder = true;
            line.SeverityKey = "Stable";
            patient.Details.Add(line);
        }

    }
}
