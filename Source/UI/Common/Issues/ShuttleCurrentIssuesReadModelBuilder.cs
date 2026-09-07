using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleCurrentIssuesReadModelBuilder
    {
        private readonly ShuttleCurrentSourceIssuesBuilder sourceIssuesBuilder =
            new ShuttleCurrentSourceIssuesBuilder();
        private readonly ShuttleCurrentAssemblyIssuesBuilder assemblyIssuesBuilder =
            new ShuttleCurrentAssemblyIssuesBuilder();
        private readonly ShuttleCurrentPowerIssuesBuilder powerIssuesBuilder =
            new ShuttleCurrentPowerIssuesBuilder();
        private readonly ShuttleCurrentCargoIssuesBuilder cargoIssuesBuilder =
            new ShuttleCurrentCargoIssuesBuilder();
        private readonly ShuttleCurrentCargoDependencyIssuesBuilder cargoDependencyIssuesBuilder =
            new ShuttleCurrentCargoDependencyIssuesBuilder();
        private readonly ShuttleCurrentDefenseIssuesBuilder defenseIssuesBuilder =
            new ShuttleCurrentDefenseIssuesBuilder();
        private readonly ShuttleCurrentMedicalIssuesBuilder medicalIssuesBuilder =
            new ShuttleCurrentMedicalIssuesBuilder();
        private readonly ShuttleCurrentFoodSupplyIssuesBuilder foodIssuesBuilder =
            new ShuttleCurrentFoodSupplyIssuesBuilder();
        private readonly ShuttleCurrentPrisonIssuesBuilder prisonIssuesBuilder =
            new ShuttleCurrentPrisonIssuesBuilder();

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
            List<ShuttleIssueReadModel> issues = new List<ShuttleIssueReadModel>();

            this.sourceIssuesBuilder.AddIssues(model, issues);
            this.assemblyIssuesBuilder.AddIssues(model, issues);
            this.powerIssuesBuilder.AddIssues(model, issues);
            this.cargoIssuesBuilder.AddIssues(model, cargoSnapshot, issues);
            this.cargoDependencyIssuesBuilder.AddIssues(model, cargoSnapshot, issues);
            ShuttleFoodSupplyIssueSnapshot foodSupply =
                this.foodIssuesBuilder.AddIssues(model, issues);
            this.defenseIssuesBuilder.AddIssues(model, weaponBayModel, issues);
            this.medicalIssuesBuilder.AddIssues(model, cargoSnapshot, issues);
            this.prisonIssuesBuilder.AddIssues(model, foodSupply, issues);

            issues.Sort(ShuttleIssueSortComparer.Compare);
            return issues;
        }
    }
}
