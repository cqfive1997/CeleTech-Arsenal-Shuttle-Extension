using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyRemoval
{
    internal static class ShuttleModuleRemovalTargetResolver
    {
        internal static bool TryResolveInstalledModule(
            ShuttleCommandContext context,
            string segmentInstanceID,
            string moduleSlotID,
            string expectedModuleInstanceID,
            out ShuttleModule module,
            out string failureReason)
        {
            module = null;
            failureReason = null;
            if (context == null || context.AssemblyState == null)
            {
                failureReason = "CT_Shuttle_Command_AssemblyStateUnavailable".Translate().ToString();
                return false;
            }

            ShuttleModuleSlot slot = context.AssemblyState.GetModuleSlot(segmentInstanceID, moduleSlotID);
            if (slot == null || string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
            {
                failureReason = "CT_Shuttle_Command_ModuleRemoveMissing".Translate().ToString();
                return false;
            }

            if (!string.IsNullOrEmpty(expectedModuleInstanceID) &&
                slot.InstalledModuleInstanceID != expectedModuleInstanceID)
            {
                failureReason = "CT_Shuttle_Command_ModuleTargetChanged".Translate().ToString();
                return false;
            }

            module = context.AssemblyState.GetModule(slot.InstalledModuleInstanceID);
            if (module == null ||
                module.ModuleInstanceID != slot.InstalledModuleInstanceID ||
                !module.IsInstalledIn(segmentInstanceID, moduleSlotID))
            {
                failureReason = "CT_Shuttle_Command_ModuleRemoveMissing".Translate().ToString();
                return false;
            }

            return true;
        }

        internal static bool TryResolveInstalledSegment(
            ShuttleCommandContext context,
            string segmentSlotID,
            out ShuttleSegmentSlot slot,
            out ShuttleSegment segment,
            out string failureReason)
        {
            slot = null;
            segment = null;
            failureReason = null;
            if (context == null || context.AssemblyState == null)
            {
                failureReason = "CT_Shuttle_Command_AssemblyStateUnavailable".Translate().ToString();
                return false;
            }

            slot = context.AssemblyState.GetSegmentSlot(segmentSlotID);
            if (slot == null || string.IsNullOrEmpty(slot.InstalledSegmentInstanceID))
            {
                failureReason = "CT_Shuttle_Command_SegmentRemoveMissing".Translate().ToString();
                return false;
            }

            segment = context.AssemblyState.GetSegment(slot.InstalledSegmentInstanceID);
            if (segment == null || segment.SegmentInstanceID != slot.InstalledSegmentInstanceID)
            {
                failureReason = "CT_Shuttle_Command_SegmentRemoveMissing".Translate().ToString();
                return false;
            }

            return true;
        }
    }
}
