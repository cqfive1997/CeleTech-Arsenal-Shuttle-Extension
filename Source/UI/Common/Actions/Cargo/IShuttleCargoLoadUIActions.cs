using System.Collections.Generic;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo
{
    internal interface IShuttleCargoLoadUIActions
    {
        bool BeginLoadCargo(
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables,
            bool replaceExistingQueue);

        bool ClearQueuedLoad();

        bool CancelQueuedLoadEntry(
            int transporterIndex,
            int queueIndex,
            int count);
    }
}
