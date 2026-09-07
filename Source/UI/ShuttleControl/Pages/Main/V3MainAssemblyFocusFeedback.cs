using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal static class V3MainAssemblyFocusFeedback
    {
        private const int FocusDurationTicks = 180;
        private const int SolidFocusTicks = 120;
        private const int BlinkIntervalTicks = 10;

        internal static void ClearExpired(V3MainPageState state)
        {
            if (state == null ||
                state.AssemblyFocusUntilTick <= 0)
            {
                return;
            }

            int tick = GetCurrentTick();
            if (tick <= state.AssemblyFocusUntilTick)
            {
                return;
            }

            state.FocusedSegmentSlotID = null;
            state.FocusedModuleSlotID = null;
            state.AssemblyFocusStartTick = 0;
            state.AssemblyFocusUntilTick = 0;
        }

        internal static void FocusSegment(
            V3MainPageState state,
            ShuttleControlSegmentSlotModel segment)
        {
            if (state == null || segment == null)
            {
                return;
            }

            int tick = GetCurrentTick();
            state.FocusedSegmentSlotID = segment.SlotID;
            state.FocusedModuleSlotID = null;
            state.AssemblyFocusStartTick = tick;
            state.AssemblyFocusUntilTick = tick + FocusDurationTicks;
        }

        internal static void FocusModule(
            V3MainPageState state,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            if (state == null || moduleSlot == null)
            {
                return;
            }

            int tick = GetCurrentTick();
            state.FocusedModuleSlotID = moduleSlot.SlotID;
            state.AssemblyFocusStartTick = tick;
            state.AssemblyFocusUntilTick = tick + FocusDurationTicks;
        }

        internal static bool ShouldDrawSegmentFocus(
            V3MainPageState state,
            ShuttleControlSegmentSlotModel segment)
        {
            return state != null &&
                segment != null &&
                state.FocusedSegmentSlotID == segment.SlotID &&
                ShouldDrawFocusBorder(state);
        }

        internal static bool ShouldDrawModuleFocus(
            V3MainPageState state,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            return state != null &&
                moduleSlot != null &&
                state.FocusedModuleSlotID == moduleSlot.SlotID &&
                ShouldDrawFocusBorder(state);
        }

        private static bool ShouldDrawFocusBorder(V3MainPageState state)
        {
            int tick = GetCurrentTick();
            if (state.AssemblyFocusUntilTick <= 0 || tick > state.AssemblyFocusUntilTick)
            {
                return false;
            }

            int elapsed = tick - state.AssemblyFocusStartTick;
            if (elapsed < SolidFocusTicks)
            {
                return true;
            }

            return ((tick / BlinkIntervalTicks) % 2) == 0;
        }

        private static int GetCurrentTick()
        {
            return ShuttleTickUtility.TicksGameOrZero();
        }
    }
}
