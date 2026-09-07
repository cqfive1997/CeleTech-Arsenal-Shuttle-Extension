using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions
{
    /// <summary>
    /// One caller-created loose Thing and the stable ordinary transporter index selected by a
    /// Cargo planner. No destination holder crosses the Cargo transaction boundary.
    /// </summary>
    internal sealed class ShuttleCargoLooseDepositEntry
    {
        internal ShuttleCargoLooseDepositEntry(int transporterIndex, Thing thing)
        {
            this.TransporterIndex = transporterIndex;
            this.Thing = thing;
        }

        internal int TransporterIndex { get; private set; }

        internal Thing Thing { get; private set; }
    }
}
