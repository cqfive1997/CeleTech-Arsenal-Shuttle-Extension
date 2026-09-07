using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainModuleSelection
    {
        internal void EnsureSelectedSegment(
            V3MainPageState state,
            ShuttleControlReadModel model)
        {
            if (state == null)
            {
                return;
            }

            if (model == null || model.SegmentSlots == null || model.SegmentSlots.Count == 0)
            {
                state.SelectedSegmentSlotID = null;
                state.SelectedModuleSlotID = null;
                return;
            }

            if (model.FindSegmentSlot(state.SelectedSegmentSlotID) != null)
            {
                return;
            }

            ShuttleControlSegmentSlotModel first = model.SegmentSlots[0];
            state.SelectedSegmentSlotID = first != null ? first.SlotID : null;
            state.SelectedModuleSlotID = null;
        }

        internal ShuttleControlSegmentSlotModel FindSelectedSegment(
            V3MainPageState state,
            ShuttleControlReadModel model)
        {
            if (state == null || model == null)
            {
                return null;
            }

            return model.FindSegmentSlot(state.SelectedSegmentSlotID);
        }

        internal ShuttleControlModuleSlotModel FindSelectedModule(
            V3MainPageState state,
            ShuttleControlSegmentSlotModel segment)
        {
            if (state == null || segment == null || segment.ModuleSlots == null)
            {
                return null;
            }

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleControlModuleSlotModel moduleSlot = segment.ModuleSlots[i];
                if (moduleSlot != null && moduleSlot.SlotID == state.SelectedModuleSlotID)
                {
                    return moduleSlot;
                }
            }

            return null;
        }

        internal void EnsureSelectedModule(
            V3MainPageState state,
            ShuttleControlSegmentSlotModel segment)
        {
            if (state == null)
            {
                return;
            }

            if (segment == null || segment.ModuleSlots == null || segment.ModuleSlots.Count == 0)
            {
                state.SelectedModuleSlotID = null;
                return;
            }

            if (this.FindSelectedModule(state, segment) != null)
            {
                return;
            }

            ShuttleControlModuleSlotModel first = segment.ModuleSlots[0];
            state.SelectedModuleSlotID = first != null ? first.SlotID : null;
        }

        internal void SelectSegment(
            V3MainPageState state,
            ShuttleControlSegmentSlotModel segment)
        {
            if (state == null)
            {
                return;
            }

            string slotID = segment != null ? segment.SlotID : null;
            if (state.SelectedSegmentSlotID != slotID)
            {
                state.SelectedModuleSlotID = null;
            }

            state.SelectedSegmentSlotID = slotID;
        }

        internal void SelectModule(
            V3MainPageState state,
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            if (state == null)
            {
                return;
            }

            if (segment != null)
            {
                state.SelectedSegmentSlotID = segment.SlotID;
            }

            state.SelectedModuleSlotID = moduleSlot != null ? moduleSlot.SlotID : null;
        }

        internal bool IsSegmentInstalled(ShuttleControlSegmentSlotModel segment)
        {
            return segment != null && !string.IsNullOrEmpty(segment.InstalledSegmentInstanceID);
        }

        internal bool IsModuleInstalled(ShuttleControlModuleSlotModel moduleSlot)
        {
            return moduleSlot != null && !string.IsNullOrEmpty(moduleSlot.InstalledModuleInstanceID);
        }

        internal bool HasAnyDisabledInstalledModule(ShuttleControlSegmentSlotModel segment)
        {
            if (segment == null || segment.ModuleSlots == null)
            {
                return false;
            }

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleControlModuleSlotModel moduleSlot = segment.ModuleSlots[i];
                if (this.IsModuleInstalled(moduleSlot) && !moduleSlot.InstalledModuleEnabled)
                {
                    return true;
                }
            }

            return false;
        }

        internal int CountInstalledModules(ShuttleControlSegmentSlotModel segment)
        {
            int count = 0;
            if (segment == null || segment.ModuleSlots == null)
            {
                return count;
            }

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                if (this.IsModuleInstalled(segment.ModuleSlots[i]))
                {
                    count++;
                }
            }

            return count;
        }

        internal int CountModuleSlots(ShuttleControlSegmentSlotModel segment)
        {
            return segment != null && segment.ModuleSlots != null
                ? segment.ModuleSlots.Count
                : 0;
        }
    }
}
