using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main
{
    internal static class ShuttleMainInstallPolicy
    {
        internal static bool CanRequestSegmentInstall(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlSegmentSlotModel segment)
        {
            return commandAvailable &&
                segment != null &&
                !segment.IsRemovalInProgress &&
                !segment.IsLocked &&
                string.IsNullOrEmpty(segment.InstalledSegmentInstanceID);
        }

        internal static bool CanRequestSegmentReplace(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlSegmentSlotModel segment)
        {
            if (!commandAvailable ||
                segment == null ||
                segment.IsRemovalInProgress ||
                segment.IsLocked ||
                segment.IsFixed ||
                string.IsNullOrEmpty(segment.InstalledSegmentInstanceID))
            {
                return false;
            }

            return !HasInstalledModules(segment);
        }

        internal static bool CanRequestModuleInstall(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            return commandAvailable &&
                moduleSlot != null &&
                !moduleSlot.IsRemovalInProgress &&
                !moduleSlot.IsLocked &&
                string.IsNullOrEmpty(moduleSlot.InstalledModuleInstanceID);
        }

        internal static bool CanRequestModuleReplace(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            return commandAvailable &&
                moduleSlot != null &&
                !moduleSlot.IsRemovalInProgress &&
                !moduleSlot.IsLocked &&
                !string.IsNullOrEmpty(moduleSlot.InstalledModuleInstanceID);
        }

        internal static bool CanInstallSegmentDefIntoSlot(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlSegmentSlotModel segment,
            ShuttleSegmentBaseDef segmentDef)
        {
            return CanInstallSegmentDefIntoSlot(
                commandAvailable,
                hasActiveConstructionOrder,
                segment,
                segmentDef,
                false);
        }

        internal static bool CanInstallSegmentDefIntoSlot(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlSegmentSlotModel segment,
            ShuttleSegmentBaseDef segmentDef,
            bool ignoreResearch)
        {
            if (!CanRequestSegmentInstall(commandAvailable, hasActiveConstructionOrder, segment) ||
                segmentDef == null)
            {
                return false;
            }

            if (!ignoreResearch && !ShuttleResearchGateUtility.IsUnlocked(segmentDef))
            {
                return false;
            }

            return CanInstallSegmentDefIntoSlotCore(segment, segmentDef, true);
        }

        internal static bool CanReplaceSegmentDefIntoSlot(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlSegmentSlotModel segment,
            ShuttleSegmentBaseDef segmentDef)
        {
            return CanReplaceSegmentDefIntoSlot(
                commandAvailable,
                hasActiveConstructionOrder,
                segment,
                segmentDef,
                false);
        }

        internal static bool CanReplaceSegmentDefIntoSlot(
            bool commandAvailable,
            bool hasActiveConstructionOrder,
            ShuttleControlSegmentSlotModel segment,
            ShuttleSegmentBaseDef segmentDef,
            bool ignoreResearch)
        {
            if (!CanRequestSegmentReplace(commandAvailable, hasActiveConstructionOrder, segment) ||
                segmentDef == null)
            {
                return false;
            }

            if (!ignoreResearch && !ShuttleResearchGateUtility.IsUnlocked(segmentDef))
            {
                return false;
            }

            return CanInstallSegmentDefIntoSlotCore(segment, segmentDef, false);
        }

        internal static bool IsSegmentDefCompatibleWithSlot(
            ShuttleControlSegmentSlotModel segment,
            ShuttleSegmentBaseDef segmentDef)
        {
            if (segment == null || segmentDef == null)
            {
                return false;
            }

            if (segment.IsLocked || !string.IsNullOrEmpty(segment.InstalledSegmentInstanceID))
            {
                return false;
            }

            return CanInstallSegmentDefIntoSlotCore(segment, segmentDef, false);
        }

        internal static bool IsSegmentDefCompatibleWithReplacementSlot(
            ShuttleControlSegmentSlotModel segment,
            ShuttleSegmentBaseDef segmentDef)
        {
            if (segment == null || segmentDef == null)
            {
                return false;
            }

            if (segment.IsLocked ||
                segment.IsFixed ||
                string.IsNullOrEmpty(segment.InstalledSegmentInstanceID) ||
                HasInstalledModules(segment))
            {
                return false;
            }

            return CanInstallSegmentDefIntoSlotCore(segment, segmentDef, false);
        }

        internal static bool CanInstallModuleDefIntoSlot(
            IShuttleModuleInstallEligibilityReadPort moduleInstallEligibilityPort,
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            ShuttleModuleBaseDef moduleDef,
            bool allowOccupiedSlot,
            out string failureReason)
        {
            failureReason = null;
            if (segment == null || moduleSlot == null || moduleDef == null)
            {
                failureReason = ShuttleUIText.Tr("CT_Shuttle_Command_ModuleSlotCompatibilityFailed");
                return false;
            }

            if (moduleInstallEligibilityPort == null)
            {
                failureReason = ShuttleUIText.Tr("CT_Shuttle_Command_ExecutorUnavailable");
                return false;
            }

            return moduleInstallEligibilityPort.CanInstallModuleForUI(
                segment.InstalledSegmentInstanceID,
                moduleSlot.SlotID,
                moduleDef,
                allowOccupiedSlot,
                out failureReason);
        }

        internal static bool SegmentAllowsModuleType(
            ShuttleControlSegmentSlotModel segment,
            ShuttleModuleBaseDef moduleDef)
        {
            if (segment == null || moduleDef == null)
            {
                return false;
            }

            ShuttleSegmentBaseDef segmentDef = DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(
                segment.InstalledSegmentDefName);
            if (segmentDef == null ||
                segmentDef.installableModuleTypes == null ||
                segmentDef.installableModuleTypes.Count == 0)
            {
                return true;
            }

            ShuttleModuleType moduleType = moduleDef.ModuleType;
            if (moduleType == ShuttleModuleType.Unknown)
            {
                return false;
            }

            System.Collections.Generic.IReadOnlyList<ShuttleModuleType> installableTypes =
                segmentDef.InstallableModuleTypeEnums;
            for (int i = 0; installableTypes != null && i < installableTypes.Count; i++)
            {
                ShuttleModuleType allowedType = installableTypes[i];
                if (allowedType == ShuttleModuleType.Optional || allowedType == moduleType)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsOptionalWildcardModuleSlot(
            ShuttleControlModuleSlotModel moduleSlot)
        {
            return moduleSlot != null &&
                !moduleSlot.IsRequired &&
                ShuttleModuleTypeCatalog.ParseSlotType(moduleSlot.SlotTypeID) ==
                ShuttleModuleType.Optional;
        }

        private static bool CanInstallSegmentDefIntoSlotCore(
            ShuttleControlSegmentSlotModel segment,
            ShuttleSegmentBaseDef segmentDef,
            bool requireEditableEmptySlot)
        {
            if (segment == null || segmentDef == null)
            {
                return false;
            }

            ShuttleSegmentType slotType = ShuttleSegmentTypeCatalog.ParseSlotType(segment.SlotTypeID);
            return ShuttleSegmentInstallCompatibility.CanInstallIntoSlot(
                slotType,
                segment.IsRequired,
                segment.IsFixed,
                segment.IsLocked,
                !string.IsNullOrEmpty(segment.InstalledSegmentInstanceID),
                segmentDef,
                requireEditableEmptySlot);
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
