using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    internal static class ShuttleAssemblyMutationRules
    {
        internal static bool CanMutateAssemblyState(ShuttleAssemblyState state, out string failureReason)
        {
            failureReason = null;
            if (state == null)
            {
                failureReason = "CT_Shuttle_Command_AssemblyStateUnavailable".Translate().ToString();
                return false;
            }

            string integrityFailureReason;
            if (state.TryGetBlockingIntegrityFailureReason(out integrityFailureReason))
            {
                failureReason = integrityFailureReason;
                return false;
            }

            return true;
        }

        internal static bool CanAllocateSegmentInstanceID(ShuttleAssemblyState state, out string failureReason)
        {
            failureReason = null;
            if (state == null)
            {
                failureReason = "CT_Shuttle_Command_AssemblyStateUnavailable".Translate().ToString();
                return false;
            }

            if (!state.CanAllocateSegmentInstanceID)
            {
                failureReason = "CT_Shuttle_Command_AssemblyTopologyCorrupted".Translate().ToString();
                return false;
            }

            return true;
        }

        internal static bool CanAllocateModuleInstanceID(ShuttleAssemblyState state, out string failureReason)
        {
            failureReason = null;
            if (state == null)
            {
                failureReason = "CT_Shuttle_Command_AssemblyStateUnavailable".Translate().ToString();
                return false;
            }

            if (!state.CanAllocateModuleInstanceID)
            {
                failureReason = "CT_Shuttle_Command_AssemblyTopologyCorrupted".Translate().ToString();
                return false;
            }

            return true;
        }

        internal static bool CanInstallSegmentUnderCurrentLoadBudget(
            ShuttleAssemblyState state,
            ShuttleSegmentBaseDef segmentDef)
        {
            // TODO: Validate against total shuttle load/complexity budget once profile aggregation
            // is wired into the new rewrite. First version allows installation here.
            return state != null && segmentDef != null;
        }

        internal static bool CanInstallModuleUnderCurrentLoadBudget(
            ShuttleAssemblyState state,
            ShuttleSegment targetSegment,
            ShuttleModuleSlot targetSlot,
            ShuttleModuleBaseDef moduleDef,
            bool replacingExistingModule,
            out string failureReason)
        {
            failureReason = null;
            if (state == null)
            {
                failureReason = "CT_Shuttle_Command_ComplexityStateUnavailable".Translate().ToString();
                return false;
            }

            if (targetSegment == null)
            {
                failureReason = "CT_Shuttle_Command_ComplexitySegmentUnavailable".Translate().ToString();
                return false;
            }

            if (moduleDef == null)
            {
                failureReason = "CT_Shuttle_Command_ComplexityModuleDefUnavailable".Translate().ToString();
                return false;
            }

            if (moduleDef.moduleComplexityCost < 0)
            {
                failureReason = "CT_Shuttle_Command_ComplexityNegativeCost".Translate(moduleDef.defName).ToString();
                return false;
            }

            ShuttleSegmentBaseDef segmentDef = targetSegment.SegmentDef;
            if (segmentDef == null || segmentDef.maxModuleComplexity < 0)
            {
                return true;
            }

            long currentComplexity = CalculateInstalledModuleComplexity(
                state,
                targetSegment,
                replacingExistingModule ? targetSlot : null);
            long projectedComplexity = SaturatingAddComplexity(
                currentComplexity,
                moduleDef.moduleComplexityCost);
            if (projectedComplexity <= (long)segmentDef.maxModuleComplexity)
            {
                return true;
            }

            failureReason = "CT_Shuttle_Command_ComplexityExceeded".Translate(
                targetSegment.SegmentInstanceID,
                segmentDef.defName,
                moduleDef.defName,
                projectedComplexity,
                segmentDef.maxModuleComplexity,
                currentComplexity,
                moduleDef.moduleComplexityCost).ToString();
            return false;
        }

        private static long CalculateInstalledModuleComplexity(
            ShuttleAssemblyState state,
            ShuttleSegment targetSegment,
            ShuttleModuleSlot excludedSlot)
        {
            if (state == null || targetSegment == null || targetSegment.ModuleSlots == null)
            {
                return 0;
            }

            long complexity = 0L;
            for (int i = 0; i < targetSegment.ModuleSlots.Count; i++)
            {
                ShuttleModuleSlot slot = targetSegment.ModuleSlots[i];
                if (slot == null || string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
                {
                    continue;
                }

                if (IsSameModuleSlot(slot, excludedSlot))
                {
                    continue;
                }

                ShuttleModule module = state.GetModule(slot.InstalledModuleInstanceID);
                if (module == null || module.ModuleDef == null || module.ModuleDef.moduleComplexityCost <= 0)
                {
                    continue;
                }

                complexity = SaturatingAddComplexity(
                    complexity,
                    module.ModuleDef.moduleComplexityCost);
            }

            return complexity;
        }

        private static long SaturatingAddComplexity(long left, int right)
        {
            if (left < 0L)
            {
                left = 0L;
            }

            if (right <= 0)
            {
                return left;
            }

            if (long.MaxValue - left < right)
            {
                return long.MaxValue;
            }

            return left + right;
        }

        private static bool IsSameModuleSlot(ShuttleModuleSlot left, ShuttleModuleSlot right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left.ParentSegmentInstanceID == right.ParentSegmentInstanceID &&
                left.SlotID == right.SlotID;
        }
    }
}
