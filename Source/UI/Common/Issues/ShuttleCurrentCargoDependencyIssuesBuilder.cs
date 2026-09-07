using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    /// <summary>
    /// Projects actionable Cargo logistics dependencies into the typed current-issue model.
    /// It reads detached models only and does not own logistics policy or Cargo state.
    /// </summary>
    internal sealed class ShuttleCurrentCargoDependencyIssuesBuilder
    {
        private readonly ShuttleReadModelCargoDependencyInspector dependencyInspector =
            new ShuttleReadModelCargoDependencyInspector();

        internal void AddIssues(
            ShuttleControlReadModel model,
            ShuttleCargoSnapshot cargoSnapshot,
            List<ShuttleIssueReadModel> issues)
        {
            if (model == null || issues == null)
            {
                return;
            }

            ShuttleCargoLogisticsReadState logistics =
                this.dependencyInspector.GetCargoLogisticsState(model);
            if (this.dependencyInspector.HasHabitatCargoFoodDependency(model, logistics))
            {
                this.AddIssue(
                    issues,
                    "cargo-dependency:habitat-food",
                    ShuttleIssueCategory.Cargo,
                    "CT_Shuttle_Issue_HabitatLogisticsUnavailable_Title",
                    "CT_Shuttle_LogisticsDependency_HabitatFood",
                    ShuttleIssueActionKind.OpenCargo,
                    130);
            }

            if (this.dependencyInspector.HasRefrigeratedAutoTransferDependency(
                cargoSnapshot,
                logistics))
            {
                this.AddIssue(
                    issues,
                    "cargo-dependency:refrigerated-auto-transfer",
                    ShuttleIssueCategory.Refrigeration,
                    "CT_Shuttle_Issue_RefrigeratedLoadingBlocked_Title",
                    "CT_Shuttle_LogisticsDependency_RefrigeratedAutoTransfer",
                    ShuttleIssueActionKind.OpenCargo,
                    131);
            }

            if (this.dependencyInspector.HasAutoWorkTableCargoDependency(model, logistics))
            {
                this.AddIssue(
                    issues,
                    "cargo-dependency:auto-worktable",
                    ShuttleIssueCategory.General,
                    "CT_Shuttle_Issue_AutoWorkTableLogisticsUnavailable_Title",
                    "CT_Shuttle_LogisticsDependency_AutoWorkTableProduction",
                    ShuttleIssueActionKind.OpenProcessing,
                    132);
            }
        }

        private void AddIssue(
            List<ShuttleIssueReadModel> issues,
            string stableId,
            ShuttleIssueCategory category,
            string titleKey,
            string detailKey,
            ShuttleIssueActionKind actionKind,
            int sortPriority)
        {
            ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                stableId,
                ShuttleIssueSeverity.Warning,
                category,
                titleKey,
                detailKey,
                actionKind,
                sortPriority);
            ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
        }
    }
}
