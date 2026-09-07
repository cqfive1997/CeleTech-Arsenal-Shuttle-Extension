using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    public sealed class SetShuttlePrisonCellSupplyPolicyCommand : IShuttleCommand
    {
        public const string ID = "set-shuttle-prison-cell-supply-policy";

        public SetShuttlePrisonCellSupplyPolicyCommand(
            bool cargoFoodSupplyEnabled,
            bool refrigeratedFoodSupplyEnabled,
            FoodPreferability maximumFoodPreferability)
        {
            this.CargoFoodSupplyEnabled = cargoFoodSupplyEnabled;
            this.RefrigeratedFoodSupplyEnabled = refrigeratedFoodSupplyEnabled;
            this.MaximumFoodPreferability = maximumFoodPreferability;
        }

        public bool CargoFoodSupplyEnabled { get; private set; }

        public bool RefrigeratedFoodSupplyEnabled { get; private set; }

        public FoodPreferability MaximumFoodPreferability { get; private set; }

        public string CommandID
        {
            get { return ID; }
        }
    }
}
