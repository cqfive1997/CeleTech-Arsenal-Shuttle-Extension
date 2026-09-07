using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main
{
    internal interface IShuttleMainRemovalWorkerUIActions
    {
        bool CanAssignSegmentRemovalWorker(ShuttleControlSegmentSlotModel segment);

        bool AssignSegmentRemovalWorker(ShuttleControlSegmentSlotModel segment);

        bool CanAssignModuleRemovalWorker(ShuttleControlModuleSlotModel moduleSlot);

        bool AssignModuleRemovalWorker(ShuttleControlModuleSlotModel moduleSlot);
    }
}
