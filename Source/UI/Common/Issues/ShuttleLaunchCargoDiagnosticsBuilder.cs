using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleLaunchCargoDiagnosticsBuilder
    {
        internal void AddDiagnostics(
            ShuttleControlReadModel model,
            ShuttleCargoSnapshot cargoSnapshot,
            List<ShuttleIssueReadModel> diagnostics)
        {
            if (cargoSnapshot == null || diagnostics == null)
            {
                return;
            }

            float plannedMass = Mathf.Max(0f, cargoSnapshot.TotalPlannedMassKg);
            if (plannedMass <= 0f)
            {
                plannedMass = ShuttleUIMetricFormatter.GetCargoLoadedMassKg(cargoSnapshot);
            }

            float capacity = ShuttleUIMetricFormatter.GetCargoCapacity(
                model,
                cargoSnapshot,
                true);
            if (cargoSnapshot.HasTransporter &&
                capacity > 0f &&
                plannedMass > capacity + 0.001f)
            {
                ShuttleIssueReadModel overCapacity = new ShuttleIssueReadModel();
                overCapacity.StableId = "launch:cargo:over-capacity:total";
                overCapacity.Severity = ShuttleIssueSeverity.Blocking;
                overCapacity.Category = ShuttleIssueCategory.Cargo;
                overCapacity.Title = "CT_Shuttle_Issue_PlannedCargoOverloaded_Title".Translate().ToString();
                overCapacity.Detail = "CT_Shuttle_Issue_PlannedCargoOverloaded_Detail".Translate().ToString();
                overCapacity.ActionKind = ShuttleIssueActionKind.OpenCargo;
                overCapacity.ActionLabel =
                    ShuttleIssueActionResolver.ResolveActionLabel(overCapacity.ActionKind);
                overCapacity.NavigationTarget = new ShuttleIssueNavigationTarget
                {
                    Kind = ShuttleIssueNavigationTargetKind.CargoBay
                };
                overCapacity.SortPriority = 20;
                overCapacity.Evidence = new List<ShuttleIssueEvidenceLine>();
                overCapacity.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_PlannedMass",
                    ShuttleUIMetricFormatter.FormatKgCompact(plannedMass)));
                overCapacity.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_Capacity",
                    ShuttleUIMetricFormatter.FormatKgCompact(capacity)));
                ShuttleIssueReadModelSet.AddIssueIfUnique(diagnostics, overCapacity);
            }

            if (cargoSnapshot.AssignedThingCount > 0 || cargoSnapshot.QueuedMassKg > 0f)
            {
                ShuttleIssueActionKind actionKind = ShuttleIssueActionKind.OpenLoading;
                ShuttleIssueReadModelSet.AddIssueIfUnique(
                    diagnostics,
                    new ShuttleIssueReadModel
                    {
                        StableId = "launch:loading:queued-cargo",
                        Severity = ShuttleIssueSeverity.Warning,
                        Category = ShuttleIssueCategory.Loading,
                        Title = "CT_Shuttle_Issue_LoadingQueued_Title".Translate().ToString(),
                        Detail = "CT_Shuttle_Issue_LoadingQueued_Detail".Translate().ToString(),
                        Evidence = new List<ShuttleIssueEvidenceLine>(),
                        ActionKind = actionKind,
                        ActionLabel =
                            ShuttleIssueActionResolver.ResolveActionLabel(actionKind),
                        NavigationTarget = new ShuttleIssueNavigationTarget
                        {
                            Kind = ShuttleIssueNavigationTargetKind.CargoBay
                        },
                        SortPriority = 200
                    });
            }
        }
    }
}
