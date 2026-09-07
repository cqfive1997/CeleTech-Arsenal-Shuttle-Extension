using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewPawnHealthIssueBuilder
    {
        private const float SevereBleedRateThreshold = 0.40f;
        private const float MediumPainThreshold = 0.30f;
        private const float SeverePainThreshold = 0.75f;
        private const float MediumConsciousnessThreshold = 0.75f;
        private const float SevereConsciousnessThreshold = 0.45f;
        private const float SevereHediffSeverityThreshold = 0.45f;

        internal void AddPawnHealthIssues(List<V3CrewIssueModel> issues, Pawn pawn)
        {
            this.AddPawnHealthIssues(issues, pawn, null);
        }

        internal void AddPawnHealthIssues(
            List<V3CrewIssueModel> issues,
            Pawn pawn,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            if (issues == null ||
                (pawn == null && dynamicStatus == null))
            {
                return;
            }

            this.AddDownedIssue(issues, pawn, dynamicStatus);
            this.AddConsciousnessIssue(issues, pawn, dynamicStatus);
            this.AddBleedingIssue(issues, pawn, dynamicStatus);
            this.AddPainIssue(issues, pawn, dynamicStatus);
            if (pawn != null &&
                pawn.health != null &&
                pawn.health.hediffSet != null)
            {
                this.AddHediffIssues(issues, pawn.health.hediffSet.hediffs);
            }
        }

        private void AddHediffIssues(List<V3CrewIssueModel> issues, List<Hediff> hediffs)
        {
            if (hediffs == null)
            {
                return;
            }

            for (int i = 0; i < hediffs.Count; i++)
            {
                this.AddHediffIssue(issues, hediffs[i]);
            }
        }

        private void AddDownedIssue(
            List<V3CrewIssueModel> issues,
            Pawn pawn,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            bool isDowned = dynamicStatus != null
                ? dynamicStatus.IsDowned
                : pawn != null && pawn.Downed;
            if (!isDowned)
            {
                return;
            }

            V3CrewIssueUtility.AddIssue(
                issues,
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_Downed"),
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_DownedSummary"),
                V3CrewIssueSeverity.Severe,
                V3CrewIssueUtility.SevereHealthPriority - 4);
        }

        private void AddConsciousnessIssue(
            List<V3CrewIssueModel> issues,
            Pawn pawn,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            float consciousness = dynamicStatus != null &&
                dynamicStatus.ConsciousnessPct >= 0f
                    ? dynamicStatus.ConsciousnessPct
                    : this.GetCapacityPct(pawn, PawnCapacityDefOf.Consciousness);
            if (consciousness < 0f || consciousness >= MediumConsciousnessThreshold)
            {
                return;
            }

            V3CrewIssueUtility.AddIssue(
                issues,
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_Consciousness"),
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_ConsciousnessSummary"),
                consciousness <= SevereConsciousnessThreshold
                    ? V3CrewIssueSeverity.Severe
                    : V3CrewIssueSeverity.Medium,
                V3CrewIssueUtility.SevereHealthPriority - 2);
        }

        private void AddBleedingIssue(
            List<V3CrewIssueModel> issues,
            Pawn pawn,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            float bleedRate = dynamicStatus != null
                ? dynamicStatus.BleedRateTotal
                : this.GetBleedRateTotal(pawn);
            bool isBleeding = dynamicStatus != null
                ? dynamicStatus.IsBleeding
                : bleedRate > 0f;
            if (!isBleeding && bleedRate <= 0f)
            {
                return;
            }

            V3CrewIssueUtility.AddIssue(
                issues,
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_BloodLoss"),
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_BloodLossSummary"),
                bleedRate >= SevereBleedRateThreshold
                    ? V3CrewIssueSeverity.Severe
                    : V3CrewIssueSeverity.Medium,
                V3CrewIssueUtility.SevereHealthPriority);
        }

        private void AddPainIssue(
            List<V3CrewIssueModel> issues,
            Pawn pawn,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            float pain = dynamicStatus != null && dynamicStatus.PainPct >= 0f
                ? dynamicStatus.PainPct
                : this.GetPainPct(pawn);
            if (pain < MediumPainThreshold)
            {
                return;
            }

            V3CrewIssueUtility.AddIssue(
                issues,
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_Pain"),
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_PainSummary"),
                pain >= SeverePainThreshold
                    ? V3CrewIssueSeverity.Severe
                    : V3CrewIssueSeverity.Medium,
                V3CrewIssueUtility.SevereHealthPriority + 8);
        }

        private float GetCapacityPct(Pawn pawn, PawnCapacityDef capacityDef)
        {
            if (pawn == null ||
                pawn.health == null ||
                pawn.health.capacities == null ||
                capacityDef == null)
            {
                return -1f;
            }

            return pawn.health.capacities.GetLevel(capacityDef);
        }

        private float GetBleedRateTotal(Pawn pawn)
        {
            return pawn != null &&
                pawn.health != null &&
                pawn.health.hediffSet != null
                    ? pawn.health.hediffSet.BleedRateTotal
                    : 0f;
        }

        private float GetPainPct(Pawn pawn)
        {
            return pawn != null &&
                pawn.health != null &&
                pawn.health.hediffSet != null
                    ? pawn.health.hediffSet.PainTotal
                    : -1f;
        }

        private void AddHediffIssue(List<V3CrewIssueModel> issues, Hediff hediff)
        {
            if (hediff == null || !this.ShouldShowHediffIssue(hediff))
            {
                return;
            }

            string label = this.GetHediffLabel(hediff);
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            bool untended = this.IsHediffTendable(hediff) && !this.IsHediffTended(hediff);
            V3CrewIssueSeverity severity = this.ResolveHediffSeverity(hediff, untended);
            V3CrewIssueUtility.AddIssue(
                issues,
                label,
                this.GetHediffSummary(hediff, untended),
                severity,
                this.ResolveHediffPriority(hediff, untended, severity));
        }

        private bool ShouldShowHediffIssue(Hediff hediff)
        {
            if (hediff == null)
            {
                return false;
            }

            if (this.IsHeatstroke(hediff) ||
                this.IsBloodLoss(hediff) ||
                this.IsInfectionLikeHediff(hediff) ||
                hediff.Bleeding ||
                this.IsHediffTendable(hediff) ||
                hediff is Hediff_Injury)
            {
                return true;
            }

            return hediff.Severity > 0.01f;
        }

        private V3CrewIssueSeverity ResolveHediffSeverity(Hediff hediff, bool untended)
        {
            if (hediff == null)
            {
                return V3CrewIssueSeverity.Info;
            }

            if (hediff.Bleeding || this.IsBloodLoss(hediff))
            {
                return hediff.BleedRate >= SevereBleedRateThreshold ||
                    hediff.Severity >= SevereHediffSeverityThreshold
                        ? V3CrewIssueSeverity.Severe
                        : V3CrewIssueSeverity.Medium;
            }

            if (this.IsHeatstroke(hediff) &&
                hediff.Severity >= SevereHediffSeverityThreshold)
            {
                return V3CrewIssueSeverity.Severe;
            }

            if (untended || this.IsInfectionLikeHediff(hediff))
            {
                return V3CrewIssueSeverity.Medium;
            }

            return hediff.Severity >= SevereHediffSeverityThreshold
                ? V3CrewIssueSeverity.Medium
                : V3CrewIssueSeverity.Low;
        }

        private int ResolveHediffPriority(
            Hediff hediff,
            bool untended,
            V3CrewIssueSeverity severity)
        {
            if (severity == V3CrewIssueSeverity.Severe)
            {
                return V3CrewIssueUtility.SevereHealthPriority + 1;
            }

            if (hediff != null && (this.IsHeatstroke(hediff) || this.IsInfectionLikeHediff(hediff)))
            {
                return V3CrewIssueUtility.SevereHealthPriority + 5;
            }

            if (untended || hediff is Hediff_Injury)
            {
                return V3CrewIssueUtility.SevereHealthPriority + 6;
            }

            return V3CrewIssueUtility.SevereHealthPriority + 12;
        }

        private string GetHediffSummary(Hediff hediff, bool untended)
        {
            if (hediff == null)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_Issue_HealthConditionSummary");
            }

            if (hediff.Bleeding || this.IsBloodLoss(hediff))
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_Issue_HealthBleedingSummary");
            }

            if (untended)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_Issue_HealthUntendedSummary");
            }

            if (this.IsHediffTended(hediff))
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_Issue_HealthTendedSummary");
            }

            return ShuttleUIText.Tr("CT_ShuttleCrew_Issue_HealthConditionSummary");
        }

        private bool IsHediffTendable(Hediff hediff)
        {
            return hediff != null && hediff.TendableNow(false);
        }

        private bool IsHediffTended(Hediff hediff)
        {
            HediffWithComps withComps = hediff as HediffWithComps;
            HediffComp_TendDuration tendComp = withComps != null
                ? withComps.GetComp<HediffComp_TendDuration>()
                : null;
            return tendComp != null && tendComp.IsTended;
        }

        private bool IsInfectionLikeHediff(Hediff hediff)
        {
            HediffWithComps withComps = hediff as HediffWithComps;
            return withComps != null && withComps.GetComp<HediffComp_Immunizable>() != null;
        }

        private bool IsHeatstroke(Hediff hediff)
        {
            return this.HasDefName(hediff, "Heatstroke");
        }

        private bool IsBloodLoss(Hediff hediff)
        {
            return this.HasDefName(hediff, "BloodLoss");
        }

        private bool HasDefName(Hediff hediff, string defName)
        {
            return hediff != null &&
                hediff.def != null &&
                hediff.def.defName == defName;
        }

        private string GetHediffLabel(Hediff hediff)
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
    }
}
