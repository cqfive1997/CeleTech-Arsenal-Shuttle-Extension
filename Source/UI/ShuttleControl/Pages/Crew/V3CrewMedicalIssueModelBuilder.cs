using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewMedicalIssueModelBuilder
    {
        private const float SevereHediffSeverityThreshold = 0.45f;

        internal void AddPatientIssues(
            V3CrewCardModel card,
            ShuttleMedicalPatientReadModel patient)
        {
            if (card == null || patient == null)
            {
                return;
            }

            if (patient.HasMedicalEmergency)
            {
                V3CrewIssueUtility.AddIssue(
                    card.Issues,
                    ShuttleUIText.Tr("CT_ShuttleCrew_Issue_MedicalEmergency"),
                    ShuttleUIText.Tr("CT_ShuttleCrew_Issue_MedicalEmergencySummary"),
                    V3CrewIssueSeverity.Severe,
                    V3CrewIssueUtility.SevereHealthPriority);
            }

            if (patient.IsBleeding || patient.NeedsTend || patient.HasUntendedInjury)
            {
                V3CrewIssueUtility.AddIssue(
                    card.Issues,
                    ShuttleUIText.Tr("CT_ShuttleCrew_Issue_NeedsTreatment"),
                    ShuttleUIText.Tr("CT_ShuttleCrew_Issue_NeedsTreatmentSummary"),
                    patient.IsBleeding ? V3CrewIssueSeverity.Severe : V3CrewIssueSeverity.Medium,
                    V3CrewIssueUtility.SevereHealthPriority + 4);
            }
        }

        internal void AddMedicalReadModelIssues(
            List<V3CrewIssueModel> issues,
            IReadOnlyList<ShuttleMedicalHediffReadModel> hediffs)
        {
            if (issues == null || hediffs == null)
            {
                return;
            }

            for (int i = 0; i < hediffs.Count; i++)
            {
                ShuttleMedicalHediffReadModel hediff = hediffs[i];
                if (hediff == null || string.IsNullOrEmpty(hediff.Label))
                {
                    continue;
                }

                V3CrewIssueSeverity severity = this.ResolveSeverity(hediff);
                V3CrewIssueUtility.AddIssue(
                    issues,
                    hediff.Label,
                    this.GetSummary(hediff),
                    severity,
                    this.ResolvePriority(hediff, severity));
            }
        }

        private V3CrewIssueSeverity ResolveSeverity(ShuttleMedicalHediffReadModel hediff)
        {
            if (hediff == null)
            {
                return V3CrewIssueSeverity.Info;
            }

            if (hediff.Bleeding || (hediff.Tendable && !hediff.Tended))
            {
                return V3CrewIssueSeverity.Medium;
            }

            return hediff.Severity >= SevereHediffSeverityThreshold
                ? V3CrewIssueSeverity.Medium
                : V3CrewIssueSeverity.Low;
        }

        private int ResolvePriority(
            ShuttleMedicalHediffReadModel hediff,
            V3CrewIssueSeverity severity)
        {
            if (severity == V3CrewIssueSeverity.Severe)
            {
                return V3CrewIssueUtility.SevereHealthPriority + 1;
            }

            if (hediff != null && (hediff.Bleeding || (hediff.Tendable && !hediff.Tended)))
            {
                return V3CrewIssueUtility.SevereHealthPriority + 6;
            }

            return V3CrewIssueUtility.SevereHealthPriority + 12;
        }

        private string GetSummary(ShuttleMedicalHediffReadModel hediff)
        {
            if (hediff == null)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_Issue_HealthConditionSummary");
            }

            if (!string.IsNullOrEmpty(hediff.SummaryLabel))
            {
                return hediff.SummaryLabel;
            }

            if (hediff.Bleeding)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_Issue_HealthBleedingSummary");
            }

            if (hediff.Tendable && !hediff.Tended)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_Issue_HealthUntendedSummary");
            }

            if (hediff.Tended)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_Issue_HealthTendedSummary");
            }

            return ShuttleUIText.Tr("CT_ShuttleCrew_Issue_HealthConditionSummary");
        }
    }
}
