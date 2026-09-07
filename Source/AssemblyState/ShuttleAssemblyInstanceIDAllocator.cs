using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    internal static class ShuttleAssemblyInstanceIDAllocator
    {
        internal static bool CanAllocate(int nextID, bool exhausted)
        {
            return !exhausted &&
                nextID > 0 &&
                nextID < ShuttleAssemblyInstanceIDRules.ExhaustionThreshold;
        }

        internal static bool NormalizeSegmentAllocator(
            IList<ShuttleSegment> segments,
            ref int nextID)
        {
            if (nextID < 1)
            {
                nextID = 1;
            }

            int maxLoadedID = 0;
            bool exhausted = nextID >= ShuttleAssemblyInstanceIDRules.ExhaustionThreshold;
            for (int i = 0; segments != null && i < segments.Count; i++)
            {
                ShuttleSegment segment = segments[i];
                int parsedValue;
                if (segment == null ||
                    !ShuttleAssemblyInstanceIDRules.TryParseSegmentInstanceNumber(
                        segment.SegmentInstanceID,
                        out parsedValue))
                {
                    continue;
                }

                if (parsedValue >= ShuttleAssemblyInstanceIDRules.ExhaustionThreshold)
                {
                    exhausted = true;
                }
                else if (parsedValue > maxLoadedID)
                {
                    maxLoadedID = parsedValue;
                }
            }

            return FinishNormalization(maxLoadedID, ref nextID, exhausted);
        }

        internal static bool NormalizeModuleAllocator(
            IList<ShuttleModule> modules,
            ref int nextID)
        {
            if (nextID < 1)
            {
                nextID = 1;
            }

            int maxLoadedID = 0;
            bool exhausted = nextID >= ShuttleAssemblyInstanceIDRules.ExhaustionThreshold;
            for (int i = 0; modules != null && i < modules.Count; i++)
            {
                ShuttleModule module = modules[i];
                int parsedValue;
                if (module == null ||
                    !ShuttleAssemblyInstanceIDRules.TryParseModuleInstanceNumber(
                        module.ModuleInstanceID,
                        out parsedValue))
                {
                    continue;
                }

                if (parsedValue >= ShuttleAssemblyInstanceIDRules.ExhaustionThreshold)
                {
                    exhausted = true;
                }
                else if (parsedValue > maxLoadedID)
                {
                    maxLoadedID = parsedValue;
                }
            }

            return FinishNormalization(maxLoadedID, ref nextID, exhausted);
        }

        internal static bool TryAllocateSegment(
            IList<ShuttleSegment> segments,
            ref int nextID,
            ref bool exhausted,
            out string allocated)
        {
            allocated = null;
            while (CanAllocate(nextID, exhausted))
            {
                string candidate = ShuttleAssemblyInstanceIDRules.MakeSegmentInstanceID(nextID);
                if (!IsSegmentInstanceIDInUse(segments, candidate))
                {
                    allocated = candidate;
                    AdvanceAfterAllocation(ref nextID, ref exhausted);
                    return true;
                }

                AdvanceAfterAllocation(ref nextID, ref exhausted);
            }

            MarkExhausted(ref nextID, ref exhausted);
            return false;
        }

        internal static bool TryAllocateModule(
            IList<ShuttleModule> modules,
            ref int nextID,
            ref bool exhausted,
            out string allocated)
        {
            allocated = null;
            while (CanAllocate(nextID, exhausted))
            {
                string candidate = ShuttleAssemblyInstanceIDRules.MakeModuleInstanceID(nextID);
                if (!IsModuleInstanceIDInUse(modules, candidate))
                {
                    allocated = candidate;
                    AdvanceAfterAllocation(ref nextID, ref exhausted);
                    return true;
                }

                AdvanceAfterAllocation(ref nextID, ref exhausted);
            }

            MarkExhausted(ref nextID, ref exhausted);
            return false;
        }

        private static bool FinishNormalization(int maxLoadedID, ref int nextID, bool exhausted)
        {
            if (!exhausted && maxLoadedID >= nextID)
            {
                nextID = maxLoadedID + 1;
                exhausted = nextID >= ShuttleAssemblyInstanceIDRules.ExhaustionThreshold;
            }

            if (exhausted)
            {
                nextID = ShuttleAssemblyInstanceIDRules.ExhaustionThreshold;
            }

            return exhausted;
        }

        private static void AdvanceAfterAllocation(ref int nextID, ref bool exhausted)
        {
            if (nextID >= ShuttleAssemblyInstanceIDRules.ExhaustionThreshold - 1)
            {
                MarkExhausted(ref nextID, ref exhausted);
                return;
            }

            nextID = nextID + 1;
        }

        private static void MarkExhausted(ref int nextID, ref bool exhausted)
        {
            exhausted = true;
            nextID = ShuttleAssemblyInstanceIDRules.ExhaustionThreshold;
        }

        private static bool IsSegmentInstanceIDInUse(
            IList<ShuttleSegment> segments,
            string segmentInstanceID)
        {
            if (string.IsNullOrEmpty(segmentInstanceID) || segments == null)
            {
                return false;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                ShuttleSegment segment = segments[i];
                if (segment != null && segment.SegmentInstanceID == segmentInstanceID)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsModuleInstanceIDInUse(
            IList<ShuttleModule> modules,
            string moduleInstanceID)
        {
            if (string.IsNullOrEmpty(moduleInstanceID) || modules == null)
            {
                return false;
            }

            for (int i = 0; i < modules.Count; i++)
            {
                ShuttleModule module = modules[i];
                if (module != null && module.ModuleInstanceID == moduleInstanceID)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
