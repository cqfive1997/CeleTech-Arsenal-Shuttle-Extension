using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    internal sealed class ShuttleSegmentMutationService
    {
        private readonly ShuttleAssemblyBootstrapper bootstrapper;

        internal ShuttleSegmentMutationService(ShuttleAssemblyBootstrapper bootstrapper)
        {
            this.bootstrapper = bootstrapper ?? new ShuttleAssemblyBootstrapper();
        }

        internal bool InstallSegment(
            ShuttleAssemblyState state,
            string segmentSlotID,
            ShuttleSegmentBaseDef segmentDef)
        {
            if (state == null || segmentDef == null)
            {
                ShuttleLog.Warn("AssemblyMutation", "InstallSegment called with null state or segmentDef.");
                return false;
            }

            state.EnsureInitialized();
            string safetyFailureReason;
            if (!ShuttleAssemblyMutationRules.CanMutateAssemblyState(state, out safetyFailureReason) ||
                !ShuttleAssemblyMutationRules.CanAllocateSegmentInstanceID(state, out safetyFailureReason))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot install segment " + segmentDef.defName + " into slot " + segmentSlotID +
                    ": " + safetyFailureReason);
                return false;
            }

            ShuttleSegmentSlot slot = state.GetSegmentSlot(segmentSlotID);
            if (!this.CanInstallSegmentIntoSlot(slot, segmentDef))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot install segment " + segmentDef.defName + " into slot " + segmentSlotID + ": slot missing, occupied, locked, or incompatible.");
                return false;
            }

            if (!ShuttleAssemblyMutationRules.CanInstallSegmentUnderCurrentLoadBudget(state, segmentDef))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot install segment " + segmentDef.defName + " into slot " + segmentSlotID + ": load budget check failed.");
                return false;
            }

            string segmentInstanceID;
            if (!state.TryAllocateNextSegmentInstanceID(out segmentInstanceID))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot install segment " + segmentDef.defName + " into slot " + segmentSlotID +
                    ": segment instance ID allocator is unavailable.");
                return false;
            }

            ShuttleSegment segment = new ShuttleSegment(
                segmentInstanceID,
                segmentDef,
                slot.SlotIndex);
            segment.SetFixed(slot.IsFixed);
            segment.EnsureInitialized();

            state.AddSegment(segment);
            slot.SetInstalledSegmentInstanceID(segment.SegmentInstanceID);
            this.bootstrapper.MaterializeModuleSlots(state, segment);
            state.MarkDirty(ShuttleDirtyFlags.AssemblyTopology | ShuttleDirtyFlags.Profile);
            return true;
        }

        internal bool RemoveSegment(ShuttleAssemblyState state, string segmentSlotID)
        {
            if (state == null)
            {
                ShuttleLog.Warn("AssemblyMutation", "RemoveSegment called with null state.");
                return false;
            }

            state.EnsureInitialized();
            string safetyFailureReason;
            if (!ShuttleAssemblyMutationRules.CanMutateAssemblyState(state, out safetyFailureReason))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot remove segment from slot " + segmentSlotID + ": " + safetyFailureReason);
                return false;
            }

            ShuttleSegmentSlot slot = state.GetSegmentSlot(segmentSlotID);
            if (slot == null || string.IsNullOrEmpty(slot.InstalledSegmentInstanceID))
            {
                ShuttleLog.Warn("AssemblyMutation", "Cannot remove segment from slot " + segmentSlotID + ": slot missing or empty.");
                return false;
            }

            if (slot.IsFixed || slot.IsLocked || slot.IsRequired)
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot remove segment from slot " + segmentSlotID + ": slot is fixed, locked, or required.");
                return false;
            }

            ShuttleSegment segment = state.GetSegment(slot.InstalledSegmentInstanceID);
            if (segment == null)
            {
                ShuttleLog.Error(
                    "AssemblyMutation",
                    "Segment slot " + segmentSlotID + " points to missing segment instance " + slot.InstalledSegmentInstanceID + ".");
                return false;
            }

            if (segment.SegmentDef != null && segment.SegmentDef.isNonRemovable)
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot remove segment " + segment.SegmentInstanceID + ": segment def is non-removable.");
                return false;
            }

            if (this.HasInstalledModules(segment))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot remove segment " + segment.SegmentInstanceID + ": module slots are still occupied.");
                return false;
            }

            state.RemoveSegment(segment);
            slot.SetInstalledSegmentInstanceID(null);
            state.RebuildIndexes();
            state.MarkDirty(ShuttleDirtyFlags.AssemblyTopology | ShuttleDirtyFlags.Profile);
            return true;
        }

        internal bool ReplaceSegment(
            ShuttleAssemblyState state,
            string segmentSlotID,
            ShuttleSegmentBaseDef segmentDef)
        {
            if (state == null || segmentDef == null)
            {
                ShuttleLog.Warn("AssemblyMutation", "ReplaceSegment called with null state or segmentDef.");
                return false;
            }

            state.EnsureInitialized();
            string safetyFailureReason;
            if (!ShuttleAssemblyMutationRules.CanMutateAssemblyState(state, out safetyFailureReason) ||
                !ShuttleAssemblyMutationRules.CanAllocateSegmentInstanceID(state, out safetyFailureReason))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot replace segment in slot " + segmentSlotID +
                    " with " + segmentDef.defName + ": " + safetyFailureReason);
                return false;
            }

            ShuttleSegmentSlot slot = state.GetSegmentSlot(segmentSlotID);
            ShuttleSegment oldSegment = this.GetInstalledSegmentForSlot(state, slot);
            string validationFailureReason;
            if (!this.CanReplaceSegmentIntoSlot(
                slot,
                oldSegment,
                segmentDef,
                out validationFailureReason))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot replace segment in slot " + segmentSlotID +
                    " with " + segmentDef.defName + ": " + validationFailureReason);
                return false;
            }

            if (!ShuttleAssemblyMutationRules.CanInstallSegmentUnderCurrentLoadBudget(state, segmentDef))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot replace segment in slot " + segmentSlotID +
                    " with " + segmentDef.defName + ": load budget check failed.");
                return false;
            }

            string segmentInstanceID;
            if (!state.TryAllocateNextSegmentInstanceID(out segmentInstanceID))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot replace segment in slot " + segmentSlotID +
                    " with " + segmentDef.defName +
                    ": segment instance ID allocator is unavailable.");
                return false;
            }

            ShuttleSegment replacement = new ShuttleSegment(
                segmentInstanceID,
                segmentDef,
                slot.SlotIndex);
            replacement.SetFixed(slot.IsFixed);
            replacement.EnsureInitialized();

            if (!state.RemoveSegment(oldSegment))
            {
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot replace segment in slot " + segmentSlotID +
                    ": existing segment could not be removed safely.");
                return false;
            }

            if (!state.AddSegment(replacement))
            {
                state.AddSegment(oldSegment);
                slot.SetInstalledSegmentInstanceID(oldSegment.SegmentInstanceID);
                state.RebuildIndexes();
                ShuttleLog.Warn(
                    "AssemblyMutation",
                    "Cannot replace segment in slot " + segmentSlotID +
                    ": replacement segment could not be added.");
                return false;
            }

            slot.SetInstalledSegmentInstanceID(replacement.SegmentInstanceID);
            this.bootstrapper.MaterializeModuleSlots(state, replacement);
            state.RebuildIndexes();
            state.MarkDirty(ShuttleDirtyFlags.AssemblyTopology | ShuttleDirtyFlags.Profile);
            return true;
        }

        internal bool CanInstallSegment(
            ShuttleAssemblyState state,
            string segmentSlotID,
            ShuttleSegmentBaseDef segmentDef,
            out string failureReason)
        {
            failureReason = null;
            if (state == null)
            {
                failureReason = "CT_Shuttle_Command_AssemblyStateUnavailable".Translate().ToString();
                return false;
            }

            if (segmentDef == null)
            {
                failureReason = "CT_Shuttle_Command_SegmentInstallFailed".Translate().ToString();
                return false;
            }

            state.EnsureInitialized();
            string safetyFailureReason;
            if (!ShuttleAssemblyMutationRules.CanMutateAssemblyState(state, out safetyFailureReason))
            {
                failureReason = safetyFailureReason;
                return false;
            }

            if (!ShuttleAssemblyMutationRules.CanAllocateSegmentInstanceID(state, out safetyFailureReason))
            {
                failureReason = safetyFailureReason;
                return false;
            }

            ShuttleSegmentSlot slot = state.GetSegmentSlot(segmentSlotID);
            if (!this.CanInstallSegmentIntoSlot(slot, segmentDef))
            {
                failureReason = "CT_Shuttle_Command_SegmentInstallFailed".Translate().ToString();
                return false;
            }

            if (!ShuttleAssemblyMutationRules.CanInstallSegmentUnderCurrentLoadBudget(state, segmentDef))
            {
                failureReason = "CT_Shuttle_Command_SegmentInstallFailed".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool CanReplaceSegment(
            ShuttleAssemblyState state,
            string segmentSlotID,
            ShuttleSegmentBaseDef segmentDef,
            out string failureReason)
        {
            failureReason = null;
            if (state == null)
            {
                failureReason = "CT_Shuttle_Command_AssemblyStateUnavailable".Translate().ToString();
                return false;
            }

            if (segmentDef == null)
            {
                failureReason = "CT_Shuttle_Command_SegmentReplaceMissingDef".Translate().ToString();
                return false;
            }

            state.EnsureInitialized();
            string safetyFailureReason;
            if (!ShuttleAssemblyMutationRules.CanMutateAssemblyState(state, out safetyFailureReason))
            {
                failureReason = safetyFailureReason;
                return false;
            }

            if (!ShuttleAssemblyMutationRules.CanAllocateSegmentInstanceID(state, out safetyFailureReason))
            {
                failureReason = safetyFailureReason;
                return false;
            }

            ShuttleSegmentSlot slot = state.GetSegmentSlot(segmentSlotID);
            ShuttleSegment oldSegment = this.GetInstalledSegmentForSlot(state, slot);
            if (!this.CanReplaceSegmentIntoSlot(
                slot,
                oldSegment,
                segmentDef,
                out failureReason))
            {
                return false;
            }

            if (!ShuttleAssemblyMutationRules.CanInstallSegmentUnderCurrentLoadBudget(state, segmentDef))
            {
                failureReason = "CT_Shuttle_Command_SegmentReplaceFailed".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool CanInstallSegmentIntoSlot(ShuttleSegmentSlot slot, ShuttleSegmentBaseDef segmentDef)
        {
            if (slot == null || segmentDef == null)
            {
                return false;
            }

            if (slot.IsLocked || slot.IsOccupied)
            {
                return false;
            }

            if (!ShuttleResearchGateUtility.IsUnlocked(segmentDef))
            {
                return false;
            }

            if (segmentDef.SegmentType == ShuttleSegmentType.Unknown)
            {
                return false;
            }

            return ShuttleSegmentInstallCompatibility.CanInstallIntoSlot(
                slot.SlotType,
                slot.IsRequired,
                slot.IsFixed,
                slot.IsLocked,
                slot.IsOccupied,
                segmentDef,
                true);
        }

        private bool CanReplaceSegmentIntoSlot(
            ShuttleSegmentSlot slot,
            ShuttleSegment oldSegment,
            ShuttleSegmentBaseDef segmentDef,
            out string failureReason)
        {
            failureReason = null;
            if (slot == null || oldSegment == null || segmentDef == null)
            {
                failureReason = "CT_Shuttle_Command_SegmentReplaceMissing".Translate().ToString();
                return false;
            }

            if (string.IsNullOrEmpty(slot.InstalledSegmentInstanceID) ||
                slot.InstalledSegmentInstanceID != oldSegment.SegmentInstanceID)
            {
                failureReason = "CT_Shuttle_Command_SegmentReplaceMissing".Translate().ToString();
                return false;
            }

            if (slot.IsLocked || slot.IsFixed)
            {
                failureReason = "CT_Shuttle_Command_SegmentReplaceFixedOrLocked".Translate().ToString();
                return false;
            }

            if (oldSegment.SegmentDef != null && oldSegment.SegmentDef.isNonRemovable)
            {
                failureReason = "CT_Shuttle_Command_SegmentReplaceFixedOrLocked".Translate().ToString();
                return false;
            }

            if (this.HasInstalledModules(oldSegment))
            {
                failureReason = "CT_Shuttle_Command_SegmentReplaceHasModules".Translate().ToString();
                return false;
            }

            if (!ShuttleResearchGateUtility.IsUnlocked(segmentDef))
            {
                failureReason = "CT_Shuttle_Command_SegmentResearchLocked".Translate().ToString();
                return false;
            }

            if (segmentDef.SegmentType == ShuttleSegmentType.Unknown)
            {
                failureReason = "CT_Shuttle_Command_SegmentReplaceFailed".Translate().ToString();
                return false;
            }

            if (!ShuttleSegmentInstallCompatibility.CanInstallIntoSlot(
                slot.SlotType,
                slot.IsRequired,
                slot.IsFixed,
                slot.IsLocked,
                false,
                segmentDef,
                false))
            {
                failureReason = "CT_Shuttle_Command_SegmentReplaceFailed".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool HasInstalledModules(ShuttleSegment segment)
        {
            if (segment == null || segment.ModuleSlots == null)
            {
                return false;
            }

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleModuleSlot slot = segment.ModuleSlots[i];
                if (slot != null && !string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
                {
                    return true;
                }
            }

            return false;
        }

        private ShuttleSegment GetInstalledSegmentForSlot(
            ShuttleAssemblyState state,
            ShuttleSegmentSlot slot)
        {
            if (state == null ||
                slot == null ||
                string.IsNullOrEmpty(slot.InstalledSegmentInstanceID))
            {
                return null;
            }

            return state.GetSegment(slot.InstalledSegmentInstanceID);
        }
    }
}
