using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal sealed class V3PrisonCellStatusModelBuilder
    {
        internal void CopyStatus(
            ShuttlePrisonCellReadModel source,
            ShuttlePrisonCellReadModel target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.HasPrisonCell = source.HasPrisonCell;
            target.PrisonerSlots = source.PrisonerSlots;
            target.PrisonerCount = source.PrisonerCount;
            target.FreePrisonerSlots = source.FreePrisonerSlots;
            target.CanAdmitMore = source.CanAdmitMore;
            target.SupportsCargoFoodSupply = source.SupportsCargoFoodSupply;
            target.SupportsRefrigeratedCargoFoodSupply =
                source.SupportsRefrigeratedCargoFoodSupply;
            target.CargoFoodSupplyConfiguredEnabled =
                source.CargoFoodSupplyConfiguredEnabled;
            target.RefrigeratedFoodSupplyConfiguredEnabled =
                source.RefrigeratedFoodSupplyConfiguredEnabled;
            target.CargoFoodSupplyEnabled = source.CargoFoodSupplyEnabled;
            target.RefrigeratedCargoFoodSupplyEnabled =
                source.RefrigeratedCargoFoodSupplyEnabled;
            target.RequiresCargoLogisticsForFoodSupply =
                source.RequiresCargoLogisticsForFoodSupply;
            target.MaximumCargoFoodPreferability =
                source.MaximumCargoFoodPreferability;
            target.UnavailableReason = source.UnavailableReason;
        }
    }
}
