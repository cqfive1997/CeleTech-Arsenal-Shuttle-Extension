using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Shuttle-level assembly layout definition.
    /// It defines the available segment slots for a shuttle host and therefore controls the
    /// maximum number of installed segments. Each slot type is normalized into the canonical
    /// assembly categories such as cockpit, power, living, cargo, support, weapon, and optional.
    /// </summary>
    public sealed class ShuttleAssemblyLayoutDef : Def
    {
        // Shuttle-level segment slot topology.
        // The number of entries here is the maximum number of segments this shuttle layout can host.
        public List<ShuttleSegmentSlotDef> segmentSlots = new List<ShuttleSegmentSlotDef>();

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.segmentSlots == null)
            {
                this.segmentSlots = new List<ShuttleSegmentSlotDef>();
            }

            // Validate each declared shuttle-level segment slot once at def load time so assembly
            // code can treat the layout as authoritative topology instead of re-guessing limits.
            for (int i = 0; i < this.segmentSlots.Count; i++)
            {
                ShuttleSegmentSlotDef slotDef = this.segmentSlots[i];
                if (slotDef == null)
                {
                    yield return this.defName + " contains a null segment slot definition.";
                    continue;
                }

                slotDef.EnsureInitialized(i);

                if (string.IsNullOrEmpty(slotDef.slotTypeID))
                {
                    yield return this.defName + " segment slot at index " + i + " must define slotTypeID.";
                }

                if (slotDef.SlotType == ShuttleSegmentType.Unknown)
                {
                    yield return this.defName + " segment slot at index " + i + " defines unknown slotTypeID.";
                }
            }
        }
    }
}
