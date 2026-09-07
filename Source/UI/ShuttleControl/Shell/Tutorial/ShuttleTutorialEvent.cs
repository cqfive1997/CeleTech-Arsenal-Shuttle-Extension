namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal enum ShuttleTutorialEventKind
    {
        SegmentInstalled
    }

    internal sealed class ShuttleTutorialEvent
    {
        internal ShuttleTutorialEvent(
            ShuttleTutorialEventKind kind,
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

        internal ShuttleTutorialEventKind Kind { get; private set; }
        internal string EventID { get; private set; }
        internal string SegmentSlotID { get; private set; }
        internal string SegmentDefName { get; private set; }
        internal string SegmentKindKey { get; private set; }
        internal int TicksGame { get; private set; }
    }
}
