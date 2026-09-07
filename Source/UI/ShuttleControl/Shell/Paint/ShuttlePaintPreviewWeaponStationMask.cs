using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Paint
{
    internal static class ShuttlePaintPreviewWeaponStationMask
    {
        private const string SlotIndexMarker = ".slot.";

        internal static int FromControlModel(ShuttleControlReadModel model)
        {
            if (model == null || model.SegmentSlots == null)
            {
                return 0;
            }

            int mask = 0;
            for (int i = 0; i < model.SegmentSlots.Count; i++)
            {
                ShuttleControlSegmentSlotModel segment = model.SegmentSlots[i];
                if (segment == null || segment.ModuleSlots == null)
                {
                    continue;
                }

                for (int j = 0; j < segment.ModuleSlots.Count; j++)
                {
                    ShuttleControlModuleSlotModel module = segment.ModuleSlots[j];
                    if (!IsInstalledEnabledWeaponSlot(module))
                    {
                        continue;
                    }

                    mask = AddStation(mask, ParseParentSlotIndex(module.SlotID));
                }
            }

            return mask;
        }

        internal static int AddStation(int mask, int parentSlotIndex)
        {
            if (parentSlotIndex <= 0 || parentSlotIndex > 30)
            {
                return mask;
            }

            return mask | (1 << (parentSlotIndex - 1));
        }

        internal static bool HasStation(int mask, int parentSlotIndex)
        {
            if (parentSlotIndex <= 0 || parentSlotIndex > 30)
            {
                return false;
            }

            return (mask & (1 << (parentSlotIndex - 1))) != 0;
        }

        private static bool IsInstalledEnabledWeaponSlot(ShuttleControlModuleSlotModel module)
        {
            if (module == null ||
                string.IsNullOrEmpty(module.InstalledModuleInstanceID) ||
                !module.InstalledModuleEnabled)
            {
                return false;
            }

            ShuttleModuleType slotType =
                ShuttleModuleTypeCatalog.ParseSlotType(module.SlotTypeID);
            if (slotType == ShuttleModuleType.Weapon)
            {
                return true;
            }

            return ShuttleModuleTypeCatalog.ParseModuleType(module.InstalledModuleTypeID) ==
                ShuttleModuleType.Weapon;
        }

        private static int ParseParentSlotIndex(string parentSlotID)
        {
            if (string.IsNullOrEmpty(parentSlotID))
            {
                return -1;
            }

            int markerIndex = parentSlotID.LastIndexOf(SlotIndexMarker);
            if (markerIndex < 0)
            {
                return -1;
            }

            int index = markerIndex + SlotIndexMarker.Length;
            if (index >= parentSlotID.Length)
            {
                return -1;
            }

            int value = 0;
            for (; index < parentSlotID.Length; index++)
            {
                char c = parentSlotID[index];
                if (c < '0' || c > '9')
                {
                    return -1;
                }

                int digit = c - '0';
                if (value > (int.MaxValue - digit) / 10)
                {
                    return -1;
                }

                value = (value * 10) + digit;
            }

            return value;
        }
    }
}
