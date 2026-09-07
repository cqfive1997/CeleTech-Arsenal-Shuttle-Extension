using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    /// <summary>
    /// Creates materialized assembly topology from static defs.
    /// It initializes shuttle-level segment slots on first creation and expands segment-owned
    /// module slots only when a concrete segment is installed.
    /// It does not install segments/modules and does not compute profile/runtime values.
    /// </summary>
    internal sealed class ShuttleAssemblyBootstrapper
    {
        public void InitializeSegmentSlots(ShuttleAssemblyState state, ShuttleAssemblyLayoutDef layoutDef)
        {
            if (state == null || layoutDef == null)
            {
                ShuttleLog.Warn("AssemblyBootstrapper", "InitializeSegmentSlots called with null state or layoutDef.");
                return;
            }

            state.EnsureInitialized();

            if (layoutDef.segmentSlots == null)
            {
                layoutDef.segmentSlots = new List<ShuttleSegmentSlotDef>();
            }

            // Shuttle-level segment slots exist even when no segment is installed yet.
            // This is the authoritative source of total segment capacity for the current layout.
            for (int i = 0; i < layoutDef.segmentSlots.Count; i++)
            {
                ShuttleSegmentSlotDef slotDef = layoutDef.segmentSlots[i];
                if (slotDef == null)
                {
                    continue;
                }

                slotDef.EnsureInitialized(i);

                string runtimeSlotID = this.BuildSegmentSlotID(i);

                ShuttleSegmentSlot existingSlot = state.GetSegmentSlot(runtimeSlotID);
                if (existingSlot != null)
                {
                    if (existingSlot.UpdateDisplayMetadataIfMissing(
                        slotDef.labelKey,
                        slotDef.shortLabelKey,
                        slotDef.descriptionKey))
                    {
                        state.MarkDirty(ShuttleDirtyFlags.AssemblyTopology | ShuttleDirtyFlags.Profile);
                    }

                    continue;
                }

                state.AddSegmentSlot(new ShuttleSegmentSlot(
                    runtimeSlotID,
                    slotDef.slotTypeID,
                    i,
                    slotDef.isRequired,
                    slotDef.isLocked,
                    slotDef.isFixed,
                    slotDef.defaultSegmentDefName,
                    slotDef.labelKey,
                    slotDef.shortLabelKey,
                    slotDef.descriptionKey));
            }

            state.RebuildIndexes();
            state.MarkDirty(ShuttleDirtyFlags.AssemblyTopology | ShuttleDirtyFlags.Profile);
        }

        public void MaterializeModuleSlots(ShuttleAssemblyState state, ShuttleSegment segment)
        {
            if (state == null || segment == null)
            {
                ShuttleLog.Warn("AssemblyBootstrapper", "MaterializeModuleSlots called with null state or segment.");
                return;
            }

            // Segment-owned module slots only come into existence after a concrete segment is
            // installed. This avoids fake topology such as "empty segment with populated slots".
            segment.EnsureInitialized();
            segment.ClearModuleSlots();

            ShuttleSegmentBaseDef segmentDef = segment.SegmentDef;
            if (segmentDef == null || segmentDef.moduleSlots == null)
            {
                ShuttleLog.Warn(
                    "AssemblyBootstrapper",
                    "Segment " + segment.SegmentInstanceID + " cannot materialize module slots because its segment def is missing or has no moduleSlots.");
                state.RebuildIndexes();
                state.MarkDirty(ShuttleDirtyFlags.AssemblyTopology | ShuttleDirtyFlags.Profile);
                return;
            }

            for (int i = 0; i < segmentDef.moduleSlots.Count; i++)
            {
                ShuttleModuleSlotDef slotDef = segmentDef.moduleSlots[i];
                if (slotDef == null)
                {
                    continue;
                }

                slotDef.EnsureInitialized(i);
                string runtimeSlotID = this.BuildModuleSlotID(segment.SegmentInstanceID, i);

                segment.AddModuleSlot(new ShuttleModuleSlot(
                    runtimeSlotID,
                    slotDef.slotTypeID,
                    segment.SegmentInstanceID,
                    i,
                    slotDef.isRequired,
                    slotDef.isLocked,
                    slotDef.labelKey,
                    slotDef.shortLabelKey,
                    slotDef.descriptionKey));
            }

            state.RebuildIndexes();
            state.MarkDirty(ShuttleDirtyFlags.AssemblyTopology | ShuttleDirtyFlags.Profile);
        }

        public bool EnsureMissingModuleSlotsForCurrentSegmentDefs(ShuttleAssemblyState state)
        {
            if (state == null || state.Segments == null)
            {
                return false;
            }

            bool changed = false;
            foreach (ShuttleSegment segment in state.Segments)
            {
                if (segment == null)
                {
                    continue;
                }

                segment.EnsureInitialized();
                ShuttleSegmentBaseDef segmentDef = segment.SegmentDef;
                if (segmentDef == null || segmentDef.moduleSlots == null)
                {
                    continue;
                }

                for (int i = 0; i < segmentDef.moduleSlots.Count; i++)
                {
                    string runtimeSlotID = this.BuildModuleSlotID(segment.SegmentInstanceID, i);
                    ShuttleModuleSlotDef slotDef = segmentDef.moduleSlots[i];
                    if (slotDef == null)
                    {
                        continue;
                    }

                    slotDef.EnsureInitialized(i);
                    ShuttleModuleSlot existingSlot = segment.GetModuleSlotByID(runtimeSlotID);
                    if (existingSlot != null)
                    {
                        if (existingSlot.UpdateDisplayMetadataIfMissing(
                            slotDef.labelKey,
                            slotDef.shortLabelKey,
                            slotDef.descriptionKey))
                        {
                            changed = true;
                        }

                        continue;
                    }

                    segment.AddModuleSlot(new ShuttleModuleSlot(
                        runtimeSlotID,
                        slotDef.slotTypeID,
                        segment.SegmentInstanceID,
                        i,
                        slotDef.isRequired,
                        slotDef.isLocked,
                        slotDef.labelKey,
                        slotDef.shortLabelKey,
                        slotDef.descriptionKey));
                    changed = true;
                }
            }

            if (changed)
            {
                state.RebuildIndexes();
                state.MarkDirty(ShuttleDirtyFlags.AssemblyTopology | ShuttleDirtyFlags.Profile);
            }

            return changed;
        }

        private string BuildSegmentSlotID(int slotIndex)
        {
            // Shuttle-level slot IDs are stable addresses generated from the materialized layout order.
            return "segment.slot." + slotIndex;
        }

        private string BuildModuleSlotID(string parentSegmentInstanceID, int slotIndex)
        {
            // Segment-owned module slot IDs are stable addresses scoped to one concrete segment instance.
            return parentSegmentInstanceID + ".slot." + slotIndex;
        }
    }
}
