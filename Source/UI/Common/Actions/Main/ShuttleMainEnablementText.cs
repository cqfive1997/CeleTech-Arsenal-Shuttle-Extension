using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main
{
    internal static class ShuttleMainEnablementText
    {
        internal static string GetModuleEnablementTooltip(
            bool commandAvailable,
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            bool enabled)
        {
            if (!commandAvailable)
            {
                return Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (segment == null || string.IsNullOrEmpty(segment.InstalledSegmentInstanceID))
            {
                return Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            if (moduleSlot == null || string.IsNullOrEmpty(moduleSlot.InstalledModuleInstanceID))
            {
                return Tr("CT_Shuttle_Command_ModuleEnableTargetMissing");
            }

            if (moduleSlot.InstalledModuleEnabled == enabled)
            {
                return Tr(enabled
                    ? "CT_Shuttle_Command_ModuleAlreadyEnabled"
                    : "CT_Shuttle_Command_ModuleAlreadyDisabled");
            }

            if (!enabled && (moduleSlot.IsRequired || moduleSlot.IsLocked))
            {
                return Tr("CT_Shuttle_Command_CannotDisableRequiredOrLockedModule");
            }

            return Tr(enabled ? "CT_Shuttle_Main_Enable" : "CT_Shuttle_Main_Disable");
        }

        internal static string GetSegmentModulesEnablementTooltip(
            bool commandAvailable,
            ShuttleControlSegmentSlotModel segment,
            bool enabled,
            bool canRequestSegmentModulesEnablement)
        {
            if (!commandAvailable)
            {
                return Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (segment == null || string.IsNullOrEmpty(segment.InstalledSegmentInstanceID))
            {
                return Tr("CT_Shuttle_Command_SegmentModuleEnableTargetMissing");
            }

            if (!canRequestSegmentModulesEnablement)
            {
                return Tr("CT_Shuttle_Command_SegmentModuleEnableNoEligibleModules");
            }

            return Tr(enabled ? "CT_Shuttle_Main_Enable" : "CT_Shuttle_Main_Disable");
        }

        private static string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}
