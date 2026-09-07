using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleLaunchCrewDiagnosticsBuilder
    {
        internal void AddDiagnostics(
            ShuttleControlReadModel model,
            ShuttleCargoSnapshot cargoSnapshot,
            List<ShuttleIssueReadModel> diagnostics)
        {
            const string stableId = "launch:crew:cockpit-colonist-required";
            if (model == null ||
                diagnostics == null ||
                !model.HasCockpit ||
                model.ProvidesAutonomousLaunchControl ||
                cargoSnapshot == null ||
                !cargoSnapshot.HasTransporter ||
                ShuttleLaunchCrewRequirementUtility.HasLoadedColonistInCockpit(cargoSnapshot) ||
                ShuttleIssueReadModelSet.ContainsStableId(diagnostics, stableId))
            {
                return;
            }

            ShuttleIssueActionKind actionKind = ShuttleIssueActionKind.OpenLoading;
            ShuttleIssueReadModelSet.AddIssueIfUnique(
                diagnostics,
                new ShuttleIssueReadModel
                {
                    StableId = stableId,
                    Severity = ShuttleIssueSeverity.Blocking,
                    Category = ShuttleIssueCategory.Crew,
                    Title = "CT_Shuttle_Issue_CockpitColonistRequired".Translate().ToString(),
                    Detail = "CT_Shuttle_Issue_CockpitColonistRequired_Detail".Translate().ToString(),
                    Evidence = new List<ShuttleIssueEvidenceLine>(),
                    ActionKind = actionKind,
                    ActionLabel = ShuttleIssueActionResolver.ResolveActionLabel(actionKind),
                    NavigationTarget = new ShuttleIssueNavigationTarget
                    {
                        Kind = ShuttleIssueNavigationTargetKind.CargoBay
                    },
                    SortPriority = 30
                });
        }
    }
}
