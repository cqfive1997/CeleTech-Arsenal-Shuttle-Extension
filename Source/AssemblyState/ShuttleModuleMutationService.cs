using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    internal sealed class ShuttleModuleMutationService
    {
        internal bool InstallModule(
            ShuttleAssemblyState state,
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            string selectedStuffDefName)
        {
            if (state == null || moduleDef == null)
            {
                ShuttleLog.Warn("AssemblyMutation", "InstallModule called with null state or moduleDef.");
                return false;
            }

            ShuttleModuleSlot slot;
            string validationFailureReason;
            if (!this.TryValidateModuleInstallTarget(
                state,
                segmentInstanceID,
                moduleSlotID,
                moduleDef,
                false,
                out slot,
                out validationFailureReason))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot install module " + moduleDef.defName + " into " + segmentInstanceID + "/" + moduleSlotID +
                    ": " + validationFailureReason);
                return false;
            }

            string moduleInstanceID;
            if (!state.TryAllocateNextModuleInstanceID(out moduleInstanceID))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot install module " + moduleDef.defName + " into " + segmentInstanceID + "/" + moduleSlotID +
                    ": module instance ID allocator is unavailable.");
                return false;
            }

            ShuttleModule module = new ShuttleModule(
                moduleInstanceID,
                moduleDef,
                segmentInstanceID,
                moduleSlotID);
            module.EnsureInitialized();
            this.ApplyHullStuffSelection(module, moduleDef, selectedStuffDefName);

            state.AddModule(module);
            slot.SetInstalledModuleInstanceID(module.ModuleInstanceID);
            state.RebuildIndexes();
            state.MarkDirty(ShuttleDirtyFlags.AssemblyTopology | ShuttleDirtyFlags.Profile);
            return true;
        }

        internal bool CanInstallModule(
            ShuttleAssemblyState state,
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            bool allowOccupiedSlot,
            out string failureReason)
        {
            ShuttleModuleSlot ignoredSlot;
            return this.TryValidateModuleInstallTarget(
                state,
                segmentInstanceID,
                moduleSlotID,
                moduleDef,
                allowOccupiedSlot,
                out ignoredSlot,
                out failureReason);
        }

        internal bool CanInstallModuleForUI(
            ShuttleAssemblyState state,
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            bool allowOccupiedSlot,
            out string failureReason)
        {
            ShuttleModuleSlot ignoredSlot;
            return this.TryValidateModuleInstallTarget(
                state,
                segmentInstanceID,
                moduleSlotID,
                moduleDef,
                allowOccupiedSlot,
                out ignoredSlot,
                out failureReason);
        }

        internal bool RemoveModule(
            ShuttleAssemblyState state,
            string segmentInstanceID,
            string moduleSlotID)
        {
            if (state == null)
            {
                ShuttleLog.Warn("AssemblyMutation", "RemoveModule called with null state.");
                return false;
            }

            state.EnsureInitialized();
            string safetyFailureReason;
            if (!ShuttleAssemblyMutationRules.CanMutateAssemblyState(state, out safetyFailureReason))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot remove module from " + segmentInstanceID + "/" + moduleSlotID +
                    ": " + safetyFailureReason);
                return false;
            }

            ShuttleModuleSlot slot = state.GetModuleSlot(segmentInstanceID, moduleSlotID);
            if (slot == null || string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot remove module from " + segmentInstanceID + "/" + moduleSlotID + ": slot missing or empty.");
                return false;
            }

            if (slot.IsLocked || slot.IsRequired)
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot remove module from " + segmentInstanceID + "/" + moduleSlotID + ": slot is locked or required.");
                return false;
            }

            ShuttleModule module = state.GetModule(slot.InstalledModuleInstanceID);
            if (module == null)
            {
                ShuttleLog.Error(
                    "AssemblyMutation",
                    "Module slot " + segmentInstanceID + "/" + moduleSlotID +
                    " points to missing module instance " + slot.InstalledModuleInstanceID + ".");
                return false;
            }

            state.RemoveModule(module);
            state.RefrigeratedCargoConfig.RemoveModuleConfig(module.ModuleInstanceID);
            slot.SetInstalledModuleInstanceID(null);
            state.RebuildIndexes();
            state.MarkDirty(ShuttleDirtyFlags.AssemblyTopology | ShuttleDirtyFlags.Profile);
            return true;
        }

        internal bool SetModuleEnabled(
            ShuttleAssemblyState state,
            string segmentInstanceID,
            string moduleSlotID,
            bool enabled)
        {
            if (state == null)
            {
                ShuttleLog.Warn("AssemblyMutation", "SetModuleEnabled called with null state.");
                return false;
            }

            state.EnsureInitialized();
            string safetyFailureReason;
            if (!ShuttleAssemblyMutationRules.CanMutateAssemblyState(state, out safetyFailureReason))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot set module enabled state for " + segmentInstanceID + "/" + moduleSlotID +
                    ": " + safetyFailureReason);
                return false;
            }

            ShuttleModuleSlot slot = state.GetModuleSlot(segmentInstanceID, moduleSlotID);
            if (slot == null || string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot set module enabled state for " + segmentInstanceID + "/" + moduleSlotID +
                    ": slot missing or empty.");
                return false;
            }

            ShuttleModule module = state.GetModule(slot.InstalledModuleInstanceID);
            if (module == null)
            {
                ShuttleLog.Error(
                    "AssemblyMutation",
                    "Module slot " + segmentInstanceID + "/" + moduleSlotID +
                    " points to missing module instance " + slot.InstalledModuleInstanceID + ".");
                return false;
            }

            if (!module.IsInstalledIn(segmentInstanceID, moduleSlotID))
            {
                ShuttleLog.Error(
                    "AssemblyMutation",
                    "Module slot " + segmentInstanceID + "/" + moduleSlotID +
                    " points to module " + module.ModuleInstanceID + " but the module does not belong to that slot.");
                return false;
            }

            if (module.IsEnabled == enabled)
            {
                return true;
            }

            module.SetEnabled(enabled);
            state.NotifyModuleDispatchTopologyChanged();
            state.MarkDirty(
                ShuttleDirtyFlags.Profile |
                ShuttleDirtyFlags.RuntimeSync |
                ShuttleDirtyFlags.ExternalSync |
                ShuttleDirtyFlags.ReadModel);
            return true;
        }

        internal bool ReplaceModule(
            ShuttleAssemblyState state,
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            string selectedStuffDefName)
        {
            if (state == null || moduleDef == null)
            {
                ShuttleLog.Warn("AssemblyMutation", "ReplaceModule called with null state or moduleDef.");
                return false;
            }

            ShuttleModuleSlot slot;
            string validationFailureReason;
            if (!this.TryValidateModuleInstallTarget(
                state,
                segmentInstanceID,
                moduleSlotID,
                moduleDef,
                true,
                out slot,
                out validationFailureReason))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot replace module in " + segmentInstanceID + "/" + moduleSlotID +
                    " with " + moduleDef.defName + ": " + validationFailureReason);
                return false;
            }

            string newModuleInstanceID;
            if (!state.TryAllocateNextModuleInstanceID(out newModuleInstanceID))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot replace module in " + segmentInstanceID + "/" + moduleSlotID +
                    " with " + moduleDef.defName + ": module instance ID allocator is unavailable.");
                return false;
            }

            string oldModuleInstanceID = slot.InstalledModuleInstanceID;
            if (string.IsNullOrEmpty(oldModuleInstanceID))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot replace module in " + segmentInstanceID + "/" + moduleSlotID +
                    ": slot is empty.");
                return false;
            }

            if (slot.IsLocked)
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot replace module in " + segmentInstanceID + "/" + moduleSlotID +
                    ": occupied slot is locked.");
                return false;
            }

            ShuttleModule oldModule = state.GetModule(oldModuleInstanceID);
            if (oldModule == null)
            {
                ShuttleLog.Error(
                    "AssemblyMutation",
                    "Module slot " + segmentInstanceID + "/" + moduleSlotID +
                    " points to missing module instance " + oldModuleInstanceID + ".");
                return false;
            }

            if (oldModule.ModuleInstanceID != oldModuleInstanceID ||
                !oldModule.IsInstalledIn(segmentInstanceID, moduleSlotID))
            {
                ShuttleLog.Error(
                    "AssemblyMutation",
                    "Module slot " + segmentInstanceID + "/" + moduleSlotID +
                    " points to module " + oldModuleInstanceID + " but the module does not belong to that slot.");
                return false;
            }

            if (!state.RemoveModule(oldModule))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot replace module in " + segmentInstanceID + "/" + moduleSlotID +
                    ": existing module could not be removed safely.");
                return false;
            }

            ShuttleModule module = new ShuttleModule(
                newModuleInstanceID,
                moduleDef,
                segmentInstanceID,
                moduleSlotID);
            module.EnsureInitialized();
            this.ApplyHullStuffSelection(module, moduleDef, selectedStuffDefName);

            if (!state.AddModule(module))
            {
                state.AddModule(oldModule);
                slot.SetInstalledModuleInstanceID(oldModuleInstanceID);
                state.RebuildIndexes();

                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot replace module in " + segmentInstanceID + "/" + moduleSlotID +
                    ": replacement module could not be added.");
                return false;
            }

            state.RefrigeratedCargoConfig.RemoveModuleConfig(oldModule.ModuleInstanceID);
            slot.SetInstalledModuleInstanceID(module.ModuleInstanceID);
            state.RebuildIndexes();
            state.MarkDirty(ShuttleDirtyFlags.AssemblyTopology | ShuttleDirtyFlags.Profile);
            return true;
        }

        private bool CanInstallModuleIntoSlot(
            ShuttleSegment segment,
            ShuttleModuleSlot slot,
            ShuttleModuleBaseDef moduleDef,
            bool allowOccupiedSlot)
        {
            if (segment == null || slot == null || moduleDef == null)
            {
                return false;
            }

            if (!moduleDef.playerInstallable)
            {
                return false;
            }

            if (!ShuttleResearchGateUtility.IsUnlocked(moduleDef))
            {
                return false;
            }

            if (slot.IsLocked || (!allowOccupiedSlot && slot.IsOccupied))
            {
                return false;
            }

            bool optionalWildcardSlot = this.IsOptionalWildcardModuleSlot(slot);
            if (!this.ModuleSupportsSegment(segment.SegmentDef, moduleDef, optionalWildcardSlot))
            {
                return false;
            }

            if (segment.SegmentDef != null &&
                segment.SegmentDef.installableModuleTypes != null &&
                segment.SegmentDef.installableModuleTypes.Count > 0)
            {
                if (moduleDef.ModuleType == ShuttleModuleType.Unknown)
                {
                    return false;
                }

                if (!this.SegmentAllowsModuleType(segment.SegmentDef, moduleDef))
                {
                    return false;
                }
            }

            if (optionalWildcardSlot)
            {
                return true;
            }

            if (moduleDef.installableModuleSlotTypes == null || moduleDef.installableModuleSlotTypes.Count == 0)
            {
                return true;
            }

            if (moduleDef.ModuleType == ShuttleModuleType.Unknown)
            {
                return false;
            }

            return ShuttleModuleTypeCatalog.MatchesAnyInstallableType(
                slot.SlotType,
                moduleDef.InstallableModuleSlotTypeEnums);
        }

        private bool IsOptionalWildcardModuleSlot(ShuttleModuleSlot slot)
        {
            return slot != null &&
                !slot.IsRequired &&
                slot.SlotType == ShuttleModuleType.Optional;
        }

        private bool ModuleSupportsSegment(
            ShuttleSegmentBaseDef segmentDef,
            ShuttleModuleBaseDef moduleDef,
            bool requireExplicitSegmentType)
        {
            if (moduleDef == null)
            {
                return false;
            }

            if (moduleDef.installableSegmentTypes == null ||
                moduleDef.installableSegmentTypes.Count == 0)
            {
                return !requireExplicitSegmentType;
            }

            return segmentDef != null &&
                ShuttleSegmentTypeCatalog.MatchesAnyInstallableType(
                    segmentDef.SegmentType,
                    moduleDef.InstallableSegmentTypeEnums);
        }

        private bool SegmentAllowsModuleType(
            ShuttleSegmentBaseDef segmentDef,
            ShuttleModuleBaseDef moduleDef)
        {
            if (segmentDef == null || moduleDef == null)
            {
                return false;
            }

            IReadOnlyList<ShuttleModuleType> installableTypes = segmentDef.InstallableModuleTypeEnums;
            if (installableTypes == null || installableTypes.Count == 0)
            {
                return true;
            }

            ShuttleModuleType moduleType = moduleDef.ModuleType;
            if (moduleType == ShuttleModuleType.Unknown)
            {
                return false;
            }

            for (int i = 0; i < installableTypes.Count; i++)
            {
                ShuttleModuleType allowedType = installableTypes[i];
                if (allowedType == ShuttleModuleType.Optional || allowedType == moduleType)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryValidateModuleInstallTarget(
            ShuttleAssemblyState state,
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            bool allowOccupiedSlot,
            out ShuttleModuleSlot slot,
            out string failureReason)
        {
            slot = null;
            failureReason = null;

            if (state == null)
            {
                failureReason = "CT_Shuttle_Command_AssemblyStateUnavailable".Translate().ToString();
                return false;
            }

            if (!ShuttleAssemblyMutationRules.CanMutateAssemblyState(state, out failureReason))
            {
                return false;
            }

            if (moduleDef == null)
            {
                failureReason = "CT_Shuttle_Command_ModuleDefUnavailable".Translate().ToString();
                return false;
            }

            state.EnsureInitialized();
            if (!ShuttleAssemblyMutationRules.CanAllocateModuleInstanceID(state, out failureReason))
            {
                return false;
            }

            ShuttleSegment segment = state.GetSegment(segmentInstanceID);
            slot = state.GetModuleSlot(segmentInstanceID, moduleSlotID);
            if (!this.CanInstallModuleIntoSlot(segment, slot, moduleDef, allowOccupiedSlot))
            {
                failureReason = "CT_Shuttle_Command_ModuleSlotCompatibilityFailed".Translate().ToString();
                return false;
            }

            string budgetFailureReason;
            if (!ShuttleAssemblyMutationRules.CanInstallModuleUnderCurrentLoadBudget(
                    state,
                    segment,
                    slot,
                    moduleDef,
                    allowOccupiedSlot,
                    out budgetFailureReason))
            {
                failureReason = budgetFailureReason;
                return false;
            }

            return true;
        }

        private void ApplyHullStuffSelection(
            ShuttleModule module,
            ShuttleModuleBaseDef moduleDef,
            string selectedStuffDefName)
        {
            if (module == null || !(moduleDef is ShuttleHullPlatingModuleDef))
            {
                return;
            }

            string resolvedStuffDefName =
                ShuttleHullArmorStuffUtility.ResolveSelectedStuffDefNameOrFallback(selectedStuffDefName);
            if (!string.IsNullOrEmpty(resolvedStuffDefName))
            {
                module.SetSelectedStuffDefName(resolvedStuffDefName);
            }
        }
    }
}
