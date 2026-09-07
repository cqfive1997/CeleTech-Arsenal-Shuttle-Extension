namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Shared read-only compatibility rules for installing segment defs into shuttle-level
    /// segment slots. UI filtering and assembly mutation must agree on these rules.
    /// </summary>
    public static class ShuttleSegmentInstallCompatibility
    {
        public static bool CanInstallIntoSlot(
            ShuttleSegmentType slotType,
            bool isRequired,
            bool isFixed,
            bool isLocked,
            bool isOccupied,
            ShuttleSegmentBaseDef segmentDef,
            bool requireEditableEmptySlot)
        {
            if (segmentDef == null)
            {
                return false;
            }

            if (requireEditableEmptySlot && (isLocked || isOccupied))
            {
                return false;
            }

            if (segmentDef.SegmentType == ShuttleSegmentType.Unknown)
            {
                return false;
            }

            if (IsFlexibleOptionalSlot(slotType, isRequired, isFixed))
            {
                return !IsCockpitSegment(segmentDef);
            }

            if (segmentDef.installableSegmentSlotTypes == null ||
                segmentDef.installableSegmentSlotTypes.Count == 0)
            {
                return true;
            }

            return ShuttleSegmentTypeCatalog.MatchesAnyInstallableType(
                slotType,
                segmentDef.InstallableSegmentSlotTypeEnums);
        }

        public static bool IsFlexibleOptionalSlot(
            ShuttleSegmentType slotType,
            bool isRequired,
            bool isFixed)
        {
            return !isRequired &&
                !isFixed &&
                slotType != ShuttleSegmentType.Cockpit;
        }

        public static bool IsCockpitSegment(ShuttleSegmentBaseDef segmentDef)
        {
            if (segmentDef == null)
            {
                return false;
            }

            if (segmentDef is ShuttleCockpitSegmentDef ||
                segmentDef.SegmentType == ShuttleSegmentType.Cockpit)
            {
                return true;
            }

            return segmentDef.InstallableSegmentSlotTypeEnums != null &&
                segmentDef.InstallableSegmentSlotTypeEnums.Count == 1 &&
                segmentDef.InstallableSegmentSlotTypeEnums[0] == ShuttleSegmentType.Cockpit;
        }
    }
}
