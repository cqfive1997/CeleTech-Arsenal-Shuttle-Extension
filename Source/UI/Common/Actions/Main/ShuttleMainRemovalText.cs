using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main
{
    internal static class ShuttleMainRemovalText
    {
        internal static string GetSegmentRemoveTooltip(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlSegmentSlotModel segment,
            bool canRemoveSegment)
        {
            if (!commandAvailable)
            {
                return Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (hasActiveConstructionOrder)
            {
                return Tr("CT_Shuttle_Command_AssemblyConstructionAlreadyActive");
            }

            if (canRemoveSegment)
            {
                return Tr("CT_Shuttle_Main_Remove");
            }

            if (segment != null && !string.IsNullOrEmpty(segment.RemovalTooltip))
            {
                return segment.RemovalTooltip;
            }

            return Tr("CT_Shuttle_UI_CannotRemoveSegment");
        }

        internal static string GetModuleRemoveTooltip(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlModuleSlotModel moduleSlot,
            bool canRemoveModule)
        {
            if (!commandAvailable)
            {
                return Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (hasActiveConstructionOrder)
            {
                return Tr("CT_Shuttle_Command_AssemblyConstructionAlreadyActive");
            }

            if (canRemoveModule)
            {
                return Tr("CT_Shuttle_Main_Remove");
            }

            if (moduleSlot != null && !string.IsNullOrEmpty(moduleSlot.RemovalTooltip))
            {
                return moduleSlot.RemovalTooltip;
            }

            return Tr("CT_Shuttle_Command_ModuleRequiredOrLocked");
        }

        internal static string GetNoActiveRemovalTooltip(
            string modelTooltip)
        {
            return !string.IsNullOrEmpty(modelTooltip)
                ? modelTooltip
                : Tr("CT_Shuttle_Command_ModuleRemovalNoneActive");
        }

        private static string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}
