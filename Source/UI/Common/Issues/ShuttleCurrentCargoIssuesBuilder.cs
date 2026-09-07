using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleCurrentCargoIssuesBuilder
    {
        private const float CargoNearCapacityWarningRatio = 0.95f;

        internal void AddIssues(
            ShuttleControlReadModel model,
            ShuttleCargoSnapshot cargoSnapshot,
            List<ShuttleIssueReadModel> issues)
        {
            this.AddCargoIssues(model, cargoSnapshot, issues);
            this.AddRefrigerationIssues(cargoSnapshot, issues);
        }

        private void AddCargoIssues(
            ShuttleControlReadModel model,
            ShuttleCargoSnapshot cargoSnapshot,
            List<ShuttleIssueReadModel> issues)
        {
            if (cargoSnapshot == null || issues == null)
            {
                return;
            }

            float usedMass = ShuttleUIMetricFormatter.GetCargoLoadedMassKg(cargoSnapshot);
            float capacity = ShuttleUIMetricFormatter.GetCargoCapacity(
                model,
                cargoSnapshot,
                true);
            if (!cargoSnapshot.HasTransporter || capacity <= 0f)
            {
                return;
            }

            if (usedMass <= capacity + 0.001f)
            {
                if (usedMass >= capacity * CargoNearCapacityWarningRatio)
                {
                    ShuttleIssueReadModel nearCapacity = new ShuttleIssueReadModel();
                    nearCapacity.StableId = "cargo:near-capacity:total";
                    nearCapacity.Severity = ShuttleIssueSeverity.Warning;
                    nearCapacity.Category = ShuttleIssueCategory.Cargo;
                    nearCapacity.Title = "CT_Shuttle_Issue_CargoNearCapacity_Title".Translate().ToString();
                    nearCapacity.Detail = "CT_Shuttle_Issue_CargoNearCapacity_Detail".Translate().ToString();
                    nearCapacity.ActionKind = ShuttleIssueActionKind.OpenCargo;
                    nearCapacity.ActionLabel =
                        ShuttleIssueActionResolver.ResolveActionLabel(nearCapacity.ActionKind);
                    nearCapacity.NavigationTarget = new ShuttleIssueNavigationTarget
                    {
                        Kind = ShuttleIssueNavigationTargetKind.CargoBay
                    };
                    nearCapacity.SortPriority = 80;
                    nearCapacity.Evidence = new List<ShuttleIssueEvidenceLine>();
                    nearCapacity.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                        "CT_Shuttle_Issue_Evidence_UsedMass",
                        ShuttleUIMetricFormatter.FormatKgCompact(usedMass)));
                    nearCapacity.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                        "CT_Shuttle_Issue_Evidence_Capacity",
                        ShuttleUIMetricFormatter.FormatKgCompact(capacity)));
                    ShuttleIssueReadModelSet.AddIssueIfUnique(issues, nearCapacity);
                }

                return;
            }

            ShuttleIssueReadModel issue = new ShuttleIssueReadModel();
            issue.StableId = "cargo:over-capacity:total";
            issue.Severity = ShuttleIssueSeverity.Blocking;
            issue.Category = ShuttleIssueCategory.Cargo;
            issue.Title = "CT_Shuttle_Issue_CargoOverloaded_Title".Translate().ToString();
            issue.Detail = "CT_Shuttle_Issue_CargoOverloaded_Detail".Translate().ToString();
            issue.ActionKind = ShuttleIssueActionKind.OpenCargo;
            issue.ActionLabel = ShuttleIssueActionResolver.ResolveActionLabel(issue.ActionKind);
            issue.NavigationTarget = new ShuttleIssueNavigationTarget
            {
                Kind = ShuttleIssueNavigationTargetKind.CargoBay
            };
            issue.SortPriority = 0;
            issue.Evidence = new List<ShuttleIssueEvidenceLine>();
            issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                "CT_Shuttle_Issue_Evidence_UsedMass",
                ShuttleUIMetricFormatter.FormatKgCompact(usedMass)));
            issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                "CT_Shuttle_Issue_Evidence_Capacity",
                ShuttleUIMetricFormatter.FormatKgCompact(capacity)));
            issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                "CT_Shuttle_Issue_Evidence_Bay",
                "CT_Shuttle_Issue_Evidence_Total".Translate().ToString()));
            ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
        }

        private void AddRefrigerationIssues(
            ShuttleCargoSnapshot cargoSnapshot,
            List<ShuttleIssueReadModel> issues)
        {
            if (cargoSnapshot == null ||
                cargoSnapshot.RefrigeratedCargoModules == null ||
                issues == null)
            {
                return;
            }

            for (int i = 0; i < cargoSnapshot.RefrigeratedCargoModules.Count; i++)
            {
                ShuttleRefrigeratedCargoModuleSnapshot module =
                    cargoSnapshot.RefrigeratedCargoModules[i];
                if (module == null ||
                    module.ThingCount <= 0 ||
                    (module.IsEnabled && module.CoolingActive))
                {
                    continue;
                }

                string moduleKey = !string.IsNullOrEmpty(module.ModuleInstanceID)
                    ? module.ModuleInstanceID
                    : i.ToString();
                ShuttleIssueReadModel issue = new ShuttleIssueReadModel();
                issue.StableId = "refrigeration:cargo-at-risk:" + moduleKey;
                issue.Severity = ShuttleIssueSeverity.Warning;
                issue.Category = ShuttleIssueCategory.Refrigeration;
                issue.Title = "CT_Shuttle_Issue_ColdCargoAtRisk_Title".Translate().ToString();
                issue.Detail = "CT_Shuttle_Issue_ColdCargoAtRisk_Detail".Translate().ToString();
                issue.ActionKind = ShuttleIssueActionKind.OpenCargo;
                issue.ActionLabel =
                    ShuttleIssueActionResolver.ResolveActionLabel(issue.ActionKind);
                issue.NavigationTarget = new ShuttleIssueNavigationTarget
                {
                    Kind = ShuttleIssueNavigationTargetKind.CargoBay,
                    CargoBayKey = "cold:" + moduleKey
                };
                issue.SortPriority = 60;
                issue.Evidence = new List<ShuttleIssueEvidenceLine>();
                issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_Location",
                    !string.IsNullOrEmpty(module.Label)
                        ? module.Label
                        : "CT_Shuttle_Issue_Evidence_RefrigeratedBay".Translate().ToString()));
                issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_ItemCount",
                    module.ThingCount.ToString()));
                issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_UsedMass",
                    ShuttleUIMetricFormatter.FormatKgCompact(Mathf.Max(0f, module.StoredMassKg))));
                ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
            }
        }
    }
}
