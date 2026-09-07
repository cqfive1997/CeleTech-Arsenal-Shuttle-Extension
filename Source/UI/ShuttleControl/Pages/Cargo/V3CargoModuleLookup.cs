using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoModuleLookup
    {
        internal V3CargoLogisticsState GetCargoLogisticsState(
            ShuttleControlReadModel controlModel)
        {
            V3CargoLogisticsState state = new V3CargoLogisticsState();
            state.InternalBusPowered = controlModel != null && controlModel.InternalBusPowered;
            if (controlModel != null && controlModel.HasCargoLogistics)
            {
                state.Installed = true;
                state.Enabled = true;
                state.SupportsItemTransfer |= controlModel.CargoLogisticsSupportsItemTransfer;
                state.SupportsItemConsumption |= controlModel.CargoLogisticsSupportsItemConsumption;
                state.SupportsItemDeposit |= controlModel.CargoLogisticsSupportsItemDeposit;
            }

            this.MergeInstalledCargoLogisticsModules(controlModel, ref state);
            return state;
        }

        internal V3CargoHabitatFoodState GetHabitatCargoFoodState(
            ShuttleControlReadModel controlModel)
        {
            V3CargoHabitatFoodState state = new V3CargoHabitatFoodState();
            if (controlModel == null || controlModel.SegmentSlots == null)
            {
                return state;
            }

            for (int i = 0; i < controlModel.SegmentSlots.Count; i++)
            {
                ShuttleControlSegmentSlotModel segment = controlModel.SegmentSlots[i];
                if (segment == null || segment.ModuleSlots == null)
                {
                    continue;
                }

                for (int j = 0; j < segment.ModuleSlots.Count; j++)
                {
                    this.MergeHabitatModule(segment.ModuleSlots[j], ref state);
                }
            }

            return state;
        }

        internal bool HasMatchingModule(
            ShuttleControlReadModel controlModel,
            bool requireEnabled,
            params string[] needles)
        {
            if (controlModel == null ||
                controlModel.SegmentSlots == null ||
                needles == null)
            {
                return false;
            }

            for (int i = 0; i < controlModel.SegmentSlots.Count; i++)
            {
                ShuttleControlSegmentSlotModel segment = controlModel.SegmentSlots[i];
                if (segment == null || segment.ModuleSlots == null)
                {
                    continue;
                }

                for (int j = 0; j < segment.ModuleSlots.Count; j++)
                {
                    ShuttleControlModuleSlotModel module = segment.ModuleSlots[j];
                    if (this.ModuleMatches(module, requireEnabled, needles))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void MergeInstalledCargoLogisticsModules(
            ShuttleControlReadModel controlModel,
            ref V3CargoLogisticsState state)
        {
            if (controlModel == null || controlModel.SegmentSlots == null)
            {
                return;
            }

            for (int i = 0; i < controlModel.SegmentSlots.Count; i++)
            {
                ShuttleControlSegmentSlotModel segment = controlModel.SegmentSlots[i];
                if (segment == null || segment.ModuleSlots == null)
                {
                    continue;
                }

                for (int j = 0; j < segment.ModuleSlots.Count; j++)
                {
                    this.MergeCargoLogisticsModule(segment.ModuleSlots[j], ref state);
                }
            }
        }

        private void MergeCargoLogisticsModule(
            ShuttleControlModuleSlotModel module,
            ref V3CargoLogisticsState state)
        {
            if (module == null || string.IsNullOrEmpty(module.InstalledModuleInstanceID))
            {
                return;
            }

            ShuttleCargoLogisticsModuleDef logisticsDef =
                this.GetModuleDef(module.InstalledModuleDefName) as ShuttleCargoLogisticsModuleDef;
            if (logisticsDef == null && !this.ModuleTextContains(module, "logistics"))
            {
                return;
            }

            state.Installed = true;
            state.Enabled = state.Enabled || module.InstalledModuleEnabled;
            if (module.InstalledModuleEnabled && logisticsDef != null)
            {
                state.SupportsItemTransfer |= logisticsDef.supportsItemTransfer;
                state.SupportsItemConsumption |= logisticsDef.supportsItemConsumption;
                state.SupportsItemDeposit |= logisticsDef.supportsItemDeposit;
            }

            if (string.IsNullOrEmpty(state.Label))
            {
                state.Label = this.ResolveModuleLabel(module);
            }
        }

        private void MergeHabitatModule(
            ShuttleControlModuleSlotModel module,
            ref V3CargoHabitatFoodState state)
        {
            if (module == null || string.IsNullOrEmpty(module.InstalledModuleDefName))
            {
                return;
            }

            ShuttleHabitatModuleDef habitatDef =
                this.GetModuleDef(module.InstalledModuleDefName) as ShuttleHabitatModuleDef;
            if (habitatDef == null)
            {
                return;
            }

            state.Installed = true;
            if (!module.InstalledModuleEnabled)
            {
                return;
            }

            state.Enabled = true;
            if (!habitatDef.allowCargoFoodWithdrawal)
            {
                return;
            }

            state.AllowsCargoFoodWithdrawal = true;
            if (habitatDef.requireCargoLogisticsForFoodWithdrawal)
            {
                state.RequiresCargoLogistics = true;
            }
            else
            {
                state.HasNoLogisticsRequiredCargoFoodWithdrawal = true;
            }
        }

        private bool ModuleMatches(
            ShuttleControlModuleSlotModel module,
            bool requireEnabled,
            string[] needles)
        {
            if (module == null || string.IsNullOrEmpty(module.InstalledModuleInstanceID))
            {
                return false;
            }

            if (requireEnabled && !module.InstalledModuleEnabled)
            {
                return false;
            }

            string text = this.JoinLower(
                module.InstalledModuleDefName,
                module.InstalledModuleTypeID,
                module.InstalledModuleLabel,
                module.SlotTypeID,
                module.SlotID);
            for (int i = 0; i < needles.Length; i++)
            {
                if (!string.IsNullOrEmpty(needles[i]) &&
                    text.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ModuleTextContains(
            ShuttleControlModuleSlotModel module,
            string needle)
        {
            return this.ModuleMatches(module, false, new string[] { needle });
        }

        private ShuttleModuleBaseDef GetModuleDef(string defName)
        {
            return string.IsNullOrEmpty(defName)
                ? null
                : DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(defName);
        }

        private string ResolveModuleLabel(ShuttleControlModuleSlotModel module)
        {
            if (module == null)
            {
                return ShuttleUIText.Tr("CT_Shuttle_UI_MissingModule");
            }

            string resolved =
                ShuttleAssemblyDisplayTextResolver.ResolveModuleDisplayName(module);
            if (!string.IsNullOrEmpty(resolved) && resolved != "-")
            {
                return resolved;
            }

            ShuttleModuleBaseDef moduleDef = this.GetModuleDef(module.InstalledModuleDefName);
            if (moduleDef != null)
            {
                return ShuttleAssemblyDisplayTextResolver.ResolveDefDisplayName(moduleDef);
            }

            return !string.IsNullOrEmpty(module.InstalledModuleDefName)
                ? module.InstalledModuleDefName
                : ShuttleUIText.Tr("CT_Shuttle_UI_MissingModule");
        }

        private string JoinLower(params string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return string.Empty;
            }

            string result = string.Empty;
            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrEmpty(values[i]))
                {
                    result += " " + values[i].ToLowerInvariant();
                }
            }

            return result;
        }
    }

    internal struct V3CargoLogisticsState
    {
        internal bool Installed;
        internal bool Enabled;
        internal bool SupportsItemTransfer;
        internal bool SupportsItemConsumption;
        internal bool SupportsItemDeposit;
        internal bool InternalBusPowered;
        internal string Label;

        internal bool SupportsPoweredItemTransfer
        {
            get
            {
                return this.Installed &&
                    this.Enabled &&
                    this.SupportsItemTransfer &&
                    this.InternalBusPowered;
            }
        }

        internal bool SupportsPoweredItemConsumption
        {
            get
            {
                return this.Installed &&
                    this.Enabled &&
                    this.SupportsItemConsumption &&
                    this.InternalBusPowered;
            }
        }

        internal bool SupportsPoweredItemConsumptionAndDeposit
        {
            get
            {
                return this.Installed &&
                    this.Enabled &&
                    this.SupportsItemConsumption &&
                    this.SupportsItemDeposit &&
                    this.InternalBusPowered;
            }
        }
    }

    internal struct V3CargoHabitatFoodState
    {
        internal bool Installed;
        internal bool Enabled;
        internal bool AllowsCargoFoodWithdrawal;
        internal bool RequiresCargoLogistics;
        internal bool HasNoLogisticsRequiredCargoFoodWithdrawal;
    }
}
