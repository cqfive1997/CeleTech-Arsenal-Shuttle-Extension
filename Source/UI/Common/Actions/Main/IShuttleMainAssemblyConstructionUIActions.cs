using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main
{
    internal interface IShuttleMainAssemblyConstructionUIActions
    {
        bool CanCancelAssemblyConstruction(ShuttleAssemblyConstructionReadModel construction);

        bool CancelAssemblyConstruction(ShuttleAssemblyConstructionReadModel construction);

        bool CanCancelQueuedAssemblyConstruction(
            ShuttleAssemblyConstructionQueueItemReadModel queuedOrder);

        bool CancelQueuedAssemblyConstruction(
            ShuttleAssemblyConstructionQueueItemReadModel queuedOrder);

        bool CanDebugCompleteAssemblyConstruction(ShuttleAssemblyConstructionReadModel construction);

        bool DebugCompleteAssemblyConstruction(ShuttleAssemblyConstructionReadModel construction);

        bool CanDebugFillAssemblyConstructionMaterials(ShuttleAssemblyConstructionReadModel construction);

        bool DebugFillAssemblyConstructionMaterials(ShuttleAssemblyConstructionReadModel construction);
    }
}
