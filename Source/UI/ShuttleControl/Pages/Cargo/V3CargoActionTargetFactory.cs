using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal static class V3CargoActionTargetFactory
    {
        internal static ShuttleCargoStackActionTarget CreateStack(
            V3CargoStackCardModel stack)
        {
            ShuttleCargoStackActionTarget target =
                new ShuttleCargoStackActionTarget();
            if (stack == null)
            {
                return target;
            }

            target.Label = stack.Label;
            target.DefName = stack.DefName;
            target.StackCount = stack.StackCount;
            target.MassKg = stack.MassKg;
            target.Category = (ShuttleCargoActionCategory)(int)stack.Category;
            target.DisplayThing = stack.DisplayThing;
            target.SourceKind = (ShuttleCargoStackActionSourceKind)(int)stack.SourceKind;
            target.TransporterIndex = stack.TransporterIndex;
            target.LoadedIndex = stack.LoadedIndex;
            target.ThingIDNumber = stack.ThingIDNumber;
            target.ModuleInstanceID = stack.ModuleInstanceID;
            target.ColdIndex = stack.ColdIndex;
            for (int i = 0; stack.Members != null && i < stack.Members.Count; i++)
            {
                V3CargoStackMemberModel member = stack.Members[i];
                if (member == null)
                {
                    continue;
                }

                target.Members.Add(new ShuttleCargoStackMemberActionTarget
                {
                    SourceKind = (ShuttleCargoStackActionSourceKind)(int)member.SourceKind,
                    DefName = member.DefName,
                    StackCount = member.StackCount,
                    TransporterIndex = member.TransporterIndex,
                    LoadedIndex = member.LoadedIndex,
                    ThingIDNumber = member.ThingIDNumber,
                    ModuleInstanceID = member.ModuleInstanceID,
                    ColdIndex = member.ColdIndex
                });
            }
            return target;
        }

        internal static ShuttleCargoBayActionTarget CreateBay(
            V3CargoBayCardModel bay)
        {
            ShuttleCargoBayActionTarget target = new ShuttleCargoBayActionTarget();
            if (bay == null)
            {
                return target;
            }

            CopyBayFields(bay, target);
            for (int i = 0; bay.Items != null && i < bay.Items.Count; i++)
            {
                target.Items.Add(CreateStack(bay.Items[i]));
            }

            return target;
        }

        internal static ShuttleCargoPageActionContext CreatePageContext(
            V3CargoPageReadModel pageModel)
        {
            ShuttleCargoPageActionContext context =
                new ShuttleCargoPageActionContext();
            if (pageModel == null)
            {
                return context;
            }

            for (int i = 0; pageModel.Bays != null && i < pageModel.Bays.Count; i++)
            {
                context.Bays.Add(CreateBay(pageModel.Bays[i]));
            }

            context.HasCargoLogisticsModule = pageModel.HasCargoLogisticsModule;
            context.CargoLogisticsModuleEnabled =
                pageModel.CargoLogisticsModuleEnabled;
            context.CargoLogisticsSupportsItemTransfer =
                pageModel.CargoLogisticsSupportsItemTransfer;
            context.CargoLogisticsSupportsItemConsumption =
                pageModel.CargoLogisticsSupportsItemConsumption;
            context.CargoLogisticsSupportsItemDeposit =
                pageModel.CargoLogisticsSupportsItemDeposit;
            context.CargoLogisticsInternalBusPowered =
                pageModel.CargoLogisticsInternalBusPowered;
            context.CargoLogisticsCapabilitySummary =
                pageModel.CargoLogisticsCapabilitySummary;
            return context;
        }

        private static void CopyBayFields(
            V3CargoBayCardModel source,
            ShuttleCargoBayActionTarget target)
        {
            target.BayKey = source.BayKey;
            target.Label = source.Label;
            target.FilterSummary = source.FilterSummary;
            target.IsFilterSummaryPlaceholder = source.IsFilterSummaryPlaceholder;
            target.HasPawnFilterDetails = source.HasPawnFilterDetails;
            target.AllowHumans = source.AllowHumans;
            target.AllowAnimals = source.AllowAnimals;
            target.AllowMechs = source.AllowMechs;
            target.ItemFilterSummary = source.ItemFilterSummary;
            target.IsEnabled = source.IsEnabled;
            target.IsRefrigerated = source.IsRefrigerated;
            target.CoolingActive = source.CoolingActive;
            target.StatusText = source.StatusText;
            target.HasAutoTransferDetails = source.HasAutoTransferDetails;
            target.AutoTransferEnabled = source.AutoTransferEnabled;
            target.AutoTransferFilterSummary = source.AutoTransferFilterSummary;
            target.InactiveReason = source.InactiveReason;
            target.RegionIndex = source.RegionIndex;
            target.ModuleInstanceID = source.ModuleInstanceID;
            target.HasCustomAutoTransferFilter = source.HasCustomAutoTransferFilter;
            target.ItemFilter = source.ItemFilter;
            target.AutoTransferFilter = source.AutoTransferFilter;
            target.UsedMassKg = source.UsedMassKg;
            target.CapacityKg = source.CapacityKg;
        }
    }
}
