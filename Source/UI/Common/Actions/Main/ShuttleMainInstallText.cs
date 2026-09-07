using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main
{
    internal static class ShuttleMainInstallText
    {
        internal static string GetSegmentInstallTooltip(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlSegmentSlotModel segment,
            bool canInstallSegment)
        {
            if (!commandAvailable)
            {
                return Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (canInstallSegment)
            {
                return Tr(hasActiveConstructionOrder
                    ? "CT_Shuttle_AssemblyConstruction_AddToQueue"
                    : "CT_Shuttle_Main_Install");
            }

            if (segment != null && !string.IsNullOrEmpty(segment.RemovalTooltip))
            {
                return segment.RemovalTooltip;
            }

            return Tr("CT_Shuttle_UI_CannotInstallSegment");
        }

        internal static string GetSegmentReplaceTooltip(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlSegmentSlotModel segment,
            bool canReplaceSegment)
        {
            if (!commandAvailable)
            {
                return Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (canReplaceSegment)
            {
                return Tr(hasActiveConstructionOrder
                    ? "CT_Shuttle_AssemblyConstruction_AddToQueue"
                    : "CT_Shuttle_Main_Swap");
            }

            if (segment == null || string.IsNullOrEmpty(segment.InstalledSegmentInstanceID))
            {
                return Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            if (segment.IsFixed || segment.IsLocked)
            {
                return Tr("CT_Shuttle_Command_SegmentReplaceFixedOrLocked");
            }

            if (HasInstalledModules(segment))
            {
                return Tr("CT_Shuttle_Command_SegmentReplaceHasModules");
            }

            if (segment != null && !string.IsNullOrEmpty(segment.RemovalTooltip))
            {
                return segment.RemovalTooltip;
            }

            return Tr("CT_Shuttle_UI_CannotReplaceSegment");
        }

        internal static string GetModuleInstallTooltip(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            bool canInstallModule)
        {
            if (!commandAvailable)
            {
                return Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (canInstallModule)
            {
                return Tr(hasActiveConstructionOrder
                    ? "CT_Shuttle_AssemblyConstruction_AddToQueue"
                    : "CT_Shuttle_Main_Install");
            }

            if (segment == null || string.IsNullOrEmpty(segment.InstalledSegmentInstanceID))
            {
                return Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            if (moduleSlot != null && !string.IsNullOrEmpty(moduleSlot.RemovalTooltip))
            {
                return moduleSlot.RemovalTooltip;
            }

            return Tr("CT_Shuttle_UI_NoCompatibleModuleDefs");
        }

        internal static string GetModuleReplaceTooltip(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlModuleSlotModel moduleSlot,
            bool canReplaceModule)
        {
            if (!commandAvailable)
            {
                return Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (canReplaceModule)
            {
                return Tr(hasActiveConstructionOrder
                    ? "CT_Shuttle_AssemblyConstruction_AddToQueue"
                    : "CT_Shuttle_Main_Swap");
            }

            if (moduleSlot != null && !string.IsNullOrEmpty(moduleSlot.RemovalTooltip))
            {
                return moduleSlot.RemovalTooltip;
            }

            return Tr("CT_Shuttle_UI_CannotReplaceModule");
        }

        private static string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }

        private static bool HasInstalledModules(ShuttleControlSegmentSlotModel segment)
        {
            if (segment == null || segment.ModuleSlots == null)
            {
                return false;
            }

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleControlModuleSlotModel moduleSlot = segment.ModuleSlots[i];
                if (moduleSlot != null &&
                    !string.IsNullOrEmpty(moduleSlot.InstalledModuleInstanceID))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
