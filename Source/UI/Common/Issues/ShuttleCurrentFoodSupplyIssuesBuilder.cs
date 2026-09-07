using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleCurrentFoodSupplyIssuesBuilder
    {
        private const int FoodLowThreshold = 10;

        private readonly ShuttleFoodSupplyIssueAnalyzer analyzer =
            new ShuttleFoodSupplyIssueAnalyzer();

        internal ShuttleFoodSupplyIssueSnapshot AddIssues(
            ShuttleControlReadModel model,
            List<ShuttleIssueReadModel> issues)
        {
            ShuttleFoodSupplyIssueSnapshot snapshot =
                this.analyzer.Analyze(model);
            if (snapshot == null || issues == null ||
                !snapshot.GeneralRelevant || !snapshot.GeneralAssessable)
            {
                return snapshot;
            }

            if (snapshot.GeneralAvailableCount <= 0)
            {
                this.AddFoodIssue(
                    issues,
                    "food:general-depleted",
                    "CT_Shuttle_Issue_FoodDepleted_Title",
                    "CT_Shuttle_Issue_FoodDepleted_Detail",
                    snapshot.GeneralAvailableCount,
                    145);
            }
            else if (snapshot.GeneralAvailableCount < FoodLowThreshold)
            {
                this.AddFoodIssue(
                    issues,
                    "food:general-low",
                    "CT_Shuttle_Issue_FoodInsufficient_Title",
                    "CT_Shuttle_Issue_FoodInsufficient_Detail",
                    snapshot.GeneralAvailableCount,
                    146);
            }

            return snapshot;
        }

        private void AddFoodIssue(
            List<ShuttleIssueReadModel> issues,
            string stableId,
            string titleKey,
            string detailKey,
            int availableCount,
            int sortPriority)
        {
            ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                stableId,
                ShuttleIssueSeverity.Warning,
                ShuttleIssueCategory.General,
                titleKey,
                detailKey,
                ShuttleIssueActionKind.OpenCargo,
                sortPriority);
            issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                "CT_Shuttle_Issue_Evidence_AvailableFood",
                availableCount.ToString()));
            ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
        }
    }
}
