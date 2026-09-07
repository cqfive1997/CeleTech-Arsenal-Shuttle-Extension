using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main
{
    internal static class ShuttleMainRemovalPolicy
    {
        internal static bool CanRequestSegmentRemove(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlSegmentSlotModel segment)
        {
            if (!commandAvailable ||
                hasActiveConstructionOrder ||
                segment == null ||
                string.IsNullOrEmpty(segment.InstalledSegmentInstanceID) ||
                segment.IsRemovalInProgress ||
                segment.IsFixed ||
                segment.IsLocked ||
                segment.IsRequired)
            {
                return false;
            }

            if (segment.ModuleSlots == null)
            {
                return true;
            }

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleControlModuleSlotModel moduleSlot = segment.ModuleSlots[i];
                if (moduleSlot != null &&
                    !string.IsNullOrEmpty(moduleSlot.InstalledModuleInstanceID))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool CanCancelSegmentRemoval(
            bool commandAvailable,
            ShuttleControlSegmentSlotModel segment)
        {
            return commandAvailable &&
                segment != null &&
                segment.IsRemovalInProgress &&
                segment.CanCancelRemoval &&
                !string.IsNullOrEmpty(segment.SlotID);
        }

        internal static bool CanAssignSegmentRemovalWorker(
            bool commandAvailable,
            ShuttleControlSegmentSlotModel segment)
        {
            return commandAvailable &&
                segment != null &&
                segment.IsRemovalInProgress &&
                segment.CanAssignRemovalWorker &&
                !string.IsNullOrEmpty(segment.SlotID);
        }

        internal static bool CanRequestModuleRemove(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            return commandAvailable &&
                !hasActiveConstructionOrder &&
                moduleSlot != null &&
                !moduleSlot.IsRemovalInProgress &&
                !moduleSlot.IsLocked &&
                !moduleSlot.IsRequired &&
                !string.IsNullOrEmpty(moduleSlot.InstalledModuleInstanceID);
        }

        internal static bool CanCancelModuleRemoval(
            bool commandAvailable,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            return commandAvailable &&
                moduleSlot != null &&
                moduleSlot.IsRemovalInProgress &&
                moduleSlot.CanCancelRemoval &&
                !string.IsNullOrEmpty(moduleSlot.InstalledModuleInstanceID);
        }

        internal static bool CanAssignModuleRemovalWorker(
            bool commandAvailable,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            return commandAvailable &&
                moduleSlot != null &&
                moduleSlot.IsRemovalInProgress &&
                moduleSlot.CanAssignRemovalWorker &&
                !string.IsNullOrEmpty(moduleSlot.InstalledModuleInstanceID);
        }
    }
}
