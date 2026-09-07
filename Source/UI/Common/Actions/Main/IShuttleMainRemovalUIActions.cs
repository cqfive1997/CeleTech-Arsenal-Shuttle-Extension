using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main
{
    internal interface IShuttleMainRemovalUIActions
    {
        bool CanRemoveSegment(
            ShuttleControlSegmentSlotModel segment,
            bool hasActiveConstructionOrder);

        bool RemoveSegment(
            ShuttleControlSegmentSlotModel segment,
            bool hasActiveConstructionOrder);

        string GetSegmentRemoveTooltip(
            ShuttleControlSegmentSlotModel segment,
            bool hasActiveConstructionOrder);

        bool CanCancelSegmentRemoval(ShuttleControlSegmentSlotModel segment);

        bool CancelSegmentRemoval(ShuttleControlSegmentSlotModel segment);

        bool CanRemoveModule(
            ShuttleControlModuleSlotModel moduleSlot,
            bool hasActiveConstructionOrder);

        bool RemoveModule(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            bool hasActiveConstructionOrder);

        string GetModuleRemoveTooltip(
            ShuttleControlModuleSlotModel moduleSlot,
            bool hasActiveConstructionOrder);

        bool CanCancelModuleRemoval(ShuttleControlModuleSlotModel moduleSlot);

        bool CancelModuleRemoval(ShuttleControlModuleSlotModel moduleSlot);
    }
}
