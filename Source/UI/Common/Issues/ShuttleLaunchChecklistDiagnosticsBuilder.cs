using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleLaunchChecklistDiagnosticsBuilder
    {
        internal void AddDiagnostics(
            ShuttleControlReadModel model,
            List<ShuttleIssueReadModel> diagnostics)
        {
            if (model == null || model.LaunchChecklist == null || diagnostics == null)
            {
                return;
            }

            for (int i = 0; i < model.LaunchChecklist.Count; i++)
            {
                ShuttleLaunchChecklistItemModel item = model.LaunchChecklist[i];
                if (!this.ShouldShowChecklistItem(item) ||
                    this.IsCockpitColonistRequired(item))
                {
                    continue;
                }

                ShuttleIssueActionKind actionKind =
                    ShuttleIssueActionResolver.ResolveLaunchActionKind(
                        item.Category,
                        item.TargetPage,
                        item.Code);
                ShuttleIssueCategory category =
                    ShuttleIssueClassifier.ResolveCategory(item.Category, item.Code);
                ShuttleIssueReadModelSet.AddIssueIfUnique(
                    diagnostics,
                    new ShuttleIssueReadModel
                    {
                        StableId = this.BuildChecklistStableId(item),
                        Severity = this.ResolveChecklistSeverity(item),
                        Category = category,
                        Title = !string.IsNullOrEmpty(item.Message) ? item.Message : "-",
                        Detail = ShuttleIssuePlayerTextHelper.BuildDetail(
                            model,
                            item.Code,
                            category,
                            item.Message,
                            null),
                        Evidence = ShuttleIssuePlayerTextHelper.BuildEvidence(
                            model,
                            item.Code,
                            category,
                            item.ReferenceID),
                        ActionKind = actionKind,
                        ActionLabel =
                            ShuttleIssueActionResolver.ResolveActionLabel(actionKind),
                        NavigationTarget =
                            ShuttleIssueNavigationTargetFactory.BuildNavigationTarget(
                                model,
                                category,
                                item.Code,
                                item.ReferenceID),
                        SortPriority = item.SortPriority
                    });
            }
        }

        private ShuttleIssueSeverity ResolveChecklistSeverity(
            ShuttleLaunchChecklistItemModel item)
        {
            if (item != null && item.BlocksLaunch)
            {
                return ShuttleIssueSeverity.Blocking;
            }

            string severity = item != null && !string.IsNullOrEmpty(item.Severity)
                ? item.Severity.ToLowerInvariant()
                : string.Empty;
            if (severity.Contains("error") || severity.Contains("fatal"))
            {
                return ShuttleIssueSeverity.Error;
            }

            if (severity.Contains("warn") || (item != null && item.IsRiskOnly))
            {
                return ShuttleIssueSeverity.Warning;
            }

            return ShuttleIssueSeverity.Info;
        }

        private bool ShouldShowChecklistItem(ShuttleLaunchChecklistItemModel item)
        {
            if (item == null)
            {
                return false;
            }

            if (item.BlocksLaunch || item.IsRiskOnly)
            {
                return true;
            }

            string severity = !string.IsNullOrEmpty(item.Severity)
                ? item.Severity.ToLowerInvariant()
                : string.Empty;
            return severity.Contains("error") ||
                severity.Contains("fatal") ||
                severity.Contains("warn") ||
                severity.Contains("block");
        }

        private string BuildChecklistStableId(ShuttleLaunchChecklistItemModel item)
        {
            if (item == null)
            {
                return "launch:unknown";
            }

            string code = !string.IsNullOrEmpty(item.Code) ? item.Code : "check";
            string reference = !string.IsNullOrEmpty(item.ReferenceID) ? item.ReferenceID : "no-ref";
            return "launch:" + code + ":" + reference;
        }

        private bool IsCockpitColonistRequired(ShuttleLaunchChecklistItemModel item)
        {
            return item != null &&
                string.Equals(
                    item.Code,
                    "cockpit-colonist-required",
                    System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
