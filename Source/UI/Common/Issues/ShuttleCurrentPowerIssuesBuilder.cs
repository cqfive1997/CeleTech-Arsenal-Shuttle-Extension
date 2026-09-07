using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleCurrentPowerIssuesBuilder
    {
        internal void AddIssues(
            ShuttleControlReadModel model,
            List<ShuttleIssueReadModel> issues)
        {
            if (model == null || issues == null)
            {
                return;
            }

            float demandWatts = Mathf.Max(0f, model.ActiveInternalDemandWatts);
            float unmetWatts = Mathf.Max(0f, model.UnmetInternalDemandWatts);
            if (demandWatts > 0.1f && !model.InternalBusPowered)
            {
                ShuttleIssueReadModel issue = new ShuttleIssueReadModel();
                issue.StableId = "power:internal-bus-offline";
                issue.Severity = ShuttleIssueSeverity.Error;
                issue.Category = ShuttleIssueCategory.Power;
                issue.Title = "CT_Shuttle_Issue_PowerBusOffline_Title".Translate().ToString();
                issue.Detail = "CT_Shuttle_Issue_PowerBusOffline_Detail".Translate().ToString();
                issue.ActionKind = ShuttleIssueActionKind.OpenAssembly;
                issue.ActionLabel =
                    ShuttleIssueActionResolver.ResolveActionLabel(issue.ActionKind);
                issue.NavigationTarget = new ShuttleIssueNavigationTarget();
                issue.SortPriority = 20;
                issue.Evidence = new List<ShuttleIssueEvidenceLine>();
                issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_Demand",
                    demandWatts.ToString("0.#") + " W"));
                ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
                return;
            }

            if (unmetWatts > 0.1f)
            {
                ShuttleIssueReadModel issue = new ShuttleIssueReadModel();
                issue.StableId = "power:unmet-internal-demand";
                issue.Severity = ShuttleIssueSeverity.Warning;
                issue.Category = ShuttleIssueCategory.Power;
                issue.Title = "CT_Shuttle_Issue_InternalPowerInsufficient_Title".Translate().ToString();
                issue.Detail = "CT_Shuttle_Issue_InternalPowerInsufficient_Detail".Translate().ToString();
                issue.ActionKind = ShuttleIssueActionKind.OpenAssembly;
                issue.ActionLabel =
                    ShuttleIssueActionResolver.ResolveActionLabel(issue.ActionKind);
                issue.NavigationTarget = new ShuttleIssueNavigationTarget();
                issue.SortPriority = 40;
                issue.Evidence = new List<ShuttleIssueEvidenceLine>();
                issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_UnmetDemand",
                    unmetWatts.ToString("0.#") + " W"));
                issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_Demand",
                    demandWatts.ToString("0.#") + " W"));
                ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
            }
        }
    }
}
