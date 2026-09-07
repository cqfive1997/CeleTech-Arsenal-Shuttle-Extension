using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    internal sealed class ShuttleRefrigeratedCargoCommandHandler : IShuttleCommandHandler
    {
        public bool CanHandle(IShuttleCommand command)
        {
            return command is TransferLoadedCargoToRefrigeratedCargoCommand ||
                command is TransferRefrigeratedCargoToLoadedCargoCommand ||
                command is TransferLoadedCargoGroupToRefrigeratedCargoCommand ||
                command is TransferRefrigeratedCargoGroupToLoadedCargoCommand ||
                command is UnloadRefrigeratedCargoBayCommand ||
                command is SetRefrigeratedCargoAutoTransferEnabledCommand ||
                command is SetRefrigeratedCargoAutoTransferFilterCommand ||
                command is ClearRefrigeratedCargoAutoTransferFilterCommand ||
                command is SetRefrigeratedCargoLabelCommand;
        }

        public ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context)
        {
            if (context == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            TransferLoadedCargoToRefrigeratedCargoCommand toCold =
                command as TransferLoadedCargoToRefrigeratedCargoCommand;
            if (toCold != null)
            {
                return this.ExecuteTransferLoadedCargoToCold(context, toCold);
            }

            TransferRefrigeratedCargoToLoadedCargoCommand toNormal =
                command as TransferRefrigeratedCargoToLoadedCargoCommand;
            if (toNormal != null)
            {
                return this.ExecuteTransferColdCargoToLoaded(context, toNormal);
            }

            TransferLoadedCargoGroupToRefrigeratedCargoCommand groupToCold =
                command as TransferLoadedCargoGroupToRefrigeratedCargoCommand;
            if (groupToCold != null)
            {
                return this.ExecuteTransferLoadedCargoGroupToCold(
                    context,
                    groupToCold);
            }

            TransferRefrigeratedCargoGroupToLoadedCargoCommand groupToNormal =
                command as TransferRefrigeratedCargoGroupToLoadedCargoCommand;
            if (groupToNormal != null)
            {
                return this.ExecuteTransferColdCargoGroupToLoaded(
                    context,
                    groupToNormal);
            }

            UnloadRefrigeratedCargoBayCommand unloadBay =
                command as UnloadRefrigeratedCargoBayCommand;
            if (unloadBay != null)
            {
                return this.ExecuteUnloadRefrigeratedCargoBay(context, unloadBay);
            }

            SetRefrigeratedCargoAutoTransferEnabledCommand setEnabled =
                command as SetRefrigeratedCargoAutoTransferEnabledCommand;
            if (setEnabled != null)
            {
                return this.ExecuteSetAutoTransferEnabled(context, setEnabled);
            }

            SetRefrigeratedCargoAutoTransferFilterCommand setFilter =
                command as SetRefrigeratedCargoAutoTransferFilterCommand;
            if (setFilter != null)
            {
                return this.ExecuteSetAutoTransferFilter(context, setFilter);
            }

            ClearRefrigeratedCargoAutoTransferFilterCommand clearFilter =
                command as ClearRefrigeratedCargoAutoTransferFilterCommand;
            if (clearFilter != null)
            {
                return this.ExecuteClearAutoTransferFilter(context, clearFilter);
            }

            SetRefrigeratedCargoLabelCommand setLabel =
                command as SetRefrigeratedCargoLabelCommand;
            if (setLabel != null)
            {
                return this.ExecuteSetLabel(context, setLabel);
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_UnsupportedRefrigeratedCargo".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteTransferLoadedCargoToCold(
            ShuttleCommandContext context,
            TransferLoadedCargoToRefrigeratedCargoCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoTransferMissing".Translate().ToString());
            }


            if (this.HasActiveGlobalUnload(context))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Cargo_ManualMoveBlockedByUnloading".Translate().ToString());
            }

            ShuttleProfile profile = context.ReconcileProfileToHost();
            IShuttleCargoColdTransferService service = this.BuildService(context, profile);
            int movedCount;
            string failureReason;
            if (!service.TryTransferLoadedCargoToCold(
                command.ModuleInstanceID,
                command.TransporterIndex,
                command.LoadedIndex,
                command.ThingIDNumber,
                command.DefName,
                command.Count,
                "manual-command",
                out movedCount,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_RefrigeratedCargoMovedToCold".Translate(movedCount).ToString());
        }

        private ShuttleCommandResult ExecuteTransferColdCargoToLoaded(
            ShuttleCommandContext context,
            TransferRefrigeratedCargoToLoadedCargoCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoTransferMissing".Translate().ToString());
            }


            if (this.HasActiveGlobalUnload(context))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Cargo_ManualMoveBlockedByUnloading".Translate().ToString());
            }

            ShuttleProfile profile = context.ReconcileProfileToHost();
            IShuttleCargoColdTransferService service = this.BuildService(context, profile);
            int movedCount;
            string failureReason;
            if (!service.TryTransferColdCargoToLoadedCargo(
                command.ModuleInstanceID,
                command.ColdIndex,
                command.ThingIDNumber,
                command.DefName,
                command.Count,
                "manual-command",
                out movedCount,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_RefrigeratedCargoMovedToNormal".Translate(movedCount).ToString());
        }

        private ShuttleCommandResult ExecuteTransferLoadedCargoGroupToCold(
            ShuttleCommandContext context,
            TransferLoadedCargoGroupToRefrigeratedCargoCommand command)
        {
            int requestedTotal;
            string validationFailure;
            if (!this.TryValidateLoadedCargoGroupCommand(
                command,
                out requestedTotal,
                out validationFailure))
            {
                return ShuttleCommandResult.Failed(validationFailure);
            }

            if (this.HasActiveGlobalUnload(context))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Cargo_ManualMoveBlockedByUnloading".Translate().ToString());
            }

            ShuttleProfile profile = context.ReconcileProfileToHost();
            IShuttleCargoColdTransferService service = this.BuildService(context, profile);
            int movedTotal = 0;
            for (int i = 0; i < command.Entries.Count; i++)
            {
                TransferLoadedCargoGroupEntry entry = command.Entries[i];
                int movedCount;
                string failureReason;
                if (!service.TryTransferLoadedCargoToCold(
                    command.ModuleInstanceID,
                    entry.TransporterIndex,
                    entry.LoadedIndex,
                    entry.ThingIDNumber,
                    entry.DefName,
                    entry.Count,
                    "manual-group-command",
                    out movedCount,
                    out failureReason))
                {
                    return this.BuildGroupTransferFailure(
                        movedTotal,
                        requestedTotal,
                        failureReason);
                }

                movedTotal += movedCount;
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_RefrigeratedCargoMovedToCold".Translate(movedTotal).ToString());
        }

        private ShuttleCommandResult ExecuteTransferColdCargoGroupToLoaded(
            ShuttleCommandContext context,
            TransferRefrigeratedCargoGroupToLoadedCargoCommand command)
        {
            int requestedTotal;
            string validationFailure;
            if (!this.TryValidateColdCargoGroupCommand(
                command,
                out requestedTotal,
                out validationFailure))
            {
                return ShuttleCommandResult.Failed(validationFailure);
            }

            if (this.HasActiveGlobalUnload(context))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Cargo_ManualMoveBlockedByUnloading".Translate().ToString());
            }

            ShuttleProfile profile = context.ReconcileProfileToHost();
            IShuttleCargoColdTransferService service = this.BuildService(context, profile);
            int movedTotal = 0;
            for (int i = 0; i < command.Entries.Count; i++)
            {
                TransferRefrigeratedCargoGroupEntry entry = command.Entries[i];
                int movedCount;
                string failureReason;
                if (!service.TryTransferColdCargoToLoadedCargo(
                    command.ModuleInstanceID,
                    entry.ColdIndex,
                    entry.ThingIDNumber,
                    entry.DefName,
                    entry.Count,
                    "manual-group-command",
                    out movedCount,
                    out failureReason))
                {
                    return this.BuildGroupTransferFailure(
                        movedTotal,
                        requestedTotal,
                        failureReason);
                }

                movedTotal += movedCount;
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_RefrigeratedCargoMovedToNormal".Translate(movedTotal).ToString());
        }

        private bool TryValidateLoadedCargoGroupCommand(
            TransferLoadedCargoGroupToRefrigeratedCargoCommand command,
            out int requestedTotal,
            out string failureReason)
        {
            requestedTotal = 0;
            failureReason = null;
            if (command == null ||
                string.IsNullOrEmpty(command.ModuleInstanceID) ||
                command.Entries == null ||
                command.Entries.Count == 0)
            {
                failureReason = "CT_Shuttle_Command_RefrigeratedCargoTransferMissing".Translate().ToString();
                return false;
            }

            for (int i = 0; i < command.Entries.Count; i++)
            {
                TransferLoadedCargoGroupEntry entry = command.Entries[i];
                if (entry == null ||
                    entry.TransporterIndex < 0 ||
                    entry.LoadedIndex < 0 ||
                    entry.ThingIDNumber <= 0 ||
                    string.IsNullOrEmpty(entry.DefName) ||
                    entry.Count <= 0 ||
                    requestedTotal > int.MaxValue - entry.Count)
                {
                    failureReason = "CT_Shuttle_Command_RefrigeratedCargoTransferMissing".Translate().ToString();
                    return false;
                }

                requestedTotal += entry.Count;
            }

            return true;
        }

        private bool TryValidateColdCargoGroupCommand(
            TransferRefrigeratedCargoGroupToLoadedCargoCommand command,
            out int requestedTotal,
            out string failureReason)
        {
            requestedTotal = 0;
            failureReason = null;
            if (command == null ||
                string.IsNullOrEmpty(command.ModuleInstanceID) ||
                command.Entries == null ||
                command.Entries.Count == 0)
            {
                failureReason = "CT_Shuttle_Command_RefrigeratedCargoTransferMissing".Translate().ToString();
                return false;
            }

            for (int i = 0; i < command.Entries.Count; i++)
            {
                TransferRefrigeratedCargoGroupEntry entry = command.Entries[i];
                if (entry == null ||
                    entry.ColdIndex < 0 ||
                    entry.ThingIDNumber <= 0 ||
                    string.IsNullOrEmpty(entry.DefName) ||
                    entry.Count <= 0 ||
                    requestedTotal > int.MaxValue - entry.Count)
                {
                    failureReason = "CT_Shuttle_Command_RefrigeratedCargoTransferMissing".Translate().ToString();
                    return false;
                }

                requestedTotal += entry.Count;
            }

            return true;
        }

        private ShuttleCommandResult BuildGroupTransferFailure(
            int movedCount,
            int requestedCount,
            string failureReason)
        {
            string reason = !string.IsNullOrEmpty(failureReason)
                ? failureReason
                : "CT_Shuttle_Command_RefrigeratedCargoTransferMissing".Translate().ToString();
            if (movedCount <= 0)
            {
                return ShuttleCommandResult.Failed(reason);
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_CargoGroupTransferPartial".Translate(
                    movedCount,
                    requestedCount,
                    reason).ToString());
        }

        private ShuttleCommandResult ExecuteUnloadRefrigeratedCargoBay(
            ShuttleCommandContext context,
            UnloadRefrigeratedCargoBayCommand command)
        {
            if (command == null ||
                string.IsNullOrEmpty(command.ModuleInstanceID) ||
                command.Entries == null ||
                command.Entries.Count == 0)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Cargo_BayUnloadNoContents".Translate().ToString());
            }


            if (this.HasActiveGlobalUnload(context))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Cargo_ManualMoveBlockedByUnloading".Translate().ToString());
            }

            ShuttleProfile profile = context.ReconcileProfileToHost();
            IShuttleCargoColdTransferService service = this.BuildService(context, profile);
            int unloadedStackCount;
            int unloadedThingCount;
            string failureReason;
            if (!service.TryUnloadColdCargoBay(
                command.ModuleInstanceID,
                this.BuildColdUnloadTargets(command.Entries),
                "manual-command",
                out unloadedStackCount,
                out unloadedThingCount,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_RefrigeratedCargoBayUnloaded".Translate(
                    unloadedStackCount,
                    unloadedThingCount).ToString());
        }

        private ShuttleCommandResult ExecuteSetAutoTransferEnabled(
            ShuttleCommandContext context,
            SetRefrigeratedCargoAutoTransferEnabledCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoConfigMissing".Translate().ToString());
            }

            ShuttleModule module;
            ShuttleRefrigeratedCargoModuleDef moduleDef;
            ShuttleRefrigeratedCargoConfigState configState;
            ShuttleCommandResult validation = this.TryResolveConfigTarget(
                context,
                command.ModuleInstanceID,
                out module,
                out moduleDef,
                out configState);
            if (!validation.Success)
            {
                return validation;
            }

            if (!module.IsEnabled)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoModuleDisabled".Translate().ToString());
            }

            ShuttleRefrigeratedCargoModuleConfig config =
                configState.GetOrCreateModuleConfig(module.ModuleInstanceID, moduleDef);
            if (config == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoConfigUnavailable".Translate().ToString());
            }

            config.SetAutoTransferEnabled(command.Enabled);
            this.MarkRefrigeratedConfigChanged(context);
            this.TryReconcileEligibleOrdinaryCargo(
                context,
                module.ModuleInstanceID,
                config,
                "cold-routing-enabled");
            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_RefrigeratedCargoAutoTransferUpdated".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetAutoTransferFilter(
            ShuttleCommandContext context,
            SetRefrigeratedCargoAutoTransferFilterCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoConfigMissing".Translate().ToString());
            }

            ShuttleModule module;
            ShuttleRefrigeratedCargoModuleDef moduleDef;
            ShuttleRefrigeratedCargoConfigState configState;
            ShuttleCommandResult validation = this.TryResolveConfigTarget(
                context,
                command.ModuleInstanceID,
                out module,
                out moduleDef,
                out configState);
            if (!validation.Success)
            {
                return validation;
            }

            if (!module.IsEnabled)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoModuleDisabled".Translate().ToString());
            }

            ShuttleRefrigeratedCargoModuleConfig config =
                configState.GetOrCreateModuleConfig(module.ModuleInstanceID, moduleDef);
            if (config == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoConfigUnavailable".Translate().ToString());
            }

            config.SetCustomAutoTransferFilter(command.AutoTransferFilter);
            this.MarkRefrigeratedConfigChanged(context);
            this.TryReconcileEligibleOrdinaryCargo(
                context,
                module.ModuleInstanceID,
                config,
                "cold-routing-filter-updated");
            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_RefrigeratedCargoAutoTransferFilterUpdated".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteClearAutoTransferFilter(
            ShuttleCommandContext context,
            ClearRefrigeratedCargoAutoTransferFilterCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoConfigMissing".Translate().ToString());
            }

            ShuttleModule module;
            ShuttleRefrigeratedCargoModuleDef moduleDef;
            ShuttleRefrigeratedCargoConfigState configState;
            ShuttleCommandResult validation = this.TryResolveConfigTarget(
                context,
                command.ModuleInstanceID,
                out module,
                out moduleDef,
                out configState);
            if (!validation.Success)
            {
                return validation;
            }

            if (!module.IsEnabled)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoModuleDisabled".Translate().ToString());
            }

            ShuttleRefrigeratedCargoModuleConfig config =
                configState.GetOrCreateModuleConfig(module.ModuleInstanceID, moduleDef);
            if (config == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoConfigUnavailable".Translate().ToString());
            }

            config.ClearCustomAutoTransferFilter();
            this.MarkRefrigeratedConfigChanged(context);
            this.TryReconcileEligibleOrdinaryCargo(
                context,
                module.ModuleInstanceID,
                config,
                "cold-routing-filter-cleared");
            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_RefrigeratedCargoAutoTransferFilterCleared".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetLabel(
            ShuttleCommandContext context,
            SetRefrigeratedCargoLabelCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoConfigMissing".Translate().ToString());
            }

            ShuttleModule module;
            ShuttleRefrigeratedCargoModuleDef moduleDef;
            ShuttleRefrigeratedCargoConfigState configState;
            ShuttleCommandResult validation = this.TryResolveConfigTarget(
                context,
                command.ModuleInstanceID,
                out module,
                out moduleDef,
                out configState);
            if (!validation.Success)
            {
                return validation;
            }

            ShuttleRefrigeratedCargoModuleConfig config =
                configState.GetOrCreateModuleConfig(module.ModuleInstanceID, moduleDef);
            if (config == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoConfigUnavailable".Translate().ToString());
            }

            config.SetLabel(command.Label);
            this.MarkRefrigeratedConfigChanged(context);
            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_RefrigeratedCargoLabelUpdated".Translate().ToString());
        }

        private ShuttleCommandResult TryResolveConfigTarget(
            ShuttleCommandContext context,
            string moduleInstanceID,
            out ShuttleModule module,
            out ShuttleRefrigeratedCargoModuleDef moduleDef,
            out ShuttleRefrigeratedCargoConfigState configState)
        {
            module = null;
            moduleDef = null;
            configState = null;

            if (context == null || context.AssemblyState == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_AssemblyUnavailable".Translate().ToString());
            }

            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoModuleMissing".Translate().ToString());
            }

            module = context.AssemblyState.GetModule(moduleInstanceID);
            moduleDef = module != null ? module.ModuleDef as ShuttleRefrigeratedCargoModuleDef : null;
            if (module == null || moduleDef == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoModuleMissing".Translate().ToString());
            }

            configState = context.AssemblyState.RefrigeratedCargoConfig;
            if (configState == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_RefrigeratedCargoConfigUnavailable".Translate().ToString());
            }

            return ShuttleCommandResult.Succeeded(string.Empty);
        }

        private void MarkRefrigeratedConfigChanged(ShuttleCommandContext context)
        {
            if (context != null && context.AssemblyState != null)
            {
                context.AssemblyState.MarkDirty(ShuttleDirtyFlags.RuntimeSync | ShuttleDirtyFlags.ReadModel);
            }
        }

        private void TryReconcileEligibleOrdinaryCargo(
            ShuttleCommandContext context,
            string moduleInstanceID,
            ShuttleRefrigeratedCargoModuleConfig config,
            string reason)
        {
            if (context == null || config == null || !config.AutoTransferEnabled)
            {
                return;
            }

            ShuttleProfile profile = context.ReconcileProfileToHost();
            IShuttleCargoColdTransferService service = this.BuildService(context, profile);
            int movedStackCount;
            int movedThingCount;
            string failureReason;
            ShuttleRefrigeratedCargoEventReconciler.TryReconcile(
                context.AssemblyState,
                moduleInstanceID,
                service,
                reason,
                out movedStackCount,
                out movedThingCount,
                out failureReason);
        }

        private IShuttleCargoColdTransferService BuildService(
            ShuttleCommandContext context,
            ShuttleProfile profile)
        {
            return new ShuttleCargoColdTransferService(
                context.Host,
                context.AssemblyState,
                context.GetRuntimeState(),
                profile,
                context.CargoBackend);
        }

        private List<ShuttleColdCargoUnloadTarget> BuildColdUnloadTargets(
            List<UnloadRefrigeratedCargoBayEntry> entries)
        {
            List<ShuttleColdCargoUnloadTarget> targets =
                new List<ShuttleColdCargoUnloadTarget>();
            for (int i = 0; entries != null && i < entries.Count; i++)
            {
                UnloadRefrigeratedCargoBayEntry entry = entries[i];
                if (entry != null)
                {
                    targets.Add(new ShuttleColdCargoUnloadTarget(
                        entry.ColdIndex,
                        entry.ThingIDNumber,
                        entry.DefName,
                        entry.Count));
                }
            }

            return targets;
        }

        private bool HasActiveGlobalUnload(ShuttleCommandContext context)
        {
            ShuttleRuntimeState runtimeState = context != null
                ? context.GetRuntimeState()
                : null;
            return runtimeState != null &&
                runtimeState.CargoUnload != null &&
                runtimeState.CargoUnload.IsActive;
        }
    }
}
