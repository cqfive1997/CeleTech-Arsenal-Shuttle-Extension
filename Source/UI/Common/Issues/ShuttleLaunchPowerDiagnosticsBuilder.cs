using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleLaunchPowerDiagnosticsBuilder
    {
        internal void AddDiagnostics(
            ShuttleControlReadModel model,
            List<ShuttleIssueReadModel> diagnostics)
        {
            if (model == null || diagnostics == null)
            {
                return;
            }

            float demandWatts = Mathf.Max(0f, model.ActiveInternalDemandWatts);
            float unmetWatts = Mathf.Max(0f, model.UnmetInternalDemandWatts);
            if (demandWatts > 0.1f && !model.InternalBusPowered)
            {
                ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                    "launch:power:internal-bus-offline",
                    ShuttleIssueSeverity.Warning,
                    ShuttleIssueCategory.Power,
                    "CT_Shuttle_Issue_LaunchPowerUnavailable_Title",
                    "CT_Shuttle_Issue_LaunchPowerUnavailable_Detail",
                    ShuttleIssueActionKind.OpenAssembly,
                    25);
                issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_Demand",
                    demandWatts.ToString("0.#") + " W"));
                ShuttleIssueReadModelSet.AddIssueIfUnique(diagnostics, issue);
                return;
            }

            if (unmetWatts > 0.1f)
            {
                ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                    "launch:power:unmet-internal-demand",
                    ShuttleIssueSeverity.Warning,
                    ShuttleIssueCategory.Power,
                    "CT_Shuttle_Issue_LaunchPowerInsufficient_Title",
                    "CT_Shuttle_Issue_LaunchPowerInsufficient_Detail",
                    ShuttleIssueActionKind.OpenAssembly,
                    35);
                issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_UnmetDemand",
                    unmetWatts.ToString("0.#") + " W"));
                ShuttleIssueReadModelSet.AddIssueIfUnique(diagnostics, issue);
            }
        }
    }
}
