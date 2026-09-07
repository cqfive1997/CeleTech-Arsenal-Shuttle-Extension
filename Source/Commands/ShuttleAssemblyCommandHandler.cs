using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyMutation;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Handles assembly topology mutations. It mutates AssemblyState through the mutation
    /// controller and never owns profile or runtime truth itself.
    /// </summary>
    internal sealed class ShuttleAssemblyCommandHandler : IShuttleCommandHandler
    {
        private readonly ShuttleAssemblyMutationGuard mutationGuard = new ShuttleAssemblyMutationGuard();

        public bool CanHandle(IShuttleCommand command)
        {
            return command is InstallSegmentCommand ||
                   command is ReplaceSegmentCommand ||
                   command is InstallModuleCommand ||
                   command is ReplaceModuleCommand ||
                   command is SetModuleEnabledCommand ||
                   command is SetSegmentModulesEnabledCommand;
        }

        public ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context)
        {
            if (context == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            InstallSegmentCommand installSegment = command as InstallSegmentCommand;
            if (installSegment != null)
            {
                return this.ExecuteInstallSegment(context, installSegment);
            }

            ReplaceSegmentCommand replaceSegment = command as ReplaceSegmentCommand;
            if (replaceSegment != null)
            {
                return this.ExecuteReplaceSegment(context, replaceSegment);
            }

            InstallModuleCommand installModule = command as InstallModuleCommand;
            if (installModule != null)
            {
                return this.ExecuteInstallModule(context, installModule);
            }

            ReplaceModuleCommand replaceModule = command as ReplaceModuleCommand;
            if (replaceModule != null)
            {
                return this.ExecuteReplaceModule(context, replaceModule);
            }

            SetModuleEnabledCommand setModuleEnabled = command as SetModuleEnabledCommand;
            if (setModuleEnabled != null)
            {
                return this.ExecuteSetModuleEnabled(context, setModuleEnabled);
            }

            SetSegmentModulesEnabledCommand setSegmentModulesEnabled =
                command as SetSegmentModulesEnabledCommand;
            if (setSegmentModulesEnabled != null)
            {
                return this.ExecuteSetSegmentModulesEnabled(context, setSegmentModulesEnabled);
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_UnsupportedAssembly".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteInstallSegment(ShuttleCommandContext context, InstallSegmentCommand command)
        {
            // Assembly work is not gated by internal shuttle power. Lack of stored energy,
            // reactor output, grid export, or an online bus affects installed module runtime
            // behavior, not segment/module install or replacement eligibility.
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_SegmentInstallMissing".Translate().ToString());
            }

            ShuttleSegmentBaseDef segmentDef = DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(command.SegmentDefName);
            if (segmentDef == null)
            {
                ShuttleLog.Warn("AssemblyCommand", "InstallSegment could not resolve segment def " + command.SegmentDefName + ".");
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_SegmentInstallFailed".Translate().ToString());
            }

            if (!ShuttleResearchGateUtility.IsUnlocked(segmentDef))
            {
                return ShuttleCommandResult.Failed(this.BuildSegmentResearchLockedMessage(segmentDef));
            }

            string failureReason;
            if (!this.mutationGuard.CanInstallSegment(
                context,
                command.SegmentSlotID,
                segmentDef,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            bool result = this.InstallSegment(context, command.SegmentSlotID, segmentDef);
            return result
                ? ShuttleCommandResult.Succeeded("CT_Shuttle_Command_SegmentInstalled".Translate().ToString(), true)
                : ShuttleCommandResult.Failed("CT_Shuttle_Command_SegmentInstallFailed".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteReplaceSegment(ShuttleCommandContext context, ReplaceSegmentCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_SegmentReplaceMissing".Translate().ToString());
            }

            ShuttleSegmentBaseDef segmentDef =
                DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(command.SegmentDefName);
            if (segmentDef == null)
            {
                ShuttleLog.Warn("AssemblyCommand", "ReplaceSegment could not resolve segment def " + command.SegmentDefName + ".");
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_SegmentReplaceMissingDef".Translate().ToString());
            }

            if (!segmentDef.playerInstallable)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleDefNotInstallable".Translate().ToString());
            }

            if (!ShuttleResearchGateUtility.IsUnlocked(segmentDef))
            {
                return ShuttleCommandResult.Failed(this.BuildSegmentResearchLockedMessage(segmentDef));
            }

            string failureReason;
            if (!this.mutationGuard.CanReplaceSegment(
                context,
                command.SegmentSlotID,
                segmentDef,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            bool result = this.ReplaceSegment(context, command.SegmentSlotID, segmentDef);
            return result
                ? ShuttleCommandResult.Succeeded("CT_Shuttle_Command_SegmentReplaced".Translate().ToString(), true)
                : ShuttleCommandResult.Failed("CT_Shuttle_Command_SegmentReplaceFailed".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteInstallModule(ShuttleCommandContext context, InstallModuleCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleInstallMissing".Translate().ToString());
            }

            string failureReason;
            ShuttleModuleBaseDef moduleDef = DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(command.ModuleDefName);
            if (moduleDef == null)
            {
                ShuttleLog.Warn("AssemblyCommand", "InstallModule could not resolve module def " + command.ModuleDefName + ".");
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleInstallMissingDef".Translate().ToString());
            }

            if (!moduleDef.playerInstallable)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleDefNotInstallable".Translate().ToString());
            }

            if (!ShuttleResearchGateUtility.IsUnlocked(moduleDef))
            {
                return ShuttleCommandResult.Failed(this.BuildModuleResearchLockedMessage(moduleDef));
            }

            if (!this.mutationGuard.CanInstallModule(
                context,
                command.SegmentInstanceID,
                command.ModuleSlotID,
                moduleDef,
                false,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            bool result = this.InstallModule(
                context,
                command.SegmentInstanceID,
                command.ModuleSlotID,
                moduleDef,
                command.SelectedStuffDefName);
            return result
                ? ShuttleCommandResult.Succeeded("CT_Shuttle_Command_ModuleInstalled".Translate().ToString(), true)
                : ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleInstallFailed".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteReplaceModule(ShuttleCommandContext context, ReplaceModuleCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleReplaceMissing".Translate().ToString());
            }

            string failureReason;
            ShuttleModuleBaseDef moduleDef = DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(command.ModuleDefName);
            if (moduleDef == null)
            {
                ShuttleLog.Warn("AssemblyCommand", "ReplaceModule could not resolve module def " + command.ModuleDefName + ".");
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleReplaceMissingDef".Translate().ToString());
            }

            if (!moduleDef.playerInstallable)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleDefNotInstallable".Translate().ToString());
            }

            if (!ShuttleResearchGateUtility.IsUnlocked(moduleDef))
            {
                return ShuttleCommandResult.Failed(this.BuildModuleResearchLockedMessage(moduleDef));
            }

            if (!this.mutationGuard.CanReplaceModule(
                context,
                command.SegmentInstanceID,
                command.ModuleSlotID,
                moduleDef,
                ShuttleTickUtility.TicksGameOrMinusOne(),
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            ShuttleModule oldModule = this.FindInstalledModule(context.AssemblyState, command.SegmentInstanceID, command.ModuleSlotID);
            ShuttleProfile profile;
            bool result = this.ReplaceModule(
                context,
                command.SegmentInstanceID,
                command.ModuleSlotID,
                moduleDef,
                command.SelectedStuffDefName,
                out profile);
            if (result && context.ModuleRuntimeCoordinator != null)
            {
                ShuttleRuntimeState runtimeState = context.GetRuntimeState();
                int ticksGame = ShuttleTickUtility.TicksGameOrMinusOne();

                context.ModuleRuntimeCoordinator.NotifyModuleRemoved(
                    context.Host,
                    context.AssemblyState,
                    profile,
                    runtimeState,
                    oldModule,
                    context.StoredEnergySink,
                    ticksGame);

                ShuttleModule newModule = this.FindInstalledModule(context.AssemblyState, command.SegmentInstanceID, command.ModuleSlotID);
                if (newModule != null)
                {
                    context.ModuleRuntimeCoordinator.NotifyModuleInstalled(
                        context.Host,
                        context.AssemblyState,
                        profile,
                        runtimeState,
                        newModule,
                        context.StoredEnergySink,
                        ticksGame);
                }
                else
                {
                    ShuttleLog.Warn(
                        "AssemblyCommand",
                        "ReplaceModule succeeded but no replacement module was found in " +
                        command.SegmentInstanceID + "/" + command.ModuleSlotID + ".");
                }
            }

            return result
                ? ShuttleCommandResult.Succeeded("CT_Shuttle_Command_ModuleReplaced".Translate().ToString(), true)
                : ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleReplaceFailed".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetModuleEnabled(
            ShuttleCommandContext context,
            SetModuleEnabledCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleEnableMissing".Translate().ToString());
            }

            string failureReason;
            if (!this.TryValidateAssemblyIntegrityForEnablement(context, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            ShuttleModuleSlot slot;
            ShuttleModule module;
            if (!this.TryResolveInstalledModule(
                context.AssemblyState,
                command.SegmentInstanceID,
                command.ModuleSlotID,
                command.ModuleInstanceID,
                out slot,
                out module,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (module.IsEnabled == command.Enabled)
            {
                return ShuttleCommandResult.Succeeded(this.GetModuleAlreadyEnablementMessage(command.Enabled));
            }

            if (!this.CanSetModuleEnabled(context, slot, module, command.Enabled, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            bool changed = this.SetModuleEnabled(
                context,
                command.SegmentInstanceID,
                command.ModuleSlotID,
                command.Enabled);
            return changed
                ? ShuttleCommandResult.Succeeded(this.GetModuleEnablementChangedMessage(command.Enabled), true)
                : ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleEnableFailed".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetSegmentModulesEnabled(
            ShuttleCommandContext context,
            SetSegmentModulesEnabledCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.SegmentSlotID))
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_SegmentModuleEnableMissing".Translate().ToString());
            }

            string failureReason;
            if (!this.TryValidateAssemblyIntegrityForEnablement(context, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            ShuttleSegment segment = this.FindInstalledSegmentInSegmentSlot(
                context.AssemblyState,
                command.SegmentSlotID);
            if (segment == null || segment.ModuleSlots == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_SegmentModuleEnableTargetMissing".Translate().ToString());
            }

            int changedCount = 0;
            int eligibleCount = 0;
            int skippedCount = 0;
            string firstFailureReason = null;

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleModuleSlot slot = segment.ModuleSlots[i];
                if (slot == null || string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
                {
                    continue;
                }

                ShuttleModule module = context.AssemblyState != null
                    ? context.AssemblyState.GetModule(slot.InstalledModuleInstanceID)
                    : null;
                if (module == null)
                {
                    skippedCount++;
                    if (firstFailureReason == null)
                    {
                        firstFailureReason = "CT_Shuttle_Command_ModuleEnableTargetMissing".Translate().ToString();
                    }

                    continue;
                }

                if (module.IsEnabled == command.Enabled)
                {
                    continue;
                }

                string slotFailureReason;
                if (!this.CanSetModuleEnabled(context, slot, module, command.Enabled, out slotFailureReason))
                {
                    skippedCount++;
                    if (firstFailureReason == null)
                    {
                        firstFailureReason = slotFailureReason;
                    }

                    continue;
                }

                eligibleCount++;
                if (this.SetModuleEnabled(context, segment.SegmentInstanceID, slot.SlotID, command.Enabled))
                {
                    changedCount++;
                }
                else
                {
                    skippedCount++;
                    if (firstFailureReason == null)
                    {
                        firstFailureReason = "CT_Shuttle_Command_ModuleEnableFailed".Translate().ToString();
                    }
                }
            }

            if (changedCount > 0)
            {
                string key = command.Enabled
                    ? skippedCount > 0
                        ? "CT_Shuttle_Command_SegmentModulesEnabledPartial"
                        : "CT_Shuttle_Command_SegmentModulesEnabled"
                    : skippedCount > 0
                        ? "CT_Shuttle_Command_SegmentModulesDisabledPartial"
                        : "CT_Shuttle_Command_SegmentModulesDisabled";
                return skippedCount > 0
                    ? ShuttleCommandResult.Succeeded(key.Translate(changedCount, skippedCount).ToString(), true)
                    : ShuttleCommandResult.Succeeded(key.Translate(changedCount).ToString(), true);
            }

            if (!string.IsNullOrEmpty(firstFailureReason))
            {
                return ShuttleCommandResult.Failed(firstFailureReason);
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_SegmentModuleEnableNoEligibleModules".Translate().ToString());
        }

        private bool InstallSegment(ShuttleCommandContext context, string segmentSlotID, ShuttleSegmentBaseDef segmentDef)
        {
            if (segmentDef == null)
            {
                ShuttleLog.Warn("AssemblyCommand", "InstallSegment called with null segment def.");
                return false;
            }

            string failureReason;
            if (!this.mutationGuard.CanInstallSegment(
                context,
                segmentSlotID,
                segmentDef,
                out failureReason))
            {
                ShuttleLog.Warn("AssemblyCommand", "InstallSegment blocked by mutation guard: " + failureReason);
                return false;
            }

            if (context.AssemblyMutationController == null ||
                !context.AssemblyMutationController.InstallSegment(context.AssemblyState, segmentSlotID, segmentDef))
            {
                return false;
            }

            context.MarkProfileDirty(ProfileDirtyReason.AssemblyChanged);
            context.ReconcileProfileToHost();
            return true;
        }

        private bool ReplaceSegment(
            ShuttleCommandContext context,
            string segmentSlotID,
            ShuttleSegmentBaseDef segmentDef)
        {
            if (segmentDef == null)
            {
                ShuttleLog.Warn("AssemblyCommand", "ReplaceSegment called with null segment def.");
                return false;
            }

            string failureReason;
            if (!this.mutationGuard.CanReplaceSegment(
                context,
                segmentSlotID,
                segmentDef,
                out failureReason))
            {
                ShuttleLog.Warn("AssemblyCommand", "ReplaceSegment blocked by mutation guard: " + failureReason);
                return false;
            }

            if (context.AssemblyMutationController == null ||
                !context.AssemblyMutationController.ReplaceSegment(
                    context.AssemblyState,
                    segmentSlotID,
                    segmentDef))
            {
                return false;
            }

            context.MarkProfileDirty(ProfileDirtyReason.AssemblyChanged);
            context.ReconcileProfileToHost();
            return true;
        }

        private bool InstallModule(
            ShuttleCommandContext context,
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            string selectedStuffDefName)
        {
            string failureReason;
            if (!this.mutationGuard.CanInstallModule(
                context,
                segmentInstanceID,
                moduleSlotID,
                moduleDef,
                false,
                out failureReason))
            {
                ShuttleLog.Warn("AssemblyCommand", "InstallModule blocked by mutation guard: " + failureReason);
                return false;
            }

            if (context.AssemblyMutationController == null ||
                !context.AssemblyMutationController.InstallModule(
                    context.AssemblyState,
                    segmentInstanceID,
                    moduleSlotID,
                    moduleDef,
                    selectedStuffDefName))
            {
                return false;
            }

            context.MarkProfileDirty(ProfileDirtyReason.AssemblyChanged);
            ShuttleProfile profile = context.ReconcileProfileToHost();
            ShuttleModule module = this.FindInstalledModule(context.AssemblyState, segmentInstanceID, moduleSlotID);
            if (module != null && context.ModuleRuntimeCoordinator != null)
            {
                context.ModuleRuntimeCoordinator.NotifyModuleInstalled(
                    context.Host,
                    context.AssemblyState,
                    profile,
                    context.GetRuntimeState(),
                    module,
                    context.StoredEnergySink,
                    ShuttleTickUtility.TicksGameOrMinusOne());
            }
            return true;
        }

        private bool ReplaceModule(
            ShuttleCommandContext context,
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            string selectedStuffDefName,
            out ShuttleProfile profile)
        {
            profile = null;
            string failureReason;
            if (!this.mutationGuard.CanReplaceModule(
                context,
                segmentInstanceID,
                moduleSlotID,
                moduleDef,
                ShuttleTickUtility.TicksGameOrMinusOne(),
                out failureReason))
            {
                ShuttleLog.Warn("AssemblyCommand", "ReplaceModule blocked by mutation guard: " + failureReason);
                return false;
            }

            if (context.AssemblyMutationController == null ||
                !context.AssemblyMutationController.ReplaceModule(
                    context.AssemblyState,
                    segmentInstanceID,
                    moduleSlotID,
                    moduleDef,
                    selectedStuffDefName))
            {
                return false;
            }

            context.MarkProfileDirty(ProfileDirtyReason.AssemblyChanged);
            profile = context.ReconcileProfileToHost();
            return true;
        }

        private bool SetModuleEnabled(
            ShuttleCommandContext context,
            string segmentInstanceID,
            string moduleSlotID,
            bool enabled)
        {
            if (context.AssemblyMutationController == null ||
                !context.AssemblyMutationController.SetModuleEnabled(
                    context.AssemblyState,
                    segmentInstanceID,
                    moduleSlotID,
                    enabled))
            {
                return false;
            }

            context.MarkProfileDirty(ProfileDirtyReason.AssemblyChanged);
            context.ReconcileProfileToHost();
            return true;
        }

        private bool TryResolveInstalledModule(
            ShuttleAssemblyState state,
            string segmentInstanceID,
            string moduleSlotID,
            string expectedModuleInstanceID,
            out ShuttleModuleSlot slot,
            out ShuttleModule module,
            out string failureReason)
        {
            slot = null;
            module = null;
            failureReason = null;

            if (state == null)
            {
                failureReason = "CT_Shuttle_Command_AssemblyStateUnavailable".Translate().ToString();
                return false;
            }

            slot = state.GetModuleSlot(segmentInstanceID, moduleSlotID);
            if (slot == null || string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
            {
                failureReason = "CT_Shuttle_Command_ModuleEnableTargetMissing".Translate().ToString();
                return false;
            }

            if (!string.IsNullOrEmpty(expectedModuleInstanceID) &&
                slot.InstalledModuleInstanceID != expectedModuleInstanceID)
            {
                failureReason = "CT_Shuttle_Command_ModuleEnableTargetChanged".Translate().ToString();
                return false;
            }

            module = state.GetModule(slot.InstalledModuleInstanceID);
            if (module == null ||
                module.ModuleInstanceID != slot.InstalledModuleInstanceID ||
                !module.IsInstalledIn(segmentInstanceID, moduleSlotID))
            {
                failureReason = "CT_Shuttle_Command_ModuleEnableTargetMissing".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool CanSetModuleEnabled(
            ShuttleCommandContext context,
            ShuttleModuleSlot slot,
            ShuttleModule module,
            bool enabled,
            out string failureReason)
        {
            failureReason = null;
            if (context == null || slot == null || module == null)
            {
                failureReason = "CT_Shuttle_Command_ModuleEnableTargetMissing".Translate().ToString();
                return false;
            }

            if (!this.TryValidateAssemblyIntegrityForEnablement(context, out failureReason))
            {
                return false;
            }

            if (enabled)
            {
                return true;
            }

            if (slot.IsRequired || slot.IsLocked)
            {
                failureReason = "CT_Shuttle_Command_CannotDisableRequiredOrLockedModule".Translate().ToString();
                return false;
            }

            ShuttleModuleBaseDef moduleDef = module.ModuleDef;
            if (moduleDef == null)
            {
                failureReason = "CT_Shuttle_Command_CannotDisableUnknownModule".Translate().ToString();
                return false;
            }

            if (moduleDef is ShuttleCockpitModuleDef || moduleDef.ModuleType == ShuttleModuleType.Cockpit)
            {
                failureReason = "CT_Shuttle_Command_CannotDisableCoreControlModule".Translate().ToString();
                return false;
            }

            return this.mutationGuard.CanRemoveModule(
                context,
                module,
                ShuttleTickUtility.TicksGameOrMinusOne(),
                false,
                out failureReason);
        }

        private bool TryValidateAssemblyIntegrityForEnablement(
            ShuttleCommandContext context,
            out string failureReason)
        {
            failureReason = null;
            if (context == null || context.AssemblyState == null)
            {
                return true;
            }

            return !context.AssemblyState.TryGetBlockingIntegrityFailureReason(out failureReason);
        }

        private string BuildSegmentResearchLockedMessage(ShuttleSegmentBaseDef segmentDef)
        {
            string detail = ShuttleResearchGateUtility.GetLockedReason(segmentDef);
            return string.IsNullOrEmpty(detail)
                ? "CT_Shuttle_Command_SegmentResearchLocked".Translate().ToString()
                : "CT_Shuttle_Command_SegmentResearchLocked".Translate().ToString() + " " + detail;
        }

        private string BuildModuleResearchLockedMessage(ShuttleModuleBaseDef moduleDef)
        {
            string detail = ShuttleResearchGateUtility.GetLockedReason(moduleDef);
            return string.IsNullOrEmpty(detail)
                ? "CT_Shuttle_Command_ModuleResearchLocked".Translate().ToString()
                : "CT_Shuttle_Command_ModuleResearchLocked".Translate().ToString() + " " + detail;
        }

        private string GetModuleEnablementChangedMessage(bool enabled)
        {
            return enabled
                ? "CT_Shuttle_Command_ModuleEnabled".Translate().ToString()
                : "CT_Shuttle_Command_ModuleDisabled".Translate().ToString();
        }

        private string GetModuleAlreadyEnablementMessage(bool enabled)
        {
            return enabled
                ? "CT_Shuttle_Command_ModuleAlreadyEnabled".Translate().ToString()
                : "CT_Shuttle_Command_ModuleAlreadyDisabled".Translate().ToString();
        }

        private ShuttleModule FindInstalledModule(ShuttleAssemblyState state, string segmentInstanceID, string moduleSlotID)
        {
            if (state == null)
            {
                return null;
            }

            ShuttleModuleSlot slot = state.GetModuleSlot(segmentInstanceID, moduleSlotID);
            if (slot == null || string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
            {
                return null;
            }

            return state.GetModule(slot.InstalledModuleInstanceID);
        }

        private ShuttleSegment FindInstalledSegmentInSegmentSlot(ShuttleAssemblyState state, string segmentSlotID)
        {
            if (state == null)
            {
                return null;
            }

            ShuttleSegmentSlot segmentSlot = state.GetSegmentSlot(segmentSlotID);
            if (segmentSlot == null || string.IsNullOrEmpty(segmentSlot.InstalledSegmentInstanceID))
            {
                return null;
            }

            return state.GetSegment(segmentSlot.InstalledSegmentInstanceID);
        }

    }
}
