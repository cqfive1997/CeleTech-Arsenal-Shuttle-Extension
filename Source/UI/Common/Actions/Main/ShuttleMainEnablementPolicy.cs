using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main
{
    internal static class ShuttleMainEnablementPolicy
    {
        internal static bool CanRequestModuleEnablement(
            bool commandAvailable,
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            bool enabled)
        {
            if (!commandAvailable ||
                segment == null ||
                moduleSlot == null ||
                string.IsNullOrEmpty(segment.InstalledSegmentInstanceID) ||
                string.IsNullOrEmpty(moduleSlot.InstalledModuleInstanceID) ||
                moduleSlot.IsRemovalInProgress)
            {
                return false;
            }

            if (moduleSlot.InstalledModuleEnabled == enabled)
            {
                return false;
            }

            if (!enabled)
            {
                return !moduleSlot.IsLocked && !moduleSlot.IsRequired;
            }

            return true;
        }

        internal static bool CanRequestSegmentModulesEnablement(
            bool commandAvailable,
            ShuttleControlSegmentSlotModel segment,
            bool enabled)
        {
            if (!commandAvailable ||
                segment == null ||
                string.IsNullOrEmpty(segment.InstalledSegmentInstanceID) ||
                segment.ModuleSlots == null)
            {
                return false;
            }

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleControlModuleSlotModel moduleSlot = segment.ModuleSlots[i];
                if (moduleSlot == null ||
                    string.IsNullOrEmpty(moduleSlot.InstalledModuleInstanceID) ||
                    moduleSlot.InstalledModuleEnabled == enabled)
                {
                    continue;
                }

                if (!enabled && (moduleSlot.IsLocked || moduleSlot.IsRequired))
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
