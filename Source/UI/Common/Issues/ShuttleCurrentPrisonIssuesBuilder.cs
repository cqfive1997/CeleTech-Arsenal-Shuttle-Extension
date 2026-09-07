using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleCurrentPrisonIssuesBuilder
    {
        internal void AddIssues(
            ShuttleControlReadModel model,
            ShuttleFoodSupplyIssueSnapshot foodSupply,
            List<ShuttleIssueReadModel> issues)
        {
            if (model == null || model.PrisonCell == null || issues == null)
            {
                return;
            }

            ShuttlePrisonCellReadModel prison = model.PrisonCell;
            int prisonerCount = Mathf.Max(0, prison.PrisonerCount);
            if (prisonerCount <= 0 && prison.Prisoners != null)
            {
                prisonerCount = prison.Prisoners.Count;
            }

            if (prisonerCount <= 0)
            {
                return;
            }

            if (!prison.HasPrisonCell)
            {
                ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                    "prison:prisoners-without-cell",
                    ShuttleIssueSeverity.Error,
                    ShuttleIssueCategory.PrisonCell,
                    "CT_Shuttle_Issue_PrisonCellPrisonersWithoutModule_Title",
                    "CT_Shuttle_Issue_PrisonCellPrisonersWithoutModule_Detail",
                    ShuttleIssueActionKind.OpenAssembly,
                    150);
                issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_PrisonerCount",
                    prisonerCount.ToString()));
                ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
                return;
            }

            bool foodDepleted = this.AddPrisonFoodIssue(foodSupply, issues);
            this.AddPrisonCareIssues(prison, issues, foodDepleted);
        }

        private bool AddPrisonFoodIssue(
            ShuttleFoodSupplyIssueSnapshot foodSupply,
            List<ShuttleIssueReadModel> issues)
        {
            if (foodSupply == null || issues == null ||
                !foodSupply.PrisonerRelevant || !foodSupply.PrisonerAssessable)
            {
                return false;
            }

            string stableId;
            string titleKey;
            string detailKey;
            int sortPriority;
            bool depleted;
            if (foodSupply.PrisonerAvailableCount <= 0)
            {
                stableId = "prison:food-depleted";
                titleKey = "CT_Shuttle_Issue_PrisonFoodDepleted_Title";
                detailKey = "CT_Shuttle_Issue_PrisonFoodDepleted_Detail";
                sortPriority = 155;
                depleted = true;
            }
            else if (foodSupply.PrisonerAvailableCount < 10)
            {
                stableId = "prison:food-low";
                titleKey = "CT_Shuttle_Issue_PrisonFoodInsufficient_Title";
                detailKey = "CT_Shuttle_Issue_PrisonFoodInsufficient_Detail";
                sortPriority = 156;
                depleted = false;
            }
            else
            {
                return false;
            }

            ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                stableId,
                ShuttleIssueSeverity.Warning,
                ShuttleIssueCategory.PrisonCell,
                titleKey,
                detailKey,
                ShuttleIssueActionKind.OpenPrisonCell,
                sortPriority);
            issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                "CT_Shuttle_Issue_Evidence_AvailableFood",
                foodSupply.PrisonerAvailableCount.ToString()));
            ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
            return depleted;
        }

        private void AddPrisonCareIssues(
            ShuttlePrisonCellReadModel prison,
            List<ShuttleIssueReadModel> issues,
            bool suppressFoodBlockedIssue)
        {
            if (prison == null || prison.Prisoners == null)
            {
                return;
            }

            int feedingBlocked = 0;
            int tendingBlocked = 0;
            for (int i = 0; i < prison.Prisoners.Count; i++)
            {
                ShuttleHeldPrisonerReadModel prisoner = prison.Prisoners[i];
                if (prisoner == null || prisoner.IsDead)
                {
                    continue;
                }

                if (!prisoner.CareAvailabilityKnown)
                {
                    continue;
                }

                if (prisoner.NeedsFeeding && !prisoner.CanFeed)
                {
                    feedingBlocked++;
                }

                if (prisoner.NeedsTending && !prisoner.CanTend)
                {
                    tendingBlocked++;
                }
            }

            if (feedingBlocked > 0 && !suppressFoodBlockedIssue)
            {
                ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                    "prison:feeding-blocked",
                    ShuttleIssueSeverity.Warning,
                    ShuttleIssueCategory.PrisonCell,
                    "CT_Shuttle_Issue_PrisonCellFeedingBlocked_Title",
                    "CT_Shuttle_Issue_PrisonCellFeedingBlocked_Detail",
                    ShuttleIssueActionKind.OpenPrisonCell,
                    160);
                issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_PrisonerCount",
                    feedingBlocked.ToString()));
                ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
            }

            if (tendingBlocked > 0)
            {
                ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                    "prison:tending-blocked",
                    ShuttleIssueSeverity.Warning,
                    ShuttleIssueCategory.PrisonCell,
                    "CT_Shuttle_Issue_PrisonCellTendingBlocked_Title",
                    "CT_Shuttle_Issue_PrisonCellTendingBlocked_Detail",
                    ShuttleIssueActionKind.OpenPrisonCell,
                    170);
                issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_PrisonerCount",
                    tendingBlocked.ToString()));
                ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
            }
        }
    }
}
