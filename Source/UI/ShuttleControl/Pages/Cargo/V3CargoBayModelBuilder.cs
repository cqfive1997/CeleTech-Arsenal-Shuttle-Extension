using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoBayModelBuilder
    {
        private readonly V3CargoStackModelBuilder stackBuilder;
        private readonly V3CargoItemStatsBuilder statsBuilder;
        private readonly V3CargoCapacitySummaryBuilder capacityBuilder;
        private readonly V3CargoVanillaDefaultSorter defaultSorter =
            new V3CargoVanillaDefaultSorter();
        private readonly V3CargoTransferableGrouper transferableGrouper =
            new V3CargoTransferableGrouper();
        private readonly V3CargoRegionSettingsResolver regionSettingsResolver =
            new V3CargoRegionSettingsResolver();

        internal V3CargoBayModelBuilder(
            V3CargoStackModelBuilder stackBuilder,
            V3CargoItemStatsBuilder statsBuilder,
            V3CargoCapacitySummaryBuilder capacityBuilder)
        {
            this.stackBuilder = stackBuilder;
            this.statsBuilder = statsBuilder;
            this.capacityBuilder = capacityBuilder;
        }

        internal V3CargoBayCardModel BuildNormalBay(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            IReadOnlyList<ShuttleCargoRegionReadModel> cargoRegionSettings,
            int regionIndex,
            int regionCount,
            bool aggregateFallback)
        {
            ShuttleCargoRegionReadModel regionSettings = aggregateFallback
                ? null
                : this.regionSettingsResolver.Resolve(cargoRegionSettings, regionIndex);

            V3CargoBayCardModel bay = new V3CargoBayCardModel();
            bay.BayKey = "cargo:" + regionIndex.ToString();
            bay.RegionIndex = aggregateFallback || regionSettings == null
                ? -1
                : regionSettings.RegionIndex;
            bay.Label = ShuttleAssemblyDisplayTextResolver.ResolveCargoRegionDisplayName(
                regionIndex,
                regionSettings != null ? regionSettings.Label : null,
                regionSettings != null && regionSettings.HasCustomLabel);
            bay.FilterSummary = aggregateFallback
                ? ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterSettingsUnavailable")
                : this.GetNormalFilterSummary(regionSettings);
            bay.IsFilterSummaryPlaceholder = aggregateFallback || regionSettings == null;

            bay.HasPawnFilterDetails = regionSettings != null;
            bay.AllowHumans = regionSettings != null && regionSettings.AllowHumans;
            bay.AllowAnimals = regionSettings != null && regionSettings.AllowAnimals;
            bay.AllowMechs = regionSettings != null && regionSettings.AllowMechs;
            bay.ItemFilterSummary =
                regionSettings != null
                    ? this.GetNormalItemFilterSummary(regionSettings.ItemFilter)
                    : ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterUnavailable");
            bay.ItemFilter = this.CopyFilter(regionSettings != null ? regionSettings.ItemFilter : null);
            bay.IsRefrigerated = false;
            bay.CoolingActive = false;
            bay.StatusText = ShuttleUIText.Tr("CT_Shuttle_Cargo_Bay");
            bay.CapacityKg = this.capacityBuilder.GetNormalBayCapacity(
                controlModel,
                cargoSnapshot,
                regionCount);

            this.AddOrdinaryStacks(bay, cargoSnapshot, regionIndex, aggregateFallback);
            this.FinalizeDisplayFields(bay);
            return bay;
        }

        internal void AddRefrigeratedBays(
            V3CargoPageReadModel pageModel,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            if (pageModel == null ||
                cargoSnapshot == null ||
                cargoSnapshot.RefrigeratedCargoModules == null)
            {
                return;
            }

            for (int i = 0; i < cargoSnapshot.RefrigeratedCargoModules.Count; i++)
            {
                ShuttleRefrigeratedCargoModuleSnapshot module =
                    cargoSnapshot.RefrigeratedCargoModules[i];
                if (module == null)
                {
                    continue;
                }

                pageModel.Bays.Add(this.BuildRefrigeratedBay(module, i));
            }
        }

        internal void AddUnassignedCargoBay(
            V3CargoPageReadModel pageModel,
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            if (pageModel == null || cargoSnapshot == null)
            {
                return;
            }

            // With no configured normal regions, the ordinary fallback bay already shows
            // every loaded stack. A separate recovery projection would duplicate those rows.
            if (this.capacityBuilder.GetNormalCargoRegionCount(
                controlModel,
                cargoSnapshot) <= 0)
            {
                return;
            }

            V3CargoBayCardModel bay = new V3CargoBayCardModel();
            bay.BayKey = "cargo:unassigned";
            bay.RegionIndex = -1;
            bay.Label = ShuttleUIText.Tr("CT_Shuttle_UI_BlockedUnassigned");
            bay.FilterSummary = ShuttleUIText.Tr("CT_Shuttle_UI_NoCargoRegionAllowsRows");
            bay.IsFilterSummaryPlaceholder = true;
            bay.ItemFilterSummary = bay.FilterSummary;
            bay.IsRefrigerated = false;
            bay.IsRecoveryBay = true;
            bay.CoolingActive = false;
            bay.StatusText = ShuttleUIText.Tr("CT_Shuttle_Cargo_Blocked");
            bay.CapacityKg = this.capacityBuilder.GetNormalBayCapacity(
                controlModel,
                cargoSnapshot,
                1);

            // Region index -1 is the snapshot's explicit unmatched assignment. Keep those
            // real transporter contents visible and recoverable without inventing a new holder.
            this.AddOrdinaryStacks(bay, cargoSnapshot, -1, false);
            if (bay.Items.Count <= 0)
            {
                return;
            }

            this.FinalizeDisplayFields(bay);
            pageModel.Bays.Add(bay);
        }

        private V3CargoBayCardModel BuildRefrigeratedBay(
            ShuttleRefrigeratedCargoModuleSnapshot module,
            int moduleIndex)
        {
            V3CargoBayCardModel bay = new V3CargoBayCardModel();
            bay.BayKey = "cold:" + (module.ModuleInstanceID ?? moduleIndex.ToString());
            bay.ModuleInstanceID = module.ModuleInstanceID;
            bay.Label = ShuttleAssemblyDisplayTextResolver.ResolveRefrigeratedCargoDisplayName(
                module.ModuleDefName,
                module.Label,
                module.HasCustomLabel,
                ShuttleUIText.Tr("CT_Shuttle_Module_RefrigeratedCargo_Name") +
                    " " + (moduleIndex + 1).ToString());
            bay.IsEnabled = module.IsEnabled;
            bay.IsRefrigerated = true;
            bay.IsRecoveryBay = false;
            bay.CoolingActive = module.IsEnabled && module.CoolingActive;
            bay.StatusText = this.GetRefrigeratedStatusText(module);
            bay.FilterSummary = this.GetRefrigeratedFilterSummary(module);
            bay.IsFilterSummaryPlaceholder = module.AutoTransferFilter == null;

            bay.HasAutoTransferDetails = true;
            bay.AutoTransferEnabled = module.IsEnabled && module.AutoTransferEnabled;
            bay.AutoTransferFilterSummary = this.GetRefrigeratedItemFilterSummary(module);
            bay.InactiveReason = module.InactiveReason;
            bay.HasCustomAutoTransferFilter = module.HasCustomAutoTransferFilter;
            bay.AutoTransferFilter = this.CopyFilter(module.AutoTransferFilter);
            bay.UsedMassKg = Mathf.Max(0f, module.StoredMassKg);
            bay.CapacityKg = Mathf.Max(0f, module.CapacityKg);

            this.AddRefrigeratedStacks(bay, module);
            this.FinalizeDisplayFields(bay);
            return bay;
        }

        private void AddOrdinaryStacks(
            V3CargoBayCardModel bay,
            ShuttleCargoSnapshot cargoSnapshot,
            int regionIndex,
            bool aggregateFallback)
        {
            if (bay == null || cargoSnapshot == null || cargoSnapshot.Items == null)
            {
                return;
            }

            for (int i = 0; i < cargoSnapshot.Items.Count; i++)
            {
                ShuttleCargoItemSnapshot item = cargoSnapshot.Items[i];
                if (!this.ShouldShowOrdinaryItem(item, regionIndex, aggregateFallback))
                {
                    continue;
                }

                V3CargoStackCardModel stack = this.stackBuilder.BuildLoadedStack(item);
                if (stack == null)
                {
                    continue;
                }

                bay.Items.Add(stack);
                bay.UsedMassKg += Mathf.Max(0f, stack.MassKg);
                this.statsBuilder.AddStackToStats(stack);
            }
        }

        private void AddRefrigeratedStacks(
            V3CargoBayCardModel bay,
            ShuttleRefrigeratedCargoModuleSnapshot module)
        {
            if (bay == null || module == null || module.Items == null)
            {
                return;
            }

            for (int i = 0; i < module.Items.Count; i++)
            {
                V3CargoStackCardModel stack =
                    this.stackBuilder.BuildRefrigeratedStack(module.Items[i]);
                if (stack == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(stack.ModuleInstanceID))
                {
                    stack.ModuleInstanceID = module.ModuleInstanceID;
                }

                for (int memberIndex = 0;
                    stack.Members != null && memberIndex < stack.Members.Count;
                    memberIndex++)
                {
                    V3CargoStackMemberModel member = stack.Members[memberIndex];
                    if (member != null && string.IsNullOrEmpty(member.ModuleInstanceID))
                    {
                        member.ModuleInstanceID = stack.ModuleInstanceID;
                    }
                }

                bay.Items.Add(stack);
                this.statsBuilder.AddStackToStats(stack);
            }
        }

        private bool ShouldShowOrdinaryItem(
            ShuttleCargoItemSnapshot item,
            int regionIndex,
            bool aggregateFallback)
        {
            if (item == null || !item.IsLoaded || item.IsPawn)
            {
                return false;
            }

            return aggregateFallback || item.CargoRegionIndex == regionIndex;
        }

        private string GetNormalFilterSummary(ShuttleCargoRegionReadModel settings)
        {
            return settings != null
                ? this.GetNormalItemFilterSummary(settings.ItemFilter)
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterUnavailable");
        }

        private string GetRefrigeratedFilterSummary(
            ShuttleRefrigeratedCargoModuleSnapshot module)
        {
            if (module == null)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdSettingsUnavailable");
            }

            string mode = module.AutoTransferEnabled
                ? ShuttleUIText.Tr("CT_Shuttle_Cargo_AutoTransferOn")
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_AutoTransferOff");
            string summary = mode + ": " + this.GetRefrigeratedItemFilterSummary(module);
            if (!module.CoolingActive && !string.IsNullOrEmpty(module.InactiveReason))
            {
                summary += " / " + module.InactiveReason;
            }

            return summary;
        }

        private string GetRefrigeratedStatusText(
            ShuttleRefrigeratedCargoModuleSnapshot module)
        {
            if (module == null || !module.ModuleResolved)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdSettingsUnavailable");
            }

            if (!module.IsEnabled)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Cargo_RefrigeratedModuleDisabled");
            }

            if (module.CoolingActive)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Cargo_Cold");
            }

            return !string.IsNullOrEmpty(module.InactiveReason)
                ? module.InactiveReason
                : ShuttleUIText.Tr("CT_Shuttle_Logistics_StatusUnavailable");
        }

        private string GetNormalItemFilterSummary(ThingFilter filter)
        {
            return filter != null
                ? ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterCustom")
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterUnavailable");
        }

        private string GetRefrigeratedItemFilterSummary(
            ShuttleRefrigeratedCargoModuleSnapshot module)
        {
            if (module == null)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdFilterConfigUnavailable");
            }

            if (!module.ModuleResolved)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdFilterConfigUnavailable");
            }

            if (!module.IsEnabled)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdFilterModuleDisabled");
            }

            if (module.AutoTransferFilter == null)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdFilterConfigUnavailable");
            }

            return module.HasCustomAutoTransferFilter
                ? ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterCustom")
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterDefault");
        }

        private ThingFilter CopyFilter(ThingFilter source)
        {
            ThingFilter copy = ThingFilter.CreateOnlyEverStorableThingFilter();
            if (source != null)
            {
                copy.CopyAllowancesFrom(source);
            }

            return copy;
        }

        private void FinalizeDisplayFields(V3CargoBayCardModel bay)
        {
            if (bay == null)
            {
                return;
            }

            this.transferableGrouper.GroupInPlace(bay.Items);
            this.defaultSorter.Sort(bay.Items);

            int stackCount = this.CountPhysicalStacks(bay.Items);
            int itemCount = 0;
            for (int i = 0; bay.Items != null && i < bay.Items.Count; i++)
            {
                V3CargoStackCardModel stack = bay.Items[i];
                if (stack != null && stack.StackCount > 0)
                {
                    itemCount += stack.StackCount;
                }
            }

            bay.ItemCount = itemCount;
            bay.CountSummary = ShuttleUIText.Tr(
                "CT_Shuttle_Cargo_CountPairFormat",
                stackCount,
                itemCount);
            bay.MassSummary = bay.IsRefrigerated
                ? ShuttleUIText.Tr(
                    "CT_Shuttle_Cargo_RefrigeratedBayStoredMass",
                    ShuttleUIMetricFormatter.FormatKgCompact(
                        Mathf.Max(0f, bay.UsedMassKg)))
                : ShuttleUIMetricFormatter.FormatKgPair(
                    Mathf.Max(0f, bay.UsedMassKg),
                    Mathf.Max(0f, bay.CapacityKg));
            bay.CountMassSummary = bay.CountSummary + " / " + bay.MassSummary;
            bay.ModeSummary = this.BuildBayModeSummary(bay);
            bay.FilterDisplayText =
                ShuttleAssemblyDisplayTextResolver.ResolveSafeDisplayText(
                    bay.FilterSummary);
            bay.HeaderTooltip =
                ShuttleAssemblyDisplayTextResolver.ResolveSafeDisplayText(bay.Label) + "\n" +
                ShuttleAssemblyDisplayTextResolver.ResolveSafeDisplayText(bay.StatusText);
            bay.Tooltip =
                ShuttleAssemblyDisplayTextResolver.ResolveSafeDisplayText(bay.Label) + "\n" +
                ShuttleAssemblyDisplayTextResolver.ResolveSafeDisplayText(bay.StatusText) + "\n" +
                bay.ModeSummary + "\n" +
                bay.MassSummary;
        }

        private int CountPhysicalStacks(List<V3CargoStackCardModel> stacks)
        {
            int count = 0;
            for (int i = 0; stacks != null && i < stacks.Count; i++)
            {
                V3CargoStackCardModel stack = stacks[i];
                count += stack != null && stack.Members != null && stack.Members.Count > 0
                    ? stack.Members.Count
                    : 1;
            }

            return count;
        }

        private string BuildBayModeSummary(V3CargoBayCardModel bay)
        {
            if (bay == null)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Cargo_Unknown");
            }

            if (bay.IsRefrigerated)
            {
                string mode = !bay.IsEnabled
                    ? ShuttleUIText.Tr("CT_Shuttle_Cargo_RefrigeratedModuleDisabled")
                    : bay.AutoTransferEnabled
                    ? ShuttleUIText.Tr("CT_Shuttle_Cargo_AutoTransferOn")
                    : ShuttleUIText.Tr("CT_Shuttle_Cargo_AutoTransferOff");
                string filter = bay.HasCustomAutoTransferFilter
                    ? ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterCustom")
                    : ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterDefault");
                return mode + " / " + filter;
            }

            return ShuttleAssemblyDisplayTextResolver.ResolveSafeDisplayText(
                bay.FilterSummary);
        }
    }
}
