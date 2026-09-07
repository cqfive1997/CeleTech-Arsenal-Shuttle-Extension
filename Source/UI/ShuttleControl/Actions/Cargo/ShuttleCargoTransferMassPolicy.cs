using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Cargo
{
    internal static class ShuttleCargoTransferMassPolicy
    {
        private const float MassEpsilon = 0.001f;

        internal static bool FitsDestinationMass(
            ShuttleCargoTransferDisplayTarget display)
        {
            if (display == null)
            {
                return true;
            }

            float selectedMassKg = GetSelectedMassKg(display);
            float capacityKg = Mathf.Max(0f, display.DestinationCapacityKg);
            float usedKg = Mathf.Max(0f, display.DestinationUsedMassKg);
            if (capacityKg <= MassEpsilon && selectedMassKg > MassEpsilon)
            {
                return false;
            }

            return selectedMassKg <= Mathf.Max(0f, capacityKg - usedKg) +
                MassEpsilon;
        }

        internal static string GetDestinationMassBlocker(
            ShuttleCargoTransferDisplayTarget display)
        {
            if (display == null)
            {
                return Tr("CT_Shuttle_Cargo_CapacityUnavailable");
            }

            float selectedMassKg = GetSelectedMassKg(display);
            if (display.DestinationCapacityKg <= MassEpsilon &&
                selectedMassKg > MassEpsilon)
            {
                return Tr("CT_Shuttle_Cargo_CapacityUnavailable");
            }

            return Tr("CT_Shuttle_Cargo_SelectedExceedsCapacity");
        }

        private static float GetSelectedMassKg(
            ShuttleCargoTransferDisplayTarget display)
        {
            if (display == null ||
                display.AvailableCount <= 0 ||
                display.SelectedCount <= 0 ||
                display.StackMassKg <= 0f)
            {
                return 0f;
            }

            int count = Math.Min(display.SelectedCount, display.AvailableCount);
            return Mathf.Max(0f, display.StackMassKg) *
                count /
                display.AvailableCount;
        }

        private static string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}
