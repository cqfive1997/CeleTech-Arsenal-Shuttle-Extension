using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoLogisticsModuleStateBuilder
    {
        internal V3CargoModuleLogisticsState Build(
            ShuttleControlReadModel controlModel,
            V3CargoModuleLookup moduleLookup)
        {
            V3CargoModuleLogisticsState state = new V3CargoModuleLogisticsState();
            if (moduleLookup == null)
            {
                return state;
            }

            state.HabitatState = moduleLookup.GetHabitatCargoFoodState(controlModel);
            state.PrisonState = this.BuildPrisonFoodState(controlModel, moduleLookup);
            state.HasMedical = moduleLookup.HasMatchingModule(
                controlModel,
                false,
                "medical",
                "medbay",
                "med bay",
                "hospital");
            state.MedicalEnabled = moduleLookup.HasMatchingModule(
                controlModel,
                true,
                "medical",
                "medbay",
                "med bay",
                "hospital");
            state.HasProcessing = moduleLookup.HasMatchingModule(
                controlModel,
                false,
                "worktable",
                "work table",
                "workbench",
                "production",
                "processing");
            state.ProcessingEnabled = moduleLookup.HasMatchingModule(
                controlModel,
                true,
                "worktable",
                "work table",
                "workbench",
                "production",
                "processing");
            return state;
        }

        private V3CargoPrisonFoodState BuildPrisonFoodState(
            ShuttleControlReadModel controlModel,
            V3CargoModuleLookup moduleLookup)
        {
            V3CargoPrisonFoodState state = new V3CargoPrisonFoodState();
            state.Installed = moduleLookup.HasMatchingModule(
                controlModel,
                false,
                "prison-cell",
                "prison cell",
                "prisoncell");
            state.Enabled = moduleLookup.HasMatchingModule(
                controlModel,
                true,
                "prison-cell",
                "prison cell",
                "prisoncell");

            ShuttlePrisonCellReadModel prisonCell = controlModel != null
                ? controlModel.PrisonCell
                : null;
            if (prisonCell != null)
            {
                state.SupplyEnabled = prisonCell.CargoFoodSupplyEnabled;
                state.RefrigeratedSupplyEnabled =
                    prisonCell.RefrigeratedCargoFoodSupplyEnabled;
                state.RequiresCargoLogistics =
                    prisonCell.RequiresCargoLogisticsForFoodSupply;
                state.MaximumFoodPreferability =
                    prisonCell.MaximumCargoFoodPreferability;
            }

            return state;
        }
    }

    internal struct V3CargoModuleLogisticsState
    {
        internal V3CargoHabitatFoodState HabitatState;
        internal V3CargoPrisonFoodState PrisonState;
        internal bool HasMedical;
        internal bool MedicalEnabled;
        internal bool HasProcessing;
        internal bool ProcessingEnabled;
    }

    internal struct V3CargoPrisonFoodState
    {
        internal bool Installed;
        internal bool Enabled;
        internal bool SupplyEnabled;
        internal bool RefrigeratedSupplyEnabled;
        internal bool RequiresCargoLogistics;
        internal RimWorld.FoodPreferability MaximumFoodPreferability;
    }
}
