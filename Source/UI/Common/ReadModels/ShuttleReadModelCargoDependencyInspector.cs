using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels
{
    internal sealed class ShuttleReadModelCargoDependencyInspector
    {
        internal ShuttleCargoLogisticsReadState GetCargoLogisticsState(
            ShuttleControlReadModel model)
        {
            ShuttleCargoLogisticsReadState state = new ShuttleCargoLogisticsReadState();
            if (model == null || model.SegmentSlots == null)
            {
                return state;
            }

            this.HasInstalledModuleMatching(
                model,
                delegate(ShuttleModuleBaseDef moduleDef, ShuttleControlModuleSlotModel module)
                {
                    ShuttleCargoLogisticsModuleDef logisticsDef =
                        moduleDef as ShuttleCargoLogisticsModuleDef;
                    if (module == null || logisticsDef == null)
                    {
                        return false;
                    }

                    state.Installed = true;
                    if (module.InstalledModuleEnabled)
                    {
                        state.Enabled = true;
                        state.SupportsItemTransfer |= logisticsDef.supportsItemTransfer;
                        state.SupportsItemConsumption |= logisticsDef.supportsItemConsumption;
                        state.SupportsItemDeposit |= logisticsDef.supportsItemDeposit;
                    }

                    return false;
                });

            state.InternalBusPowered = model.InternalBusPowered;
            return state;
        }

        internal bool HasHabitatCargoFoodDependency(
            ShuttleControlReadModel model,
            ShuttleCargoLogisticsReadState logistics)
        {
            if (logistics.SupportsPoweredItemTransfer)
            {
                return false;
            }

            return this.HasInstalledModuleMatching(
                model,
                delegate(ShuttleModuleBaseDef moduleDef, ShuttleControlModuleSlotModel module)
                {
                    ShuttleHabitatModuleDef habitatDef = moduleDef as ShuttleHabitatModuleDef;
                    return module != null &&
                        module.InstalledModuleEnabled &&
                        habitatDef != null &&
                        habitatDef.allowCargoFoodWithdrawal &&
                        habitatDef.requireCargoLogisticsForFoodWithdrawal;
                });
        }

        internal bool HasRefrigeratedAutoTransferDependency(
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttleCargoLogisticsReadState logistics)
        {
            if (logistics.SupportsPoweredItemTransfer ||
                cargoSnapshot == null ||
                cargoSnapshot.RefrigeratedCargoModules == null)
            {
                return false;
            }

            for (int i = 0; i < cargoSnapshot.RefrigeratedCargoModules.Count; i++)
            {
                ShuttleRefrigeratedCargoModuleSnapshot module =
                    cargoSnapshot.RefrigeratedCargoModules[i];
                if (module != null &&
                    module.IsEnabled &&
                    module.AutoTransferEnabled)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool HasAutoWorkTableCargoDependency(
            ShuttleControlReadModel model,
            ShuttleCargoLogisticsReadState logistics)
        {
            if (logistics.SupportsPoweredItemConsumptionAndDeposit)
            {
                return false;
            }

            return this.HasInstalledModuleMatching(
                model,
                delegate(ShuttleModuleBaseDef moduleDef, ShuttleControlModuleSlotModel module)
                {
                    ShuttleAutoWorkTableModuleDef workTableDef =
                        moduleDef as ShuttleAutoWorkTableModuleDef;
                    return module != null &&
                        module.InstalledModuleEnabled &&
                        workTableDef != null &&
                        workTableDef.requiresCargoLogistics;
                });
        }

        internal bool HasHabitatCargoFoodConsumer(ShuttleControlReadModel model)
        {
            if (model == null ||
                model.Habitat == null ||
                !model.Habitat.HasHabitat ||
                !model.Habitat.SupportsDining ||
                !this.HasHabitatFoodConsumerInReadModel(model.Habitat))
            {
                return false;
            }

            return this.HasInstalledModuleMatching(
                model,
                delegate(ShuttleModuleBaseDef moduleDef, ShuttleControlModuleSlotModel module)
                {
                    ShuttleHabitatModuleDef habitatDef = moduleDef as ShuttleHabitatModuleDef;
                    return module != null &&
                        module.InstalledModuleEnabled &&
                        habitatDef != null &&
                        habitatDef.habitatSupportsDining &&
                        habitatDef.allowCargoFoodWithdrawal;
                });
        }

        private bool HasHabitatFoodConsumerInReadModel(ShuttleHabitatReadModel habitat)
        {
            if (habitat == null)
            {
                return false;
            }

            if (habitat.DiningOccupants > 0)
            {
                return true;
            }

            if (habitat.Occupants == null)
            {
                return false;
            }

            for (int i = 0; i < habitat.Occupants.Count; i++)
            {
                ShuttleHabitatOccupantReadModel occupant = habitat.Occupants[i];
                if (occupant != null &&
                    (occupant.MostUrgentNeed == HabitatNeedKind.Food ||
                        (occupant.FoodPct >= 0f && occupant.FoodPct < 0.35f)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasInstalledModuleMatching(
            ShuttleControlReadModel model,
            Func<ShuttleModuleBaseDef, ShuttleControlModuleSlotModel, bool> predicate)
        {
            if (model == null || model.SegmentSlots == null || predicate == null)
            {
                return false;
            }

            for (int i = 0; i < model.SegmentSlots.Count; i++)
            {
                if (this.HasMatchingModuleInSegment(model.SegmentSlots[i], predicate))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasMatchingModuleInSegment(
            ShuttleControlSegmentSlotModel segment,
            Func<ShuttleModuleBaseDef, ShuttleControlModuleSlotModel, bool> predicate)
        {
            if (segment == null || segment.ModuleSlots == null)
            {
                return false;
            }

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleControlModuleSlotModel module = segment.ModuleSlots[i];
                if (module == null || string.IsNullOrEmpty(module.InstalledModuleDefName))
                {
                    continue;
                }

                ShuttleModuleBaseDef moduleDef = this.GetModuleDef(module.InstalledModuleDefName);
                if (moduleDef != null && predicate(moduleDef, module))
                {
                    return true;
                }
            }

            return false;
        }

        private ShuttleModuleBaseDef GetModuleDef(string defName)
        {
            return string.IsNullOrEmpty(defName)
                ? null
                : DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(defName);
        }
    }
}
