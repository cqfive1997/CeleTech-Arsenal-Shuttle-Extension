using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleLaunchCooldownDiagnosticsBuilder
    {
        internal void AddDiagnostics(
            ShuttleControlReadModel model,
            List<ShuttleIssueReadModel> diagnostics)
        {
            if (model == null || diagnostics == null || !model.HasLaunchCooldown)
            {
                return;
            }

            ShuttleIssueActionKind actionKind = ShuttleIssueActionKind.OpenLaunch;
            ShuttleIssueReadModelSet.AddIssueIfUnique(
                diagnostics,
                new ShuttleIssueReadModel
                {
                    StableId = "launch:cooldown",
                    Severity = ShuttleIssueSeverity.Blocking,
                    Category = ShuttleIssueCategory.Launch,
                    Title = "CT_Shuttle_Issue_LaunchCooldown_Title".Translate().ToString(),
                    Detail = "CT_Shuttle_Issue_LaunchCooldown_Detail".Translate().ToString(),
                    Evidence = new List<ShuttleIssueEvidenceLine>(),
                    ActionKind = actionKind,
                    ActionLabel = ShuttleIssueActionResolver.ResolveActionLabel(actionKind),
                    NavigationTarget = new ShuttleIssueNavigationTarget
                    {
                        Kind = ShuttleIssueNavigationTargetKind.None
                    },
                    SortPriority = 10
                });
        }
    }
}
