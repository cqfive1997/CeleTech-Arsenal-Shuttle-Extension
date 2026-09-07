using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Separates currently held passenger intent from passengers that vanilla can board now.
    /// </summary>
    internal sealed class ShuttlePassengerBoardingSelection
    {
        internal readonly List<Pawn> HeldPawns = new List<Pawn>();
        internal readonly List<TransferableOneWay> QueueTransferables =
            new List<TransferableOneWay>();

        internal static ShuttlePassengerBoardingSelection Build(
            ThingWithComps host,
            List<TransferableOneWay> passengerTransferables)
        {
            ShuttlePassengerBoardingSelection result =
                new ShuttlePassengerBoardingSelection();
            ShuttleHeldPassengerSnapshot held = ShuttleHeldPassengerQuery.Build(host);
            HashSet<int> heldThingIDs = new HashSet<int>();
            if (passengerTransferables == null)
            {
                return result;
            }

            for (int i = 0; i < passengerTransferables.Count; i++)
            {
                TransferableOneWay transferable = passengerTransferables[i];
                if (transferable == null ||
                    transferable.CountToTransfer <= 0 ||
                    transferable.things == null)
                {
                    continue;
                }

                int remaining = transferable.CountToTransfer;
                for (int j = 0; j < transferable.things.Count && remaining > 0; j++)
                {
                    Pawn pawn = transferable.things[j] as Pawn;
                    if (pawn == null)
                    {
                        continue;
                    }

                    if (held.Contains(pawn))
                    {
                        if (heldThingIDs.Add(pawn.thingIDNumber))
                        {
                            result.HeldPawns.Add(pawn);
                        }
                    }
                    else
                    {
                        TransferableOneWay queueTransferable = new TransferableOneWay();
                        queueTransferable.things.Add(pawn);
                        queueTransferable.AdjustTo(1);
                        result.QueueTransferables.Add(queueTransferable);
                    }

                    remaining--;
                }
            }

            return result;
        }
    }
}
