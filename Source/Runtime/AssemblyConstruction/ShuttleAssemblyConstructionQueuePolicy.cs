using System;
using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    internal static class ShuttleAssemblyConstructionQueuePolicy
    {
        internal const int MaxQueuedOrders = 16;

        internal static bool CanEnqueue(
            ShuttleAssemblyConstructionState state,
            ShuttleAssemblyConstructionOrder candidate,
            out string failureReason)
        {
            failureReason = null;
            if (state == null || candidate == null)
            {
                failureReason = "CT_Shuttle_Command_ContextUnavailable".Translate().ToString();
                return false;
            }

            if (state.HasActiveOrder && state.QueuedOrderCount >= MaxQueuedOrders)
            {
                failureReason = "CT_Shuttle_AssemblyConstruction_QueueFull"
                    .Translate(MaxQueuedOrders)
                    .ToString();
                return false;
            }

            ShuttleAssemblyConstructionOrder active = state.ActiveOrder;
            if (TargetsConflict(active, candidate))
            {
                failureReason = "CT_Shuttle_AssemblyConstruction_TargetAlreadyQueued"
                    .Translate()
                    .ToString();
                return false;
            }

            IReadOnlyList<ShuttleAssemblyConstructionOrder> queued = state.QueuedOrders;
            for (int i = 0; queued != null && i < queued.Count; i++)
            {
                if (TargetsConflict(queued[i], candidate))
                {
                    failureReason = "CT_Shuttle_AssemblyConstruction_TargetAlreadyQueued"
                        .Translate()
                        .ToString();
                    return false;
                }
            }

            return true;
        }

        private static bool TargetsConflict(
            ShuttleAssemblyConstructionOrder left,
            ShuttleAssemblyConstructionOrder right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            bool leftSegment = IsSegmentOrder(left.Kind);
            bool rightSegment = IsSegmentOrder(right.Kind);
            if (leftSegment || rightSegment)
            {
                return Same(left.SegmentSlotID, right.SegmentSlotID);
            }

            return Same(left.SegmentInstanceID, right.SegmentInstanceID) &&
                Same(left.ModuleSlotID, right.ModuleSlotID);
        }

        private static bool IsSegmentOrder(ShuttleAssemblyConstructionKind kind)
        {
            return kind == ShuttleAssemblyConstructionKind.Segment ||
                kind == ShuttleAssemblyConstructionKind.SegmentReplacement;
        }

        private static bool Same(string left, string right)
        {
            return !string.IsNullOrEmpty(left) &&
                string.Equals(left, right, StringComparison.Ordinal);
        }
    }
}
