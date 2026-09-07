using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewCardPresentationBuilder
    {
        private readonly V3CrewClassificationFormatter classificationFormatter;
        private readonly V3CrewNeedIssueModelBuilder needIssueBuilder;
        private readonly V3CrewPawnHealthIssueBuilder healthIssueBuilder;

        internal V3CrewCardPresentationBuilder(
            V3CrewClassificationFormatter classificationFormatter,
            V3CrewNeedIssueModelBuilder needIssueBuilder,
            V3CrewPawnHealthIssueBuilder healthIssueBuilder)
        {
            this.classificationFormatter = classificationFormatter;
            this.needIssueBuilder = needIssueBuilder;
            this.healthIssueBuilder = healthIssueBuilder;
        }

        internal void FinalizeCrewCard(V3CrewCardModel card, Pawn pawn)
        {
            this.FinalizeCrewCard(card, pawn, null);
        }

        internal void FinalizeCrewCard(
            V3CrewCardModel card,
            Pawn pawn,
            ShuttlePawnDynamicStatusRecord dynamicStatus)
        {
            if (card == null)
            {
                return;
            }

            card.TitleLabel = this.GetPawnTitleLabel(pawn);
            card.IdentityLabel = this.BuildIdentityLabel(card, pawn);
            card.CurrentActivityLabel =
                ShuttleUIText.Tr("CT_ShuttleCrew_CurrentPrefix") +
                this.classificationFormatter.GetDisplayActivityLabel(card);

            if (string.IsNullOrEmpty(card.CompartmentLabel))
            {
                card.CompartmentLabel = this.classificationFormatter.GetCompartmentLabel(card);
            }

            card.CompartmentTooltip =
                ShuttleUIText.Tr("CT_ShuttleCrew_CompartmentPrefix") +
                card.CompartmentLabel;
            this.needIssueBuilder.AddNeedIssues(card);
            this.healthIssueBuilder.AddPawnHealthIssues(
                card.Issues,
                pawn,
                dynamicStatus);
            this.AddAssignmentIssue(card);

            if (card.Issues.Count > 1)
            {
                card.Issues.Sort(V3CrewIssueUtility.Compare);
            }

            card.IssueTooltip = V3CrewIssueUtility.BuildIssueTooltip(card);
            card.RiskLevel = V3CrewVisuals.ResolveRisk(
                card.Issues,
                card.DisplayThing != null ||
                    card.FoodPct >= 0f ||
                    card.RestPct >= 0f ||
                    card.MoodPct >= 0f ||
                    card.JoyPct >= 0f ||
                    card.EnergyPct >= 0f);
        }

        private void AddAssignmentIssue(V3CrewCardModel card)
        {
            if (card == null || card.SourceKind != V3CrewCardSourceKind.Unknown)
            {
                return;
            }

            V3CrewIssueUtility.AddIssue(
                card.Issues,
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_Unassigned"),
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_UnassignedSummary"),
                V3CrewIssueSeverity.Medium,
                V3CrewIssueUtility.AssignmentPriority);
        }

        private string BuildIdentityLabel(V3CrewCardModel card, Pawn pawn)
        {
            string category = card != null && !string.IsNullOrEmpty(card.CategoryLabel)
                ? card.CategoryLabel
                : ShuttleUIText.Tr("CT_ShuttleCrew_Member");
            string age = this.GetAgeLabel(pawn);
            return !string.IsNullOrEmpty(age) ? category + " - " + age : category;
        }

        private string GetPawnTitleLabel(Pawn pawn)
        {
            if (pawn != null &&
                pawn.story != null &&
                !string.IsNullOrEmpty(pawn.story.TitleCap))
            {
                return pawn.story.TitleCap;
            }

            return string.Empty;
        }

        private string GetAgeLabel(Pawn pawn)
        {
            if (pawn == null || pawn.ageTracker == null)
            {
                return string.Empty;
            }

            return pawn.ageTracker.Adult
                ? ShuttleUIText.Tr("CT_ShuttleCrew_Adult")
                : ShuttleUIText.Tr("CT_ShuttleCrew_Juvenile");
        }
    }
}
