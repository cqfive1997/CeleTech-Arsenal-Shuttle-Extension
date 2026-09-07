using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleLaunchDiagnosticsReadModelBuilder
    {
        private readonly ShuttleLaunchChecklistDiagnosticsBuilder checklistDiagnosticsBuilder =
            new ShuttleLaunchChecklistDiagnosticsBuilder();
        private readonly ShuttleLaunchPowerDiagnosticsBuilder powerDiagnosticsBuilder =
            new ShuttleLaunchPowerDiagnosticsBuilder();
        private readonly ShuttleLaunchCargoDiagnosticsBuilder cargoDiagnosticsBuilder =
            new ShuttleLaunchCargoDiagnosticsBuilder();
        private readonly ShuttleLaunchCrewDiagnosticsBuilder crewDiagnosticsBuilder =
            new ShuttleLaunchCrewDiagnosticsBuilder();
        private readonly ShuttleLaunchCooldownDiagnosticsBuilder cooldownDiagnosticsBuilder =
            new ShuttleLaunchCooldownDiagnosticsBuilder();

        internal List<ShuttleIssueReadModel> Build(
            ShuttleControlReadModel model,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            return this.Build(model, cargoSnapshot, null);
        }

        internal List<ShuttleIssueReadModel> Build(
            ShuttleControlReadModel model,
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttleWeaponBayReadModel weaponBayModel)
        {
            List<ShuttleIssueReadModel> diagnostics =
                new List<ShuttleIssueReadModel>();

            this.checklistDiagnosticsBuilder.AddDiagnostics(model, diagnostics);
            this.powerDiagnosticsBuilder.AddDiagnostics(model, diagnostics);
            this.cargoDiagnosticsBuilder.AddDiagnostics(model, cargoSnapshot, diagnostics);
            this.crewDiagnosticsBuilder.AddDiagnostics(model, cargoSnapshot, diagnostics);
            this.cooldownDiagnosticsBuilder.AddDiagnostics(model, diagnostics);

            diagnostics.Sort(ShuttleIssueSortComparer.Compare);
            return diagnostics;
        }
    }
}
