namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal enum ShuttleAssemblyEventKind
    {
        SegmentInstalled
    }

    internal sealed class ShuttleAssemblyEvent
    {
        internal ShuttleAssemblyEvent(
            ShuttleAssemblyEventKind kind,
            string eventID,
            string segmentSlotID,
            string segmentDefName,
            string segmentKindKey,
            int ticksGame)
        {
            this.Kind = kind;
            this.EventID = eventID;
            this.SegmentSlotID = segmentSlotID;
            this.SegmentDefName = segmentDefName;
            this.SegmentKindKey = segmentKindKey;
            this.TicksGame = ticksGame;
        }

        internal ShuttleAssemblyEventKind Kind { get; private set; }
        internal string EventID { get; private set; }
        internal string SegmentSlotID { get; private set; }
        internal string SegmentDefName { get; private set; }
        internal string SegmentKindKey { get; private set; }
        internal int TicksGame { get; private set; }
    }

    internal interface IShuttleAssemblyEventReadPort
    {
        bool TryDequeueAssemblyEvent(out ShuttleAssemblyEvent assemblyEvent);
    }
}
